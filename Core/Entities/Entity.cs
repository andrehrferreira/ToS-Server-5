public struct Entity
{
    public int Id;
    public FixedString32 Name;
    public FVector Position;
    public FRotator Rotation;
    public int AnimState;
    public EntityState Flags;

    //AIO Control
    public (int, int, int) CurrentCell; 
    public FVector LastPosition; 
}

[Flags]
public enum EntityState : int
{
    None = 0,
    IsAlive = 1 << 0,
    IsInCombat = 1 << 1,
    IsMoving = 1 << 2,
    IsCasting = 1 << 3,
    IsInvisible = 1 << 4,
    IsStunned = 1 << 5
}