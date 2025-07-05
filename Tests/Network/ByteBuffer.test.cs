
namespace Tests
{
    public class ByteBufferTests : AbstractTest
    {
        public ByteBufferTests()
        {
            Describe("ByteBuffer", () =>
            {
                It("should write and read a byte value", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    buffer.Write((byte)42);

                    buffer = new ByteBuffer(buffer.GetBuffer());
                    byte result = buffer.Read<byte>();

                    Expect(result).ToBe(42);
                });

                It("should write and read an integer value", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    buffer.Write(12345);

                    buffer = new ByteBuffer(buffer.GetBuffer());
                    int result = buffer.Read<int>();

                    Expect(result).ToBe(12345);
                });

                It("should write and read a boolean value", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    buffer.Write(true);
                    buffer.Write(false);

                    buffer = new ByteBuffer(buffer.GetBuffer());
                    bool trueResult = buffer.Read<bool>();
                    bool falseResult = buffer.Read<bool>();

                    Expect(trueResult).ToBeTrue();
                    Expect(falseResult).ToBeFalse();
                });

                It("should write and read a float value", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    buffer.Write(3.14f);

                    buffer = new ByteBuffer(buffer.GetBuffer());
                    float result = buffer.ReadFloat();

                    Expect(result).ToBe(3.14f);
                });

                It("should write and read a string value", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    string text = "Hello, World!";
                    buffer.Write(text);

                    buffer = new ByteBuffer(buffer.GetBuffer());
                    string result = buffer.ReadString();

                    Expect(result).ToBe("Hello, World!");
                });

                It("should handle buffer underflow when reading an int", () =>
                {
                    var buffer = new ByteBuffer();

                    try
                    {
                        buffer.ReadInt();
                    }
                    catch (InvalidOperationException ex)
                    {
                        Expect(ex).NotToBeNull();
                        Expect(ex.Message).ToBe("Buffer underflow");
                    }
                });

                It("should handle buffer underflow when reading a byte", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();

                    try
                    {
                        buffer.ReadByte();
                    }
                    catch (InvalidOperationException ex)
                    {
                        Expect(ex).NotToBeNull();
                    }
                });

                It("should handle buffer underflow when reading a float", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();

                    try
                    {
                        buffer.ReadFloat();
                    }
                    catch (InvalidOperationException ex)
                    {
                        Expect(ex).NotToBeNull();
                    }
                });

                It("should handle buffer underflow when reading a string", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    buffer.Write(5);

                    buffer = new ByteBuffer(buffer.GetBuffer());

                    try
                    {
                        buffer.ReadString();
                    }
                    catch (InvalidOperationException ex)
                    {
                        Expect(ex).NotToBeNull();
                    }
                });

                It("should write and read multiple types sequentially", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    buffer.Write((byte)42);
                    buffer.Write(12345);
                    buffer.Write(3.14f);
                    buffer.Write("Test");

                    buffer = new ByteBuffer(buffer.GetBuffer());

                    byte byteResult = buffer.Read<byte>();
                    int intResult = buffer.Read<int>();
                    float floatResult = buffer.Read<float>();
                    string stringResult = buffer.Read<string>();

                    Expect(byteResult).ToBe(42);
                    Expect(intResult).ToBe(12345);
                    Expect(floatResult).ToBe(3.14f);
                    Expect(stringResult).ToBe("Test");
                });

                It("should correctly handle an empty buffer", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    byte[] result = buffer.GetBuffer();
                    Expect(result.Length).ToBe(4096);
                });

                It("should dispose and release resources properly", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    buffer.Write(42);
                    buffer.Dispose();

                    try
                    {
                        buffer.GetBuffer();
                        Expect(false).ToBeTrue();
                    }
                    catch (ObjectDisposedException ex)
                    {
                        Expect(ex).NotToBeNull();
                    }
                });

                It("should write and read a negative integer value", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    buffer.Write(-9876);

                    var copiedBuffer = new ByteBuffer(buffer);
                    int result = copiedBuffer.Read<int>();

                    Expect(result).ToBe(-9876);

                    copiedBuffer.Dispose();
                    buffer.Dispose();
                });

                It("should throw exception when reading unsupported type", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();

                    try
                    {
                        buffer.Read<double>(); 
                        Expect(false).ToBeTrue(); 
                    }
                    catch (NotSupportedException ex)
                    {
                        Expect(ex).NotToBeNull();
                    }
                });

                It("should handle buffer overflow when writing a large string", () =>
                {
                    try
                    {
                        var longString = new string('A', 5000);
                        var buffer = ByteBuffer.CreateEmptyBuffer();
                        buffer.Write(longString);

                        buffer = new ByteBuffer(buffer.GetBuffer());
                        string result = buffer.ReadString();
                    }
                    catch (InvalidOperationException ex)
                    {
                        Expect(ex).NotToBeNull();
                    }
                });

                It("should handle writing and reading a boolean false value", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    buffer.Write(false);

                    buffer = new ByteBuffer(buffer.GetBuffer());
                    bool result = buffer.Read<bool>();

                    Expect(result).ToBeFalse();
                });

                It("should correctly read written data even after multiple buffer copies", () =>
                {
                    var originalBuffer = ByteBuffer.CreateEmptyBuffer();
                    originalBuffer.Write(123);
                    originalBuffer.Write(456);
                    originalBuffer.Write("Test string");

                    var copiedBuffer = new ByteBuffer(originalBuffer);
                    copiedBuffer.ResetOffset();

                    int firstValue = copiedBuffer.Read<int>();
                    int secondValue = copiedBuffer.Read<int>();
                    string stringValue = copiedBuffer.ReadString();

                    Expect(firstValue).ToBe(123);
                    Expect(secondValue).ToBe(456);
                    Expect(stringValue).ToBe("Test string");
                });

                It("should write and read a FVector value", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    var vector = new FVector(10, 20, 30);
                    buffer.Write(vector);

                    buffer = new ByteBuffer(buffer.GetBuffer());
                    var result = buffer.Read<FVector>();

                    Expect(result.X).ToBe(10f);
                    Expect(result.Y).ToBe(20f);
                    Expect(result.Z).ToBe(30f);
                });

                It("should handle buffer underflow when reading a FVector", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    buffer.Write(10);

                    buffer = new ByteBuffer(buffer.GetBuffer());

                    try
                    {
                        buffer.Read<FVector>();
                    }
                    catch (InvalidOperationException ex)
                    {
                        Expect(ex).NotToBeNull();
                        Expect(ex.Message).ToBe("Buffer underflow");
                    }
                });

                It("should write and read a FVector with negative values", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    var vector = new FVector(-10, -20, -30);
                    buffer.Write(vector);

                    buffer = new ByteBuffer(buffer.GetBuffer());
                    var result = buffer.Read<FVector>();

                    Expect(result.X).ToBe(-10f);
                    Expect(result.Y).ToBe(-20f);
                    Expect(result.Z).ToBe(-30f);
                });

                It("should write and read multiple Vector3 values sequentially", () =>
                {
                    var buffer = ByteBuffer.CreateEmptyBuffer();
                    var vector1 = new FVector(1, 2, 3);
                    var vector2 = new FVector(4, 5, 6);
                    buffer.Write(vector1);
                    buffer.Write(vector2);

                    buffer = new ByteBuffer(buffer.GetBuffer());
                    var result1 = buffer.Read<FVector>();
                    var result2 = buffer.Read<FVector>();

                    Expect(result1.X).ToBe(1);
                    Expect(result1.Y).ToBe(2);
                    Expect(result1.Z).ToBe(3);

                    Expect(result2.X).ToBe(4);
                    Expect(result2.Y).ToBe(5);
                    Expect(result2.Z).ToBe(6);
                });
            });
        }
    }
}