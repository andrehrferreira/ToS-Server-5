using System;
using System.Text;
using FsCheck;
using SharpFuzz;
using System.Linq;

namespace Tests
{
    public class FlatBufferPropertyTests : AbstractTest
    {
        public FlatBufferPropertyTests()
        {
            Describe("FlatBuffer Property-Based Tests", () =>
            {
                It("should roundtrip random byte arrays", () =>
                {
                    Prop.ForAll<byte[]>(data =>
                    {
                        data ??= Array.Empty<byte>();
                        using var buffer = new FlatBuffer(data.Length + sizeof(int));
                        buffer.Write(data.Length);
                        buffer.WriteBytes(data);
                        buffer.RestorePosition(0);
                        int len = buffer.Read<int>();
                        var read = buffer.ReadBytes(len);
                        return data.SequenceEqual(read);
                    }).QuickCheckThrowOnFailure();
                });

                It("should roundtrip random UTF8 strings", () =>
                {
                    var gen = Arb.Generate<string>().Where(s => s == null || s.Length < 100);
                    Prop.ForAll(gen, text =>
                    {
                        text ??= string.Empty;
                        var bytes = Encoding.UTF8.GetByteCount(text);
                        using var buffer = new FlatBuffer(bytes + sizeof(int));
                        buffer.WriteUtf8String(text);
                        buffer.RestorePosition(0);
                        var read = buffer.ReadUtf8String();
                        return text == read;
                    }).QuickCheckThrowOnFailure();
                });

                It("should not crash on fuzzed inputs", () =>
                {
                    Fuzzer.Run(data =>
                    {
                        using var buffer = new FlatBuffer(data.Length);
                        try
                        {
                            buffer.WriteBytes(data.ToArray());
                            buffer.RestorePosition(0);
                            buffer.ReadUtf8String();
                        }
                        catch
                        {
                            // Exceptions are expected for malformed input but should not crash
                        }
                    });
                });

                It("should throw on corrupted length prefixes", () =>
                {
                    using var buffer = new FlatBuffer(4);
                    buffer.Write<int>(1000);
                    buffer.RestorePosition(0);
                    bool threw = false;
                    try
                    {
                        buffer.ReadUtf8String();
                    }
                    catch (IndexOutOfRangeException)
                    {
                        threw = true;
                    }
                    Expect(threw).ToBe(true);

                    buffer.Reset();
                    buffer.Write<int>(-1);
                    buffer.RestorePosition(0);
                    threw = false;
                    try
                    {
                        buffer.ReadUtf8String();
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        threw = true;
                    }
                    Expect(threw).ToBe(true);
                });
            });
        }
    }
}
