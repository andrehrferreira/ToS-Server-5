using System.Runtime.CompilerServices;

public struct FVector3
{
    public float X = 0;
    public float Y = 0;
    public float Z = 0;

    public static FVector3 Zero => new FVector3(0, 0, 0);
    public static readonly FVector3 UnitX = new FVector3(1f, 0f, 0f);

    public FVector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Size()
    {
        return MathF.Sqrt(X * X + Y * Y + Z * Z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Length()
    {
        return Size();
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
    public static float Dot(FVector3 a, FVector3 b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FVector3 Cross(FVector3 a, FVector3 b)
    {
        return new FVector3(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(FVector3 a, FVector3 b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceSquared(FVector3 a, FVector3 b)
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

    public static FVector3 operator +(FVector3 a, FVector3 b)
        => new FVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static FVector3 operator -(FVector3 a, FVector3 b)
        => new FVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static FVector3 operator *(FVector3 a, float scalar)
        => new FVector3(a.X * scalar, a.Y * scalar, a.Z * scalar);

    public static FVector3 operator /(FVector3 a, float scalar)
        => new FVector3(a.X / scalar, a.Y / scalar, a.Z / scalar);

    public static bool operator ==(FVector3 a, FVector3 b)
        => MathF.Abs(a.X - b.X) < 1e-6f &&
           MathF.Abs(a.Y - b.Y) < 1e-6f &&
           MathF.Abs(a.Z - b.Z) < 1e-6f;

    public static bool operator !=(FVector3 a, FVector3 b)
        => !(a == b);

    public override bool Equals(object obj)
        => obj is FVector3 other && this == other;

    public override int GetHashCode()
        => HashCode.Combine(X, Y, Z);
}
