using Wormhole;
using System.Text;

namespace Tests
{
    public class LZ4Tests : AbstractTest
    {
        public LZ4Tests()
        {
            Describe("LZ4 Compression", () =>
            {
                It("should handle minimal data safely", () =>
                {
                    // Test with minimal data that LZ4 can handle safely
                    byte[] original = new byte[] { 0x42 };
                    byte[] compressed = new byte[32];
                    byte[] decompressed = new byte[32];

                    unsafe
                    {
                        fixed (byte* src = original)
                        fixed (byte* dst = compressed)
                        {
                            int compressedSize = LZ4.Compress(src, original.Length, dst, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);

                            fixed (byte* src2 = compressed)
                            fixed (byte* dst2 = decompressed)
                            {
                                int decompressedSize = LZ4.Decompress(src2, compressedSize, dst2, decompressed.Length);
                                Expect(decompressedSize).ToBe(original.Length);
                                Expect(decompressed[0]).ToBe(original[0]);
                            }
                        }
                    }
                });

                It("should compress and decompress small data", () =>
                {
                    byte[] original = Encoding.UTF8.GetBytes("Hello World");
                    byte[] compressed = new byte[256];
                    byte[] decompressed = new byte[256];

                    unsafe
                    {
                        fixed (byte* src = original)
                        fixed (byte* dst = compressed)
                        {
                            int compressedSize = LZ4.Compress(src, original.Length, dst, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);
                            // Note: Small data may not compress smaller than original due to compression overhead

                            fixed (byte* src2 = compressed)
                            fixed (byte* dst2 = decompressed)
                            {
                                int decompressedSize = LZ4.Decompress(src2, compressedSize, dst2, decompressed.Length);
                                Expect(decompressedSize).ToBe(original.Length);

                                for (int i = 0; i < original.Length; i++)
                                {
                                    Expect(decompressed[i]).ToBe(original[i]);
                                }
                            }
                        }
                    }
                });

                It("should handle round-trip compression correctly", () =>
                {
                    byte[] original = Encoding.UTF8.GetBytes("This is a test string for LZ4 compression algorithm.");
                    byte[] compressed = new byte[1024];
                    byte[] decompressed = new byte[1024];

                    unsafe
                    {
                        fixed (byte* src = original)
                        fixed (byte* dst = compressed)
                        {
                            int compressedSize = LZ4.Compress(src, original.Length, dst, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);

                            fixed (byte* src2 = compressed)
                            fixed (byte* dst2 = decompressed)
                            {
                                int decompressedSize = LZ4.Decompress(src2, compressedSize, dst2, decompressed.Length);
                                Expect(decompressedSize).ToBe(original.Length);

                                for (int i = 0; i < original.Length; i++)
                                {
                                    Expect(decompressed[i]).ToBe(original[i]);
                                }
                            }
                        }
                    }
                });

                It("should compress repetitive data efficiently", () =>
                {
                    byte[] original = new byte[1000];
                    for (int i = 0; i < original.Length; i++)
                    {
                        original[i] = (byte)(i % 10); // Repetitive pattern
                    }

                    byte[] compressed = new byte[1024];

                    unsafe
                    {
                        fixed (byte* src = original)
                        fixed (byte* dst = compressed)
                        {
                            int compressedSize = LZ4.Compress(src, original.Length, dst, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);
                            // Repetitive data should compress well
                            Expect(compressedSize).ToBeLessThan(original.Length / 2);
                        }
                    }
                });

                It("should handle random data compression", () =>
                {
                    byte[] original = new byte[1000];
                    Random random = new Random(42); // Fixed seed for reproducibility

                    for (int i = 0; i < original.Length; i++)
                    {
                        original[i] = (byte)random.Next(256);
                    }

                    byte[] compressed = new byte[1024];
                    byte[] decompressed = new byte[1024];

                    unsafe
                    {
                        fixed (byte* src = original)
                        fixed (byte* dst = compressed)
                        {
                            int compressedSize = LZ4.Compress(src, original.Length, dst, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);

                            fixed (byte* src2 = compressed)
                            fixed (byte* dst2 = decompressed)
                            {
                                int decompressedSize = LZ4.Decompress(src2, compressedSize, dst2, decompressed.Length);
                                Expect(decompressedSize).ToBe(original.Length);

                                for (int i = 0; i < original.Length; i++)
                                {
                                    Expect(decompressed[i]).ToBe(original[i]);
                                }
                            }
                        }
                    }
                });


                It("should handle large data", () =>
                {
                    int size = 10000;
                    byte[] original = new byte[size];
                    for (int i = 0; i < size; i++)
                    {
                        original[i] = (byte)(i % 256);
                    }

                    byte[] compressed = new byte[size + 1024];
                    byte[] decompressed = new byte[size];

                    unsafe
                    {
                        fixed (byte* src = original)
                        fixed (byte* dst = compressed)
                        {
                            int compressedSize = LZ4.Compress(src, original.Length, dst, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);

                            fixed (byte* src2 = compressed)
                            fixed (byte* dst2 = decompressed)
                            {
                                int decompressedSize = LZ4.Decompress(src2, compressedSize, dst2, decompressed.Length);
                                Expect(decompressedSize).ToBe(original.Length);

                                for (int i = 0; i < original.Length; i++)
                                {
                                    Expect(decompressed[i]).ToBe(original[i]);
                                }
                            }
                        }
                    }
                });

                It("should handle data with long repeated sequences", () =>
                {
                    byte[] original = new byte[2000];
                    // Fill with repeated pattern
                    for (int i = 0; i < original.Length; i++)
                    {
                        original[i] = (byte)(i < 1000 ? 0xAA : 0xBB);
                    }

                    byte[] compressed = new byte[1024];
                    byte[] decompressed = new byte[2000];

                    unsafe
                    {
                        fixed (byte* src = original)
                        fixed (byte* dst = compressed)
                        {
                            int compressedSize = LZ4.Compress(src, original.Length, dst, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);
                            // Should achieve good compression ratio
                            Expect(compressedSize).ToBeLessThan(original.Length / 3);

                            fixed (byte* src2 = compressed)
                            fixed (byte* dst2 = decompressed)
                            {
                                int decompressedSize = LZ4.Decompress(src2, compressedSize, dst2, decompressed.Length);
                                Expect(decompressedSize).ToBe(original.Length);

                                for (int i = 0; i < original.Length; i++)
                                {
                                    Expect(decompressed[i]).ToBe(original[i]);
                                }
                            }
                        }
                    }
                });

                It("should handle multiple compressions of same data consistently", () =>
                {
                    byte[] original = Encoding.UTF8.GetBytes("Consistent compression test data");
                    byte[] compressed1 = new byte[256];
                    byte[] compressed2 = new byte[256];

                    unsafe
                    {
                        fixed (byte* src = original)
                        fixed (byte* dst1 = compressed1)
                        fixed (byte* dst2 = compressed2)
                        {
                            int size1 = LZ4.Compress(src, original.Length, dst1, compressed1.Length);
                            int size2 = LZ4.Compress(src, original.Length, dst2, compressed2.Length);

                            Expect(size1).ToBe(size2);

                            // Compressed data should be identical for same input
                            for (int i = 0; i < size1; i++)
                            {
                                Expect(compressed1[i]).ToBe(compressed2[i]);
                            }
                        }
                    }
                });

                It("should handle binary data correctly", () =>
                {
                    byte[] original = new byte[] { 0x00, 0xFF, 0x55, 0xAA, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC };
                    byte[] compressed = new byte[64];
                    byte[] decompressed = new byte[64];

                    unsafe
                    {
                        fixed (byte* src = original)
                        fixed (byte* dst = compressed)
                        {
                            int compressedSize = LZ4.Compress(src, original.Length, dst, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);

                            fixed (byte* src2 = compressed)
                            fixed (byte* dst2 = decompressed)
                            {
                                int decompressedSize = LZ4.Decompress(src2, compressedSize, dst2, decompressed.Length);
                                Expect(decompressedSize).ToBe(original.Length);

                                for (int i = 0; i < original.Length; i++)
                                {
                                    Expect(decompressed[i]).ToBe(original[i]);
                                }
                            }
                        }
                    }
                });

                It("should handle data that doesn't compress well", () =>
                {
                    // Create data that doesn't have good compression patterns
                    byte[] original = new byte[1000];
                    for (int i = 0; i < original.Length; i++)
                    {
                        original[i] = (byte)(i * 7 % 256); // Pseudo-random pattern
                    }

                    byte[] compressed = new byte[1024];
                    byte[] decompressed = new byte[1024];

                    unsafe
                    {
                        fixed (byte* src = original)
                        fixed (byte* dst = compressed)
                        {
                            int compressedSize = LZ4.Compress(src, original.Length, dst, compressed.Length);
                            Expect(compressedSize).ToBeGreaterThan(0);

                            fixed (byte* src2 = compressed)
                            fixed (byte* dst2 = decompressed)
                            {
                                int decompressedSize = LZ4.Decompress(src2, compressedSize, dst2, decompressed.Length);
                                Expect(decompressedSize).ToBe(original.Length);

                                for (int i = 0; i < original.Length; i++)
                                {
                                    Expect(decompressed[i]).ToBe(original[i]);
                                }
                            }
                        }
                    }
                });
            });
        }
    }
}
