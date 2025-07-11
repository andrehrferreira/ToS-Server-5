namespace Tests
{
    public class QuantizationTests : AbstractTest
    {
        public QuantizationTests()
        {
            Describe("Quantization", () =>
            {
                It("should compress and decompress float values", () =>
                {
                    float min = -100f;
                    float max = 100f;
                    float original = 12.34f;

                    short q = Quantization.ToShort(original, min, max);
                    float result = Quantization.ToFloat(q, min, max);

                    Expect(result).ToBeApproximately(original, 0.01f);
                });

                It("should compress and decompress vector values", () =>
                {
                    float min = -50f;
                    float max = 50f;
                    var vec = new FVector(10.5f, -20.25f, 3.75f);

                    Quantization.ToShort(vec, min, max, out short qx, out short qy, out short qz);
                    var result = Quantization.ToVector(qx, qy, qz, min, max);

                    Expect(result.X).ToBeApproximately(vec.X, 0.01f);
                    Expect(result.Y).ToBeApproximately(vec.Y, 0.01f);
                    Expect(result.Z).ToBeApproximately(vec.Z, 0.01f);
                });
            });
        }
    }
}
