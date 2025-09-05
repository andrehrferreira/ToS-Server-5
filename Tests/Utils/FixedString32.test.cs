using System.Text;
using Wormhole;

namespace Tests
{
    public class FixedString32Tests : AbstractTest
    {
        public FixedString32Tests()
        {
            Describe("FixedString32 Operations", () =>
            {
                It("should set and get simple string", () =>
                {
                    var fixedString = new FixedString32();
                    fixedString.Set("hello");

                    string result = fixedString.ToString();

                    Expect(result).ToBe("hello");
                });

                It("should handle empty string", () =>
                {
                    var fixedString = new FixedString32();
                    fixedString.Set("");

                    string result = fixedString.ToString();

                    Expect(result).ToBe("");
                });

                It("should handle null string gracefully", () =>
                {
                    var fixedString = new FixedString32();

                    try
                    {
                        fixedString.Set(null);
                        string result = fixedString.ToString();
                        Expect(result).ToBe("");
                    }
                    catch (Exception ex)
                    {
                        Expect(ex).NotToBeNull();
                    }
                });

                It("should truncate long strings to fit buffer", () =>
                {
                    var fixedString = new FixedString32();
                    string longString = "This is a very long string that exceeds 31 characters and should be truncated";
                    fixedString.Set(longString);

                    string result = fixedString.ToString();

                    Expect(result.Length).ToBe(31);
                    Expect(result).ToBe(longString.Substring(0, 31));
                });

                It("should handle string with exactly 31 characters", () =>
                {
                    var fixedString = new FixedString32();
                    string exactString = "1234567890123456789012345678901";
                    fixedString.Set(exactString);

                    string result = fixedString.ToString();

                    Expect(result).ToBe(exactString);
                    Expect(result.Length).ToBe(31);
                });

                It("should handle UTF-8 characters", () =>
                {
                    var fixedString = new FixedString32();
                    string utf8String = "Hello, 世界!";
                    fixedString.Set(utf8String);
                    string result = fixedString.ToString();
                    Expect(result).ToBe(utf8String);
                });

                It("should handle special characters", () =>
                {
                    var fixedString = new FixedString32();
                    string specialString = "Hello@#$%^&*()";
                    fixedString.Set(specialString);
                    string result = fixedString.ToString();
                    Expect(result).ToBe(specialString);
                });

                It("should allow overwriting existing string", () =>
                {
                    var fixedString = new FixedString32();
                    fixedString.Set("first");
                    fixedString.Set("second");

                    string result = fixedString.ToString();

                    Expect(result).ToBe("second");
                });

                It("should handle shorter string after longer string", () =>
                {
                    var fixedString = new FixedString32();
                    fixedString.Set("very long string");
                    fixedString.Set("short");

                    string result = fixedString.ToString();

                    Expect(result).ToBe("short");
                });

                It("should maintain null terminator", () =>
                {
                    var fixedString = new FixedString32();
                    fixedString.Set("test");

                    unsafe
                    {
                        Expect(fixedString.Buffer[4]).ToBe((byte)0);
                    }
                });

                It("should handle multiple consecutive sets", () =>
                {
                    var fixedString = new FixedString32();

                    fixedString.Set("first");
                    Expect(fixedString.ToString()).ToBe("first");

                    fixedString.Set("second");
                    Expect(fixedString.ToString()).ToBe("second");

                    fixedString.Set("third");
                    Expect(fixedString.ToString()).ToBe("third");
                });

                It("should handle strings with numbers", () =>
                {
                    var fixedString = new FixedString32();
                    string numberString = "Player123";
                    fixedString.Set(numberString);

                    string result = fixedString.ToString();

                    Expect(result).ToBe(numberString);
                });

                It("should be consistent across multiple operations", () =>
                {
                    var fixedString = new FixedString32();
                    string testString = "consistency test";

                    fixedString.Set(testString);
                    string result1 = fixedString.ToString();
                    string result2 = fixedString.ToString();

                    Expect(result1).ToBe(result2);
                    Expect(result1).ToBe(testString);
                });

                It("should handle whitespace and newlines", () =>
                {
                    var fixedString = new FixedString32();
                    string whitespaceString = "hello world\ntest";
                    fixedString.Set(whitespaceString);

                    string result = fixedString.ToString();

                    Expect(result).ToBe(whitespaceString);
                });

                It("should have correct struct size", () =>
                {
                    unsafe
                    {
                        int size = sizeof(FixedString32);
                        Expect(size).ToBe(32);
                    }
                });
            });
        }
    }
}
