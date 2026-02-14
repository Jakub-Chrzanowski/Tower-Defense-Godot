using Godot;

public enum EnemyType { Grunt, Fast, Tank, Flying, Boss }
public enum TowerType { Archer, Cannon, Frost }

public sealed class Enemy
{
    public EnemyType Type;
    public float Hp;
    public float MaxHp;
    public float Speed; // px/s
    public Vector2 Pos;
    public int Segment;
    public bool ReachedEnd;
    public float Radius;
    public int Reward;

    // Status
    public float SlowMul = 1f;
    public float SlowTime = 0f;
}

public sealed class Tower
{
    public TowerType Type;
    public Vector2 Pos;
    public int Level = 1;
    public float Range;
    public float Damage;
    public float FireInterval;
    public float Cooldown;
    public float SplashRadius; // cannon
    public float SlowMul;      // frost
    public float SlowTime;     // frost
}

public sealed class Projectile
{
    public Vector2 Pos;
    public Vector2 Vel;
    public float TimeLeft;
}
