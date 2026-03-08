using Godot;

public enum TowerType
{
	Archer,
	Cannon,
	Frost
}

public sealed class Enemy
{
	public float Hp;
	public float MaxHp;
	public float Speed; // px/s
	public Vector2 Pos;
	public int Segment;
	public bool ReachedEnd;
	public float Radius;

	// Frost effect
	public float SlowMultiplier = 1.0f;
	public float SlowTimeLeft = 0.0f;
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

	// Cannon
	public float SplashRadius;

	// Frost
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
	public Vector2 CenterNorm; // 0..1
	public float SizePx;
	public bool HasTower;
}
