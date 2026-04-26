using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class GameEngine
{
	public Vector2 WorldSize { get; private set; }

	public int Lives { get; private set; } = 3;
	public int Coins { get; private set; } = 120;
	public TowerType SelectedTowerType { get; set; } = TowerType.Archer;
	public int? SelectedTowerIndex { get; private set; }

	public bool WaveRunning { get; private set; }
	public bool Victory { get; private set; }
	public bool Defeat { get; private set; }

	private MapDef _map = Maps.All[0];

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

	private int _toSpawn;
	private float _spawnEvery;
	private float _spawnTimer;
	private bool _finishedSpawning;

	public event Action? OnHudChanged;
	public event Action? OnVictory;
	public event Action? OnDefeat;
	public event Action? OnTowerBuilt;
	public event Action? OnTowerUpgraded;
	public event Action? OnTowerSold;
	public event Action? OnShot;
	public event Action? OnLeak;
	public event Action? OnEnemyKilled;
	public event Action? OnWaveReward;

	private readonly HashSet<Enemy> _rewardedEnemies = new();

	public void LoadMap(int mapId)
	{
		_map = Maps.All[Math.Clamp(mapId, 0, Maps.All.Length - 1)];
		Reset();
	}

	public void SetWorldSize(Vector2 size)
	{
		if (size.X <= 0 || size.Y <= 0) return;
		WorldSize = size;

		_pathPx.Clear();
		foreach (var wp in _map.Path)
			_pathPx.Add(new Vector2(wp.X * size.X, wp.Y * size.Y));
	}

	public void Reset()
	{
		Lives = 3;
		// Pobierz monety z sesji (przenoszone między mapami)
		Coins = GameSession.PersistentCoins;
		SelectedTowerType = TowerType.Archer;
		SelectedTowerIndex = null;
		WaveRunning = false;
		Victory = false;
		Defeat = false;

		_enemies.Clear();
		_towers.Clear();
		_projectiles.Clear();
		_rewardedEnemies.Clear();

		_pads.Clear();
		foreach (var p in _map.Pads)
			_pads.Add(new Pad { CenterNorm = p.CenterNorm, SizePx = p.SizePx, HasTower = false });

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
		if (padIndex >= 0) _pads[padIndex].HasTower = false;

		_towers.RemoveAt(idx);
		SelectedTowerIndex = null;

		OnTowerSold?.Invoke();
		OnHudChanged?.Invoke();
		return true;
	}

	// Zapisz aktualne monety do sesji (wywołaj przed przejściem do następnej mapy)
	public void SaveCoinsToSession()
	{
		GameSession.PersistentCoins = Coins;
	}

	public void Update(float dt)
	{
		if (WorldSize.X <= 0 || WorldSize.Y <= 0) return;
		if (Defeat || Victory) return;
		if (_pathPx.Count < 2) return;

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

		for (int i = _projectiles.Count - 1; i >= 0; i--)
		{
			var p = _projectiles[i];
			p.TimeLeft -= dt;
			p.Pos += p.Vel * dt;
			if (p.TimeLeft <= 0f) _projectiles.RemoveAt(i);
		}

		// kill rewards
		for (int i = _enemies.Count - 1; i >= 0; i--)
		{
			var e = _enemies[i];
			if (e.Hp <= 0 && !_rewardedEnemies.Contains(e))
			{
				_rewardedEnemies.Add(e);
				Coins += e.Reward;
				OnEnemyKilled?.Invoke();
				OnHudChanged?.Invoke();
			}

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
			Coins += 60; // znerf: było 100
			SaveCoinsToSession();
			OnWaveReward?.Invoke();
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

	private readonly Queue<EnemyType> _spawnQueue = new();

	private void PrepareSingleWave()
	{
		_spawnQueue.Clear();

		EnemyType[] pattern = new[]
		{
			EnemyType.Grunt, EnemyType.Fast, EnemyType.Grunt,
			EnemyType.Tank,  EnemyType.Fast, EnemyType.Grunt,
			EnemyType.Flying, EnemyType.Tank, EnemyType.Fast,
			EnemyType.Grunt, EnemyType.Flying, EnemyType.Tank
		};
		foreach (var t in pattern)
			_spawnQueue.Enqueue(t);

		_toSpawn = _spawnQueue.Count;
		_spawnEvery = 0.75f;
		_spawnTimer = 0f;
		_finishedSpawning = false;
	}

	private void SpawnEnemy()
	{
		float baseSize = Mathf.Min(WorldSize.X, WorldSize.Y);
		var start = _pathPx[0];

		var type = _spawnQueue.Count > 0 ? _spawnQueue.Dequeue() : EnemyType.Grunt;

		Enemy e = type switch
		{
			EnemyType.Fast => new Enemy
			{
				Type = EnemyType.Fast,
				MaxHp = 60, Hp = 60,
				Speed = baseSize * 0.30f,
				Pos = start, Segment = 0,
				Radius = baseSize * 0.018f,
				Reward = 6  // znerf: było 8
			},
			EnemyType.Tank => new Enemy
			{
				Type = EnemyType.Tank,
				MaxHp = 400, Hp = 400,
				Speed = baseSize * 0.09f,
				Pos = start, Segment = 0,
				Radius = baseSize * 0.030f,
				Reward = 18  // znerf: było 25
			},
			EnemyType.Flying => new Enemy
			{
				Type = EnemyType.Flying,
				MaxHp = 90, Hp = 90,
				Speed = baseSize * 0.20f,
				Pos = start, Segment = 0,
				Radius = baseSize * 0.020f,
				Reward = 10  // znerf: było 15
			},
			_ => new Enemy
			{
				Type = EnemyType.Grunt,
				MaxHp = 120, Hp = 120,
				Speed = baseSize * 0.16f,
				Pos = start, Segment = 0,
				Radius = baseSize * 0.022f,
				Reward = 7   // znerf: było 10
			}
		};

		_enemies.Add(e);
	}

	private void MovePath(Enemy e, float dt)
	{
		if (e.Type == EnemyType.Flying)
		{
			MoveFlying(e, dt);
			return;
		}

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
		Coins = Math.Max(0, Coins - 5);
		OnLeak?.Invoke();
		OnHudChanged?.Invoke();
	}

	private void MoveFlying(Enemy e, float dt)
	{
		float remaining = e.Speed * e.SlowMultiplier * dt;

		while (remaining > 0f && !e.ReachedEnd)
		{
			var dest = _pathPx[_pathPx.Count - 1];
			var toEnd = dest - e.Pos;
			float dist = toEnd.Length();

			if (dist <= remaining)
			{
				e.Pos = dest;
				ReachEnd(e);
				break;
			}
			else
			{
				e.Pos += toEnd / dist * remaining;
				remaining = 0f;
			}
		}
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
		TowerType.Frost  => 60,
		_ => 50
	};

	public static int GetUpgradeCost(TowerType type, int newLevel) => (type, newLevel) switch
	{
		(TowerType.Archer, 2) => 55,
		(TowerType.Archer, 3) => 75,
		(TowerType.Cannon, 2) => 85,
		(TowerType.Cannon, 3) => 110,
		(TowerType.Frost,  2) => 75,
		(TowerType.Frost,  3) => 95,
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
					Type = type, Pos = pos, Level = level,
					Range = rangeBase,
					Damage = 34 + (level - 1) * 18,
					FireInterval = 0.55f - (level - 1) * 0.06f,
					Cooldown = 0.05f, SplashRadius = 0,
					SlowMultiplier = 1.0f, SlowDuration = 0
				};

			case TowerType.Cannon:
				return new Tower
				{
					Type = type, Pos = pos, Level = level,
					Range = rangeBase * 0.95f,
					Damage = 55 + (level - 1) * 24,
					FireInterval = 0.95f - (level - 1) * 0.08f,
					Cooldown = 0.05f,
					SplashRadius = baseSize * (0.060f + (level - 1) * 0.015f),
					SlowMultiplier = 1.0f, SlowDuration = 0
				};

			case TowerType.Frost:
				return new Tower
				{
					Type = type, Pos = pos, Level = level,
					Range = rangeBase * 1.05f,
					Damage = 18 + (level - 1) * 10,
					FireInterval = 0.50f - (level - 1) * 0.05f,
					Cooldown = 0.05f, SplashRadius = 0,
					SlowMultiplier = 0.70f - (level - 1) * 0.05f,
					SlowDuration = 0.90f + (level - 1) * 0.25f
				};

			default:
				return CreateTower(TowerType.Archer, pos, level, worldSize);
		}
	}
}

public static class Maps
{
	public static readonly MapDef[] All =
	{
		new MapDef
		{
			Name = "Easy",
			Path = new []
			{
				new Vector2(0.50f, 0.10f),
				new Vector2(0.50f, 0.42f),
				new Vector2(0.75f, 0.55f),
				new Vector2(0.25f, 0.68f),
				new Vector2(0.50f, 0.92f),
			},
			Pads = new []
			{
				new PadDef(new Vector2(0.28f, 0.50f), 56),
				new PadDef(new Vector2(0.72f, 0.38f), 56),
				new PadDef(new Vector2(0.72f, 0.70f), 56),
				new PadDef(new Vector2(0.28f, 0.78f), 56),
			}
		},
		new MapDef
		{
			Name = "Medium",
			Path = new []
			{
				new Vector2(0.10f, 0.20f),
				new Vector2(0.55f, 0.20f),
				new Vector2(0.55f, 0.52f),
				new Vector2(0.20f, 0.70f),
				new Vector2(0.82f, 0.84f),
			},
			Pads = new []
			{
				new PadDef(new Vector2(0.32f, 0.34f), 56),
				new PadDef(new Vector2(0.70f, 0.36f), 56),
				new PadDef(new Vector2(0.38f, 0.62f), 56),
				new PadDef(new Vector2(0.70f, 0.68f), 56),
				new PadDef(new Vector2(0.50f, 0.84f), 56),
			}
		},
		new MapDef
		{
			Name = "Hard",
			Path = new []
			{
				new Vector2(0.90f, 0.12f),
				new Vector2(0.30f, 0.16f),
				new Vector2(0.30f, 0.40f),
				new Vector2(0.78f, 0.46f),
				new Vector2(0.78f, 0.70f),
				new Vector2(0.18f, 0.82f),
			},
			Pads = new []
			{
				new PadDef(new Vector2(0.62f, 0.24f), 56),
				new PadDef(new Vector2(0.44f, 0.32f), 56),
				new PadDef(new Vector2(0.56f, 0.58f), 56),
				new PadDef(new Vector2(0.34f, 0.62f), 56),
				new PadDef(new Vector2(0.58f, 0.82f), 56),
			}
		}
	};
}
