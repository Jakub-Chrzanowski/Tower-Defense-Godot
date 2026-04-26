using Godot;

public enum TowerType { Archer, Cannon, Frost }

public enum EnemyType
{
	Grunt,
	Fast,
	Tank,
	Flying,
	Boss   // E2
}

public sealed class Enemy
{
	public EnemyType Type;
	public float Hp;
	public float MaxHp;
	public float Speed;
	public Vector2 Pos;
	public int Segment;
	public bool ReachedEnd;
	public float Radius;
	public int Reward;
	public float SlowMultiplier = 1.0f;
	public float SlowTimeLeft   = 0.0f;
	public bool IsBoss          = false;
}

public sealed class Tower
{
	public TowerType Type;
	public Vector2 Pos;
	public int Level;
	public float Range;
	public float Damage;
	public float FireInterval;
	public float Cooldown;
	public float SplashRadius;
	public float SlowMultiplier;
	public float SlowDuration;
}

public sealed class Projectile
{
	public Vector2 Pos;
	public Vector2 Vel;
	public float TimeLeft;
	public TowerType SourceType;
}

public sealed class Pad
{
	public Vector2 CenterNorm;
	public float SizePx;
	public bool HasTower;
}

public sealed class MapDef
{
	public required Vector2[] Path;
	public required PadDef[]  Pads;
	public required string    Name;
}

public readonly record struct PadDef(Vector2 CenterNorm, float SizePx);
