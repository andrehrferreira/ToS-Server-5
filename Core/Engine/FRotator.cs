using System.Runtime.CompilerServices;

public struct FRotator
{
    public float Pitch; 
    public float Yaw;   
    public float Roll;  

    public FRotator(float pitch, float yaw, float roll)
    {
        Pitch = pitch;
        Yaw = yaw;
        Roll = roll;
    }

    public static FRotator Zero => new FRotator(0, 0, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Normalize()
    {
        Pitch = NormalizeAxis(Pitch);
        Yaw = NormalizeAxis(Yaw);
        Roll = NormalizeAxis(Roll);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float NormalizeAxis(float angle)
    {
        angle = angle % 360.0f;
        if (angle > 180.0f)
            angle -= 360.0f;
        else if (angle < -180.0f)
            angle += 360.0f;
        return angle;
    }

    public override string ToString()
    {
        return $"Pitch={Pitch:0.00} Yaw={Yaw:0.00} Roll={Roll:0.00}";
    }

    public static FRotator operator +(FRotator a, FRotator b)
        => new FRotator(a.Pitch + b.Pitch, a.Yaw + b.Yaw, a.Roll + b.Roll);

    public static FRotator operator -(FRotator a, FRotator b)
        => new FRotator(a.Pitch - b.Pitch, a.Yaw - b.Yaw, a.Roll - b.Roll);

    public static FRotator operator *(FRotator a, float scalar)
        => new FRotator(a.Pitch * scalar, a.Yaw * scalar, a.Roll * scalar);

    public static FRotator operator /(FRotator a, float scalar)
        => new FRotator(a.Pitch / scalar, a.Yaw / scalar, a.Roll / scalar);

    public static bool operator ==(FRotator a, FRotator b)
        => MathF.Abs(a.Pitch - b.Pitch) < 1e-6f &&
           MathF.Abs(a.Yaw - b.Yaw) < 1e-6f &&
           MathF.Abs(a.Roll - b.Roll) < 1e-6f;

    public static bool operator !=(FRotator a, FRotator b)
        => !(a == b);

    public override bool Equals(object obj)
        => obj is FRotator other && this == other;

    public override int GetHashCode()
        => HashCode.Combine(Pitch, Yaw, Roll);
}
