using Godot;
using System;
using System.Collections.Generic;

public sealed class GameEngine
{
	public Vector2 WorldSize { get; private set; }

	// A3
	public int Lives { get; private set; } = 3;

	// B
	public int Coins { get; private set; } = 220;
	public TowerType SelectedTowerType { get; set; } = TowerType.Archer;
	public int? SelectedTowerIndex { get; private set; }

	// A2/A3 flow
	public bool WaveRunning { get; private set; }
	public bool Victory { get; private set; }
	public bool Defeat { get; private set; }

	// Path + pads
	private readonly List<Vector2> _pathPx = new();
	public IReadOnlyList<Vector2> PathPx => _pathPx;

	private readonly List<Pad> _pads = new();
	public IReadOnlyList<Pad> Pads => _pads;

	private readonly List<Enemy> _enemies = new();
	public IReadOnlyList<Enemy> Enemies => _enemies;

	private readonly List<Tower> _towers = new();
	public IReadOnlyList<Tower> Towers => _towers;

	private readonly List<Projectile> _projectiles = new();
	public IReadOnlyList<Projectile> Projectiles => _projectiles;

	// Single wave (still A2/A3 scope)
	private int _toSpawn;
	private float _spawnEvery;
	private float _spawnTimer;
	private bool _finishedSpawning;

	public event Action? OnHudChanged;
	public event Action? OnVictory;
	public event Action? OnDefeat;

	// Sound hooks
	public event Action? OnTowerBuilt;
	public event Action? OnTowerUpgraded;
	public event Action? OnTowerSold;
	public event Action? OnShot;
	public event Action? OnLeak;

	private readonly Vector2[] _pathNorm =
	{
		new(0.50f, 0.10f),
		new(0.50f, 0.42f),
		new(0.75f, 0.55f),
		new(0.25f, 0.68f),
		new(0.50f, 0.92f),
	};

	public void SetWorldSize(Vector2 size)
	{
		if (size.X <= 0 || size.Y <= 0) return;
		WorldSize = size;

		_pathPx.Clear();
		foreach (var wp in _pathNorm)
			_pathPx.Add(new Vector2(wp.X * size.X, wp.Y * size.Y));
	}

	public void Reset()
	{
		Lives = 3;
		Coins = 220;
		SelectedTowerType = TowerType.Archer;
		SelectedTowerIndex = null;

		WaveRunning = false;
		Victory = false;
		Defeat = false;

		_enemies.Clear();
		_towers.Clear();
		_projectiles.Clear();

		_pads.Clear();
		_pads.Add(new Pad { CenterNorm = new Vector2(0.28f, 0.50f), SizePx = 56, HasTower = false });
		_pads.Add(new Pad { CenterNorm = new Vector2(0.72f, 0.38f), SizePx = 56, HasTower = false });
		_pads.Add(new Pad { CenterNorm = new Vector2(0.72f, 0.70f), SizePx = 56, HasTower = false });
		_pads.Add(new Pad { CenterNorm = new Vector2(0.28f, 0.78f), SizePx = 56, HasTower = false });

		PrepareSingleWave();
		OnHudChanged?.Invoke();
	}

	public void StartWave()
	{
		if (Defeat || Victory || WaveRunning) return;
		WaveRunning = true;
		OnHudChanged?.Invoke();
	}

	public bool TrySelectTower(Vector2 clickPos)
	{
		SelectedTowerIndex = null;

		float hitRadius = Mathf.Min(WorldSize.X, WorldSize.Y) * 0.055f;
		for (int i = 0; i < _towers.Count; i++)
		{
			if (_towers[i].Pos.DistanceTo(clickPos) <= hitRadius)
			{
				SelectedTowerIndex = i;
				OnHudChanged?.Invoke();
				return true;
			}
		}

		OnHudChanged?.Invoke();
		return false;
	}

	public bool TryPlaceTower(Vector2 clickPos)
	{
		if (Defeat || Victory || WaveRunning) return false;

		foreach (var pad in _pads)
		{
			if (pad.HasTower) continue;

			var c = new Vector2(pad.CenterNorm.X * WorldSize.X, pad.CenterNorm.Y * WorldSize.Y);
			float half = pad.SizePx / 2f;
			var r = new Rect2(c.X - half, c.Y - half, pad.SizePx, pad.SizePx);
			if (!r.HasPoint(clickPos)) continue;

			int cost = TowerBalance.GetBuildCost(SelectedTowerType);
			if (Coins < cost) return false;

			Coins -= cost;
			pad.HasTower = true;

			_towers.Add(TowerBalance.CreateTower(SelectedTowerType, c, 1, WorldSize));
			SelectedTowerIndex = _towers.Count - 1;

			OnTowerBuilt?.Invoke();
			OnHudChanged?.Invoke();
			return true;
		}

		return false;
	}

	public bool UpgradeSelectedTower()
	{
		if (Defeat || Victory || WaveRunning) return false;
		if (SelectedTowerIndex is null) return false;

		int idx = SelectedTowerIndex.Value;
		if (idx < 0 || idx >= _towers.Count) return false;

		var old = _towers[idx];
		if (old.Level >= 3) return false;

		int newLevel = old.Level + 1;
		int cost = TowerBalance.GetUpgradeCost(old.Type, newLevel);
		if (Coins < cost) return false;

		Coins -= cost;
		_towers[idx] = TowerBalance.CreateTower(old.Type, old.Pos, newLevel, WorldSize);
		SelectedTowerIndex = idx;

		OnTowerUpgraded?.Invoke();
		OnHudChanged?.Invoke();
		return true;
	}

	public bool SellSelectedTower()
	{
		if (Defeat || Victory || WaveRunning) return false;
		if (SelectedTowerIndex is null) return false;

		int idx = SelectedTowerIndex.Value;
		if (idx < 0 || idx >= _towers.Count) return false;

		var tower = _towers[idx];
		int refund = TowerBalance.GetSellRefund(tower.Type, tower.Level);
		Coins += refund;

		int padIndex = FindNearestPadIndex(tower.Pos);
		if (padIndex >= 0)
			_pads[padIndex].HasTower = false;

		_towers.RemoveAt(idx);
		SelectedTowerIndex = null;

		OnTowerSold?.Invoke();
		OnHudChanged?.Invoke();
		return true;
	}

	public void Update(float dt)
	{
		if (WorldSize.X <= 0 || WorldSize.Y <= 0) return;
		if (Defeat || Victory) return;
		if (_pathPx.Count < 2) return;

		// Spawn
		if (WaveRunning && !_finishedSpawning)
		{
			_spawnTimer -= dt;
			while (_spawnTimer <= 0f && _toSpawn > 0)
			{
				SpawnEnemy();
				_toSpawn--;
				_spawnTimer += _spawnEvery;
			}

			if (_toSpawn <= 0) _finishedSpawning = true;
		}

		// Move enemies
		foreach (var e in _enemies)
		{
			if (e.Hp <= 0 || e.ReachedEnd) continue;

			if (e.SlowTimeLeft > 0)
			{
				e.SlowTimeLeft -= dt;
				if (e.SlowTimeLeft <= 0)
				{
					e.SlowTimeLeft = 0;
					e.SlowMultiplier = 1.0f;
				}
			}

			MovePath(e, dt);
		}

		// Towers shoot
		foreach (var t in _towers)
		{
			t.Cooldown -= dt;
			if (t.Cooldown > 0f) continue;

			var target = FindTarget(t.Pos, t.Range);
			if (target == null) continue;

			FireTower(t, target);
			t.Cooldown = t.FireInterval;
			OnShot?.Invoke();
		}

		// Projectiles (visual only)
		for (int i = _projectiles.Count - 1; i >= 0; i--)
		{
			var p = _projectiles[i];
			p.TimeLeft -= dt;
			p.Pos += p.Vel * dt;
			if (p.TimeLeft <= 0f)
				_projectiles.RemoveAt(i);
		}

		// Cleanup
		for (int i = _enemies.Count - 1; i >= 0; i--)
		{
			var e = _enemies[i];
			if (e.Hp <= 0 || e.ReachedEnd)
				_enemies.RemoveAt(i);
		}

		if (Lives <= 0 && !Defeat)
		{
			Defeat = true;
			WaveRunning = false;
			OnHudChanged?.Invoke();
			OnDefeat?.Invoke();
			return;
		}

		if (WaveRunning && _finishedSpawning && _enemies.Count == 0 && !Victory)
		{
			Victory = true;
			WaveRunning = false;
			OnHudChanged?.Invoke();
			OnVictory?.Invoke();
		}
	}

	private void FireTower(Tower tower, Enemy target)
	{
		AddProjectile(tower.Pos, target.Pos, tower.Type);

		switch (tower.Type)
		{
			case TowerType.Archer:
				target.Hp -= tower.Damage;
				break;

			case TowerType.Cannon:
				foreach (var e in _enemies)
				{
					if (e.Hp <= 0 || e.ReachedEnd) continue;
					if (e.Pos.DistanceTo(target.Pos) <= tower.SplashRadius)
						e.Hp -= tower.Damage;
				}
				break;

			case TowerType.Frost:
				target.Hp -= tower.Damage;
				target.SlowMultiplier = MathF.Min(target.SlowMultiplier, tower.SlowMultiplier);
				target.SlowTimeLeft = MathF.Max(target.SlowTimeLeft, tower.SlowDuration);
				break;
		}
	}

	private void PrepareSingleWave()
	{
		_toSpawn = 12;
		_spawnEvery = 0.75f;
		_spawnTimer = 0f;
		_finishedSpawning = false;
	}

	private void SpawnEnemy()
	{
		float baseSize = Mathf.Min(WorldSize.X, WorldSize.Y);
		var start = _pathPx[0];

		_enemies.Add(new Enemy
		{
			MaxHp = 120,
			Hp = 120,
			Speed = baseSize * 0.16f,
			Pos = start,
			Segment = 0,
			Radius = baseSize * 0.022f
		});
	}

	private void MovePath(Enemy e, float dt)
	{
		float remaining = e.Speed * e.SlowMultiplier * dt;

		while (remaining > 0f && !e.ReachedEnd)
		{
			var b = _pathPx[e.Segment + 1];
			var toB = b - e.Pos;
			float dist = toB.Length();

			if (dist <= remaining)
			{
				e.Pos = b;
				remaining -= dist;
				e.Segment++;

				if (e.Segment >= _pathPx.Count - 1)
				{
					ReachEnd(e);
					break;
				}
			}
			else
			{
				e.Pos += toB / dist * remaining;
				remaining = 0f;
			}
		}
	}

	private void ReachEnd(Enemy e)
	{
		e.ReachedEnd = true;
		Lives -= 1;
		OnLeak?.Invoke();
		OnHudChanged?.Invoke();
	}

	private Enemy? FindTarget(Vector2 from, float range)
	{
		Enemy? best = null;
		float bestDist = float.MaxValue;

		foreach (var e in _enemies)
		{
			if (e.Hp <= 0 || e.ReachedEnd) continue;

			float d = e.Pos.DistanceSquaredTo(from);
			if (d > range * range) continue;

			if (d < bestDist)
			{
				bestDist = d;
				best = e;
			}
		}

		return best;
	}

	private void AddProjectile(Vector2 from, Vector2 to, TowerType sourceType)
	{
		var dir = to - from;
		if (dir.Length() < 1f) return;

		dir = dir.Normalized();
		float speed = Mathf.Min(WorldSize.X, WorldSize.Y) * 0.90f;

		_projectiles.Add(new Projectile
		{
			Pos = from,
			Vel = dir * speed,
			TimeLeft = 0.14f,
			SourceType = sourceType
		});
	}

	private int FindNearestPadIndex(Vector2 pos)
	{
		int bestIdx = -1;
		float best = float.MaxValue;

		for (int i = 0; i < _pads.Count; i++)
		{
			var c = new Vector2(_pads[i].CenterNorm.X * WorldSize.X, _pads[i].CenterNorm.Y * WorldSize.Y);
			float d = c.DistanceSquaredTo(pos);
			if (d < best)
			{
				best = d;
				bestIdx = i;
			}
		}

		return bestIdx;
	}
}

public static class TowerBalance
{
	public static int GetBuildCost(TowerType type) => type switch
	{
		TowerType.Archer => 40,
		TowerType.Cannon => 70,
		TowerType.Frost => 60,
		_ => 50
	};

	public static int GetUpgradeCost(TowerType type, int newLevel) => (type, newLevel) switch
	{
		(TowerType.Archer, 2) => 55,
		(TowerType.Archer, 3) => 75,

		(TowerType.Cannon, 2) => 85,
		(TowerType.Cannon, 3) => 110,

		(TowerType.Frost, 2) => 75,
		(TowerType.Frost, 3) => 95,
		_ => 0
	};

	public static int GetSellRefund(TowerType type, int level)
	{
		int total = GetBuildCost(type);
		if (level >= 2) total += GetUpgradeCost(type, 2);
		if (level >= 3) total += GetUpgradeCost(type, 3);
		return Mathf.RoundToInt(total * 0.60f);
	}

	public static Tower CreateTower(TowerType type, Vector2 pos, int level, Vector2 worldSize)
	{
		float baseSize = Mathf.Min(worldSize.X, worldSize.Y);
		float rangeBase = baseSize * 0.22f + (level - 1) * baseSize * 0.03f;

		switch (type)
		{
			case TowerType.Archer:
				return new Tower
				{
					Type = type,
					Pos = pos,
					Level = level,
					Range = rangeBase,
					Damage = 34 + (level - 1) * 18,
					FireInterval = 0.55f - (level - 1) * 0.06f,
					Cooldown = 0.05f,
					SplashRadius = 0,
					SlowMultiplier = 1.0f,
					SlowDuration = 0
				};

			case TowerType.Cannon:
				return new Tower
				{
					Type = type,
					Pos = pos,
					Level = level,
					Range = rangeBase * 0.95f,
					Damage = 55 + (level - 1) * 24,
					FireInterval = 0.95f - (level - 1) * 0.08f,
					Cooldown = 0.05f,
					SplashRadius = baseSize * (0.060f + (level - 1) * 0.015f),
					SlowMultiplier = 1.0f,
					SlowDuration = 0
				};

			case TowerType.Frost:
				return new Tower
				{
					Type = type,
					Pos = pos,
					Level = level,
					Range = rangeBase * 1.05f,
					Damage = 18 + (level - 1) * 10,
					FireInterval = 0.50f - (level - 1) * 0.05f,
					Cooldown = 0.05f,
					SplashRadius = 0,
					SlowMultiplier = 0.70f - (level - 1) * 0.05f,
					SlowDuration = 0.90f + (level - 1) * 0.25f
				};

			default:
				return new Tower
				{
					Type = TowerType.Archer,
					Pos = pos,
					Level = 1,
					Range = rangeBase,
					Damage = 34,
					FireInterval = 0.55f,
					Cooldown = 0.05f,
					SplashRadius = 0,
					SlowMultiplier = 1.0f,
					SlowDuration = 0
				};
		}
	}
}
