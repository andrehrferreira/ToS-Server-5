namespace Tests
{
    public class FVectorTests : AbstractTest
    {
        public FVectorTests()
        {
            Describe("FVector", () =>
            {
                It("should compute size and squared size", () =>
                {
                    var v = new FVector(3, 4, 0);
                    Expect(v.Size()).ToBeApproximately(5f);
                    Expect(v.SizeSquared()).ToBeApproximately(25f);
                });

                It("should normalize vector", () =>
                {
                    var v = new FVector(3, 0, 0);
                    v.Normalize();
                    Expect(v.X).ToBe(1f);
                    Expect(v.Y).ToBe(0f);
                    Expect(v.Z).ToBe(0f);
                });

                It("should compute dot and cross products", () =>
                {
                    var a = new FVector(1, 2, 3);
                    var b = new FVector(4, 5, 6);
                    var dot = FVector.Dot(a, b);
                    var cross = FVector.Cross(new FVector(1, 0, 0), new FVector(0, 1, 0));
                    Expect(dot).ToBe(32f);
                    Expect(cross.X).ToBe(0f);
                    Expect(cross.Y).ToBe(0f);
                    Expect(cross.Z).ToBe(1f);
                });

                It("should support arithmetic operators", () =>
                {
                    var a = new FVector(1, 2, 3);
                    var b = new FVector(4, 5, 6);
                    var sum = a + b;
                    var diff = b - a;
                    var mul = a * 2f;
                    var div = b / 2f;
                    Expect(sum.X).ToBe(5f);
                    Expect(sum.Y).ToBe(7f);
                    Expect(sum.Z).ToBe(9f);
                    Expect(diff.X).ToBe(3f);
                    Expect(diff.Y).ToBe(3f);
                    Expect(diff.Z).ToBe(3f);
                    Expect(mul.X).ToBe(2f);
                    Expect(mul.Y).ToBe(4f);
                    Expect(mul.Z).ToBe(6f);
                    Expect(div.X).ToBe(2f);
                    Expect(div.Y).ToBe(2.5f);
                    Expect(div.Z).ToBe(3f);
                });

                It("should compare equality", () =>
                {
                    var a = new FVector(1, 2, 3);
                    var b = new FVector(1, 2, 3);
                    var c = new FVector(3, 2, 1);
                    Expect(a == b).ToBe(true);
                    Expect(a != c).ToBe(true);
                });
            });
        }
    }
}
