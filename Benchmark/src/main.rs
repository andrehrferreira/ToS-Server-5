use tokio::net::UdpSocket;
use tokio::sync::Semaphore;
use std::sync::Arc;
use std::time::{Instant, Duration};
use std::net::SocketAddr;
use std::sync::atomic::{AtomicUsize, Ordering};

#[tokio::main]
async fn main() -> std::io::Result<()> {
    const SERVER_ADDR: &str = "127.0.0.1:3565";
    const CLIENT_COUNT: usize = 10_000;
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

                while start.elapsed() < TEST_DURATION {
                    match socket.recv_from(&mut buf).await {
                        Ok((size, addr)) => {
                            if size == 0 {
                                continue;
                            }
                            let packet_type = buf[0];
                            match packet_type {
                                1 => {
                                    if size >= 9 {
                                        let mut pong_packet = [0u8; 9];
                                        pong_packet[0] = 2u8;
                                        pong_packet[1..9].copy_from_slice(&buf[1..9]);
                                        let _ = socket.send_to(&pong_packet, addr).await;
                                        packets_sent.fetch_add(1, Ordering::Relaxed);
                                    } else {
                                        eprintln!("Received Ping with invalid size: {}", size);
                                    }
                                }
                                9 => { /* ConnectionAccepted */ }
                                _ => { /* Ignored */ }
                            }
                            packets_received.fetch_add(1, Ordering::Relaxed);
                        }
                        Err(_) => {
                            continue;
                        }
                    }
                }
                drop(permit);
            });
        }

        // pausa entre batches para evitar pressão no sistema
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
            "Time: {}s | Packets Received: {} (+{}) | Packets Sent: {} (+{})",
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
