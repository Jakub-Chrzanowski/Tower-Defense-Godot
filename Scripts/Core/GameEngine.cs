using Godot;
using System;
using System.Collections.Generic;

public sealed class GameEngine
{
	public Vector2 WorldSize { get; private set; }

   
	public int Lives { get; private set; } = 3;


	public bool WaveRunning { get; private set; }
	public bool Victory { get; private set; }
	public bool Defeat { get; private set; }

  
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
		if (Defeat || Victory) return;
		if (WaveRunning) return;

		WaveRunning = true;
		OnHudChanged?.Invoke();
	}

	public bool TryPlaceTower(Vector2 clickPos)
	{
		if (Defeat || Victory) return false;
		if (WaveRunning) return false; 

		foreach (var pad in _pads)
		{
			if (pad.HasTower) continue;

			var c = new Vector2(pad.CenterNorm.X * WorldSize.X, pad.CenterNorm.Y * WorldSize.Y);
			float half = pad.SizePx / 2f;
			var r = new Rect2(c.X - half, c.Y - half, pad.SizePx, pad.SizePx);
			if (!r.HasPoint(clickPos)) continue;

			pad.HasTower = true;

			float baseSize = Mathf.Min(WorldSize.X, WorldSize.Y);
			_towers.Add(new Tower
			{
				Pos = c,
				Range = baseSize * 0.22f,
				Damage = 34f,
				FireInterval = 0.55f,
				Cooldown = 0.05f
			});

			OnHudChanged?.Invoke();
			return true;
		}

		return false;
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

	 
		foreach (var e in _enemies)
		{
			if (e.Hp <= 0 || e.ReachedEnd) continue;
			MovePath(e, dt);
		}

	
		foreach (var t in _towers)
		{
			t.Cooldown -= dt;
			if (t.Cooldown > 0f) continue;

			var target = FindTarget(t.Pos, t.Range);
			if (target == null) continue;

			
			target.Hp -= t.Damage;
			t.Cooldown = t.FireInterval;

			
			AddProjectile(t.Pos, target.Pos);
		}

	 
		for (int i = _projectiles.Count - 1; i >= 0; i--)
		{
			var p = _projectiles[i];
			p.TimeLeft -= dt;
			p.Pos += p.Vel * dt;
			if (p.TimeLeft <= 0f) _projectiles.RemoveAt(i);
		}

	   
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

	private void PrepareSingleWave()
	{
		_toSpawn = 10;
		_spawnEvery = 0.8f;
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
		float remaining = e.Speed * dt;

		while (remaining > 0f && !e.ReachedEnd)
		{
			var a = _pathPx[e.Segment];
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

	private void AddProjectile(Vector2 from, Vector2 to)
	{
		var dir = (to - from);
		if (dir.Length() < 1f) return;

		dir = dir.Normalized();
		float speed = Mathf.Min(WorldSize.X, WorldSize.Y) * 0.9f;

		_projectiles.Add(new Projectile
		{
			Pos = from,
			Vel = dir * speed,
			TimeLeft = 0.15f	
		});
	}
}
