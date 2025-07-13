use tokio::net::UdpSocket;
use tokio::sync::Semaphore;
use std::sync::Arc;
use std::time::{Instant, Duration, SystemTime, UNIX_EPOCH};
use std::net::SocketAddr;
use std::sync::atomic::{AtomicUsize, Ordering};

use rand::Rng;
use rand::SeedableRng;
use rand::rngs::StdRng;

// ZigZag encoding/decoding functions (same as C# FlatBuffer)
fn zigzag_encode_i64(value: i64) -> u64 {
    ((value as u64) << 1) ^ ((value >> 63) as u64)
}

fn zigzag_decode_u64(value: u64) -> i64 {
    ((value >> 1) as i64) ^ (-((value & 1) as i64))
}

// VarLong encoding/decoding functions
fn encode_varint_u64(mut value: u64) -> Vec<u8> {
    let mut result = Vec::new();

    while value >= 0x80 {
        result.push((value as u8) | 0x80);
        value >>= 7;
    }
    result.push(value as u8);

    result
}

fn decode_varint_u64(bytes: &[u8]) -> Result<(u64, usize), &'static str> {
    let mut result = 0u64;
    let mut shift = 0;
    let mut bytes_read = 0;

    for &byte in bytes {
        result |= ((byte & 0x7F) as u64) << shift;
        bytes_read += 1;

        if (byte & 0x80) == 0 {
            return Ok((result, bytes_read));
        }

        shift += 7;
        if shift > 70 {
            return Err("VarLong too long");
        }
    }

    Err("Incomplete VarLong")
}

// Helper function to encode a timestamp for ping packet
fn encode_ping_timestamp(timestamp: i64) -> Vec<u8> {
    let zigzag_encoded = zigzag_encode_i64(timestamp);
    encode_varint_u64(zigzag_encoded)
}

#[tokio::main]
async fn main() -> std::io::Result<()> {
    // Test ZigZag and VarLong encoding/decoding
    let test_timestamp = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .unwrap()
        .as_millis() as i64;

    let encoded = encode_ping_timestamp(test_timestamp);
    if let Ok((decoded_value, _)) = decode_varint_u64(&encoded) {
        let decoded_timestamp = zigzag_decode_u64(decoded_value);
        println!("ZigZag+VarLong test: {} -> {} bytes -> {}",
                 test_timestamp, encoded.len(), decoded_timestamp);
        assert_eq!(test_timestamp, decoded_timestamp);
    } else {
        eprintln!("Failed to decode test timestamp");
    }

    println!("Starting benchmark with {} clients...", CLIENT_COUNT);
    const SERVER_ADDR: &str = "127.0.0.1:3565";
    const CLIENT_COUNT: usize = 500; // 10_000;
    const TEST_DURATION: Duration = Duration::from_secs(60);
    const BATCH_SIZE: usize = 10000;

    let packets_received = Arc::new(AtomicUsize::new(0));
    let packets_sent = Arc::new(AtomicUsize::new(0));
    let sem = Arc::new(Semaphore::new(30_000));

    let server_addr: SocketAddr = SERVER_ADDR.parse().unwrap();
    let start = Instant::now();

    for batch_start in (0..CLIENT_COUNT).step_by(BATCH_SIZE) {
        let batch_end = std::cmp::min(batch_start + BATCH_SIZE, CLIENT_COUNT);

        for _ in batch_start..batch_end {
            let permit = sem.clone().acquire_owned().await.unwrap();
            let packets_received = packets_received.clone();
            let packets_sent = packets_sent.clone();
            let server_addr = server_addr.clone();
            let start = start.clone();

            tokio::spawn(async move {
                let socket = match UdpSocket::bind("0.0.0.0:0").await {
                    Ok(s) => s,
                    Err(e) => {
                        eprintln!("Failed to bind socket: {}", e);
                        drop(permit);
                        return;
                    }
                };

                let connect_packet = [0u8];
                if let Ok(_) = socket.send_to(&connect_packet, &server_addr).await {
                    packets_sent.fetch_add(1, Ordering::Relaxed);
                }

                let mut buf = [0u8; 1024];
                let mut update_interval = tokio::time::interval(Duration::from_millis(200));
                let mut rng = StdRng::from_entropy();

                while start.elapsed() < TEST_DURATION {
                    tokio::select! {
                        _ = update_interval.tick() => {
                            let position = (
                                rng.gen_range(-1000.0..=1000.0f32),
                                rng.gen_range(-1000.0..=1000.0f32),
                                rng.gen_range(-1000.0..=1000.0f32),
                            );

                            let rotation = (
                                rng.gen_range(-180.0..=180.0f32), // Pitch
                                rng.gen_range(-180.0..=180.0f32), // Yaw
                                rng.gen_range(-180.0..=180.0f32), // Roll
                            );

                            let quantized_pos = (
                                (position.0 / 0.1f32).round() as i16,
                                (position.1 / 0.1f32).round() as i16,
                                (position.2 / 0.1f32).round() as i16,
                            );

                            let quantized_rot = (
                                (rotation.0 / 0.1f32).round() as i16,
                                (rotation.1 / 0.1f32).round() as i16,
                                (rotation.2 / 0.1f32).round() as i16,
                            );

                            let mut packet = [0u8; 13];
                            packet[0] = 11u8;
                            packet[1..3].copy_from_slice(&quantized_pos.0.to_le_bytes());
                            packet[3..5].copy_from_slice(&quantized_pos.1.to_le_bytes());
                            packet[5..7].copy_from_slice(&quantized_pos.2.to_le_bytes());
                            packet[7..9].copy_from_slice(&quantized_rot.0.to_le_bytes());
                            packet[9..11].copy_from_slice(&quantized_rot.1.to_le_bytes());
                            packet[11..13].copy_from_slice(&quantized_rot.2.to_le_bytes());

                            if let Ok(_) = socket.send_to(&packet, &server_addr).await {
                                packets_sent.fetch_add(1, Ordering::Relaxed);
                            }
                        },
                        result = socket.recv_from(&mut buf) => {
                            match result {
                                Ok((size, addr)) => {
                                    if size == 0 { continue; }

                                    let mut offset = 0;
                                    while offset < size {
                                        let packet_type = buf[offset];
                                        match packet_type {
                                            // Ping
                                            1 => {
                                                if offset + 3 > size {
                                                    break;
                                                }

                                                let sent_timestamp_ms = u16::from_le_bytes([buf[offset + 1], buf[offset + 2]]);

                                                let mut pong_packet = [0u8; 3];
                                                pong_packet[0] = 2;
                                                pong_packet[1..3].copy_from_slice(&buf[offset + 1..offset + 3]);

                                                let _ = socket.send_to(&pong_packet, addr).await;
                                                packets_sent.fetch_add(1, Ordering::Relaxed);
                                                offset += 3;
                                            }
                                            // ConnectionAccepted
                                            9 => {
                                                offset += 5;
                                            }
                                            // Benchmark packet
                                            4 => {
                                                if offset + 19 > size {
                                                    break;
                                                }

                                                if offset + 20 <= size {
                                                    let possible_extra = size - (offset + 19);
                                                    if possible_extra >= 1 {
                                                        offset += 20;
                                                    } else {
                                                        offset += 19;
                                                    }
                                                } else {
                                                    offset += 19;
                                                }
                                            }
                                            _ => {
                                                //println!("Unknown packet type: {}", packet_type);
                                                // Unknown packet type, stop processing
                                                break;
                                            }
                                        }

                                        packets_received.fetch_add(1, Ordering::Relaxed);
                                    }
                                }
                                Err(_) => { continue; }
                            }
                        }
                    }
                }
                drop(permit);
            });
        }

        tokio::time::sleep(Duration::from_millis(1000)).await;
    }

    let report_interval = Duration::from_secs(1);
    let mut last_received = 0;
    let mut last_sent = 0;
    let mut interval = tokio::time::interval(report_interval);

    for _ in 0..TEST_DURATION.as_secs() {
        interval.tick().await;
        let recv = packets_received.load(Ordering::Relaxed);
        let sent = packets_sent.load(Ordering::Relaxed);

        println!(
            "Time: {}s | Received: {} (+{}) | Sent: {} (+{})",
            start.elapsed().as_secs(),
            recv,
            recv.saturating_sub(last_received),
            sent,
            sent.saturating_sub(last_sent)
        );

        last_received = recv;
        last_sent = sent;
    }

    let total_sent = packets_sent.load(Ordering::Relaxed);
    let total_received = packets_received.load(Ordering::Relaxed);
    let elapsed_secs = start.elapsed().as_secs_f64();

    let sent_per_sec = total_sent as f64 / elapsed_secs;
    let received_per_sec = total_received as f64 / elapsed_secs;
    let packet_loss = if total_sent > 0 {
        100.0 * total_sent.saturating_sub(total_received) as f64 / total_sent as f64
    } else {
        0.0
    };

    println!("\n====== Test Completed ======");
    println!("Duration: {:.2} seconds", elapsed_secs);
    println!("Total Packets Sent: {}", total_sent);
    println!("Total Packets Received: {}", total_received);
    println!("Packets Sent per Second: {:.2}", sent_per_sec);
    println!("Packets Received per Second: {:.2}", received_per_sec);
    println!("Estimated Packet Loss: {:.2}%", packet_loss);
    println!("============================");

    Ok(())
}
