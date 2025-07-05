using System.Text;

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
                    unsafe
                    {
                        byte[] data = new byte[0];
                        fixed (byte* ptr = data)
                        {
                            uint result = CRC32C.Compute(ptr, 0);
                            
                            // CRC32C of empty data should be consistent
                            uint result2 = CRC32C.Compute(ptr, 0);
                            Expect(result).ToBe(result2);
                        }
                    }
                });

                It("should compute CRC32C for single byte", () =>
                {
                    unsafe
                    {
                        byte[] data = new byte[] { 0x42 };
                        fixed (byte* ptr = data)
                        {
                            uint result = CRC32C.Compute(ptr, data.Length);
                            
                            Expect(result).NotToBe(0u);
                        }
                    }
                });

                It("should compute CRC32C for ASCII string 'hello'", () =>
                {
                    unsafe
                    {
                        byte[] data = Encoding.ASCII.GetBytes("hello");
                        fixed (byte* ptr = data)
                        {
                            uint result = CRC32C.Compute(ptr, data.Length);
                            
                            // Should return a non-zero value for "hello"
                            Expect(result).NotToBe(0u);
                        }
                    }
                });

                It("should compute CRC32C for ASCII string 'world'", () =>
                {
                    unsafe
                    {
                        byte[] data = Encoding.ASCII.GetBytes("world");
                        fixed (byte* ptr = data)
                        {
                            uint result = CRC32C.Compute(ptr, data.Length);
                            
                            // Should return a non-zero value for "world"
                            Expect(result).NotToBe(0u);
                        }
                    }
                });

                It("should compute different CRC32C for 'hello' and 'world'", () =>
                {
                    unsafe
                    {
                        byte[] helloData = Encoding.ASCII.GetBytes("hello");
                        byte[] worldData = Encoding.ASCII.GetBytes("world");
                        
                        uint helloResult, worldResult;
                        fixed (byte* helloPtr = helloData)
                        fixed (byte* worldPtr = worldData)
                        {
                            helloResult = CRC32C.Compute(helloPtr, helloData.Length);
                            worldResult = CRC32C.Compute(worldPtr, worldData.Length);
                        }
                        
                        Expect(helloResult).NotToBe(worldResult);
                    }
                });

                It("should compute consistent CRC32C for same data", () =>
                {
                    unsafe
                    {
                        byte[] data = Encoding.ASCII.GetBytes("test data");
                        
                        uint result1, result2;
                        fixed (byte* ptr = data)
                        {
                            result1 = CRC32C.Compute(ptr, data.Length);
                            result2 = CRC32C.Compute(ptr, data.Length);
                        }
                        
                        Expect(result1).ToBe(result2);
                    }
                });

                It("should compute different CRC32C for different data", () =>
                {
                    unsafe
                    {
                        byte[] data1 = Encoding.ASCII.GetBytes("data1");
                        byte[] data2 = Encoding.ASCII.GetBytes("data2");
                        
                        uint result1, result2;
                        fixed (byte* ptr1 = data1)
                        fixed (byte* ptr2 = data2)
                        {
                            result1 = CRC32C.Compute(ptr1, data1.Length);
                            result2 = CRC32C.Compute(ptr2, data2.Length);
                        }
                        
                        Expect(result1).NotToBe(result2);
                    }
                });

                It("should compute CRC32C for large data", () =>
                {
                    unsafe
                    {
                        byte[] data = new byte[1024];
                        for (int i = 0; i < data.Length; i++)
                        {
                            data[i] = (byte)(i % 256);
                        }
                        
                        fixed (byte* ptr = data)
                        {
                            uint result = CRC32C.Compute(ptr, data.Length);
                            
                            Expect(result).NotToBe(0u);
                        }
                    }
                });

                It("should compute CRC32C for binary data", () =>
                {
                    unsafe
                    {
                        byte[] data = new byte[] { 0x00, 0xFF, 0x55, 0xAA, 0x12, 0x34, 0x56, 0x78 };
                        fixed (byte* ptr = data)
                        {
                            uint result = CRC32C.Compute(ptr, data.Length);
                            
                            Expect(result).NotToBe(0u);
                        }
                    }
                });

                It("should compute CRC32C for incremental data sizes", () =>
                {
                    unsafe
                    {
                        byte[] baseData = Encoding.ASCII.GetBytes("incremental");
                        uint previousResult = 0;
                        
                        for (int length = 1; length <= baseData.Length; length++)
                        {
                            fixed (byte* ptr = baseData)
                            {
                                uint result = CRC32C.Compute(ptr, length);
                                
                                // Each incremental computation should be different
                                Expect(result).NotToBe(previousResult);
                                previousResult = result;
                            }
                        }
                    }
                });

                It("should have correct checksum size constant", () =>
                {
                    Expect(CRC32C.ChecksumSize).ToBe(4);
                });

                It("should compute CRC32C for all zero bytes", () =>
                {
                    unsafe
                    {
                        byte[] data = new byte[16]; // All zeros by default
                        fixed (byte* ptr = data)
                        {
                            uint result = CRC32C.Compute(ptr, data.Length);
                            
                            Expect(result).NotToBe(0u);
                        }
                    }
                });

                It("should compute CRC32C for all 0xFF bytes", () =>
                {
                    unsafe
                    {
                        byte[] data = new byte[16];
                        for (int i = 0; i < data.Length; i++)
                        {
                            data[i] = 0xFF;
                        }
                        
                        fixed (byte* ptr = data)
                        {
                            uint result = CRC32C.Compute(ptr, data.Length);
                            
                            Expect(result).NotToBe(0u);
                        }
                    }
                });

                It("should compute CRC32C for data with sizes that trigger different code paths", () =>
                {
                    unsafe
                    {
                        // Test sizes that may trigger different optimizations (1, 4, 8, 16, 32 bytes)
                        int[] testSizes = { 1, 4, 8, 16, 32 };
                        
                        foreach (int size in testSizes)
                        {
                            byte[] data = new byte[size];
                            for (int i = 0; i < size; i++)
                            {
                                data[i] = (byte)(i + 1);
                            }
                            
                            fixed (byte* ptr = data)
                            {
                                uint result = CRC32C.Compute(ptr, size);
                                
                                // Should compute a valid CRC32C for each size
                                Expect(result).NotToBe(0u);
                            }
                        }
                    }
                });
            });
        }
    }
} 