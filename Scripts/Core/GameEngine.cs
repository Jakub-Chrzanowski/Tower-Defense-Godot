using Godot;
using System;
using System.Collections.Generic;

public sealed class GameEngine
{
    public Vector2 WorldSize { get; private set; }

    public int Lives { get; private set; } = 20;
    public int Coins { get; private set; } = 120;
    public int WaveIndex { get; private set; } = 0;
    public bool WaveRunning { get; private set; } = false;
    public bool Victory { get; private set; } = false;
    public bool Defeat { get; private set; } = false;

    public TowerType SelectedTowerType { get; set; } = TowerType.Archer;
    public int? SelectedTowerIndex { get; private set; }

    private MapDef _map = MapDefs.Maps[0];

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

    // wave spawn
    private int _spawnGroupIndex;
    private int _spawnRemaining;
    private float _spawnEvery;
    private float _spawnTimer;
    private EnemyType _spawnType;
    private bool _waveFinishedSpawning;

    public event Action? OnVictory;
    public event Action? OnDefeat;
    public event Action? OnHudChanged;
    public event Action<int>? OnWaveChanged;

    public void LoadMap(int mapId)
    {
        _map = MapDefs.Maps[Math.Clamp(mapId, 0, MapDefs.Maps.Length - 1)];
    }

    public void Reset()
    {
        Lives = 20;
        Coins = 120;
        WaveIndex = 0;
        WaveRunning = false;
        Victory = false;
        Defeat = false;
        SelectedTowerIndex = null;

        _enemies.Clear();
        _towers.Clear();
        _projectiles.Clear();

        _pads.Clear();
        foreach (var p in _map.Pads)
            _pads.Add(new Pad { CenterNorm = p.CenterNorm, SizePx = p.SizePx, HasTower = false });

        PrepareWave();
        OnWaveChanged?.Invoke(WaveIndex + 1);
        OnHudChanged?.Invoke();
    }

    public void SetWorldSize(Vector2 size)
    {
        if (size.X <= 0 || size.Y <= 0) return;
        WorldSize = size;

        _pathPx.Clear();
        foreach (var wp in _map.Path)
            _pathPx.Add(new Vector2(wp.X * size.X, wp.Y * size.Y));
    }

    public void StartWave()
    {
        if (Defeat || Victory) return;
        if (WaveRunning) return;

        WaveRunning = true;
        _waveFinishedSpawning = false;
        _spawnTimer = 0f;
        OnHudChanged?.Invoke();
    }

    public void Update(float dt)
    {
        if (WorldSize.X <= 0 || WorldSize.Y <= 0) return;
        if (Defeat || Victory) return;

        // spawn
        if (WaveRunning && !_waveFinishedSpawning)
        {
            _spawnTimer -= dt;
            while (_spawnTimer <= 0f && _spawnRemaining > 0)
            {
                SpawnEnemy(_spawnType);
                _spawnRemaining--;
                _spawnTimer += _spawnEvery;
            }

            if (_spawnRemaining <= 0)
            {
                _spawnGroupIndex++;
                if (_spawnGroupIndex >= _map.Waves[WaveIndex].Groups.Count)
                {
                    _waveFinishedSpawning = true;
                }
                else
                {
                    var g = _map.Waves[WaveIndex].Groups[_spawnGroupIndex];
                    _spawnType = g.Type;
                    _spawnRemaining = g.Count;
                    _spawnEvery = g.Interval;
                    _spawnTimer = 0f;
                }
            }
        }

        // move
        foreach (var e in _enemies)
        {
            if (e.Hp <= 0 || e.ReachedEnd) continue;

            if (e.SlowTime > 0)
            {
                e.SlowTime -= dt;
                if (e.SlowTime <= 0) { e.SlowMul = 1f; e.SlowTime = 0f; }
            }

            if (e.Type == EnemyType.Flying) MoveFlying(e, dt);
            else MovePath(e, dt);
        }

        // towers
        for (int i = 0; i < _towers.Count; i++)
        {
            var t = _towers[i];
            t.Cooldown -= dt;
            if (t.Cooldown > 0f) continue;

            var target = FindTarget(t.Pos, t.Range);
            if (target is null) continue;

            Fire(t, target);
            t.Cooldown = t.FireInterval;
        }

        // projectiles (visual)
        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            var p = _projectiles[i];
            p.TimeLeft -= dt;
            p.Pos += p.Vel * dt;
            if (p.TimeLeft <= 0f) _projectiles.RemoveAt(i);
        }

        // cleanup dead
        int coinsBefore = Coins;
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            var e = _enemies[i];
            if (e.Hp <= 0)
            {
                Coins += e.Reward;
                _enemies.RemoveAt(i);
            }
        }
        if (Coins != coinsBefore) OnHudChanged?.Invoke();

        // end wave
        if (WaveRunning && _waveFinishedSpawning && _enemies.Count == 0)
        {
            WaveRunning = false;

            if (WaveIndex + 1 >= _map.Waves.Count)
            {
                Victory = true;
                OnHudChanged?.Invoke();
                OnVictory?.Invoke();
            }
            else
            {
                WaveIndex++;
                PrepareWave();
                OnWaveChanged?.Invoke(WaveIndex + 1);
                OnHudChanged?.Invoke();
            }
        }

        if (Lives <= 0 && !Defeat)
        {
            Defeat = true;
            OnHudChanged?.Invoke();
            OnDefeat?.Invoke();
        }
    }

    // input
    public bool TryPlaceTower(Vector2 clickPos)
    {
        if (Defeat || Victory) return false;
        if (WaveRunning) return false;

        for (int i = 0; i < _pads.Count; i++)
        {
            var pad = _pads[i];
            if (pad.HasTower) continue;

            var c = new Vector2(pad.CenterNorm.X * WorldSize.X, pad.CenterNorm.Y * WorldSize.Y);
            float half = pad.SizePx / 2f;
            var r = new Rect2(c.X - half, c.Y - half, pad.SizePx, pad.SizePx);
            if (!r.HasPoint(clickPos)) continue;

            int cost = TowerStats.GetCost(SelectedTowerType, 1);
            if (Coins < cost) return false;

            Coins -= cost;
            pad.HasTower = true;

            _towers.Add(TowerStats.Create(SelectedTowerType, c, 1, WorldSize));
            SelectedTowerIndex = _towers.Count - 1;
            OnHudChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool TrySelectTower(Vector2 clickPos)
    {
        SelectedTowerIndex = null;

        float hit = MathF.Min(WorldSize.X, WorldSize.Y) * 0.055f;
        for (int i = 0; i < _towers.Count; i++)
        {
            if (_towers[i].Pos.DistanceTo(clickPos) <= hit)
            {
                SelectedTowerIndex = i;
                OnHudChanged?.Invoke();
                return true;
            }
        }

        OnHudChanged?.Invoke();
        return false;
    }

    public bool UpgradeSelectedTower()
    {
        if (SelectedTowerIndex is null) return false;
        if (WaveRunning) return false;

        int idx = SelectedTowerIndex.Value;
        var t = _towers[idx];
        if (t.Level >= 3) return false;

        int next = t.Level + 1;
        int cost = TowerStats.GetCost(t.Type, next);
        if (Coins < cost) return false;

        Coins -= cost;
        _towers[idx] = TowerStats.Create(t.Type, t.Pos, next, WorldSize);
        SelectedTowerIndex = idx;
        OnHudChanged?.Invoke();
        return true;
    }

    public bool SellSelectedTower()
    {
        if (SelectedTowerIndex is null) return false;
        if (WaveRunning) return false;

        int idx = SelectedTowerIndex.Value;
        var t = _towers[idx];

        int spent = TowerStats.GetCost(t.Type, 1);
        if (t.Level >= 2) spent += TowerStats.GetCost(t.Type, 2);
        if (t.Level >= 3) spent += TowerStats.GetCost(t.Type, 3);
        int refund = (int)MathF.Round(spent * 0.6f);

        Coins += refund;

        // free nearest pad
        int padIdx = FindNearestPadIndex(t.Pos);
        if (padIdx >= 0) _pads[padIdx].HasTower = false;

        _towers.RemoveAt(idx);
        SelectedTowerIndex = null;
        OnHudChanged?.Invoke();
        return true;
    }

    private int FindNearestPadIndex(Vector2 pos)
    {
        int best = -1;
        float bestD = float.MaxValue;

        for (int i = 0; i < _pads.Count; i++)
        {
            var p = _pads[i];
            var c = new Vector2(p.CenterNorm.X * WorldSize.X, p.CenterNorm.Y * WorldSize.Y);
            float d = c.DistanceSquaredTo(pos);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    private void PrepareWave()
    {
        _spawnGroupIndex = 0;
        var g = _map.Waves[WaveIndex].Groups[_spawnGroupIndex];
        _spawnType = g.Type;
        _spawnRemaining = g.Count;
        _spawnEvery = g.Interval;
        _spawnTimer = 0f;
        _waveFinishedSpawning = false;
    }

    private void Fire(Tower t, Enemy target)
    {
        AddProjectile(t.Pos, target.Pos);

        if (t.Type == TowerType.Cannon)
        {
            float r = t.SplashRadius;
            foreach (var e in _enemies)
            {
                if (e.Hp <= 0 || e.ReachedEnd) continue;
                if (e.Pos.DistanceTo(target.Pos) <= r)
                    e.Hp -= t.Damage;
            }
        }
        else if (t.Type == TowerType.Frost)
        {
            target.Hp -= t.Damage;
            target.SlowMul = MathF.Min(target.SlowMul, t.SlowMul);
            target.SlowTime = MathF.Max(target.SlowTime, t.SlowTime);
        }
        else
        {
            target.Hp -= t.Damage;
        }
    }

    private void AddProjectile(Vector2 from, Vector2 to)
    {
        var dir = (to - from);
        float len = dir.Length();
        if (len < 1f) return;
        dir /= len;

        float speed = MathF.Min(WorldSize.X, WorldSize.Y) * 0.9f;

        _projectiles.Add(new Projectile
        {
            Pos = from,
            Vel = dir * speed,
            TimeLeft = 0.14f
        });
    }

    private Enemy? FindTarget(Vector2 from, float range)
    {
        Enemy? best = null;
        float bestD = float.MaxValue;
        float rangeSq = range * range;

        foreach (var e in _enemies)
        {
            if (e.Hp <= 0 || e.ReachedEnd) continue;

            float d = e.Pos.DistanceSquaredTo(from);
            if (d > rangeSq) continue;
            if (d < bestD) { bestD = d; best = e; }
        }
        return best;
    }

    private void MovePath(Enemy e, float dt)
    {
        if (_pathPx.Count < 2) return;

        float remaining = e.Speed * e.SlowMul * dt;

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

    private void MoveFlying(Enemy e, float dt)
    {
        var end = _pathPx[^1];
        var to = end - e.Pos;
        float dist = to.Length();

        if (dist < 2f) { ReachEnd(e); return; }

        e.Pos += to / dist * (e.Speed * e.SlowMul * dt);
    }

    private void ReachEnd(Enemy e)
    {
        e.ReachedEnd = true;
        Lives -= EnemyStats.GetLeakDamage(e.Type);
        OnHudChanged?.Invoke();
    }

    private void SpawnEnemy(EnemyType type)
    {
        var start = _pathPx[0];
        float baseSize = MathF.Min(WorldSize.X, WorldSize.Y);
        var s = EnemyStats.Get(type);

        _enemies.Add(new Enemy
        {
            Type = type,
            MaxHp = s.Hp,
            Hp = s.Hp,
            Speed = s.SpeedMul * baseSize * 0.16f,
            Pos = start,
            Segment = 0,
            Radius = baseSize * s.RadiusMul,
            Reward = s.Reward
        });
    }
}

// ===== data =====
public sealed class WaveDef
{
    public readonly List<SpawnGroup> Groups = new();
}
public readonly record struct SpawnGroup(EnemyType Type, int Count, float Interval);

public sealed class MapDef
{
    public required Vector2[] Path; // normalized
    public required PadDef[] Pads;
    public required List<WaveDef> Waves;
}
public readonly record struct PadDef(Vector2 CenterNorm, float SizePx);
public sealed class Pad
{
    public Vector2 CenterNorm;
    public float SizePx;
    public bool HasTower;
}

public static class MapDefs
{
    public static readonly MapDef[] Maps = new[]
    {
        new MapDef
        {
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
            },
            Waves = WaveFactory.CreateEasy()
        },
        new MapDef
        {
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
            },
            Waves = WaveFactory.CreateMedium()
        },
        new MapDef
        {
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
            },
            Waves = WaveFactory.CreateHard()
        }
    };
}

public static class WaveFactory
{
    public static List<WaveDef> CreateEasy()
    {
        var waves = new List<WaveDef>();
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Grunt, 10, 0.7f) } });
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Grunt, 12, 0.6f), new SpawnGroup(EnemyType.Fast, 6, 0.55f) } });
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Tank, 5, 1.1f), new SpawnGroup(EnemyType.Fast, 10, 0.5f) } });
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Flying, 8, 0.75f), new SpawnGroup(EnemyType.Grunt, 14, 0.55f) } });
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Boss, 1, 2.0f), new SpawnGroup(EnemyType.Grunt, 10, 0.55f) } });
        return waves;
    }

    public static List<WaveDef> CreateMedium()
    {
        var waves = new List<WaveDef>();
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Grunt, 14, 0.55f) } });
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Fast, 12, 0.48f), new SpawnGroup(EnemyType.Grunt, 10, 0.55f) } });
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Tank, 8, 1.0f), new SpawnGroup(EnemyType.Fast, 12, 0.45f) } });
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Flying, 10, 0.65f), new SpawnGroup(EnemyType.Tank, 6, 1.0f) } });
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Boss, 1, 2.0f), new SpawnGroup(EnemyType.Flying, 10, 0.6f) } });
        return waves;
    }

    public static List<WaveDef> CreateHard()
    {
        var waves = new List<WaveDef>();
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Grunt, 18, 0.48f) } });
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Fast, 18, 0.40f), new SpawnGroup(EnemyType.Grunt, 10, 0.48f) } });
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Tank, 10, 0.95f), new SpawnGroup(EnemyType.Flying, 10, 0.58f) } });
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Flying, 14, 0.52f), new SpawnGroup(EnemyType.Fast, 16, 0.38f) } });
        waves.Add(new WaveDef { Groups = { new SpawnGroup(EnemyType.Boss, 1, 2.0f), new SpawnGroup(EnemyType.Tank, 10, 0.9f), new SpawnGroup(EnemyType.Flying, 10, 0.55f) } });
        return waves;
    }
}

public readonly record struct EnemyStat(float Hp, float SpeedMul, float RadiusMul, int Reward);

public static class EnemyStats
{
    public static EnemyStat Get(EnemyType t) => t switch
    {
        EnemyType.Grunt => new EnemyStat(120, 1.00f, 0.022f, 4),
        EnemyType.Fast  => new EnemyStat(90,  1.35f, 0.020f, 4),
        EnemyType.Tank  => new EnemyStat(280, 0.75f, 0.026f, 8),
        EnemyType.Flying=> new EnemyStat(140, 1.10f, 0.021f, 6),
        EnemyType.Boss  => new EnemyStat(1300,0.85f, 0.036f, 35),
        _ => new EnemyStat(120, 1.00f, 0.022f, 4)
    };

    public static int GetLeakDamage(EnemyType t) => t switch
    {
        EnemyType.Grunt => 1,
        EnemyType.Fast => 1,
        EnemyType.Tank => 2,
        EnemyType.Flying => 1,
        EnemyType.Boss => 8,
        _ => 1
    };
}

public static class TowerStats
{
    public static int GetCost(TowerType t, int level) => (t, level) switch
    {
        (TowerType.Archer, 1) => 40,
        (TowerType.Archer, 2) => 55,
        (TowerType.Archer, 3) => 75,

        (TowerType.Cannon, 1) => 70,
        (TowerType.Cannon, 2) => 90,
        (TowerType.Cannon, 3) => 120,

        (TowerType.Frost, 1) => 60,
        (TowerType.Frost, 2) => 80,
        (TowerType.Frost, 3) => 110,
        _ => 50
    };

    public static Tower Create(TowerType type, Vector2 pos, int level, Vector2 worldSize)
    {
        float baseSize = MathF.Min(worldSize.X, worldSize.Y);
        float range = baseSize * 0.22f + (level - 1) * baseSize * 0.03f;

        return type switch
        {
            TowerType.Archer => new Tower
            {
                Type = type, Pos = pos, Level = level,
                Range = range,
                Damage = 34 + (level - 1) * 18,
                FireInterval = 0.55f - (level - 1) * 0.06f,
                Cooldown = 0.05f,
                SplashRadius = 0,
                SlowMul = 1,
                SlowTime = 0
            },

            TowerType.Cannon => new Tower
            {
                Type = type, Pos = pos, Level = level,
                Range = range * 0.95f,
                Damage = 55 + (level - 1) * 28,
                FireInterval = 0.95f - (level - 1) * 0.08f,
                Cooldown = 0.05f,
                SplashRadius = baseSize * (0.06f + (level - 1) * 0.015f),
                SlowMul = 1,
                SlowTime = 0
            },

            TowerType.Frost => new Tower
            {
                Type = type, Pos = pos, Level = level,
                Range = range * 1.05f,
                Damage = 18 + (level - 1) * 10,
                FireInterval = 0.50f - (level - 1) * 0.05f,
                Cooldown = 0.05f,
                SplashRadius = 0,
                SlowMul = 0.65f - (level - 1) * 0.05f,
                SlowTime = 0.9f + (level - 1) * 0.35f
            },

            _ => new Tower { Type = type, Pos = pos, Level = level, Range = range, Damage = 30, FireInterval = 0.7f, Cooldown = 0.05f }
        };
    }
}
