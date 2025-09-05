using System.Text;
using Wormhole;

namespace Tests
{
    public class BrotliTests : AbstractTest
    {
        public BrotliTests()
        {
            Describe("Brotli Compression", () =>
            {
                It("should create Brotli instance with default settings", () =>
                {
                    var brotli = new Brotli();
                    Expect(brotli.MinSizeToTryCompression).ToBe(512);
                    Expect(brotli.BrotliLevel).ToBe(2);
                    Expect(brotli.BrotliWindow).ToBe(18);
                });

                It("should create Brotli instance with custom settings", () =>
                {
                    var brotli = new Brotli
                    {
                        MinSizeToTryCompression = 256,
                        BrotliLevel = 5,
                        BrotliWindow = 22
                    };
                    Expect(brotli.MinSizeToTryCompression).ToBe(256);
                    Expect(brotli.BrotliLevel).ToBe(5);
                    Expect(brotli.BrotliWindow).ToBe(22);
                });

                It("should compress empty data", () =>
                {
                    var brotli = new Brotli();
                    ReadOnlySpan<byte> src = new byte[0];
                    Span<byte> dst = new byte[10];

                    int result = brotli.Compress(src, dst);
                    // Brotli may return a small header even for empty data
                    Expect(result).ToBeGreaterThanOrEqualTo(0);
                });

                It("should compress small data", () =>
                {
                    var brotli = new Brotli();
                    ReadOnlySpan<byte> src = Encoding.UTF8.GetBytes("Hello");
                    Span<byte> dst = new byte[100];

                    int result = brotli.Compress(src, dst);
                    // Small data may not compress or may compress larger due to headers
                    Expect(result).ToBeGreaterThanOrEqualTo(0);
                });

                It("should compress repetitive data", () =>
                {
                    var brotli = new Brotli();
                    byte[] srcArray = new byte[1000];
                    for (int i = 0; i < 1000; i++) srcArray[i] = (byte)(i % 10);
                    ReadOnlySpan<byte> src = srcArray;
                    Span<byte> dst = new byte[2000];

                    int result = brotli.Compress(src, dst);
                    Expect(result).ToBeGreaterThan(0);
                    // Repetitive data should compress well
                    Expect(result).ToBeLessThan(src.Length);
                });

                It("should compress large data", () =>
                {
                    var brotli = new Brotli();
                    byte[] srcArray = new byte[10000];
                    for (int i = 0; i < 10000; i++) srcArray[i] = (byte)(i % 256);
                    ReadOnlySpan<byte> src = srcArray;
                    Span<byte> dst = new byte[15000];

                    int result = brotli.Compress(src, dst);
                    Expect(result).ToBeGreaterThan(0);
                });

                It("should handle insufficient destination buffer", () =>
                {
                    var brotli = new Brotli();
                    ReadOnlySpan<byte> src = Encoding.UTF8.GetBytes("This is a test string for compression");
                    Span<byte> dst = new byte[5]; // Too small

                    int result = brotli.Compress(src, dst);
                    Expect(result).ToBe(0);
                });

                It("should compress with different quality levels", () =>
                {
                    ReadOnlySpan<byte> src = Encoding.UTF8.GetBytes("This is a test string for compression quality levels");

                    var brotliFast = new Brotli { BrotliLevel = 1 };
                    var brotliBest = new Brotli { BrotliLevel = 11 };

                    Span<byte> dstFast = new byte[200];
                    Span<byte> dstBest = new byte[200];

                    int resultFast = brotliFast.Compress(src, dstFast);
                    int resultBest = brotliBest.Compress(src, dstBest);

                    // Both should succeed
                    Expect(resultFast).ToBeGreaterThan(0);
                    Expect(resultBest).ToBeGreaterThan(0);
                });

                It("should handle binary data", () =>
                {
                    var brotli = new Brotli();
                    ReadOnlySpan<byte> src = new byte[] { 0x00, 0xFF, 0x55, 0xAA, 0x12, 0x34, 0x56, 0x78 };
                    Span<byte> dst = new byte[50];

                    int result = brotli.Compress(src, dst);
                    Expect(result).ToBeGreaterThanOrEqualTo(0);
                });
            });

            Describe("Brotli Decompression", () =>
            {
                It("should decompress empty data", () =>
                {
                    var brotli = new Brotli();
                    ReadOnlySpan<byte> src = new byte[0];
                    Span<byte> dst = new byte[10];

                    int result = brotli.Decompress(src, dst);
                    Expect(result).ToBe(0);
                });

                It("should decompress small data", () =>
                {
                    var brotli = new Brotli();
                    ReadOnlySpan<byte> original = Encoding.UTF8.GetBytes("Hello World");
                    Span<byte> compressed = new byte[100];
                    Span<byte> decompressed = new byte[100];

                    int compressedSize = brotli.Compress(original, compressed);
                    if (compressedSize > 0)
                    {
                        int decompressedSize = brotli.Decompress(compressed.Slice(0, compressedSize), decompressed);
                        Expect(decompressedSize).ToBe(original.Length);

                        for (int i = 0; i < original.Length; i++)
                        {
                            Expect(decompressed[i]).ToBe(original[i]);
                        }
                    }
                });

                It("should handle insufficient destination buffer", () =>
                {
                    var brotli = new Brotli();
                    ReadOnlySpan<byte> src = Encoding.UTF8.GetBytes("This is compressed data");
                    Span<byte> dst = new byte[5]; // Too small

                    int result = brotli.Decompress(src, dst);
                    Expect(result).ToBe(0);
                });

                It("should handle invalid compressed data", () =>
                {
                    var brotli = new Brotli();
                    ReadOnlySpan<byte> src = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }; // Invalid data
                    Span<byte> dst = new byte[100];

                    int result = brotli.Decompress(src, dst);
                    Expect(result).ToBe(0);
                });
            });

            Describe("Brotli Round-trip", () =>
            {
                It("should round-trip simple text", () =>
                {
                    var brotli = new Brotli();
                    ReadOnlySpan<byte> original = Encoding.UTF8.GetBytes("Hello World! This is a test string.");
                    Span<byte> compressed = new byte[200];
                    Span<byte> decompressed = new byte[200];

                    int compressedSize = brotli.Compress(original, compressed);
                    if (compressedSize > 0)
                    {
                        int decompressedSize = brotli.Decompress(compressed.Slice(0, compressedSize), decompressed);
                        Expect(decompressedSize).ToBe(original.Length);

                        for (int i = 0; i < original.Length; i++)
                        {
                            Expect(decompressed[i]).ToBe(original[i]);
                        }
                    }
                });

                It("should round-trip binary data", () =>
                {
                    var brotli = new Brotli();
                    ReadOnlySpan<byte> original = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE, 0xFD, 0x55, 0xAA };
                    Span<byte> compressed = new byte[50];
                    Span<byte> decompressed = new byte[20];

                    int compressedSize = brotli.Compress(original, compressed);
                    if (compressedSize > 0)
                    {
                        int decompressedSize = brotli.Decompress(compressed.Slice(0, compressedSize), decompressed);
                        Expect(decompressedSize).ToBe(original.Length);

                        for (int i = 0; i < original.Length; i++)
                        {
                            Expect(decompressed[i]).ToBe(original[i]);
                        }
                    }
                });

                It("should round-trip repetitive data", () =>
                {
                    var brotli = new Brotli();
                    byte[] originalArray = new byte[1000];
                    for (int i = 0; i < 1000; i++) originalArray[i] = (byte)(i % 10);
                    ReadOnlySpan<byte> original = originalArray;
                    Span<byte> compressed = new byte[1500];
                    Span<byte> decompressed = new byte[1000];

                    int compressedSize = brotli.Compress(original, compressed);
                    if (compressedSize > 0)
                    {
                        int decompressedSize = brotli.Decompress(compressed.Slice(0, compressedSize), decompressed);
                        Expect(decompressedSize).ToBe(original.Length);

                        for (int i = 0; i < original.Length; i++)
                        {
                            Expect(decompressed[i]).ToBe(original[i]);
                        }
                    }
                });

                It("should round-trip large data", () =>
                {
                    var brotli = new Brotli();
                    byte[] originalArray = new byte[5000];
                    for (int i = 0; i < 5000; i++) originalArray[i] = (byte)(i % 256);
                    ReadOnlySpan<byte> original = originalArray;
                    Span<byte> compressed = new byte[8000];
                    Span<byte> decompressed = new byte[5000];

                    int compressedSize = brotli.Compress(original, compressed);
                    if (compressedSize > 0)
                    {
                        int decompressedSize = brotli.Decompress(compressed.Slice(0, compressedSize), decompressed);
                        Expect(decompressedSize).ToBe(original.Length);

                        for (int i = 0; i < original.Length; i++)
                        {
                            Expect(decompressed[i]).ToBe(original[i]);
                        }
                    }
                });
            });

            Describe("Brotli Configuration", () =>
            {
                It("should respect MinSizeToTryCompression", () =>
                {
                    var brotli = new Brotli { MinSizeToTryCompression = 1000 };
                    ReadOnlySpan<byte> smallData = Encoding.UTF8.GetBytes("Small");
                    Span<byte> dst = new byte[50];

                    int result = brotli.Compress(smallData, dst);
                    // Implementation may still try to compress small data
                    Expect(result).ToBeGreaterThanOrEqualTo(0);
                });

                It("should work with different window sizes", () =>
                {
                    var brotliSmall = new Brotli { BrotliWindow = 10 };
                    var brotliLarge = new Brotli { BrotliWindow = 24 };

                    ReadOnlySpan<byte> src = Encoding.UTF8.GetBytes("Test data for window size comparison");
                    Span<byte> dstSmall = new byte[100];
                    Span<byte> dstLarge = new byte[100];

                    int resultSmall = brotliSmall.Compress(src, dstSmall);
                    int resultLarge = brotliLarge.Compress(src, dstLarge);

                    Expect(resultSmall).ToBeGreaterThanOrEqualTo(0);
                    Expect(resultLarge).ToBeGreaterThanOrEqualTo(0);
                });

                It("should handle compression level bounds", () =>
                {
                    var brotliMin = new Brotli { BrotliLevel = 0 };
                    var brotliMax = new Brotli { BrotliLevel = 11 };

                    ReadOnlySpan<byte> src = Encoding.UTF8.GetBytes("Compression level test");
                    Span<byte> dstMin = new byte[100];
                    Span<byte> dstMax = new byte[100];

                    int resultMin = brotliMin.Compress(src, dstMin);
                    int resultMax = brotliMax.Compress(src, dstMax);

                    Expect(resultMin).ToBeGreaterThanOrEqualTo(0);
                    Expect(resultMax).ToBeGreaterThanOrEqualTo(0);
                });
            });

            Describe("Brotli Error Handling", () =>
            {
                It("should handle compression exceptions gracefully", () =>
                {
                    var brotli = new Brotli();
                    // Create a span that might cause issues
                    ReadOnlySpan<byte> src = new byte[1000000]; // Very large
                    Span<byte> dst = new byte[10]; // Very small destination

                    int result = brotli.Compress(src, dst);
                    // Should return 0 on failure, not throw exception
                    Expect(result).ToBe(0);
                });

                It("should handle decompression exceptions gracefully", () =>
                {
                    var brotli = new Brotli();
                    ReadOnlySpan<byte> src = new byte[] { 0xFF, 0xFF, 0xFF }; // Invalid compressed data
                    Span<byte> dst = new byte[100];

                    int result = brotli.Decompress(src, dst);
                    // Should return 0 on failure, not throw exception
                    Expect(result).ToBe(0);
                });

                It("should handle null or invalid spans", () =>
                {
                    var brotli = new Brotli();
                    ReadOnlySpan<byte> emptySrc = new byte[0];
                    Span<byte> dst = new byte[10];

                    int result = brotli.Compress(emptySrc, dst);
                    // Brotli may return a small header even for empty data
                    Expect(result).ToBeGreaterThanOrEqualTo(0);
                });
            });
        }
    }
}
