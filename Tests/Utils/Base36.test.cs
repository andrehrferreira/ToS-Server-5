namespace Tests
{
    public class Base36Tests : AbstractTest
    {
        public Base36Tests()
        {
            Describe("Base36 Conversion", () =>
            {
                It("should convert integer to Base36 string", () =>
                {
                    int value = 123456;
                    string base36String = Base36.ToString(value);

                    Expect(base36String).NotToBeNull();
                });

                It("should convert Base36 string back to integer", () =>
                {
                    string base36String = "2N9C";
                    int intValue = Base36.ToInt(base36String);

                    Expect(intValue).ToBe(123456);
                });

                It("should handle conversion of zero", () =>
                {
                    int value = 0;
                    string base36String = Base36.ToString(value);
                    int intValue = Base36.ToInt(base36String);

                    Expect(base36String).ToBe("0");
                    Expect(intValue).ToBe(0);
                });

                It("should throw exception for invalid Base36 string", () =>
                {
                    string invalidBase36 = "InvalidBase36";

                    try
                    {
                        int result = Base36.ToInt(invalidBase36);
                        Expect(false).ToBeTrue();
                    }
                    catch (Exception ex)
                    {
                        Expect(ex).NotToBeNull();
                    }
                });

                It("should convert maximum integer value to Base36 and back", () =>
                {
                    int maxValue = int.MaxValue;
                    string base36String = Base36.ToString(maxValue);
                    int result = Base36.ToInt(base36String);

                    Expect(result).ToBe(maxValue);
                });

                It("should convert minimum integer value (zero) correctly", () =>
                {
                    string base36String = Base36.ToString(0);
                    Expect(base36String).ToBe("0");
                });

                It("should handle lowercase Base36 strings correctly", () =>
                {
                    string base36String = "2n9c"; // Lowercase version of "2N9C"
                    int intValue = Base36.ToInt(base36String);

                    Expect(intValue).ToBe(123456);
                });

                It("should throw exception when converting negative integer", () =>
                {
                    try
                    {
                        Base36.ToString(-1);
                        Expect(false).ToBeTrue(); // Should not reach here
                    }
                    catch (ArgumentException ex)
                    {
                        Expect(ex.Message).ToBe("Value cannot be negative.");
                    }
                });

                It("should throw exception for empty Base36 string", () =>
                {
                    try
                    {
                        Base36.ToInt("");
                        Expect(false).ToBeTrue(); 
                    }
                    catch (ArgumentException ex)
                    {
                        Expect(ex.Message).ToBe("The input string cannot be null or empty.");
                    }
                });

                It("should throw exception for Base36 string with special characters", () =>
                {
                    string specialCharsBase36 = "A#B$";

                    try
                    {
                        Base36.ToInt(specialCharsBase36);
                        Expect(false).ToBeTrue();
                    }
                    catch (ArgumentException ex)
                    {
                        Expect(ex.Message).ToBe("Invalid Base36 value.");
                    }
                });
            });
        }
    }
}