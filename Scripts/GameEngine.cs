using Godot;
using System;
using System.Collections.Generic;

public sealed class GameEngine
{
	public Vector2 WorldSize { get; private set; }

	public int Lives           { get; private set; } = 3;
	public int Coins           { get; private set; } = 120;
	public TowerType SelectedTowerType  { get; set; } = TowerType.Archer;
	public int? SelectedTowerIndex      { get; private set; }
	public bool WaveRunning    { get; private set; }
	public bool Victory        { get; private set; }
	public bool Defeat         { get; private set; }

	// Boss (E2)
	public Enemy? Boss         { get; private set; }
	public bool BossSpawned    { get; private set; }
	public bool BossDefeated   { get; private set; }

	private MapDef _map = Maps.All[0];
	private int    _mapId = 0;

	private readonly List<Vector2> _pathPx = new();
	public IReadOnlyList<Vector2> PathPx => _pathPx;

	private readonly List<Pad>        _pads        = new();
	public IReadOnlyList<Pad>        Pads        => _pads;

	private readonly List<Enemy>      _enemies     = new();
	public IReadOnlyList<Enemy>      Enemies     => _enemies;

	private readonly List<Tower>      _towers      = new();
	public IReadOnlyList<Tower>      Towers      => _towers;

	private readonly List<Projectile> _projectiles = new();
	public IReadOnlyList<Projectile> Projectiles => _projectiles;

	private int   _toSpawn;
	private float _spawnEvery;
	private float _spawnTimer;
	private bool  _finishedSpawning;


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
	public event Action? OnBossSpawned;
	public event Action? OnBossDefeated;

	private readonly HashSet<Enemy> _rewardedEnemies = new();

	// ── Konfiguracja trudności per mapa ──────────────────────────────
	private static readonly WaveCfg[] DifficultyByMap = new[]
	{
		// Easy  – mało wrogów, słabsi, więcej czasu
		new WaveCfg(
			pattern: new[] {
				EnemyType.Grunt, EnemyType.Fast, EnemyType.Grunt,
				EnemyType.Grunt, EnemyType.Fast, EnemyType.Grunt
			},
			spawnInterval: 1.10f,
			hpMult:        0.70f,
			speedMult:     0.80f,
			rewardMult:    1.0f
		),
		// Medium – standardowo
		new WaveCfg(
			pattern: new[] {
				EnemyType.Grunt, EnemyType.Fast, EnemyType.Grunt,
				EnemyType.Tank,  EnemyType.Fast, EnemyType.Grunt,
				EnemyType.Flying, EnemyType.Tank
			},
			spawnInterval: 0.85f,
			hpMult:        1.00f,
			speedMult:     1.00f,
			rewardMult:    1.0f
		),
		// Hard  – dużo wrogów, twardsi, szybciej
		new WaveCfg(
			pattern: new[] {
				EnemyType.Grunt, EnemyType.Fast,   EnemyType.Grunt,
				EnemyType.Tank,  EnemyType.Fast,   EnemyType.Grunt,
				EnemyType.Flying, EnemyType.Tank,  EnemyType.Fast,
				EnemyType.Grunt, EnemyType.Flying, EnemyType.Tank,
				EnemyType.Fast,  EnemyType.Flying
			},
			spawnInterval: 0.60f,
			hpMult:        1.40f,
			speedMult:     1.20f,
			rewardMult:    1.0f
		),
	};

	private sealed class WaveCfg
	{
		public readonly EnemyType[] Pattern;
		public readonly float SpawnInterval;
		public readonly float HpMult;
		public readonly float SpeedMult;
		public readonly float RewardMult;
		public WaveCfg(EnemyType[] pattern, float spawnInterval,
					   float hpMult, float speedMult, float rewardMult)
		{
			Pattern       = pattern;
			SpawnInterval = spawnInterval;
			HpMult        = hpMult;
			SpeedMult     = speedMult;
			RewardMult    = rewardMult;
		}
	}

	// ── Init ──────────────────────────────────────────────────────────
	public void LoadMap(int mapId)
	{
		_mapId = Math.Clamp(mapId, 0, Maps.All.Length - 1);
		_map   = Maps.All[_mapId];
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
		Lives              = 3;
		Coins              = GameSession.PersistentCoins;
		SelectedTowerType  = TowerType.Archer;
		SelectedTowerIndex = null;
		WaveRunning        = false;
		Victory            = false;
		Defeat             = false;
		Boss               = null;
		BossSpawned        = false;
		BossDefeated       = false;

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

	// ── Akcje gracza ──────────────────────────────────────────────────
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
			var c    = new Vector2(pad.CenterNorm.X * WorldSize.X, pad.CenterNorm.Y * WorldSize.Y);
			float half = pad.SizePx / 2f;
			var r    = new Rect2(c.X - half, c.Y - half, pad.SizePx, pad.SizePx);
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

		var old      = _towers[idx];
		if (old.Level >= 3) return false;
		int newLevel = old.Level + 1;
		int cost     = TowerBalance.GetUpgradeCost(old.Type, newLevel);
		if (Coins < cost) return false;

		Coins -= cost;
		_towers[idx]       = TowerBalance.CreateTower(old.Type, old.Pos, newLevel, WorldSize);
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

		var tower  = _towers[idx];
		int refund = TowerBalance.GetSellRefund(tower.Type, tower.Level);
		Coins     += refund;

		int padIdx = FindNearestPadIndex(tower.Pos);
		if (padIdx >= 0) _pads[padIdx].HasTower = false;

		_towers.RemoveAt(idx);
		SelectedTowerIndex = null;
		OnTowerSold?.Invoke();
		OnHudChanged?.Invoke();
		return true;
	}

	public void SaveCoinsToSession() => GameSession.PersistentCoins = Coins;

	// ── Update ────────────────────────────────────────────────────────
	public void Update(float dt)
	{
		if (WorldSize.X <= 0 || WorldSize.Y <= 0) return;
		if (Defeat || Victory) return;
		if (_pathPx.Count < 2) return;

		// Spawn normalnych wrogów
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

		// Spawn bossa gdy wszyscy normalni zginęli/doszli do końca
		if (WaveRunning && _finishedSpawning && !BossSpawned && _enemies.Count == 0)
		{
			SpawnBoss();
		}

		// Ruch i slow wrogów
		foreach (var e in _enemies)
		{
			if (e.Hp <= 0 || e.ReachedEnd) continue;
			TickSlow(e, dt);
			MovePath(e, dt);
		}

		// Wieże strzelają
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

		// Pociski
		for (int i = _projectiles.Count - 1; i >= 0; i--)
		{
			var p = _projectiles[i];
			p.TimeLeft -= dt;
			p.Pos      += p.Vel * dt;
			if (p.TimeLeft <= 0f) _projectiles.RemoveAt(i);
		}

		// Nagrody za zabicia + usuwanie martwych
		for (int i = _enemies.Count - 1; i >= 0; i--)
		{
			var e = _enemies[i];
			if (e.Hp <= 0 && !_rewardedEnemies.Contains(e))
			{
				_rewardedEnemies.Add(e);
				Coins += e.Reward;
				if (e == Boss)
				{
					BossDefeated = true;
					Coins       += BossKillBonus();
					OnBossDefeated?.Invoke();
				}
				OnEnemyKilled?.Invoke();
				OnHudChanged?.Invoke();
			}
			if (e.Hp <= 0 || e.ReachedEnd)
			{
				if (e == Boss && e.ReachedEnd)
					BossDefeated = true; // boss dotarł do końca – traktujemy jak pokonanego dla flow
				_enemies.RemoveAt(i);
			}
		}

		// Defeat
		if (Lives <= 0 && !Defeat)
		{
			Defeat      = true;
			WaveRunning = false;
			OnHudChanged?.Invoke();
			OnDefeat?.Invoke();
			return;
		}

		// Victory – wszyscy (+ boss) pokonani / doszli do końca
		bool waveOver = WaveRunning && _finishedSpawning && BossSpawned && _enemies.Count == 0;
		if (waveOver && !Victory)
		{
			Victory     = true;
			WaveRunning = false;
			Coins      += WaveEndBonus();
			SaveCoinsToSession();
			OnWaveReward?.Invoke();
			OnHudChanged?.Invoke();
			OnVictory?.Invoke();
		}
	}

	// ── Boss (E2) ─────────────────────────────────────────────────────
	private void SpawnBoss()
	{
		float baseSize = Mathf.Min(WorldSize.X, WorldSize.Y);
		var   cfg      = GetDiffCfg();

		// Boss – 8× HP grunta, wolny ale odporny, duży promień
		var boss = new Enemy
		{
			Type        = EnemyType.Boss,
			MaxHp       = 960 * cfg.HpMult,
			Hp          = 960 * cfg.HpMult,
			Speed       = baseSize * 0.11f * cfg.SpeedMult,
			Pos         = _pathPx[0],
			Segment     = 0,
			Radius      = baseSize * 0.045f,
			Reward      = (int)(50 * cfg.RewardMult),
			IsBoss      = true,
		};

		Boss        = boss;
		BossSpawned = true;
		_enemies.Add(boss);
		OnBossSpawned?.Invoke();
		OnHudChanged?.Invoke();
	}

	private int BossKillBonus() => _mapId switch { 0 => 60, 1 => 90, 2 => 130, _ => 60 };
	private int WaveEndBonus()  => _mapId switch { 0 => 40, 1 => 60, 2 => 90,  _ => 40 };

	// ── Spawn normalnych wrogów ───────────────────────────────────────
	private readonly Queue<EnemyType> _spawnQueue = new();

	private WaveCfg GetDiffCfg()
	{
		int idx = Math.Clamp(_mapId, 0, DifficultyByMap.Length - 1);
		return DifficultyByMap[idx];
	}

	private void PrepareSingleWave()
	{
		_spawnQueue.Clear();
		var cfg = GetDiffCfg();
		foreach (var t in cfg.Pattern)
			_spawnQueue.Enqueue(t);

		_toSpawn         = _spawnQueue.Count;
		_spawnEvery      = cfg.SpawnInterval;
		_spawnTimer      = 0f;
		_finishedSpawning = false;
		BossSpawned      = false;
		BossDefeated     = false;
		Boss             = null;
	}

	private void SpawnEnemy()
	{
		float bs  = Mathf.Min(WorldSize.X, WorldSize.Y);
		var   cfg = GetDiffCfg();
		var   start = _pathPx[0];
		var   type  = _spawnQueue.Count > 0 ? _spawnQueue.Dequeue() : EnemyType.Grunt;

		Enemy e = type switch
		{
			EnemyType.Fast => new Enemy {
				Type = EnemyType.Fast,
				MaxHp = 60 * cfg.HpMult, Hp = 60 * cfg.HpMult,
				Speed = bs * 0.30f * cfg.SpeedMult,
				Pos = start, Segment = 0,
				Radius = bs * 0.018f,
				Reward = (int)(6 * cfg.RewardMult)
			},
			EnemyType.Tank => new Enemy {
				Type = EnemyType.Tank,
				MaxHp = 400 * cfg.HpMult, Hp = 400 * cfg.HpMult,
				Speed = bs * 0.09f * cfg.SpeedMult,
				Pos = start, Segment = 0,
				Radius = bs * 0.030f,
				Reward = (int)(18 * cfg.RewardMult)
			},
			EnemyType.Flying => new Enemy {
				Type = EnemyType.Flying,
				MaxHp = 90 * cfg.HpMult, Hp = 90 * cfg.HpMult,
				Speed = bs * 0.20f * cfg.SpeedMult,
				Pos = start, Segment = 0,
				Radius = bs * 0.020f,
				Reward = (int)(10 * cfg.RewardMult)
			},
			_ => new Enemy {
				Type = EnemyType.Grunt,
				MaxHp = 120 * cfg.HpMult, Hp = 120 * cfg.HpMult,
				Speed = bs * 0.16f * cfg.SpeedMult,
				Pos = start, Segment = 0,
				Radius = bs * 0.022f,
				Reward = (int)(7 * cfg.RewardMult)
			}
		};

		_enemies.Add(e);
	}

	// ── Ruch ──────────────────────────────────────────────────────────
	private static void TickSlow(Enemy e, float dt)
	{
		if (e.SlowTimeLeft <= 0) return;
		e.SlowTimeLeft -= dt;
		if (e.SlowTimeLeft <= 0) { e.SlowTimeLeft = 0; e.SlowMultiplier = 1.0f; }
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
			var   b    = _pathPx[e.Segment + 1];
			var   toB  = b - e.Pos;
			float dist = toB.Length();

			if (dist <= remaining)
			{
				e.Pos = b;
				remaining -= dist;
				e.Segment++;
				if (e.Segment >= _pathPx.Count - 1) { ReachEnd(e); break; }
			}
			else
			{
				e.Pos     += toB / dist * remaining;
				remaining  = 0f;
			}
		}
	}

	private void MoveFlying(Enemy e, float dt)
	{
		float remaining = e.Speed * e.SlowMultiplier * dt;
		while (remaining > 0f && !e.ReachedEnd)
		{
			var   dest  = _pathPx[_pathPx.Count - 1];
			var   toEnd = dest - e.Pos;
			float dist  = toEnd.Length();
			if (dist <= remaining) { e.Pos = dest; ReachEnd(e); break; }
			else { e.Pos += toEnd / dist * remaining; remaining = 0f; }
		}
	}

	private void ReachEnd(Enemy e)
	{
		e.ReachedEnd = true;
		if (e.IsBoss)
		{
			// Boss dotarł do końca – natychmiastowa przegrana
			Lives = 0;
		}
		else
		{
			Lives = Math.Max(0, Lives - 1);
			Coins = Math.Max(0, Coins - 5);
		}
		OnLeak?.Invoke();
		OnHudChanged?.Invoke();
	}

	// ── Wieże / strzały ───────────────────────────────────────────────
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
				target.Hp            -= tower.Damage;
				target.SlowMultiplier = MathF.Min(target.SlowMultiplier, tower.SlowMultiplier);
				target.SlowTimeLeft   = MathF.Max(target.SlowTimeLeft,   tower.SlowDuration);
				break;
		}
	}

	private Enemy? FindTarget(Vector2 from, float range)
	{
		Enemy? best     = null;
		float  bestDist = float.MaxValue;
		foreach (var e in _enemies)
		{
			if (e.Hp <= 0 || e.ReachedEnd) continue;
			float d = e.Pos.DistanceSquaredTo(from);
			if (d > range * range) continue;
			if (d < bestDist) { bestDist = d; best = e; }
		}
		return best;
	}

	private void AddProjectile(Vector2 from, Vector2 to, TowerType sourceType)
	{
		var dir = to - from;
		if (dir.Length() < 1f) return;
		float speed = Mathf.Min(WorldSize.X, WorldSize.Y) * 0.90f;
		_projectiles.Add(new Projectile
		{
			Pos        = from,
			Vel        = dir.Normalized() * speed,
			TimeLeft   = 0.14f,
			SourceType = sourceType
		});
	}

	private int FindNearestPadIndex(Vector2 pos)
	{
		int   bestIdx = -1;
		float best    = float.MaxValue;
		for (int i = 0; i < _pads.Count; i++)
		{
			var   c = new Vector2(_pads[i].CenterNorm.X * WorldSize.X, _pads[i].CenterNorm.Y * WorldSize.Y);
			float d = c.DistanceSquaredTo(pos);
			if (d < best) { best = d; bestIdx = i; }
		}
		return bestIdx;
	}
}

// ── TowerBalance ──────────────────────────────────────────────────────
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
		(TowerType.Archer, 2) => 55,  (TowerType.Archer, 3) => 75,
		(TowerType.Cannon, 2) => 85,  (TowerType.Cannon, 3) => 110,
		(TowerType.Frost,  2) => 75,  (TowerType.Frost,  3) => 95,
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
		float bs        = Mathf.Min(worldSize.X, worldSize.Y);
		float rangeBase = bs * 0.22f + (level - 1) * bs * 0.03f;

		return type switch
		{
			TowerType.Archer => new Tower {
				Type = type, Pos = pos, Level = level,
				Range = rangeBase,
				Damage = 34 + (level - 1) * 18,
				FireInterval = 0.55f - (level - 1) * 0.06f,
				Cooldown = 0.05f, SplashRadius = 0,
				SlowMultiplier = 1.0f, SlowDuration = 0
			},
			TowerType.Cannon => new Tower {
				Type = type, Pos = pos, Level = level,
				Range = rangeBase * 0.95f,
				Damage = 55 + (level - 1) * 24,
				FireInterval = 0.95f - (level - 1) * 0.08f,
				Cooldown = 0.05f,
				SplashRadius = bs * (0.060f + (level - 1) * 0.015f),
				SlowMultiplier = 1.0f, SlowDuration = 0
			},
			TowerType.Frost => new Tower {
				Type = type, Pos = pos, Level = level,
				Range = rangeBase * 1.05f,
				Damage = 18 + (level - 1) * 10,
				FireInterval = 0.50f - (level - 1) * 0.05f,
				Cooldown = 0.05f, SplashRadius = 0,
				SlowMultiplier = 0.70f - (level - 1) * 0.05f,
				SlowDuration = 0.90f + (level - 1) * 0.25f
			},
			_ => CreateTower(TowerType.Archer, pos, level, worldSize)
		};
	}
}

// ── Mapy ──────────────────────────────────────────────────────────────
public static class Maps
{
	public static readonly MapDef[] All =
	{
		new MapDef {
			Name = "Easy",
			Path = new[] {
				new Vector2(0.50f, 0.10f), new Vector2(0.50f, 0.42f),
				new Vector2(0.75f, 0.55f), new Vector2(0.25f, 0.68f),
				new Vector2(0.50f, 0.92f),
			},
			Pads = new[] {
				new PadDef(new Vector2(0.28f, 0.50f), 56),
				new PadDef(new Vector2(0.72f, 0.38f), 56),
				new PadDef(new Vector2(0.72f, 0.70f), 56),
				new PadDef(new Vector2(0.28f, 0.78f), 56),
			}
		},
		new MapDef {
			Name = "Medium",
			Path = new[] {
				new Vector2(0.10f, 0.20f), new Vector2(0.55f, 0.20f),
				new Vector2(0.55f, 0.52f), new Vector2(0.20f, 0.70f),
				new Vector2(0.82f, 0.84f),
			},
			Pads = new[] {
				new PadDef(new Vector2(0.32f, 0.34f), 56),
				new PadDef(new Vector2(0.70f, 0.36f), 56),
				new PadDef(new Vector2(0.38f, 0.62f), 56),
				new PadDef(new Vector2(0.70f, 0.68f), 56),
				new PadDef(new Vector2(0.50f, 0.84f), 56),
			}
		},
		new MapDef {
			Name = "Hard",
			Path = new[] {
				new Vector2(0.90f, 0.12f), new Vector2(0.30f, 0.16f),
				new Vector2(0.30f, 0.40f), new Vector2(0.78f, 0.46f),
				new Vector2(0.78f, 0.70f), new Vector2(0.18f, 0.82f),
			},
			Pads = new[] {
				new PadDef(new Vector2(0.62f, 0.24f), 56),
				new PadDef(new Vector2(0.44f, 0.32f), 56),
				new PadDef(new Vector2(0.56f, 0.58f), 56),
				new PadDef(new Vector2(0.34f, 0.62f), 56),
				new PadDef(new Vector2(0.58f, 0.82f), 56),
			}
		}
	};
}
