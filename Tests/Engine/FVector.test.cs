namespace Tests
{
    public class FVector3Tests : AbstractTest
    {
        public FVector3Tests()
        {
            Describe("FVector3 Construction", () =>
            {
                It("should create vector with default constructor", () =>
                {
                    var vector = new FVector3();

                    Expect(vector.X).ToBe(0.0f);
                    Expect(vector.Y).ToBe(0.0f);
                    Expect(vector.Z).ToBe(0.0f);
                });

                It("should create vector with parameters", () =>
                {
                    var vector = new FVector3(1.0f, 2.0f, 3.0f);

                    Expect(vector.X).ToBe(1.0f);
                    Expect(vector.Y).ToBe(2.0f);
                    Expect(vector.Z).ToBe(3.0f);
                });

                It("should create zero vector", () =>
                {
                    var zero = FVector3.Zero;

                    Expect(zero.X).ToBe(0.0f);
                    Expect(zero.Y).ToBe(0.0f);
                    Expect(zero.Z).ToBe(0.0f);
                });

                It("should handle negative values", () =>
                {
                    var vector = new FVector3(-1.5f, -2.5f, -3.5f);

                    Expect(vector.X).ToBe(-1.5f);
                    Expect(vector.Y).ToBe(-2.5f);
                    Expect(vector.Z).ToBe(-3.5f);
                });
            });

            Describe("FVector3 Size Operations", () =>
            {
                It("should calculate size correctly", () =>
                {
                    var vector = new FVector3(3.0f, 4.0f, 0.0f);

                    float size = vector.Size();

                    Expect(size).ToBe(5.0f);
                });

                It("should calculate size for 3D vector", () =>
                {
                    var vector = new FVector3(1.0f, 2.0f, 2.0f);
                    float size = vector.Size();
                    float expected = MathF.Sqrt(1 + 4 + 4);

                    Expect(size).ToBe(expected);
                });

                It("should calculate size squared correctly", () =>
                {
                    var vector = new FVector3(3.0f, 4.0f, 0.0f);
                    float sizeSquared = vector.SizeSquared();

                    Expect(sizeSquared).ToBe(25.0f);
                });

                It("should calculate size squared for 3D vector", () =>
                {
                    var vector = new FVector3(2.0f, 3.0f, 6.0f);
                    float sizeSquared = vector.SizeSquared();

                    Expect(sizeSquared).ToBe(49.0f);
                });

                It("should handle zero vector size", () =>
                {
                    var vector = FVector3.Zero;

                    Expect(vector.Size()).ToBe(0.0f);
                    Expect(vector.SizeSquared()).ToBe(0.0f);
                });
            });

            Describe("FVector3 Normalization", () =>
            {
                It("should normalize vector correctly", () =>
                {
                    var vector = new FVector3(3.0f, 4.0f, 0.0f);

                    vector.Normalize();

                    Expect(vector.X).ToBe(0.6f);
                    Expect(vector.Y).ToBe(0.8f);
                    Expect(vector.Z).ToBe(0.0f);

                    float size = vector.Size();
                    Expect(MathF.Abs(size - 1.0f)).ToBeLessThan(1e-6f);
                });

                It("should normalize 3D vector correctly", () =>
                {
                    var vector = new FVector3(1.0f, 2.0f, 2.0f);
                    vector.Normalize();

                    float size = vector.Size();
                    Expect(MathF.Abs(size - 1.0f)).ToBeLessThan(1e-6f);
                });

                It("should handle zero vector normalization", () =>
                {
                    var vector = FVector3.Zero;

                    vector.Normalize();

                    Expect(vector.X).ToBe(0.0f);
                    Expect(vector.Y).ToBe(0.0f);
                    Expect(vector.Z).ToBe(0.0f);
                });

                It("should handle very small vector normalization", () =>
                {
                    var vector = new FVector3(1e-7f, 1e-7f, 1e-7f);

                    vector.Normalize();

                    Expect(vector.Size()).ToBeLessThan(1e-6f);
                });
            });

            Describe("FVector3 Dot Product", () =>
            {
                It("should calculate dot product correctly", () =>
                {
                    var a = new FVector3(1.0f, 2.0f, 3.0f);
                    var b = new FVector3(4.0f, 5.0f, 6.0f);
                    float dot = FVector3.Dot(a, b);

                    Expect(dot).ToBe(32.0f);
                });

                It("should handle perpendicular vectors", () =>
                {
                    var a = new FVector3(1.0f, 0.0f, 0.0f);
                    var b = new FVector3(0.0f, 1.0f, 0.0f);
                    float dot = FVector3.Dot(a, b);

                    Expect(dot).ToBe(0.0f);
                });

                It("should handle parallel vectors", () =>
                {
                    var a = new FVector3(2.0f, 4.0f, 6.0f);
                    var b = new FVector3(1.0f, 2.0f, 3.0f);
                    float dot = FVector3.Dot(a, b);

                    Expect(dot).ToBe(28.0f);
                });

                It("should handle opposite vectors", () =>
                {
                    var a = new FVector3(1.0f, 2.0f, 3.0f);
                    var b = new FVector3(-1.0f, -2.0f, -3.0f);
                    float dot = FVector3.Dot(a, b);

                    Expect(dot).ToBe(-14.0f);
                });

                It("should handle zero vector dot product", () =>
                {
                    var a = new FVector3(1.0f, 2.0f, 3.0f);
                    var b = FVector3.Zero;
                    float dot = FVector3.Dot(a, b);

                    Expect(dot).ToBe(0.0f);
                });
            });

            Describe("FVector3 Cross Product", () =>
            {
                It("should calculate cross product correctly", () =>
                {
                    var a = new FVector3(1.0f, 0.0f, 0.0f);
                    var b = new FVector3(0.0f, 1.0f, 0.0f);
                    var cross = FVector3.Cross(a, b);

                    Expect(cross.X).ToBe(0.0f);
                    Expect(cross.Y).ToBe(0.0f);
                    Expect(cross.Z).ToBe(1.0f);
                });

                It("should handle reverse cross product", () =>
                {
                    var a = new FVector3(0.0f, 1.0f, 0.0f);
                    var b = new FVector3(1.0f, 0.0f, 0.0f);
                    var cross = FVector3.Cross(a, b);

                    Expect(cross.X).ToBe(0.0f);
                    Expect(cross.Y).ToBe(0.0f);
                    Expect(cross.Z).ToBe(-1.0f);
                });

                It("should calculate complex cross product", () =>
                {
                    var a = new FVector3(2.0f, 3.0f, 4.0f);
                    var b = new FVector3(5.0f, 6.0f, 7.0f);

                    var cross = FVector3.Cross(a, b);

                    Expect(cross.X).ToBe(-3.0f);
                    Expect(cross.Y).ToBe(6.0f);
                    Expect(cross.Z).ToBe(-3.0f);
                });

                It("should handle parallel vectors cross product", () =>
                {
                    var a = new FVector3(1.0f, 2.0f, 3.0f);
                    var b = new FVector3(2.0f, 4.0f, 6.0f);
                    var cross = FVector3.Cross(a, b);

                    Expect(cross.X).ToBe(0.0f);
                    Expect(cross.Y).ToBe(0.0f);
                    Expect(cross.Z).ToBe(0.0f);
                });

                It("should handle zero vector cross product", () =>
                {
                    var a = new FVector3(1.0f, 2.0f, 3.0f);
                    var b = FVector3.Zero;
                    var cross = FVector3.Cross(a, b);

                    Expect(cross.X).ToBe(0.0f);
                    Expect(cross.Y).ToBe(0.0f);
                    Expect(cross.Z).ToBe(0.0f);
                });
            });

            Describe("FVector3 Operators", () =>
            {
                It("should add vectors correctly", () =>
                {
                    var a = new FVector3(1.0f, 2.0f, 3.0f);
                    var b = new FVector3(4.0f, 5.0f, 6.0f);
                    var result = a + b;

                    Expect(result.X).ToBe(5.0f);
                    Expect(result.Y).ToBe(7.0f);
                    Expect(result.Z).ToBe(9.0f);
                });

                It("should subtract vectors correctly", () =>
                {
                    var a = new FVector3(5.0f, 7.0f, 9.0f);
                    var b = new FVector3(1.0f, 2.0f, 3.0f);
                    var result = a - b;

                    Expect(result.X).ToBe(4.0f);
                    Expect(result.Y).ToBe(5.0f);
                    Expect(result.Z).ToBe(6.0f);
                });

                It("should multiply vector by scalar correctly", () =>
                {
                    var vector = new FVector3(2.0f, 3.0f, 4.0f);
                    var result = vector * 2.5f;

                    Expect(result.X).ToBe(5.0f);
                    Expect(result.Y).ToBe(7.5f);
                    Expect(result.Z).ToBe(10.0f);
                });

                It("should divide vector by scalar correctly", () =>
                {
                    var vector = new FVector3(10.0f, 15.0f, 20.0f);
                    var result = vector / 5.0f;

                    Expect(result.X).ToBe(2.0f);
                    Expect(result.Y).ToBe(3.0f);
                    Expect(result.Z).ToBe(4.0f);
                });

                It("should handle negative scalar multiplication", () =>
                {
                    var vector = new FVector3(1.0f, 2.0f, 3.0f);
                    var result = vector * -2.0f;

                    Expect(result.X).ToBe(-2.0f);
                    Expect(result.Y).ToBe(-4.0f);
                    Expect(result.Z).ToBe(-6.0f);
                });
            });

            Describe("FVector3 Equality", () =>
            {
                It("should compare equal vectors correctly", () =>
                {
                    var a = new FVector3(1.0f, 2.0f, 3.0f);
                    var b = new FVector3(1.0f, 2.0f, 3.0f);

                    Expect(a == b).ToBe(true);
                    Expect(a != b).ToBe(false);
                    Expect(a.Equals(b)).ToBe(true);
                });

                It("should compare different vectors correctly", () =>
                {
                    var a = new FVector3(1.0f, 2.0f, 3.0f);
                    var b = new FVector3(1.0f, 2.0f, 4.0f);

                    Expect(a == b).ToBe(false);
                    Expect(a != b).ToBe(true);
                    Expect(a.Equals(b)).ToBe(false);
                });

                It("should handle floating point precision in equality", () =>
                {
                    var a = new FVector3(1.0f, 2.0f, 3.0f);
                    var b = new FVector3(1.0000001f, 2.0f, 3.0f);

                    Expect(a == b).ToBe(true);
                });

                It("should handle equality with zero vector", () =>
                {
                    var zero = FVector3.Zero;
                    var zero2 = new FVector3(0.0f, 0.0f, 0.0f);

                    Expect(zero == zero2).ToBe(true);
                });

                It("should generate consistent hash codes", () =>
                {
                    var a = new FVector3(1.0f, 2.0f, 3.0f);
                    var b = new FVector3(1.0f, 2.0f, 3.0f);

                    Expect(a.GetHashCode()).ToBe(b.GetHashCode());
                });
            });

            Describe("FVector3 String Representation", () =>
            {
                It("should convert to string correctly", () =>
                {
                    var vector = new FVector3(1.5f, 2.75f, 3.25f);

                    string result = vector.ToString();

                    Expect(result).ToBe("X=1,50 Y=2,75 Z=3,25");
                });

                It("should handle zero vector string", () =>
                {
                    var vector = FVector3.Zero;

                    string result = vector.ToString();

                    Expect(result).ToBe("X=0,00 Y=0,00 Z=0,00");
                });

                It("should handle negative values in string", () =>
                {
                    var vector = new FVector3(-1.5f, -2.25f, -3.75f);

                    string result = vector.ToString();

                    Expect(result).ToBe("X=-1,50 Y=-2,25 Z=-3,75");
                });
            });

            Describe("FVector3 Edge Cases", () =>
            {
                It("should handle very large values", () =>
                {
                    var vector = new FVector3(float.MaxValue / 2, float.MaxValue / 2, 0);
                    float size = vector.Size();

                    Expect(size).ToBeGreaterThan(0.0f);
                });

                It("should handle very small values", () =>
                {
                    var vector = new FVector3(1e-10f, 1e-10f, 1e-10f);
                    float size = vector.Size();

                    Expect(size).ToBeGreaterThan(0.0f);
                });

                It("should handle mixed positive and negative values", () =>
                {
                    var vector = new FVector3(-1.0f, 2.0f, -3.0f);
                    float sizeSquared = vector.SizeSquared();

                    Expect(sizeSquared).ToBe(14.0f);
                });

                It("should handle operations with identity values", () =>
                {
                    var vector = new FVector3(5.0f, 10.0f, 15.0f);
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
