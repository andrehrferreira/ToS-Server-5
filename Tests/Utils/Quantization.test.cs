using Wormhole;

namespace Tests
{
    public class QuantizationTests : AbstractTest
    {
        public QuantizationTests()
        {
            Describe("Quantization Constants", () =>
            {
                It("should have correct step values", () =>
                {
                    Expect(Quantization.STEP_NetQuantize).ToBe(0.01f);
                    Expect(Quantization.STEP_NetQuantize10).ToBe(0.001f);
                    Expect(Quantization.STEP_NetQuantize100).ToBe(0.0001f);
                    Expect(Quantization.DEFAULT_MAX_POS_ABS_M).ToBe(50000f);
                });
            });

            Describe("Angle Quantization", () =>
            {
                It("should quantize and dequantize angle to byte with round-trip accuracy", () =>
                {
                    float originalAngle = 45.0f;
                    byte quantized = Quantization.QuantizeAngleToByte(originalAngle);
                    float dequantized = Quantization.DequantizeAngleFromByte(quantized);
                    Expect(dequantized).ToBeApproximately(originalAngle, 360f / 256f);
                });

                It("should handle angle wrapping correctly", () =>
                {
                    float angle360 = 360.0f;
                    float angle720 = 720.0f;
                    float angleNeg90 = -90.0f;

                    byte q360 = Quantization.QuantizeAngleToByte(angle360);
                    byte q720 = Quantization.QuantizeAngleToByte(angle720);
                    byte qNeg90 = Quantization.QuantizeAngleToByte(angleNeg90);
                    Expect(q360).ToBe(q720);

                    float dqNeg90 = Quantization.DequantizeAngleFromByte(qNeg90);
                    Expect(dqNeg90).ToBeApproximately(270.0f, 360f / 256f);
                });

                It("should quantize and dequantize angle to ushort with high precision", () =>
                {
                    float originalAngle = 123.456f;
                    ushort quantized = Quantization.QuantizeAngleToU16(originalAngle);
                    float dequantized = Quantization.DequantizeAngleFromU16(quantized);
                    Expect(dequantized).ToBeApproximately(originalAngle, 360f / 65536f);
                });

                It("should handle zero and full rotation angles", () =>
                {
                    float zero = 0.0f;
                    float full = 360.0f;

                    byte qZero = Quantization.QuantizeAngleToByte(zero);
                    byte qFull = Quantization.QuantizeAngleToByte(full);

                    Expect(qZero).ToBe(qFull);

                    ushort uqZero = Quantization.QuantizeAngleToU16(zero);
                    ushort uqFull = Quantization.QuantizeAngleToU16(full);

                    Expect(uqZero).ToBe(uqFull);
                });
            });

            Describe("Scalar Quantization", () =>
            {
                It("should quantize and dequantize scalar values correctly", () =>
                {
                    float original = 123.45f;
                    float step = 0.01f;
                    int quantized = Quantization.QuantizeScalarToI32(original, step);
                    float dequantized = Quantization.DequantizeScalarFromI32(quantized, step);

                    Expect(dequantized).ToBeApproximately(original, step);
                });

                It("should handle zero scalar value", () =>
                {
                    float original = 0.0f;
                    float step = 0.1f;
                    int quantized = Quantization.QuantizeScalarToI32(original, step);
                    float dequantized = Quantization.DequantizeScalarFromI32(quantized, step);

                    Expect(quantized).ToBe(0);
                    Expect(dequantized).ToBe(0.0f);
                });

                It("should clamp scalar values within bounds", () =>
                {
                    float largeValue = 1000000.0f;
                    float step = 0.01f;
                    float maxAbs = 50000.0f;

                    int quantized = Quantization.QuantizeScalarToI32Clamped(largeValue, step, maxAbs);
                    float dequantized = Quantization.DequantizeScalarFromI32(quantized, step);

                    Expect(dequantized).ToBeApproximately(maxAbs, step);
                });

                It("should handle negative scalar values", () =>
                {
                    float original = -456.78f;
                    float step = 0.001f;
                    int quantized = Quantization.QuantizeScalarToI32(original, step);
                    float dequantized = Quantization.DequantizeScalarFromI32(quantized, step);

                    Expect(dequantized).ToBeApproximately(original, step);
                });
            });

            Describe("Vector3 Quantization - NetQuantize", () =>
            {
                It("should quantize and dequantize Vector3 with NetQuantize precision", () =>
                {
                    var original = new FVector3(123.45f, -67.89f, 0.12f);
                    var (qx, qy, qz) = Quantization.QuantizeVector3_NetQuantize(original);
                    var dequantized = Quantization.DequantizeVector3_NetQuantize(qx, qy, qz);

                    Expect(dequantized.X).ToBeApproximately(original.X, Quantization.STEP_NetQuantize);
                    Expect(dequantized.Y).ToBeApproximately(original.Y, Quantization.STEP_NetQuantize);
                    Expect(dequantized.Z).ToBeApproximately(original.Z, Quantization.STEP_NetQuantize);
                });

                It("should handle zero Vector3", () =>
                {
                    var original = FVector3.Zero;
                    var (qx, qy, qz) = Quantization.QuantizeVector3_NetQuantize(original);
                    var dequantized = Quantization.DequantizeVector3_NetQuantize(qx, qy, qz);

                    Expect(qx).ToBe(0);
                    Expect(qy).ToBe(0);
                    Expect(qz).ToBe(0);
                    Expect(dequantized).ToBe(FVector3.Zero);
                });
            });

            Describe("Vector3 Quantization - NetQuantize10", () =>
            {
                It("should quantize and dequantize Vector3 with higher precision", () =>
                {
                    var original = new FVector3(12.345f, -6.789f, 0.012f);
                    var (qx, qy, qz) = Quantization.QuantizeVector3_NetQuantize10(original);
                    var dequantized = Quantization.DequantizeVector3_NetQuantize10(qx, qy, qz);

                    Expect(dequantized.X).ToBeApproximately(original.X, Quantization.STEP_NetQuantize10);
                    Expect(dequantized.Y).ToBeApproximately(original.Y, Quantization.STEP_NetQuantize10);
                    Expect(dequantized.Z).ToBeApproximately(original.Z, Quantization.STEP_NetQuantize10);
                });
            });

            Describe("Vector3 Quantization - NetQuantize100", () =>
            {
                It("should quantize and dequantize Vector3 with highest precision", () =>
                {
                    var original = new FVector3(1.2345f, -0.6789f, 0.0012f);
                    var (qx, qy, qz) = Quantization.QuantizeVector3_NetQuantize100(original);
                    var dequantized = Quantization.DequantizeVector3_NetQuantize100(qx, qy, qz);

                    Expect(dequantized.X).ToBeApproximately(original.X, Quantization.STEP_NetQuantize100);
                    Expect(dequantized.Y).ToBeApproximately(original.Y, Quantization.STEP_NetQuantize100);
                    Expect(dequantized.Z).ToBeApproximately(original.Z, Quantization.STEP_NetQuantize100);
                });
            });

            Describe("Normal Quantization", () =>
            {
                It("should quantize and dequantize normal values correctly", () =>
                {
                    float original = 0.707f; // Approximately 1/sqrt(2)
                    short quantized = Quantization.QuantizeNormalToI16(original);
                    float dequantized = Quantization.DequantizeNormalFromI16(quantized);

                    Expect(dequantized).ToBeApproximately(original, 1f / 32767f);
                });

                It("should clamp normal values to valid range", () =>
                {
                    float tooLarge = 2.0f;
                    float tooSmall = -2.0f;

                    short qLarge = Quantization.QuantizeNormalToI16(tooLarge);
                    short qSmall = Quantization.QuantizeNormalToI16(tooSmall);

                    float dqLarge = Quantization.DequantizeNormalFromI16(qLarge);
                    float dqSmall = Quantization.DequantizeNormalFromI16(qSmall);

                    Expect(dqLarge).ToBeApproximately(1.0f, 1f / 32767f);
                    Expect(dqSmall).ToBeApproximately(-1.0f, 1f / 32767f);
                });

                It("should handle unit vectors", () =>
                {
                    float one = 1.0f;
                    float minusOne = -1.0f;
                    float zero = 0.0f;

                    short qOne = Quantization.QuantizeNormalToI16(one);
                    short qMinusOne = Quantization.QuantizeNormalToI16(minusOne);
                    short qZero = Quantization.QuantizeNormalToI16(zero);

                    Expect(qOne).ToBe((short)32767);
                    Expect(qMinusOne).ToBe((short)-32767);
                    Expect(qZero).ToBe((short)0);
                });

                It("should quantize and dequantize normal vectors", () =>
                {
                    var original = new FVector3(0.577f, 0.577f, 0.577f); // Normalized (1,1,1)
                    var (nx, ny, nz) = Quantization.QuantizeVector3Normal(original);
                    var dequantized = Quantization.DequantizeVector3Normal(nx, ny, nz);

                    // Check that it's approximately unit length
                    float length = dequantized.Length();
                    Expect(length).ToBeApproximately(1.0f, 0.01f);

                    // Check individual components are close
                    Expect(dequantized.X).ToBeApproximately(original.X, 0.01f);
                    Expect(dequantized.Y).ToBeApproximately(original.Y, 0.01f);
                    Expect(dequantized.Z).ToBeApproximately(original.Z, 0.01f);
                });

                It("should renormalize zero vectors to UnitX", () =>
                {
                    var zero = FVector3.Zero;
                    var (nx, ny, nz) = Quantization.QuantizeVector3Normal(zero);
                    var dequantized = Quantization.DequantizeVector3Normal(nx, ny, nz);

                    Expect(dequantized).ToBe(FVector3.UnitX);
                });
            });

            Describe("Velocity Quantization", () =>
            {
                It("should quantize and dequantize velocity with I32 precision", () =>
                {
                    float original = 123.45f;
                    float step = 0.01f;
                    float maxAbs = 1000.0f;

                    int quantized = Quantization.QuantizeVelocityToI32(original, step, maxAbs);
                    float dequantized = Quantization.DequantizeVelocityFromI32(quantized, step);

                    Expect(dequantized).ToBeApproximately(original, step);
                });

                It("should clamp velocity values", () =>
                {
                    float tooLarge = 2000.0f;
                    float step = 0.1f;
                    float maxAbs = 500.0f;

                    int quantized = Quantization.QuantizeVelocityToI32(tooLarge, step, maxAbs);
                    float dequantized = Quantization.DequantizeVelocityFromI32(quantized, step);

                    Expect(dequantized).ToBeApproximately(maxAbs, step);
                });

                It("should quantize and dequantize velocity with I16 precision", () =>
                {
                    float original = 45.67f;
                    float step = 0.01f;
                    float maxAbs = 300.0f;

                    short quantized = Quantization.QuantizeVelocityToI16(original, step, maxAbs);
                    float dequantized = Quantization.DequantizeVelocityFromI16(quantized, step);

                    Expect(dequantized).ToBeApproximately(original, step);
                });

                It("should handle zero velocity", () =>
                {
                    float original = 0.0f;
                    float step = 0.001f;
                    float maxAbs = 100.0f;

                    int q32 = Quantization.QuantizeVelocityToI32(original, step, maxAbs);
                    short q16 = Quantization.QuantizeVelocityToI16(original, step, maxAbs);

                    Expect(q32).ToBe(0);
                    Expect(q16).ToBe((short)0);
                });

                It("should handle negative velocities", () =>
                {
                    float original = -78.9f;
                    float step = 0.01f;
                    float maxAbs = 100.0f;

                    int quantized = Quantization.QuantizeVelocityToI32(original, step, maxAbs);
                    float dequantized = Quantization.DequantizeVelocityFromI32(quantized, step);

                    Expect(dequantized).ToBeApproximately(original, step);
                });
            });

            Describe("Edge Cases and Boundary Conditions", () =>
            {
                It("should handle maximum and minimum angle values", () =>
                {
                    float maxAngle = 359.999f;
                    float minAngle = 0.001f;

                    byte qMax = Quantization.QuantizeAngleToByte(maxAngle);
                    byte qMin = Quantization.QuantizeAngleToByte(minAngle);

                    float dqMax = Quantization.DequantizeAngleFromByte(qMax);
                    float dqMin = Quantization.DequantizeAngleFromByte(qMin);

                    Expect(dqMax).ToBeLessThan(360.0f);
                    Expect(dqMin).ToBeGreaterThanOrEqualTo(0.0f);
                });

                It("should handle extreme scalar values", () =>
                {
                    // Use values that don't cause int overflow when quantized
                    float veryLarge = 200000.0f; // Within int range when divided by step
                    float verySmall = 1e-6f;
                    float step = 0.0001f;

                    int qLarge = Quantization.QuantizeScalarToI32(veryLarge, step);
                    int qSmall = Quantization.QuantizeScalarToI32(verySmall, step);

                    float dqLarge = Quantization.DequantizeScalarFromI32(qLarge, step);
                    float dqSmall = Quantization.DequantizeScalarFromI32(qSmall, step);

                    Expect(dqLarge).ToBeApproximately(veryLarge, step);
                    Expect(dqSmall).ToBeApproximately(verySmall, step);
                });

                It("should handle Vector3 with extreme coordinates", () =>
                {
                    var extreme = new FVector3(50000f, -50000f, 0f);
                    var (qx, qy, qz) = Quantization.QuantizeVector3_NetQuantize(extreme);
                    var dequantized = Quantization.DequantizeVector3_NetQuantize(qx, qy, qz);

                    Expect(dequantized.X).ToBeApproximately(extreme.X, Quantization.STEP_NetQuantize);
                    Expect(dequantized.Y).ToBeApproximately(extreme.Y, Quantization.STEP_NetQuantize);
                    Expect(dequantized.Z).ToBeApproximately(extreme.Z, Quantization.STEP_NetQuantize);
                });
            });
        }
    }
}
