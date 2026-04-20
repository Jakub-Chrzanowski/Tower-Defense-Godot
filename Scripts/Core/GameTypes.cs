using Godot;

public enum TowerType
{
	Archer,
	Cannon,
	Frost
}

public enum EnemyType
{
<<<<<<< HEAD
	
=======
	public float Hp;
	public float MaxHp;
	public float Speed; // px/s
	public Vector2 Pos;
	public int Segment;
	public bool ReachedEnd;
	public float Radius;

	
	public float SlowMultiplier = 1.0f;
	public float SlowTimeLeft = 0.0f;
>>>>>>> parent of c1670f3 (Merge branch 'main' of https://github.com/Jakub-Chrzanowski/Tower-Defense-Godot)
}


public sealed class Tower
{
	public TowerType Type;
	public Vector2 Pos;
	public int Level;
<<<<<<< HEAD
=======

>>>>>>> parent of c1670f3 (Merge branch 'main' of https://github.com/Jakub-Chrzanowski/Tower-Defense-Godot)
	public float Range;
	public float Damage;
	public float FireInterval;
	public float Cooldown;
<<<<<<< HEAD
	public float SplashRadius;
=======

	
	public float SplashRadius;


>>>>>>> parent of c1670f3 (Merge branch 'main' of https://github.com/Jakub-Chrzanowski/Tower-Defense-Godot)
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
<<<<<<< HEAD

public sealed class MapDef
{
	public required Vector2[] Path;
	public required PadDef[] Pads;
	public required string Name;
}

public readonly record struct PadDef(Vector2 CenterNorm, float SizePx);
=======
>>>>>>> parent of c1670f3 (Merge branch 'main' of https://github.com/Jakub-Chrzanowski/Tower-Defense-Godot)
