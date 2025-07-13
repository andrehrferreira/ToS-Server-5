namespace Tests
{
    public class FVectorTests : AbstractTest
    {
        public FVectorTests()
        {
            Describe("FVector Construction", () =>
            {
                It("should create vector with default constructor", () =>
                {
                    var vector = new FVector();

                    Expect(vector.X).ToBe(0.0f);
                    Expect(vector.Y).ToBe(0.0f);
                    Expect(vector.Z).ToBe(0.0f);
                });

                It("should create vector with parameters", () =>
                {
                    var vector = new FVector(1.0f, 2.0f, 3.0f);

                    Expect(vector.X).ToBe(1.0f);
                    Expect(vector.Y).ToBe(2.0f);
                    Expect(vector.Z).ToBe(3.0f);
                });

                It("should create zero vector", () =>
                {
                    var zero = FVector.Zero;

                    Expect(zero.X).ToBe(0.0f);
                    Expect(zero.Y).ToBe(0.0f);
                    Expect(zero.Z).ToBe(0.0f);
                });

                It("should handle negative values", () =>
                {
                    var vector = new FVector(-1.5f, -2.5f, -3.5f);

                    Expect(vector.X).ToBe(-1.5f);
                    Expect(vector.Y).ToBe(-2.5f);
                    Expect(vector.Z).ToBe(-3.5f);
                });
            });

            Describe("FVector Size Operations", () =>
            {
                It("should calculate size correctly", () =>
                {
                    var vector = new FVector(3.0f, 4.0f, 0.0f);

                    float size = vector.Size();

                    Expect(size).ToBe(5.0f);
                });

                It("should calculate size for 3D vector", () =>
                {
                    var vector = new FVector(1.0f, 2.0f, 2.0f);

                    float size = vector.Size();
                    float expected = MathF.Sqrt(1 + 4 + 4); // sqrt(9) = 3

                    Expect(size).ToBe(expected);
                });

                It("should calculate size squared correctly", () =>
                {
                    var vector = new FVector(3.0f, 4.0f, 0.0f);

                    float sizeSquared = vector.SizeSquared();

                    Expect(sizeSquared).ToBe(25.0f);
                });

                It("should calculate size squared for 3D vector", () =>
                {
                    var vector = new FVector(2.0f, 3.0f, 6.0f);

                    float sizeSquared = vector.SizeSquared();

                    Expect(sizeSquared).ToBe(49.0f); // 4 + 9 + 36
                });

                It("should handle zero vector size", () =>
                {
                    var vector = FVector.Zero;

                    Expect(vector.Size()).ToBe(0.0f);
                    Expect(vector.SizeSquared()).ToBe(0.0f);
                });
            });

            Describe("FVector Normalization", () =>
            {
                It("should normalize vector correctly", () =>
                {
                    var vector = new FVector(3.0f, 4.0f, 0.0f);

                    vector.Normalize();

                    Expect(vector.X).ToBe(0.6f);
                    Expect(vector.Y).ToBe(0.8f);
                    Expect(vector.Z).ToBe(0.0f);

                    // Normalized vector should have size 1
                    float size = vector.Size();
                    Expect(MathF.Abs(size - 1.0f)).ToBeLessThan(1e-6f);
                });

                It("should normalize 3D vector correctly", () =>
                {
                    var vector = new FVector(1.0f, 2.0f, 2.0f);

                    vector.Normalize();

                    float size = vector.Size();
                    Expect(MathF.Abs(size - 1.0f)).ToBeLessThan(1e-6f);
                });

                It("should handle zero vector normalization", () =>
                {
                    var vector = FVector.Zero;

                    vector.Normalize(); // Should not crash

                    Expect(vector.X).ToBe(0.0f);
                    Expect(vector.Y).ToBe(0.0f);
                    Expect(vector.Z).ToBe(0.0f);
                });

                It("should handle very small vector normalization", () =>
                {
                    var vector = new FVector(1e-7f, 1e-7f, 1e-7f);

                    vector.Normalize(); // Should not crash

                    // Should remain as small vector
                    Expect(vector.Size()).ToBeLessThan(1e-6f);
                });
            });

            Describe("FVector Dot Product", () =>
            {
                It("should calculate dot product correctly", () =>
                {
                    var a = new FVector(1.0f, 2.0f, 3.0f);
                    var b = new FVector(4.0f, 5.0f, 6.0f);

                    float dot = FVector.Dot(a, b);

                    Expect(dot).ToBe(32.0f); // 1*4 + 2*5 + 3*6 = 4 + 10 + 18
                });

                It("should handle perpendicular vectors", () =>
                {
                    var a = new FVector(1.0f, 0.0f, 0.0f);
                    var b = new FVector(0.0f, 1.0f, 0.0f);

                    float dot = FVector.Dot(a, b);

                    Expect(dot).ToBe(0.0f);
                });

                It("should handle parallel vectors", () =>
                {
                    var a = new FVector(2.0f, 4.0f, 6.0f);
                    var b = new FVector(1.0f, 2.0f, 3.0f);

                    float dot = FVector.Dot(a, b);

                    Expect(dot).ToBe(28.0f); // 2*1 + 4*2 + 6*3 = 2 + 8 + 18
                });

                It("should handle opposite vectors", () =>
                {
                    var a = new FVector(1.0f, 2.0f, 3.0f);
                    var b = new FVector(-1.0f, -2.0f, -3.0f);

                    float dot = FVector.Dot(a, b);

                    Expect(dot).ToBe(-14.0f); // -1 + -4 + -9
                });

                It("should handle zero vector dot product", () =>
                {
                    var a = new FVector(1.0f, 2.0f, 3.0f);
                    var b = FVector.Zero;

                    float dot = FVector.Dot(a, b);

                    Expect(dot).ToBe(0.0f);
                });
            });

            Describe("FVector Cross Product", () =>
            {
                It("should calculate cross product correctly", () =>
                {
                    var a = new FVector(1.0f, 0.0f, 0.0f);
                    var b = new FVector(0.0f, 1.0f, 0.0f);

                    var cross = FVector.Cross(a, b);

                    Expect(cross.X).ToBe(0.0f);
                    Expect(cross.Y).ToBe(0.0f);
                    Expect(cross.Z).ToBe(1.0f);
                });

                It("should handle reverse cross product", () =>
                {
                    var a = new FVector(0.0f, 1.0f, 0.0f);
                    var b = new FVector(1.0f, 0.0f, 0.0f);

                    var cross = FVector.Cross(a, b);

                    Expect(cross.X).ToBe(0.0f);
                    Expect(cross.Y).ToBe(0.0f);
                    Expect(cross.Z).ToBe(-1.0f);
                });

                It("should calculate complex cross product", () =>
                {
                    var a = new FVector(2.0f, 3.0f, 4.0f);
                    var b = new FVector(5.0f, 6.0f, 7.0f);

                    var cross = FVector.Cross(a, b);

                    // Cross product formula: (a.Y*b.Z - a.Z*b.Y, a.Z*b.X - a.X*b.Z, a.X*b.Y - a.Y*b.X)
                    Expect(cross.X).ToBe(-3.0f);  // 3*7 - 4*6 = 21 - 24 = -3
                    Expect(cross.Y).ToBe(6.0f);   // 4*5 - 2*7 = 20 - 14 = 6
                    Expect(cross.Z).ToBe(-3.0f);  // 2*6 - 3*5 = 12 - 15 = -3
                });

                It("should handle parallel vectors cross product", () =>
                {
                    var a = new FVector(1.0f, 2.0f, 3.0f);
                    var b = new FVector(2.0f, 4.0f, 6.0f);

                    var cross = FVector.Cross(a, b);

                    Expect(cross.X).ToBe(0.0f);
                    Expect(cross.Y).ToBe(0.0f);
                    Expect(cross.Z).ToBe(0.0f);
                });

                It("should handle zero vector cross product", () =>
                {
                    var a = new FVector(1.0f, 2.0f, 3.0f);
                    var b = FVector.Zero;

                    var cross = FVector.Cross(a, b);

                    Expect(cross.X).ToBe(0.0f);
                    Expect(cross.Y).ToBe(0.0f);
                    Expect(cross.Z).ToBe(0.0f);
                });
            });

            Describe("FVector Operators", () =>
            {
                It("should add vectors correctly", () =>
                {
                    var a = new FVector(1.0f, 2.0f, 3.0f);
                    var b = new FVector(4.0f, 5.0f, 6.0f);

                    var result = a + b;

                    Expect(result.X).ToBe(5.0f);
                    Expect(result.Y).ToBe(7.0f);
                    Expect(result.Z).ToBe(9.0f);
                });

                It("should subtract vectors correctly", () =>
                {
                    var a = new FVector(5.0f, 7.0f, 9.0f);
                    var b = new FVector(1.0f, 2.0f, 3.0f);

                    var result = a - b;

                    Expect(result.X).ToBe(4.0f);
                    Expect(result.Y).ToBe(5.0f);
                    Expect(result.Z).ToBe(6.0f);
                });

                It("should multiply vector by scalar correctly", () =>
                {
                    var vector = new FVector(2.0f, 3.0f, 4.0f);

                    var result = vector * 2.5f;

                    Expect(result.X).ToBe(5.0f);
                    Expect(result.Y).ToBe(7.5f);
                    Expect(result.Z).ToBe(10.0f);
                });

                It("should divide vector by scalar correctly", () =>
                {
                    var vector = new FVector(10.0f, 15.0f, 20.0f);

                    var result = vector / 5.0f;

                    Expect(result.X).ToBe(2.0f);
                    Expect(result.Y).ToBe(3.0f);
                    Expect(result.Z).ToBe(4.0f);
                });

                It("should handle negative scalar multiplication", () =>
                {
                    var vector = new FVector(1.0f, 2.0f, 3.0f);

                    var result = vector * -2.0f;

                    Expect(result.X).ToBe(-2.0f);
                    Expect(result.Y).ToBe(-4.0f);
                    Expect(result.Z).ToBe(-6.0f);
                });
            });

            Describe("FVector Equality", () =>
            {
                It("should compare equal vectors correctly", () =>
                {
                    var a = new FVector(1.0f, 2.0f, 3.0f);
                    var b = new FVector(1.0f, 2.0f, 3.0f);

                    Expect(a == b).ToBe(true);
                    Expect(a != b).ToBe(false);
                    Expect(a.Equals(b)).ToBe(true);
                });

                It("should compare different vectors correctly", () =>
                {
                    var a = new FVector(1.0f, 2.0f, 3.0f);
                    var b = new FVector(1.0f, 2.0f, 4.0f);

                    Expect(a == b).ToBe(false);
                    Expect(a != b).ToBe(true);
                    Expect(a.Equals(b)).ToBe(false);
                });

                It("should handle floating point precision in equality", () =>
                {
                    var a = new FVector(1.0f, 2.0f, 3.0f);
                    var b = new FVector(1.0000001f, 2.0f, 3.0f);

                    Expect(a == b).ToBe(true); // Within tolerance
                });

                It("should handle equality with zero vector", () =>
                {
                    var zero = FVector.Zero;
                    var zero2 = new FVector(0.0f, 0.0f, 0.0f);

                    Expect(zero == zero2).ToBe(true);
                });

                It("should generate consistent hash codes", () =>
                {
                    var a = new FVector(1.0f, 2.0f, 3.0f);
                    var b = new FVector(1.0f, 2.0f, 3.0f);

                    Expect(a.GetHashCode()).ToBe(b.GetHashCode());
                });
            });

            Describe("FVector String Representation", () =>
            {
                It("should convert to string correctly", () =>
                {
                    var vector = new FVector(1.5f, 2.75f, 3.25f);

                    string result = vector.ToString();

                    Expect(result).ToBe("X=1.50 Y=2.75 Z=3.25");
                });

                It("should handle zero vector string", () =>
                {
                    var vector = FVector.Zero;

                    string result = vector.ToString();

                    Expect(result).ToBe("X=0.00 Y=0.00 Z=0.00");
                });

                It("should handle negative values in string", () =>
                {
                    var vector = new FVector(-1.5f, -2.25f, -3.75f);

                    string result = vector.ToString();

                    Expect(result).ToBe("X=-1.50 Y=-2.25 Z=-3.75");
                });
            });

            Describe("FVector Edge Cases", () =>
            {
                It("should handle very large values", () =>
                {
                    var vector = new FVector(float.MaxValue / 2, float.MaxValue / 2, 0);

                    // Should not crash
                    float size = vector.Size();

                    Expect(size).ToBeGreaterThan(0.0f);
                });

                It("should handle very small values", () =>
                {
                    var vector = new FVector(1e-10f, 1e-10f, 1e-10f);

                    float size = vector.Size();

                    Expect(size).ToBeGreaterThan(0.0f);
                });

                It("should handle mixed positive and negative values", () =>
                {
                    var vector = new FVector(-1.0f, 2.0f, -3.0f);

                    float sizeSquared = vector.SizeSquared();

                    Expect(sizeSquared).ToBe(14.0f); // 1 + 4 + 9
                });

                It("should handle operations with identity values", () =>
                {
                    var vector = new FVector(5.0f, 10.0f, 15.0f);

                    var unchanged = vector * 1.0f;
                    var doubled = vector * 2.0f;
                    var halved = vector / 2.0f;

                    Expect(unchanged == vector).ToBe(true);
                    Expect(doubled.X).ToBe(10.0f);
                    Expect(halved.X).ToBe(2.5f);
                });
            });
        }
    }
}
