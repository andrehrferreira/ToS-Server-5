/*
* Quantization
*
* Author: Andre Ferreira
*
* Copyright (c) Uzmi Games. Licensed under the MIT License.
*/

namespace Wormhole
{
    public static class Quantization
    {

        public const float STEP_NetQuantize = 0.01f;    // 1 cm
        public const float STEP_NetQuantize10 = 0.001f;   // 0.1 cm
        public const float STEP_NetQuantize100 = 0.0001f;  // 0.01 cm

        public const float DEFAULT_MAX_POS_ABS_M = 50000f;

        // --- Angle ---

        public static byte QuantizeAngleToByte(float degrees)
        {
            float d = degrees % 360f; if (d < 0) d += 360f;
            int b = (int)MathF.Round(d * (256f / 360f)) & 0xFF;
            return (byte)b;
        }

        public static float DequantizeAngleFromByte(byte q)
        {
            return q * (360f / 256f);
        }

        public static ushort QuantizeAngleToU16(float degrees)
        {
            float d = degrees % 360f; if (d < 0) d += 360f;
            return (ushort)MathF.Round(d * (65536f / 360f));
        }

        public static float DequantizeAngleFromU16(ushort u16)
        {
            return u16 * (360f / 65536f);
        }

        // --- Scalar ---

        public static int QuantizeScalarToI32(float value, float step)
        {
            return (int)MathF.Round(value / step);
        }

        public static float DequantizeScalarFromI32(int q, float step)
        {
            return q * step;
        }

        public static int QuantizeScalarToI32Clamped(float value, float step, float maxAbs)
        {
            float v = MathF.Max(-maxAbs, MathF.Min(maxAbs, value));
            return (int)MathF.Round(v / step);
        }

        // --- Vector3 ---

        public static (int qx, int qy, int qz) QuantizeVector3_NetQuantize(FVector3 v)
        {
            return (QuantizeScalarToI32(v.X, STEP_NetQuantize),
                    QuantizeScalarToI32(v.Y, STEP_NetQuantize),
                    QuantizeScalarToI32(v.Z, STEP_NetQuantize));
        }
        public static FVector3 DequantizeVector3_NetQuantize(int qx, int qy, int qz)
        {
            return new FVector3(
                DequantizeScalarFromI32(qx, STEP_NetQuantize),
                DequantizeScalarFromI32(qy, STEP_NetQuantize),
                DequantizeScalarFromI32(qz, STEP_NetQuantize)
            );
        }

        public static (int qx, int qy, int qz) QuantizeVector3_NetQuantize10(FVector3 v)
        {
            return (QuantizeScalarToI32(v.X, STEP_NetQuantize10),
                    QuantizeScalarToI32(v.Y, STEP_NetQuantize10),
                    QuantizeScalarToI32(v.Z, STEP_NetQuantize10));
        }

        public static FVector3 DequantizeVector3_NetQuantize10(int qx, int qy, int qz)
        {
            return new FVector3(
                DequantizeScalarFromI32(qx, STEP_NetQuantize10),
                DequantizeScalarFromI32(qy, STEP_NetQuantize10),
                DequantizeScalarFromI32(qz, STEP_NetQuantize10)
            );
        }

        public static (int qx, int qy, int qz) QuantizeVector3_NetQuantize100(FVector3 v)
        {
            return (QuantizeScalarToI32(v.X, STEP_NetQuantize100),
                    QuantizeScalarToI32(v.Y, STEP_NetQuantize100),
                    QuantizeScalarToI32(v.Z, STEP_NetQuantize100));
        }

        public static FVector3 DequantizeVector3_NetQuantize100(int qx, int qy, int qz)
        {
            return new FVector3(
                DequantizeScalarFromI32(qx, STEP_NetQuantize100),
                DequantizeScalarFromI32(qy, STEP_NetQuantize100),
                DequantizeScalarFromI32(qz, STEP_NetQuantize100)
            );
        }

        public static short QuantizeNormalToI16(float n)
        {
            float c = MathF.Max(-1f, MathF.Min(1f, n));
            int q = (int)MathF.Round(c * 32767f);
            return (short)Math.Clamp(q, short.MinValue, short.MaxValue);
        }

        public static float DequantizeNormalFromI16(short q)
        {
            return MathF.Max(-1f, MathF.Min(1f, q / 32767f));
        }

        public static (short nx, short ny, short nz) QuantizeVector3Normal(FVector3 n)
        {
            return (QuantizeNormalToI16(n.X),
                    QuantizeNormalToI16(n.Y),
                    QuantizeNormalToI16(n.Z));
        }

        public static FVector3 DequantizeVector3Normal(short nx, short ny, short nz, bool renormalize = true)
        {
            var v = new FVector3(
                DequantizeNormalFromI16(nx),
                DequantizeNormalFromI16(ny),
                DequantizeNormalFromI16(nz)
            );

            if (renormalize)
            {
                float len = v.Length();
                if (len > 1e-6f) v /= len;
                else v = FVector3.UnitX;
            }

            return v;
        }

        // --- Velocity ---

        public static int QuantizeVelocityToI32(float v, float step, float maxAbs)
        {
            float c = MathF.Max(-maxAbs, MathF.Min(maxAbs, v));
            return (int)MathF.Round(c / step);
        }

        public static float DequantizeVelocityFromI32(int q, float step)
        {
            return q * step;
        }

        public static short QuantizeVelocityToI16(float v, float step, float maxAbs)
        {
            float c = MathF.Max(-maxAbs, MathF.Min(maxAbs, v));
            int q = (int)MathF.Round(c / step);
            return (short)Math.Clamp(q, short.MinValue, short.MaxValue);
        }

        public static float DequantizeVelocityFromI16(short q, float step)
        {
            return q * step;
        }
    }
}
