using System.Runtime.CompilerServices;

public struct FVector
{
    public float X = 0;
    public float Y = 0;
    public float Z = 0;

    public FVector(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static FVector Zero => new FVector(0, 0, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Size()
    {
        return MathF.Sqrt(X * X + Y * Y + Z * Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float SizeSquared()
    {
        return X * X + Y * Y + Z * Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Normalize()
    {
        float size = Size();
        if (size > 1e-6f)
        {
            float invSize = 1.0f / size;
            X *= invSize;
            Y *= invSize;
            Z *= invSize;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(FVector a, FVector b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector Cross(FVector a, FVector b)
    {
        return new FVector(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(FVector a, FVector b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceSquared(FVector a, FVector b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    public override string ToString()
    {
        return $"X={X:0.00} Y={Y:0.00} Z={Z:0.00}";
    }

    public static FVector operator +(FVector a, FVector b)
        => new FVector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static FVector operator -(FVector a, FVector b)
        => new FVector(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static FVector operator *(FVector a, float scalar)
        => new FVector(a.X * scalar, a.Y * scalar, a.Z * scalar);

    public static FVector operator /(FVector a, float scalar)
        => new FVector(a.X / scalar, a.Y / scalar, a.Z / scalar);

    public static bool operator ==(FVector a, FVector b)
        => MathF.Abs(a.X - b.X) < 1e-6f &&
           MathF.Abs(a.Y - b.Y) < 1e-6f &&
           MathF.Abs(a.Z - b.Z) < 1e-6f;

    public static bool operator !=(FVector a, FVector b)
        => !(a == b);

    public override bool Equals(object obj)
        => obj is FVector other && this == other;

    public override int GetHashCode()
        => HashCode.Combine(X, Y, Z);
}
