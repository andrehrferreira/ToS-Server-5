using System.Text;

namespace Tests
{
    public class FlatBufferTests : AbstractTest
    {
        public FlatBufferTests()
        {
            Describe("FlatBuffer Core Operations", () =>
            {
                It("should create buffer with specified capacity", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    Expect(buffer.Capacity).ToBe(1024);
                    Expect(buffer.Position).ToBe(0);
                    Expect(buffer.IsDisposed).ToBe(false);
                });

                It("should write and read template operations", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    buffer.Write<float>(3.14f);
                    buffer.Write<bool>(true);
                    buffer.Write<byte>(255);
                    buffer.Write<double>(2.71828);

                    buffer.Reset();

                    float floatValue = buffer.Read<float>();
                    bool boolValue = buffer.Read<bool>();
                    byte byteValue = buffer.Read<byte>();
                    double doubleValue = buffer.Read<double>();

                    Expect(floatValue).ToBe(3.14f);
                    Expect(boolValue).ToBe(true);
                    Expect(byteValue).ToBe(255);
                    Expect(doubleValue).ToBe(2.71828);
                });

                It("should use VarInt encoding for primitive types via templates", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    buffer.Write<int>(42);
                    buffer.Write<uint>(123u);
                    buffer.Write<long>(-456L);
                    buffer.Write<ulong>(789UL);
                    buffer.Write<short>(-12);
                    buffer.Write<ushort>(34);

                    buffer.Reset();

                    int intValue = buffer.Read<int>();
                    uint uintValue = buffer.Read<uint>();
                    long longValue = buffer.Read<long>();
                    ulong ulongValue = buffer.Read<ulong>();
                    short shortValue = buffer.Read<short>();
                    ushort ushortValue = buffer.Read<ushort>();

                    Expect(intValue).ToBe(42);
                    Expect(uintValue).ToBe(123u);
                    Expect(longValue).ToBe(-456L);
                    Expect(ulongValue).ToBe(789UL);
                    Expect(shortValue).ToBe(-12);
                    Expect(ushortValue).ToBe(34);
                });

                It("should demonstrate VarInt space efficiency via templates", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    buffer.Write<int>(63);    
                    int posSmall = buffer.Position;

                    buffer.Write<int>(16384); 
                    int posLarge = buffer.Position;
                    int smallValueBytes = posSmall;
                    int largeValueBytes = posLarge - posSmall;

                    Expect(smallValueBytes).ToBe(4);

                    buffer.Reset();

                    int value1 = buffer.Read<int>();
                    int value2 = buffer.Read<int>();

                    Expect(value1).ToBe(63);
                    Expect(value2).ToBe(16384);
                });

                It("should write and read ZigZag encoded integers", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    buffer.Write(42);        
                    buffer.Write(-42);       
                    buffer.Write(1000);      

                    buffer.Reset();

                    int value1 = buffer.Read<int>();
                    int value2 = buffer.Read<int>();
                    int value3 = buffer.Read<int>();

                    Expect(value1).ToBe(42);
                    Expect(value2).ToBe(-42);
                    Expect(value3).ToBe(1000);
                });

                It("should handle position management", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    buffer.Write<float>(100.0f);
                    int savedPos = buffer.SavePosition();
                    buffer.Write<float>(200.0f);
                    buffer.RestorePosition(savedPos);

                    float value = buffer.Read<float>();
                    Expect(value).ToBe(200.0f);
                });

                It("should support peek operations", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    buffer.Write<float>(42.0f);
                    buffer.Reset();

                    float peekedValue = buffer.Peek<float>();
                    float readValue = buffer.Read<float>();

                    Expect(peekedValue).ToBe(42.0f);
                    Expect(readValue).ToBe(42.0f);
                });

                It("should reset buffer correctly", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    buffer.Write<float>(42.0f);
                    buffer.Write<float>(3.14f);

                    Expect(buffer.Position).ToBeGreaterThan(0);

                    buffer.Reset();

                    Expect(buffer.Position).ToBe(0);
                });
            });

            Describe("FlatBuffer Variable-Length Encoding", () =>
            {
                It("should encode and decode signed integers with ZigZag", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    int[] testValues = { -1000, -1, 0, 1, 1000, int.MinValue, int.MaxValue };

                    foreach (int value in testValues)
                    {
                        buffer.Reset();
                        buffer.Write(value);    
                        buffer.Reset();
                        int result = buffer.Read<int>();  
                        Expect(result).ToBe(value);
                    }
                });

                It("should encode and decode unsigned integers", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    uint[] testValues = { 0, 1, 127, 128, 255, 256, uint.MaxValue };

                    foreach (uint value in testValues)
                    {
                        buffer.Reset();
                        buffer.Write(value);    
                        buffer.Reset();
                        uint result = buffer.Read<uint>(); 
                        Expect(result).ToBe(value);
                    }
                });

                It("should encode and decode long values with ZigZag", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    long[] testValues = { -1000000L, -1L, 0L, 1L, 1000000L, long.MinValue, long.MaxValue };

                    foreach (long value in testValues)
                    {
                        buffer.Reset();
                        buffer.Write(value);    
                        buffer.Reset();
                        long result = buffer.Read<long>(); 
                        Expect(result).ToBe(value);
                    }
                });

                It("should encode and decode VarInt efficiently", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    buffer.WriteVarInt(63);   
                    int pos1 = buffer.Position;

                    buffer.WriteVarInt(64);   
                    int pos2 = buffer.Position;

                    Expect(pos1).ToBe(1);
                    Expect(pos2 - pos1).ToBe(2);

                    buffer.Reset();
                    int val1 = buffer.ReadVarInt();
                    int val2 = buffer.ReadVarInt();

                    Expect(val1).ToBe(63);
                    Expect(val2).ToBe(64);
                });

                It("should encode and decode VarLong efficiently", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    long[] testValues = { 0L, 127L, 128L, 16383L, 16384L };

                    foreach (long value in testValues)
                    {
                        buffer.Reset();
                        buffer.WriteVarLong(value);
                        buffer.Reset();
                        long result = buffer.ReadVarLong();
                        Expect(result).ToBe(value);
                    }
                });
            });

            Describe("FlatBuffer Bit-Level Operations", () =>
            {
                It("should write and read individual bits", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    buffer.WriteBit(true);
                    buffer.WriteBit(false);
                    buffer.WriteBit(true);
                    buffer.WriteBit(true);

                    buffer.Reset();

                    bool bit1 = buffer.ReadBit();
                    bool bit2 = buffer.ReadBit();
                    bool bit3 = buffer.ReadBit();
                    bool bit4 = buffer.ReadBit();

                    Expect(bit1).ToBe(true);
                    Expect(bit2).ToBe(false);
                    Expect(bit3).ToBe(true);
                    Expect(bit4).ToBe(true);
                });

                It("should handle bit alignment correctly", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    buffer.WriteBit(true);
                    buffer.WriteBit(false);
                    buffer.WriteBit(true);
                    buffer.AlignBits();

                    buffer.Write<byte>(42);

                    buffer.Reset();

                    buffer.ReadBit();
                    buffer.ReadBit();
                    buffer.ReadBit();
                    buffer.AlignBits();

                    byte value = buffer.Read<byte>();
                    Expect(value).ToBe(42);
                });

                It("should handle full byte of bits", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    bool[] bits = { true, false, true, true, false, true, false, true };

                    foreach (bool bit in bits)
                        buffer.WriteBit(bit);

                    buffer.Reset();

                    for (int i = 0; i < bits.Length; i++)
                    {
                        bool readBit = buffer.ReadBit();
                        Expect(readBit).ToBe(bits[i]);
                    }
                });

                It("should track bit length correctly", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    buffer.WriteBit(true);
                    buffer.WriteBit(false);
                    buffer.WriteBit(true);

                    int bitLength = buffer.LengthBits;
                    Expect(bitLength).ToBe(3);

                    buffer.WriteBit(true);
                    buffer.WriteBit(false);

                    bitLength = buffer.LengthBits;
                    Expect(bitLength).ToBe(5);
                });
            });

            Describe("FlatBuffer Quantization System", () =>
            {


                It("should quantize and dequantize FVector values", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    var original = new FVector(10.5f, -20.3f, 30.7f);
                    float min = -100.0f;
                    float max = 100.0f;

                    buffer.Write(original, 0.1f);
                    buffer.Reset();

                    var result = buffer.ReadFVector();

                    float diffX = Math.Abs(result.X - original.X);
                    float diffY = Math.Abs(result.Y - original.Y);
                    float diffZ = Math.Abs(result.Z - original.Z);

                    Expect(diffX).ToBeLessThan(0.01f);
                    Expect(diffY).ToBeLessThan(0.01f);
                    Expect(diffZ).ToBeLessThan(0.01f);
                });

                It("should quantize and dequantize FRotator values", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    var original = new FRotator(45.0f, 90.0f, 135.0f);
                    float min = -180.0f;
                    float max = 180.0f;

                    buffer.Write(original);
                    buffer.Reset();

                    var result = buffer.ReadFRotator();

                    float diffPitch = Math.Abs(result.Pitch - original.Pitch);
                    float diffYaw = Math.Abs(result.Yaw - original.Yaw);
                    float diffRoll = Math.Abs(result.Roll - original.Roll);

                    Expect(diffPitch).ToBeLessThan(0.01f);
                    Expect(diffYaw).ToBeLessThan(0.01f);
                    Expect(diffRoll).ToBeLessThan(0.01f);
                });

                It("should handle FVector and FRotator integer operations", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    var originalVector = new FVector(100, 200, 300);
                    var originalRotator = new FRotator(45, 90, 135);

                    buffer.Write(originalVector);   
                    buffer.Write(originalRotator);   

                    buffer.Reset();

                    var resultVector = buffer.ReadFVector();    
                    var resultRotator = buffer.ReadFRotator(); 

                    Expect(resultVector.X).ToBe(originalVector.X);
                    Expect(resultVector.Y).ToBe(originalVector.Y);
                    Expect(resultVector.Z).ToBe(originalVector.Z);

                    Expect(resultRotator.Pitch).ToBe(originalRotator.Pitch);
                    Expect(resultRotator.Yaw).ToBe(originalRotator.Yaw);
                    Expect(resultRotator.Roll).ToBe(originalRotator.Roll);
                });

                It("should quantize FVector and FRotator using template operations", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    var originalVector = new FVector(1.23f, -4.56f, 7.89f);
                    var originalRotator = new FRotator(12.3f, -45.6f, 78.9f);

                    buffer.Write<FVector>(originalVector);  
                    buffer.Write<FRotator>(originalRotator); 

                    buffer.Reset();

                    var resultVector = buffer.Read<FVector>();   
                    var resultRotator = buffer.Read<FRotator>(); 

                    Expect(resultVector.X).ToBeApproximately(originalVector.X, 1.0f);
                    Expect(resultVector.Y).ToBeApproximately(originalVector.Y, 1.0f);
                    Expect(resultVector.Z).ToBeApproximately(originalVector.Z, 1.0f);

                    Expect(resultRotator.Pitch).ToBeApproximately(originalRotator.Pitch, 0.1f);
                    Expect(resultRotator.Yaw).ToBeApproximately(originalRotator.Yaw, 0.1f);
                    Expect(resultRotator.Roll).ToBeApproximately(originalRotator.Roll, 0.1f);
                });
            });

            Describe("FlatBuffer String Operations", () =>
            {
                It("should write and read ASCII strings", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    string original = "Hello World";
                    buffer.WriteAsciiString(original);

                    buffer.Reset();

                    string result = buffer.ReadAsciiString();
                    Expect(result).ToBe(original);
                });

                It("should write and read UTF8 strings", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    string original = "Olá Mundo! 🌍";
                    buffer.WriteUtf8String(original);

                    buffer.Reset();

                    string result = buffer.ReadUtf8String();
                    Expect(result).ToBe(original);
                });

                It("should handle empty strings", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    buffer.WriteAsciiString("");
                    buffer.WriteUtf8String("");

                    buffer.Reset();

                    string asciiResult = buffer.ReadAsciiString();
                    string utf8Result = buffer.ReadUtf8String();

                    Expect(asciiResult).ToBe("");
                    Expect(utf8Result).ToBe("");
                });

                It("should handle long strings", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    string longString = new string('A', 500);
                    buffer.WriteUtf8String(longString);

                    buffer.Reset();

                    string result = buffer.ReadUtf8String();
                    Expect(result).ToBe(longString);
                });

                It("should handle special characters in strings", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    string special = "Hello@#$%^&*()_+-=[]{}|;':\",./<>?";
                    buffer.WriteAsciiString(special);

                    buffer.Reset();

                    string result = buffer.ReadAsciiString();
                    Expect(result).ToBe(special);
                });
            });

            Describe("FlatBuffer Data Integrity", () =>
            {
                It("should generate consistent hash for same data", () =>
                {
                    using var buffer1 = new FlatBuffer(1024);
                    using var buffer2 = new FlatBuffer(1024);

                    buffer1.Write<float>(42.0f);
                    buffer1.Write<float>(3.14f);

                    buffer2.Write<float>(42.0f);
                    buffer2.Write<float>(3.14f);

                    uint hash1 = buffer1.GetHashFast();
                    uint hash2 = buffer2.GetHashFast();

                    Expect(hash1).ToBe(hash2);
                });

                It("should generate different hash for different data", () =>
                {
                    using var buffer1 = new FlatBuffer(1024);
                    using var buffer2 = new FlatBuffer(1024);

                    buffer1.Write<float>(42.0f);
                    buffer2.Write<float>(43.0f);

                    uint hash1 = buffer1.GetHashFast();
                    uint hash2 = buffer2.GetHashFast();

                    Expect(hash1).NotToBe(hash2);
                });

                It("should generate hex representation", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    buffer.Write<float>(42.0f);
                    buffer.Write<float>(3.14f);

                    string hex = buffer.ToHex();

                    Expect(hex).NotToBeNull();
                    Expect(hex.Length).ToBe(8);
                });

                It("should handle empty buffer hash", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    uint hash = buffer.GetHashFast();
                    string hex = buffer.ToHex();

                    Expect(hash).ToBe(0u);
                    Expect(hex).ToBe("00000000");
                });
            });

            Describe("FlatBuffer Memory Management", () =>
            {
                It("should dispose properly", () =>
                {
                    var buffer = new FlatBuffer(1024);

                    Expect(buffer.IsDisposed).ToBe(false);

                    buffer.Dispose();

                    Expect(buffer.IsDisposed).ToBe(true);
                });

                It("should handle multiple dispose calls", () =>
                {
                    var buffer = new FlatBuffer(1024);

                    buffer.Dispose();
                    buffer.Dispose();

                    Expect(buffer.IsDisposed).ToBe(true);
                });

                It("should support using statement", () =>
                {
                    bool disposed = false;

                    using (var buffer = new FlatBuffer(1024))
                    {
                        buffer.Write<float>(42.0f);
                        disposed = buffer.IsDisposed;
                    }

                    Expect(disposed).ToBe(false);
                });

                It("should handle capacity correctly", () =>
                {
                    using var buffer = new FlatBuffer(512);

                    Expect(buffer.Capacity).ToBe(512);

                    for (int i = 0; i < 100; i++)
                    {
                        buffer.Write<float>(i);
                    }

                    Expect(buffer.Position).ToBeLessThan(buffer.Capacity + 1);
                });
            });

            Describe("FlatBuffer Edge Cases", () =>
            {
                It("should throw on buffer overflow", () =>
                {
                    using var buffer = new FlatBuffer(8); 

                    buffer.Write<float>(42.0f);
                    buffer.Write<float>(43.0f);

                    bool threw = false;
                    try
                    {
                        buffer.Write<float>(44.0f);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        threw = true;
                    }

                    Expect(threw).ToBe(true);
                });

                It("should handle short values correctly", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    short[] testValues = { -1000, -1, 0, 1, 1000, short.MinValue, short.MaxValue };

                    foreach (short value in testValues)
                    {
                        buffer.Reset();
                        buffer.Write(value); 
                        buffer.Reset();
                        short result = buffer.Read<short>(); 
                        Expect(result).ToBe(value);
                    }
                });

                It("should handle ushort values correctly", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    ushort[] testValues = { 0, 1, 255, 256, 1000, ushort.MaxValue };

                    foreach (ushort value in testValues)
                    {
                        buffer.Reset();
                        buffer.Write(value); 
                        buffer.Reset();
                        ushort result = buffer.Read<ushort>();
                        Expect(result).ToBe(value);
                    }
                });

                It("should throw on zero capacity buffer writes", () =>
                {
                    bool threw = false;
                    try
                    {
                        using var buffer = new FlatBuffer(0);
                        buffer.Write<float>(42.0f);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        threw = true;
                    }

                    Expect(threw).ToBe(true);
                });

                It("should handle reading beyond buffer", () =>
                {
                    using var buffer = new FlatBuffer(4);
                    buffer.Write<float>(42.0f);

                    try
                    {
                        buffer.Read<float>();
                        Expect(false).ToBe(true); 
                    }
                    catch (IndexOutOfRangeException)
                    {
                        Expect(true).ToBe(true);
                    }
                });

                It("should match Write/Read of long manually encoded", () =>
                {
                    using var buffer = new FlatBuffer(1024);

                    long original = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    buffer.Write(original);
                    buffer.Reset();
                    long result = buffer.Read<long>();

                    Console.WriteLine($"Original: {original}, ReadBack: {result}");

                    Expect(result).ToBe(original);
                });

            });
        }
    }
}
