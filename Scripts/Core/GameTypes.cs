using Godot;

public sealed class Enemy
{
	public float Hp;
	public float MaxHp;
	public float Speed; 
	public Vector2 Pos;
	public int Segment;
	public bool ReachedEnd;
	public float Radius;
}

public sealed class Tower
{
	public Vector2 Pos;
	public float Range;
	public float Damage;
	public float FireInterval;
	public float Cooldown;
}

public sealed class Projectile
{
	public Vector2 Pos;
	public Vector2 Vel;
	public float TimeLeft;
}

public sealed class Pad
{
	public Vector2 CenterNorm; 
	public float SizePx;
	public bool HasTower;
}
