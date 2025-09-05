using System.Text;
using Wormhole;

namespace Tests
{
    public class CRC32CTests : AbstractTest
    {
        public CRC32CTests()
        {
            Describe("CRC32C Computation", () =>
            {
                It("should compute CRC32C for empty data", () =>
                {
                    byte[] data = new byte[0];
                    uint result = CRC32C.Compute(data, 0);
                    uint result2 = CRC32C.Compute(data, 0);
                    Expect(result).ToBe(result2);
                });

                It("should compute CRC32C for single byte", () =>
                {
                    byte[] data = new byte[] { 0x42 };
                    uint result = CRC32C.Compute(data, data.Length);
                    Expect(result).NotToBe(0u);
                });

                It("should compute CRC32C for ASCII string 'hello'", () =>
                {
                    byte[] data = Encoding.ASCII.GetBytes("hello");
                    uint result = CRC32C.Compute(data, data.Length);
                    Expect(result).NotToBe(0u);
                });

                It("should compute CRC32C for ASCII string 'world'", () =>
                {
                    byte[] data = Encoding.ASCII.GetBytes("world");
                    uint result = CRC32C.Compute(data, data.Length);
                    Expect(result).NotToBe(0u);
                });

                It("should compute different CRC32C for 'hello' and 'world'", () =>
                {
                    byte[] helloData = Encoding.ASCII.GetBytes("hello");
                    byte[] worldData = Encoding.ASCII.GetBytes("world");

                    uint helloResult, worldResult;
                    helloResult = CRC32C.Compute(helloData, helloData.Length);
                    worldResult = CRC32C.Compute(worldData, worldData.Length);
                    Expect(helloResult).NotToBe(worldResult);
                });

                It("should compute consistent CRC32C for same data", () =>
                {
                    byte[] data = Encoding.ASCII.GetBytes("test data");
                    uint result1, result2;
                    result1 = CRC32C.Compute(data, data.Length);
                    result2 = CRC32C.Compute(data, data.Length);
                    Expect(result1).ToBe(result2);
                });

                It("should compute different CRC32C for different data", () =>
                {
                    byte[] data1 = Encoding.ASCII.GetBytes("data1");
                    byte[] data2 = Encoding.ASCII.GetBytes("data2");

                    uint result1, result2;
                    result1 = CRC32C.Compute(data1, data1.Length);
                    result2 = CRC32C.Compute(data2, data2.Length);
                    Expect(result1).NotToBe(result2);
                });

                It("should compute CRC32C for large data", () =>
                {
                    byte[] data = new byte[1024];

                    for (int i = 0; i < data.Length; i++)
                        data[i] = (byte)(i % 256);

                    uint result = CRC32C.Compute(data, data.Length);

                    Expect(result).NotToBe(0u);
                });

                It("should compute CRC32C for binary data", () =>
                {
                    byte[] data = new byte[] { 0x00, 0xFF, 0x55, 0xAA, 0x12, 0x34, 0x56, 0x78 };
                    uint result = CRC32C.Compute(data, data.Length);
                    Expect(result).NotToBe(0u);
                });

                It("should compute CRC32C for incremental data sizes", () =>
                {
                    byte[] baseData = Encoding.ASCII.GetBytes("incremental");
                    uint previousResult = 0;

                    for (int length = 1; length <= baseData.Length; length++)
                    {
                        uint result = CRC32C.Compute(baseData, length);

                        Expect(result).NotToBe(previousResult);

                        previousResult = result;
                    }
                });

                It("should have correct checksum size constant", () =>
                {
                    Expect(CRC32C.ChecksumSize).ToBe(4);
                });

                It("should compute CRC32C for all zero bytes", () =>
                {
                    byte[] data = new byte[16];
                    uint result = CRC32C.Compute(data, data.Length);
                    Expect(result).NotToBe(0u);
                });

                It("should compute CRC32C for all 0xFF bytes", () =>
                {
                    byte[] data = new byte[16];

                    for (int i = 0; i < data.Length; i++)
                        data[i] = 0xFF;

                    uint result = CRC32C.Compute(data, data.Length);
                    Expect(result).NotToBe(0u);
                });

                It("should compute CRC32C for data with sizes that trigger different code paths", () =>
                {
                    int[] testSizes = { 1, 4, 8, 16, 32 };

                    foreach (int size in testSizes)
                    {
                        byte[] data = new byte[size];

                        for (int i = 0; i < size; i++)
                            data[i] = (byte)(i + 1);

                        uint result = CRC32C.Compute(data, size);
                        Expect(result).NotToBe(0u);
                    }
                });
            });
        }
    }
}
