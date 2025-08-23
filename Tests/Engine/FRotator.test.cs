namespace Tests
{
    public class FRotatorTests : AbstractTest
    {
        public FRotatorTests()
        {
            Describe("FRotator Construction", () =>
            {
                It("should create rotator with parameters", () =>
                {
                    var rotator = new FRotator(45.0f, 90.0f, 180.0f);

                    Expect(rotator.Pitch).ToBe(45.0f);
                    Expect(rotator.Yaw).ToBe(90.0f);
                    Expect(rotator.Roll).ToBe(180.0f);
                });

                It("should create zero rotator", () =>
                {
                    var zero = FRotator.Zero;

                    Expect(zero.Pitch).ToBe(0.0f);
                    Expect(zero.Yaw).ToBe(0.0f);
                    Expect(zero.Roll).ToBe(0.0f);
                });

                It("should handle negative angles", () =>
                {
                    var rotator = new FRotator(-45.0f, -90.0f, -135.0f);

                    Expect(rotator.Pitch).ToBe(-45.0f);
                    Expect(rotator.Yaw).ToBe(-90.0f);
                    Expect(rotator.Roll).ToBe(-135.0f);
                });

                It("should handle angles greater than 360", () =>
                {
                    var rotator = new FRotator(450.0f, 720.0f, 900.0f);

                    Expect(rotator.Pitch).ToBe(450.0f);
                    Expect(rotator.Yaw).ToBe(720.0f);
                    Expect(rotator.Roll).ToBe(900.0f);
                });
            });

            Describe("FRotator Normalize Axis", () =>
            {
                It("should normalize positive angle within range", () =>
                {
                    float angle = 45.0f;

                    float normalized = FRotator.NormalizeAxis(angle);

                    Expect(normalized).ToBe(45.0f);
                });

                It("should normalize angle of 180 degrees", () =>
                {
                    float angle = 180.0f;

                    float normalized = FRotator.NormalizeAxis(angle);

                    Expect(normalized).ToBe(180.0f);
                });

                It("should normalize angle greater than 180", () =>
                {
                    float angle = 270.0f;

                    float normalized = FRotator.NormalizeAxis(angle);

                    Expect(normalized).ToBe(-90.0f);
                });

                It("should normalize angle of 360 degrees", () =>
                {
                    float angle = 360.0f;

                    float normalized = FRotator.NormalizeAxis(angle);

                    Expect(normalized).ToBe(0.0f);
                });

                It("should normalize angle greater than 360", () =>
                {
                    float angle = 450.0f;
                    float normalized = FRotator.NormalizeAxis(angle);
                    Expect(normalized).ToBe(90.0f);
                });

                It("should normalize negative angle within range", () =>
                {
                    float angle = -45.0f;
                    float normalized = FRotator.NormalizeAxis(angle);
                    Expect(normalized).ToBe(-45.0f);
                });

                It("should normalize angle of -180 degrees", () =>
                {
                    float angle = -180.0f;
                    float normalized = FRotator.NormalizeAxis(angle);
                    Expect(normalized).ToBe(-180.0f);
                });

                It("should normalize angle less than -180", () =>
                {
                    float angle = -270.0f;
                    float normalized = FRotator.NormalizeAxis(angle);
                    Expect(normalized).ToBe(90.0f);
                });

                It("should normalize large negative angle", () =>
                {
                    float angle = -450.0f;
                    float normalized = FRotator.NormalizeAxis(angle);
                    Expect(normalized).ToBe(-90.0f);
                });

                It("should handle multiple rotations", () =>
                {
                    float angle = 1080.0f;
                    float normalized = FRotator.NormalizeAxis(angle);
                    Expect(normalized).ToBe(0.0f);
                });
            });

            Describe("FRotator Normalize", () =>
            {
                It("should normalize all axes within range", () =>
                {
                    var rotator = new FRotator(45.0f, 90.0f, 135.0f);
                    rotator.Normalize();

                    Expect(rotator.Pitch).ToBe(45.0f);
                    Expect(rotator.Yaw).ToBe(90.0f);
                    Expect(rotator.Roll).ToBe(135.0f);
                });

                It("should normalize angles greater than 180", () =>
                {
                    var rotator = new FRotator(270.0f, 450.0f, 540.0f);
                    rotator.Normalize();

                    Expect(rotator.Pitch).ToBe(-90.0f);
                    Expect(rotator.Yaw).ToBe(90.0f);
                    Expect(rotator.Roll).ToBe(180.0f);
                });

                It("should normalize negative angles", () =>
                {
                    var rotator = new FRotator(-270.0f, -450.0f, -540.0f);
                    rotator.Normalize();

                    Expect(rotator.Pitch).ToBe(90.0f);
                    Expect(rotator.Yaw).ToBe(-90.0f);
                    Expect(rotator.Roll).ToBe(-180.0f);
                });

                It("should normalize zero rotator", () =>
                {
                    var rotator = FRotator.Zero;
                    rotator.Normalize();

                    Expect(rotator.Pitch).ToBe(0.0f);
                    Expect(rotator.Yaw).ToBe(0.0f);
                    Expect(rotator.Roll).ToBe(0.0f);
                });

                It("should normalize mixed positive and negative angles", () =>
                {
                    var rotator = new FRotator(270.0f, -270.0f, 450.0f);
                    rotator.Normalize();

                    Expect(rotator.Pitch).ToBe(-90.0f);
                    Expect(rotator.Yaw).ToBe(90.0f);
                    Expect(rotator.Roll).ToBe(90.0f);
                });
            });

            Describe("FRotator Operators", () =>
            {
                It("should add rotators correctly", () =>
                {
                    var a = new FRotator(30.0f, 45.0f, 60.0f);
                    var b = new FRotator(15.0f, 30.0f, 45.0f);
                    var result = a + b;

                    Expect(result.Pitch).ToBe(45.0f);
                    Expect(result.Yaw).ToBe(75.0f);
                    Expect(result.Roll).ToBe(105.0f);
                });

                It("should subtract rotators correctly", () =>
                {
                    var a = new FRotator(60.0f, 90.0f, 120.0f);
                    var b = new FRotator(30.0f, 45.0f, 60.0f);
                    var result = a - b;

                    Expect(result.Pitch).ToBe(30.0f);
                    Expect(result.Yaw).ToBe(45.0f);
                    Expect(result.Roll).ToBe(60.0f);
                });

                It("should multiply rotator by scalar correctly", () =>
                {
                    var rotator = new FRotator(30.0f, 45.0f, 60.0f);
                    var result = rotator * 2.0f;

                    Expect(result.Pitch).ToBe(60.0f);
                    Expect(result.Yaw).ToBe(90.0f);
                    Expect(result.Roll).ToBe(120.0f);
                });

                It("should divide rotator by scalar correctly", () =>
                {
                    var rotator = new FRotator(90.0f, 180.0f, 270.0f);
                    var result = rotator / 2.0f;

                    Expect(result.Pitch).ToBe(45.0f);
                    Expect(result.Yaw).ToBe(90.0f);
                    Expect(result.Roll).ToBe(135.0f);
                });

                It("should handle negative scalar multiplication", () =>
                {
                    var rotator = new FRotator(45.0f, 90.0f, 135.0f);
                    var result = rotator * -1.0f;

                    Expect(result.Pitch).ToBe(-45.0f);
                    Expect(result.Yaw).ToBe(-90.0f);
                    Expect(result.Roll).ToBe(-135.0f);
                });

                It("should handle fractional scalar operations", () =>
                {
                    var rotator = new FRotator(60.0f, 90.0f, 120.0f);
                    var result = rotator * 0.5f;

                    Expect(result.Pitch).ToBe(30.0f);
                    Expect(result.Yaw).ToBe(45.0f);
                    Expect(result.Roll).ToBe(60.0f);
                });
            });

            Describe("FRotator Equality", () =>
            {
                It("should compare equal rotators correctly", () =>
                {
                    var a = new FRotator(45.0f, 90.0f, 135.0f);
                    var b = new FRotator(45.0f, 90.0f, 135.0f);

                    Expect(a == b).ToBe(true);
                    Expect(a != b).ToBe(false);
                    Expect(a.Equals(b)).ToBe(true);
                });

                It("should compare different rotators correctly", () =>
                {
                    var a = new FRotator(45.0f, 90.0f, 135.0f);
                    var b = new FRotator(45.0f, 90.0f, 136.0f);

                    Expect(a == b).ToBe(false);
                    Expect(a != b).ToBe(true);
                    Expect(a.Equals(b)).ToBe(false);
                });

                It("should handle floating point precision in equality", () =>
                {
                    var a = new FRotator(45.0f, 90.0f, 135.0f);
                    var b = new FRotator(45.0000001f, 90.0f, 135.0f);

                    Expect(a == b).ToBe(true);
                });

                It("should handle equality with zero rotator", () =>
                {
                    var zero = FRotator.Zero;
                    var zero2 = new FRotator(0.0f, 0.0f, 0.0f);

                    Expect(zero == zero2).ToBe(true);
                });

                It("should generate consistent hash codes", () =>
                {
                    var a = new FRotator(45.0f, 90.0f, 135.0f);
                    var b = new FRotator(45.0f, 90.0f, 135.0f);

                    Expect(a.GetHashCode()).ToBe(b.GetHashCode());
                });

                It("should handle equality outside tolerance", () =>
                {
                    var a = new FRotator(45.0f, 90.0f, 135.0f);
                    var b = new FRotator(45.1f, 90.0f, 135.0f);

                    Expect(a == b).ToBe(false);
                });
            });

            Describe("FRotator String Representation", () =>
            {
                It("should convert to string correctly", () =>
                {
                    var rotator = new FRotator(45.5f, 90.25f, 135.75f);

                    string result = rotator.ToString();

                    Expect(result).ToBe("Pitch=45,50 Yaw=90,25 Roll=135,75");
                });

                It("should handle zero rotator string", () =>
                {
                    var rotator = FRotator.Zero;

                    string result = rotator.ToString();

                    Expect(result).ToBe("Pitch=0,00 Yaw=0,00 Roll=0,00");
                });

                It("should handle negative values in string", () =>
                {
                    var rotator = new FRotator(-45.5f, -90.25f, -135.75f);

                    string result = rotator.ToString();

                    Expect(result).ToBe("Pitch=-45,50 Yaw=-90,25 Roll=-135,75");
                });

                It("should handle large angles in string", () =>
                {
                    var rotator = new FRotator(450.0f, 720.0f, 900.0f);

                    string result = rotator.ToString();

                    Expect(result).ToBe("Pitch=450,00 Yaw=720,00 Roll=900,00");
                });
            });

            Describe("FRotator Edge Cases", () =>
            {
                It("should handle boundary angles correctly", () =>
                {
                    var rotator = new FRotator(180.0f, -180.0f, 179.9f);

                    rotator.Normalize();

                    Expect(rotator.Pitch).ToBe(180.0f);
                    Expect(rotator.Yaw).ToBe(-180.0f);
                    Expect(rotator.Roll).ToBe(179.9f);
                });

                It("should handle very small angles", () =>
                {
                    var rotator = new FRotator(0.001f, -0.001f, 0.0001f);

                    rotator.Normalize();

                    Expect(rotator.Pitch).ToBe(0.001f);
                    Expect(rotator.Yaw).ToBe(-0.001f);
                    Expect(rotator.Roll).ToBe(0.0001f);
                });

                It("should handle very large angles", () =>
                {
                    var rotator = new FRotator(3600.0f, -3600.0f, 7200.0f);

                    rotator.Normalize();

                    Expect(rotator.Pitch).ToBe(0.0f);
                    Expect(rotator.Yaw).ToBe(0.0f);
                    Expect(rotator.Roll).ToBe(0.0f);
                });

                It("should handle operations with identity values", () =>
                {
                    var rotator = new FRotator(45.0f, 90.0f, 135.0f);

                    var unchanged = rotator * 1.0f;
                    var doubled = rotator * 2.0f;
                    var halved = rotator / 2.0f;

                    Expect(unchanged == rotator).ToBe(true);
                    Expect(doubled.Pitch).ToBe(90.0f);
                    Expect(halved.Pitch).ToBe(22.5f);
                });

                It("should handle consecutive normalizations", () =>
                {
                    var rotator = new FRotator(450.0f, 810.0f, 1170.0f);

                    rotator.Normalize();
                    var firstNormalize = new FRotator(rotator.Pitch, rotator.Yaw, rotator.Roll);

                    rotator.Normalize();

                    Expect(rotator.Pitch).ToBe(firstNormalize.Pitch);
                    Expect(rotator.Yaw).ToBe(firstNormalize.Yaw);
                    Expect(rotator.Roll).ToBe(firstNormalize.Roll);
                });

                It("should handle arithmetic overflow scenarios", () =>
                {
                    var rotator = new FRotator(float.MaxValue / 1000, float.MaxValue / 1000, 0);

                    // Should not crash
                    rotator.Normalize();

                    // Result should be normalized
                    Expect(rotator.Pitch).ToBeGreaterThan(-180.0f);
                    Expect(rotator.Pitch).ToBeLessThan(180.1f);
                });

                It("should handle mixed operations", () =>
                {
                    var a = new FRotator(270.0f, -270.0f, 450.0f);
                    var b = new FRotator(90.0f, 90.0f, 90.0f);

                    var result = a + b;
                    result.Normalize();

                    Expect(result.Pitch).ToBe(0.0f);  
                    Expect(result.Yaw).ToBe(-180.0f); 
                    Expect(result.Roll).ToBe(180.0f);  
                });
            });
        }
    }
}
