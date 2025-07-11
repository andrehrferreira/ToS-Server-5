using System;

public static class Quantization
{
    public static short ToShort(float value, float min, float max)
    {
        float clamped = Math.Clamp(value, min, max);
        float normalized = (clamped - min) / (max - min);
        return (short)MathF.Round(normalized * short.MaxValue);
    }

    public static float ToFloat(short value, float min, float max)
    {
        float normalized = value / (float)short.MaxValue;
        return normalized * (max - min) + min;
    }

    public static void ToShort(FVector value, float min, float max, out short x, out short y, out short z)
    {
        x = ToShort(value.X, min, max);
        y = ToShort(value.Y, min, max);
        z = ToShort(value.Z, min, max);
    }

    public static FVector ToVector(short x, short y, short z, float min, float max)
    {
        return new FVector(ToFloat(x, min, max), ToFloat(y, min, max), ToFloat(z, min, max));
    }
}
