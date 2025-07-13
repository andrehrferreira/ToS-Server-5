using System;
using System.Text;

namespace Tests
{
    public class LZ4Tests : AbstractTest
    {
        public LZ4Tests()
        {
            Describe("LZ4 Compression", () =>
            {
                It("should compress empty data", () =>
                {
                    byte[] input = new byte[1]; // Use minimal array to avoid null pointer issues
                    byte[] output = new byte[1024];

                    unsafe
                    {
                        fixed (byte* inputPtr = input, outputPtr = output)
                        {
                            int compressedSize = LZ4.Compress(inputPtr, 0, outputPtr, output.Length); // Use 0 length
                            Expect(compressedSize).ToBeGreaterThan(0); // Should produce some output
                        }
                    }
                });

                It("should compress single byte", () =>
                {
                    byte[] input = new byte[] { 0x42 };
                    byte[] output = new byte[1024];

                    unsafe
                    {
                        fixed (byte* inputPtr = input, outputPtr = output)
                        {
                            int compressedSize = LZ4.Compress(inputPtr, input.Length, outputPtr, output.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);
                        }
                    }
                });

                It("should compress ASCII string", () =>
                {
                    byte[] input = Encoding.ASCII.GetBytes("hello world");
                    byte[] output = new byte[1024];

                    unsafe
                    {
                        fixed (byte* inputPtr = input, outputPtr = output)
                        {
                            int compressedSize = LZ4.Compress(inputPtr, input.Length, outputPtr, output.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);
                            Expect(compressedSize).ToBeLessThanOrEqualTo(input.Length + 16); // Some overhead is expected
                        }
                    }
                });

                It("should compress repetitive data efficiently", () =>
                {
                    byte[] input = new byte[1000];
                    for (int i = 0; i < input.Length; i++)
                        input[i] = 0x41; // All 'A' characters

                    byte[] output = new byte[1024];

                    unsafe
                    {
                        fixed (byte* inputPtr = input, outputPtr = output)
                        {
                            int compressedSize = LZ4.Compress(inputPtr, input.Length, outputPtr, output.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);
                            Expect(compressedSize).ToBeLessThan(input.Length / 2); // Should compress well
                        }
                    }
                });

                It("should handle insufficient output buffer", () =>
                {
                    byte[] input = new byte[1000];
                    for (int i = 0; i < input.Length; i++)
                        input[i] = (byte)(i % 256);

                    byte[] output = new byte[10]; // Very small buffer

                    unsafe
                    {
                        fixed (byte* inputPtr = input, outputPtr = output)
                        {
                            int compressedSize = LZ4.Compress(inputPtr, input.Length, outputPtr, output.Length);
                            Expect(compressedSize).ToBe(0); // Should return 0 for insufficient buffer
                        }
                    }
                });

                It("should compress large data", () =>
                {
                    byte[] input = new byte[4096];
                    for (int i = 0; i < input.Length; i++)
                        input[i] = (byte)(i % 256);

                    byte[] output = new byte[8192];

                    unsafe
                    {
                        fixed (byte* inputPtr = input, outputPtr = output)
                        {
                            int compressedSize = LZ4.Compress(inputPtr, input.Length, outputPtr, output.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);
                        }
                    }
                });
            });

            Describe("LZ4 Decompression", () =>
            {
                It("should decompress empty data", () =>
                {
                    byte[] input = new byte[1]; // Use minimal array to avoid null pointer issues
                    byte[] compressed = new byte[1024];
                    byte[] decompressed = new byte[1024];

                    unsafe
                    {
                        fixed (byte* inputPtr = input, compressedPtr = compressed, decompressedPtr = decompressed)
                        {
                            int compressedSize = LZ4.Compress(inputPtr, 0, compressedPtr, compressed.Length); // Use 0 length
                            Expect(compressedSize).ToBeGreaterThan(0);

                            int decompressedSize = LZ4.Decompress(compressedPtr, compressedSize, decompressedPtr, decompressed.Length);
                            Expect(decompressedSize).ToBe(0); // Should decompress to 0 bytes
                        }
                    }
                });

                It("should decompress single byte", () =>
                {
                    byte[] input = new byte[] { 0x42 };
                    byte[] compressed = new byte[1024];
                    byte[] decompressed = new byte[1024];

                    unsafe
                    {
                        fixed (byte* inputPtr = input, compressedPtr = compressed, decompressedPtr = decompressed)
                        {
                            int compressedSize = LZ4.Compress(inputPtr, input.Length, compressedPtr, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);

                            int decompressedSize = LZ4.Decompress(compressedPtr, compressedSize, decompressedPtr, decompressed.Length);
                            Expect(decompressedSize).ToBe(input.Length);
                            Expect(decompressed[0]).ToBe(0x42);
                        }
                    }
                });

                It("should handle insufficient output buffer for decompression", () =>
                {
                    byte[] input = new byte[1000];
                    for (int i = 0; i < input.Length; i++)
                        input[i] = (byte)(i % 256);

                    byte[] compressed = new byte[2048];
                    byte[] decompressed = new byte[10]; // Very small buffer

                    unsafe
                    {
                        fixed (byte* inputPtr = input, compressedPtr = compressed, decompressedPtr = decompressed)
                        {
                            int compressedSize = LZ4.Compress(inputPtr, input.Length, compressedPtr, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);

                            int decompressedSize = LZ4.Decompress(compressedPtr, compressedSize, decompressedPtr, decompressed.Length);
                            Expect(decompressedSize).ToBe(0); // Should return 0 for insufficient buffer
                        }
                    }
                });

                It("should handle corrupted compressed data", () =>
                {
                    byte[] corrupted = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                    byte[] decompressed = new byte[1024];

                    unsafe
                    {
                        fixed (byte* corruptedPtr = corrupted, decompressedPtr = decompressed)
                        {
                            int decompressedSize = LZ4.Decompress(corruptedPtr, corrupted.Length, decompressedPtr, decompressed.Length);
                            Expect(decompressedSize).ToBe(0); // Should return 0 for invalid data
                        }
                    }
                });
            });

            Describe("LZ4 Roundtrip Compression", () =>
            {
                It("should roundtrip ASCII string correctly", () =>
                {
                    string originalText = "The quick brown fox jumps over the lazy dog";
                    byte[] input = Encoding.ASCII.GetBytes(originalText);
                    byte[] compressed = new byte[1024];
                    byte[] decompressed = new byte[1024];

                    unsafe
                    {
                        fixed (byte* inputPtr = input, compressedPtr = compressed, decompressedPtr = decompressed)
                        {
                            int compressedSize = LZ4.Compress(inputPtr, input.Length, compressedPtr, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);

                            int decompressedSize = LZ4.Decompress(compressedPtr, compressedSize, decompressedPtr, decompressed.Length);
                            Expect(decompressedSize).ToBe(input.Length);

                            for (int i = 0; i < input.Length; i++)
                            {
                                Expect(decompressed[i]).ToBe(input[i]);
                            }
                        }
                    }
                });

                It("should roundtrip repetitive data correctly", () =>
                {
                    byte[] input = new byte[1000];
                    for (int i = 0; i < input.Length; i++)
                        input[i] = (byte)(i % 10); // Repetitive pattern

                    byte[] compressed = new byte[2048];
                    byte[] decompressed = new byte[2048];

                    unsafe
                    {
                        fixed (byte* inputPtr = input, compressedPtr = compressed, decompressedPtr = decompressed)
                        {
                            int compressedSize = LZ4.Compress(inputPtr, input.Length, compressedPtr, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);

                            int decompressedSize = LZ4.Decompress(compressedPtr, compressedSize, decompressedPtr, decompressed.Length);
                            Expect(decompressedSize).ToBe(input.Length);

                            for (int i = 0; i < input.Length; i++)
                            {
                                Expect(decompressed[i]).ToBe(input[i]);
                            }
                        }
                    }
                });

                It("should roundtrip binary data correctly", () =>
                {
                    byte[] input = new byte[] { 0x00, 0xFF, 0x55, 0xAA, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
                    byte[] compressed = new byte[1024];
                    byte[] decompressed = new byte[1024];

                    unsafe
                    {
                        fixed (byte* inputPtr = input, compressedPtr = compressed, decompressedPtr = decompressed)
                        {
                            int compressedSize = LZ4.Compress(inputPtr, input.Length, compressedPtr, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);

                            int decompressedSize = LZ4.Decompress(compressedPtr, compressedSize, decompressedPtr, decompressed.Length);
                            Expect(decompressedSize).ToBe(input.Length);

                            for (int i = 0; i < input.Length; i++)
                            {
                                Expect(decompressed[i]).ToBe(input[i]);
                            }
                        }
                    }
                });

                It("should roundtrip large data correctly", () =>
                {
                    byte[] input = new byte[4096];
                    for (int i = 0; i < input.Length; i++)
                        input[i] = (byte)(i % 256);

                    byte[] compressed = new byte[8192];
                    byte[] decompressed = new byte[8192];

                    unsafe
                    {
                        fixed (byte* inputPtr = input, compressedPtr = compressed, decompressedPtr = decompressed)
                        {
                            int compressedSize = LZ4.Compress(inputPtr, input.Length, compressedPtr, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);

                            int decompressedSize = LZ4.Decompress(compressedPtr, compressedSize, decompressedPtr, decompressed.Length);
                            Expect(decompressedSize).ToBe(input.Length);

                            for (int i = 0; i < input.Length; i++)
                            {
                                Expect(decompressed[i]).ToBe(input[i]);
                            }
                        }
                    }
                });

                It("should handle multiple small compressions", () =>
                {
                    string[] testStrings = { "a", "ab", "abc", "abcd", "hello", "world", "test" };

                    foreach (string testString in testStrings)
                    {
                        byte[] input = Encoding.ASCII.GetBytes(testString);
                        byte[] compressed = new byte[1024];
                        byte[] decompressed = new byte[1024];

                        unsafe
                        {
                            fixed (byte* inputPtr = input, compressedPtr = compressed, decompressedPtr = decompressed)
                            {
                                int compressedSize = LZ4.Compress(inputPtr, input.Length, compressedPtr, compressed.Length);
                                Expect(compressedSize).ToBeGreaterThan(0);

                                int decompressedSize = LZ4.Decompress(compressedPtr, compressedSize, decompressedPtr, decompressed.Length);
                                Expect(decompressedSize).ToBe(input.Length);

                                string result = Encoding.ASCII.GetString(decompressed, 0, decompressedSize);
                                Expect(result).ToBe(testString);
                            }
                        }
                    }
                });
            });
        }
    }
}
