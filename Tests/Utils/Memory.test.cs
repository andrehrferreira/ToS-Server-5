using System.Runtime.InteropServices;

namespace Tests
{
    public unsafe class MemoryTests : AbstractTest
    {
        public MemoryTests()
        {
            Describe("Memory Copy from IntPtr to byte[]", () =>
            {
                It("should copy data from IntPtr to byte array", () =>
                {
                    byte[] sourceData = { 1, 2, 3, 4, 5 };
                    byte[] destination = new byte[5];

                    fixed (byte* sourcePtr = sourceData)
                    {
                        IntPtr source = new IntPtr(sourcePtr);
                        Memory.Copy(source, 0, destination, 0, 5);
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        Expect(destination[i]).ToBe(sourceData[i]);
                    }
                });

                It("should copy with source offset", () =>
                {
                    byte[] sourceData = { 10, 20, 30, 40, 50, 60 };
                    byte[] destination = new byte[3];

                    fixed (byte* sourcePtr = sourceData)
                    {
                        IntPtr source = new IntPtr(sourcePtr);
                        Memory.Copy(source, 2, destination, 0, 3); // Skip first 2 bytes
                    }

                    Expect(destination[0]).ToBe(30);
                    Expect(destination[1]).ToBe(40);
                    Expect(destination[2]).ToBe(50);
                });

                It("should copy with destination offset", () =>
                {
                    byte[] sourceData = { 100, 200 };
                    byte[] destination = new byte[5];

                    fixed (byte* sourcePtr = sourceData)
                    {
                        IntPtr source = new IntPtr(sourcePtr);
                        Memory.Copy(source, 0, destination, 2, 2); // Start at index 2
                    }

                    Expect(destination[0]).ToBe(0);
                    Expect(destination[1]).ToBe(0);
                    Expect(destination[2]).ToBe(100);
                    Expect(destination[3]).ToBe(200);
                    Expect(destination[4]).ToBe(0);
                });

                It("should copy with both offsets", () =>
                {
                    byte[] sourceData = { 1, 2, 3, 4, 5, 6, 7, 8 };
                    byte[] destination = new byte[10];

                    fixed (byte* sourcePtr = sourceData)
                    {
                        IntPtr source = new IntPtr(sourcePtr);
                        Memory.Copy(source, 3, destination, 2, 4); // Source offset 3, dest offset 2, length 4
                    }

                    Expect(destination[0]).ToBe(0);
                    Expect(destination[1]).ToBe(0);
                    Expect(destination[2]).ToBe(4);
                    Expect(destination[3]).ToBe(5);
                    Expect(destination[4]).ToBe(6);
                    Expect(destination[5]).ToBe(7);
                    Expect(destination[6]).ToBe(0);
                });

                It("should handle zero length copy", () =>
                {
                    byte[] sourceData = { 1, 2, 3 };
                    byte[] destination = { 10, 20, 30 };
                    byte[] originalDest = { 10, 20, 30 };

                    fixed (byte* sourcePtr = sourceData)
                    {
                        IntPtr source = new IntPtr(sourcePtr);
                        Memory.Copy(source, 0, destination, 0, 0);
                    }

                    // Destination should remain unchanged
                    for (int i = 0; i < 3; i++)
                    {
                        Expect(destination[i]).ToBe(originalDest[i]);
                    }
                });
            });

            Describe("Memory Copy from byte[] to IntPtr", () =>
            {
                It("should copy data from byte array to IntPtr", () =>
                {
                    byte[] source = { 1, 2, 3, 4, 5 };
                    byte[] destinationData = new byte[5];

                    fixed (byte* destPtr = destinationData)
                    {
                        IntPtr destination = new IntPtr(destPtr);
                        Memory.Copy(source, 0, destination, 0, 5);
                    }

                    for (int i = 0; i < 5; i++)
                    {
                        Expect(destinationData[i]).ToBe(source[i]);
                    }
                });

                It("should copy with source offset", () =>
                {
                    byte[] source = { 10, 20, 30, 40, 50 };
                    byte[] destinationData = new byte[3];

                    fixed (byte* destPtr = destinationData)
                    {
                        IntPtr destination = new IntPtr(destPtr);
                        Memory.Copy(source, 1, destination, 0, 3);
                    }

                    Expect(destinationData[0]).ToBe(20);
                    Expect(destinationData[1]).ToBe(30);
                    Expect(destinationData[2]).ToBe(40);
                });

                It("should copy with destination offset", () =>
                {
                    byte[] source = { 100, 200 };
                    byte[] destinationData = new byte[5];

                    fixed (byte* destPtr = destinationData)
                    {
                        IntPtr destination = new IntPtr(destPtr);
                        Memory.Copy(source, 0, destination, 2, 2);
                    }

                    Expect(destinationData[0]).ToBe(0);
                    Expect(destinationData[1]).ToBe(0);
                    Expect(destinationData[2]).ToBe(100);
                    Expect(destinationData[3]).ToBe(200);
                    Expect(destinationData[4]).ToBe(0);
                });

                It("should handle large data copy", () =>
                {
                    const int size = 1000;
                    byte[] source = new byte[size];
                    byte[] destinationData = new byte[size];

                    // Fill source with pattern
                    for (int i = 0; i < size; i++)
                    {
                        source[i] = (byte)(i % 256);
                    }

                    fixed (byte* destPtr = destinationData)
                    {
                        IntPtr destination = new IntPtr(destPtr);
                        Memory.Copy(source, 0, destination, 0, size);
                    }

                    // Verify all data copied correctly
                    for (int i = 0; i < size; i++)
                    {
                        Expect(destinationData[i]).ToBe(source[i]);
                    }
                });
            });

            Describe("Memory Copy from byte[] to byte[]", () =>
            {
                It("should copy data between byte arrays", () =>
                {
                    byte[] source = { 1, 2, 3, 4, 5 };
                    byte[] destination = new byte[5];

                    Memory.Copy(source, 0, destination, 0, 5);

                    for (int i = 0; i < 5; i++)
                    {
                        Expect(destination[i]).ToBe(source[i]);
                    }
                });

                It("should copy partial data with offsets", () =>
                {
                    byte[] source = { 10, 20, 30, 40, 50, 60, 70 };
                    byte[] destination = new byte[10];

                    Memory.Copy(source, 2, destination, 3, 4);

                    Expect(destination[0]).ToBe(0);
                    Expect(destination[1]).ToBe(0);
                    Expect(destination[2]).ToBe(0);
                    Expect(destination[3]).ToBe(30);
                    Expect(destination[4]).ToBe(40);
                    Expect(destination[5]).ToBe(50);
                    Expect(destination[6]).ToBe(60);
                    Expect(destination[7]).ToBe(0);
                });

                It("should handle overlapping arrays correctly", () =>
                {
                    byte[] data = { 1, 2, 3, 4, 5, 6, 7, 8 };

                    // Copy within same array (shift right)
                    Memory.Copy(data, 0, data, 2, 4);

                    Expect(data[0]).ToBe(1);
                    Expect(data[1]).ToBe(2);
                    Expect(data[2]).ToBe(1);
                    Expect(data[3]).ToBe(2);
                    Expect(data[4]).ToBe(3);
                    Expect(data[5]).ToBe(4);
                });

                It("should copy single byte", () =>
                {
                    byte[] source = { 42 };
                    byte[] destination = new byte[3];

                    Memory.Copy(source, 0, destination, 1, 1);

                    Expect(destination[0]).ToBe(0);
                    Expect(destination[1]).ToBe(42);
                    Expect(destination[2]).ToBe(0);
                });

                It("should handle zero length copy gracefully", () =>
                {
                    byte[] source = { 1, 2, 3 };
                    byte[] destination = { 10, 20, 30 };
                    byte[] originalDest = { 10, 20, 30 };

                    Memory.Copy(source, 0, destination, 0, 0);

                    for (int i = 0; i < 3; i++)
                    {
                        Expect(destination[i]).ToBe(originalDest[i]);
                    }
                });
            });

            Describe("Memory Copy Edge Cases", () =>
            {
                It("should handle empty source array", () =>
                {
                    byte[] source = new byte[0];
                    byte[] destination = new byte[5];
                    byte[] originalDest = { 1, 2, 3, 4, 5 };

                    Array.Copy(originalDest, destination, 5);

                    Memory.Copy(source, 0, destination, 0, 0);

                    for (int i = 0; i < 5; i++)
                    {
                        Expect(destination[i]).ToBe(originalDest[i]);
                    }
                });

                It("should handle copying to edge of destination", () =>
                {
                    byte[] source = { 1, 2 };
                    byte[] destination = new byte[5];

                    Memory.Copy(source, 0, destination, 3, 2);

                    Expect(destination[0]).ToBe(0);
                    Expect(destination[1]).ToBe(0);
                    Expect(destination[2]).ToBe(0);
                    Expect(destination[3]).ToBe(1);
                    Expect(destination[4]).ToBe(2);
                });

                It("should handle copying from edge of source", () =>
                {
                    byte[] source = { 10, 20, 30, 40, 50 };
                    byte[] destination = new byte[2];

                    Memory.Copy(source, 3, destination, 0, 2);

                    Expect(destination[0]).ToBe(40);
                    Expect(destination[1]).ToBe(50);
                });

                It("should handle pattern data correctly", () =>
                {
                    byte[] source = new byte[256];
                    byte[] destination = new byte[256];

                    // Create pattern
                    for (int i = 0; i < 256; i++)
                    {
                        source[i] = (byte)i;
                    }

                    Memory.Copy(source, 0, destination, 0, 256);

                    for (int i = 0; i < 256; i++)
                    {
                        Expect(destination[i]).ToBe((byte)i);
                    }
                });
            });

            Describe("Memory Copy with IntPtr and Memory Management", () =>
            {
                It("should work with allocated unmanaged memory", () =>
                {
                    IntPtr sourcePtr = Marshal.AllocHGlobal(10);
                    IntPtr destPtr = Marshal.AllocHGlobal(10);

                    try
                    {
                        // Initialize source memory
                        byte* sourceByte = (byte*)sourcePtr;
                        for (int i = 0; i < 10; i++)
                        {
                            sourceByte[i] = (byte)(i + 1);
                        }

                        // Create managed array for copy
                        byte[] intermediateArray = new byte[10];

                        // Copy from unmanaged to managed
                        Memory.Copy(sourcePtr, 0, intermediateArray, 0, 10);

                        // Copy from managed to unmanaged
                        Memory.Copy(intermediateArray, 0, destPtr, 0, 10);

                        // Verify destination
                        byte* destByte = (byte*)destPtr;
                        for (int i = 0; i < 10; i++)
                        {
                            Expect(destByte[i]).ToBe((byte)(i + 1));
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(sourcePtr);
                        Marshal.FreeHGlobal(destPtr);
                    }
                });

                It("should handle mixed managed and unmanaged operations", () =>
                {
                    byte[] managedSource = { 11, 22, 33, 44, 55 };
                    IntPtr unmanagedDest = Marshal.AllocHGlobal(5);
                    byte[] managedDest = new byte[5];

                    try
                    {
                        // Managed to unmanaged
                        Memory.Copy(managedSource, 0, unmanagedDest, 0, 5);

                        // Unmanaged back to managed
                        Memory.Copy(unmanagedDest, 0, managedDest, 0, 5);

                        for (int i = 0; i < 5; i++)
                        {
                            Expect(managedDest[i]).ToBe(managedSource[i]);
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(unmanagedDest);
                    }
                });

                It("should handle copying with different data types", () =>
                {
                    int[] intData = { 1000, 2000, 3000 };
                    byte[] byteResult = new byte[12]; // 3 ints * 4 bytes each

                    fixed (int* intPtr = intData)
                    {
                        IntPtr source = new IntPtr(intPtr);
                        Memory.Copy(source, 0, byteResult, 0, 12);
                    }

                    // Verify the bytes represent the integers correctly
                    int reconstructed1 = BitConverter.ToInt32(byteResult, 0);
                    int reconstructed2 = BitConverter.ToInt32(byteResult, 4);
                    int reconstructed3 = BitConverter.ToInt32(byteResult, 8);

                    Expect(reconstructed1).ToBe(1000);
                    Expect(reconstructed2).ToBe(2000);
                    Expect(reconstructed3).ToBe(3000);
                });
            });
        }
    }
}
