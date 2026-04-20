using Godot;
using System.Collections.Generic;

public partial class GameController : Node2D
{
	GameEngine engine = new();

<<<<<<< HEAD
	PackedScene enemyScene =
		GD.Load<PackedScene>("res://Scenes/Enemy.tscn");

	List<Enemy> alive = new();
=======
	private Texture2D? _enemy;
	private Texture2D? _towerArcher;
	private Texture2D? _towerCannon;
	private Texture2D? _towerFrost;
	private Texture2D? _projArrow;
	private Texture2D? _projCannon;
	private Texture2D? _projFrost;

	private Hud? _hud;

	private readonly Dictionary<string, AudioStream?> _audioCache = new();
>>>>>>> parent of c1670f3 (Merge branch 'main' of https://github.com/Jakub-Chrzanowski/Tower-Defense-Godot)

	public async void StartWave()
	{
		if (engine.WaveRunning)
			return;

		engine.StartWave();

		int count = 5 + engine.WaveIndex * 2;

		for (int i = 0; i < count; i++)
		{
			SpawnEnemy();

			await ToSignal(
				GetTree().CreateTimer(0.8f),
				"timeout");
		}

<<<<<<< HEAD
		// czekaj aż wszyscy umrą
		while (alive.Count > 0)
=======
		_hud.Bind(this);

		_engine.OnHudChanged += () => _hud?.Refresh(_engine);
		_engine.OnVictory += () =>
		{
			_hud?.ShowVictory();
			PlaySfx("res://assets/sfx/sfx_victory.wav", -4);
		};
		_engine.OnDefeat += () =>
		{
			_hud?.ShowDefeat();
			PlaySfx("res://assets/sfx/sfx_defeat.wav", -4);
		};

		_engine.OnTowerBuilt += () => PlaySfx("res://assets/sfx/sfx_build.wav", -8);
		_engine.OnTowerUpgraded += () => PlaySfx("res://assets/sfx/sfx_upgrade.wav", -7);
		_engine.OnTowerSold += () => PlaySfx("res://assets/sfx/sfx_sell.wav", -8);
		_engine.OnLeak += () => PlaySfx("res://assets/sfx/sfx_leak.wav", -6);
		_engine.OnShot += () => PlaySfx("res://assets/sfx/sfx_shot.wav", -14);

		LoadTextures();

		_engine.SetWorldSize(GetViewportRect().Size);
		_engine.Reset();
		_hud.Refresh(_engine);
	}

	public override void _Notification(int what)
	{
		
		if (what == 1008)
>>>>>>> parent of c1670f3 (Merge branch 'main' of https://github.com/Jakub-Chrzanowski/Tower-Defense-Godot)
		{
			await ToSignal(
				GetTree().CreateTimer(0.5f),
				"timeout");
		}

		engine.EndWave();

		GD.Print("Wave: " + engine.WaveIndex +
				 " Gold: " + engine.Gold);
	}

	void SpawnEnemy()
	{
		var e = enemyScene.Instantiate<Enemy>();

		e.Setup(engine.WaveIndex);

		AddChild(e);
		alive.Add(e);
	}

	public void OnEnemyKilled(Enemy e)
	{
<<<<<<< HEAD
		engine.EnemyKilled(e.Reward);
		alive.Remove(e);
=======
		if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			var pos = ToLocal(mb.Position);

			// najpierw próba zaznaczenia istniejącej wieży, potem budowanie
			if (!_engine.TrySelectTower(pos))
				_engine.TryPlaceTower(pos);
		}
	}
>>>>>>> parent of c1670f3 (Merge branch 'main' of https://github.com/Jakub-Chrzanowski/Tower-Defense-Godot)

		// SFX kill
		var sfx = GetNodeOrNull<AudioStreamPlayer>("SfxKill");
		sfx?.Play();
	}
	private void _on_start_wave_button_pressed()
	{
<<<<<<< HEAD
		StartWave();
	}	
=======
		_engine.SelectedTowerType = type;
		_hud?.Refresh(_engine);
		PlaySfx("res://assets/sfx/sfx_click.wav", -12);
	}

	public void StartWave()
	{
		_engine.StartWave();
		PlaySfx("res://assets/sfx/sfx_click.wav", -12);
	}

	public void UpgradeSelected()
	{
		if (_engine.UpgradeSelectedTower())
			PlaySfx("res://assets/sfx/sfx_upgrade.wav", -7);
	}

	public void SellSelected()
	{
		if (_engine.SellSelectedTower())
			PlaySfx("res://assets/sfx/sfx_sell.wav", -8);
	}

	public void RestartLevel()
	{
		_engine.Reset();
		_hud?.Refresh(_engine);
		PlaySfx("res://assets/sfx/sfx_click.wav", -12);
	}

	public void BackToMenu()
	{
		PlaySfx("res://assets/sfx/sfx_click.wav", -12);
		SceneNav.GoTo(GetTree(), ScenePaths.MainMenu);
	}

	private void LoadTextures()
	{
		_enemy = GD.Load<Texture2D>("res://assets/sprites/enemy_grunt.png");
		_towerArcher = GD.Load<Texture2D>("res://assets/sprites/tower_archer.png");
		_towerCannon = GD.Load<Texture2D>("res://assets/sprites/tower_cannon.png");
		_towerFrost = GD.Load<Texture2D>("res://assets/sprites/tower_frost.png");

		_projArrow = GD.Load<Texture2D>("res://assets/sprites/proj_arrow.png");
		_projCannon = GD.Load<Texture2D>("res://assets/sprites/proj_cannon.png");
		_projFrost = GD.Load<Texture2D>("res://assets/sprites/proj_frost.png");
	}

	private AudioStream? GetAudio(string path)
	{
		if (_audioCache.TryGetValue(path, out var cached))
			return cached;

		var stream = GD.Load<AudioStream>(path);
		_audioCache[path] = stream;
		return stream;
	}

	private void PlaySfx(string path, float volumeDb = -8)
	{
		var stream = GetAudio(path);
		if (stream == null) return;

		var player = new AudioStreamPlayer();
		AddChild(player);
		player.Stream = stream;
		player.VolumeDb = volumeDb;
		player.Bus = "Master";
		player.Finished += () => player.QueueFree();
		player.Play();
	}

	public override void _Draw()
	{
		DrawPath();
		DrawPads();
		DrawSelectedRange();
		DrawTowers();
		DrawEnemies();
		DrawProjectiles();
	}

	private void DrawPath()
	{
		var pts = _engine.PathPx;
		if (pts.Count < 2) return;

		var width = Mathf.Min(_engine.WorldSize.X, _engine.WorldSize.Y) * 0.06f;
		DrawPolyline(ToArray(pts), new Color(0.08f, 0.08f, 0.08f, 0.85f), width);
		DrawPolyline(ToArray(pts), new Color(0.95f, 0.95f, 0.95f, 0.85f), width * 0.18f);
	}

	private static Vector2[] ToArray(IReadOnlyList<Vector2> list)
	{
		var arr = new Vector2[list.Count];
		for (int i = 0; i < list.Count; i++) arr[i] = list[i];
		return arr;
	}

	private void DrawPads()
	{
		foreach (var pad in _engine.Pads)
		{
			var c = new Vector2(pad.CenterNorm.X * _engine.WorldSize.X, pad.CenterNorm.Y * _engine.WorldSize.Y);
			float half = pad.SizePx / 2f;
			var r = new Rect2(c.X - half, c.Y - half, pad.SizePx, pad.SizePx);

			var fill = pad.HasTower ? new Color(0, 0, 0, 0.15f) : new Color(0.2f, 1f, 0.2f, 0.18f);
			var stroke = pad.HasTower ? new Color(0, 0, 0, 0.25f) : new Color(0.2f, 1f, 0.2f, 0.35f);

			DrawRect(r, fill, true);
			DrawRect(r, stroke, false, 2.0f);
		}
	}

	private void DrawSelectedRange()
	{
		if (_engine.SelectedTowerIndex is not int idx) return;
		if (idx < 0 || idx >= _engine.Towers.Count) return;

		var t = _engine.Towers[idx];
		DrawArc(t.Pos, t.Range, 0, Mathf.Tau, 64, new Color(1f, 1f, 1f, 0.20f), 2.0f, true);
	}

	private void DrawTowers()
	{
		float size = Mathf.Min(_engine.WorldSize.X, _engine.WorldSize.Y) * 0.10f;
		for (int i = 0; i < _engine.Towers.Count; i++)
		{
			var t = _engine.Towers[i];
			Texture2D? tex = t.Type switch
			{
				TowerType.Archer => _towerArcher,
				TowerType.Cannon => _towerCannon,
				TowerType.Frost => _towerFrost,
				_ => _towerArcher
			};

			var r = new Rect2(t.Pos.X - size / 2f, t.Pos.Y - size / 2f, size, size);
			if (tex != null) DrawTextureRect(tex, r, false);
			else DrawCircle(t.Pos, size * 0.35f, Colors.SteelBlue);

			if (_engine.SelectedTowerIndex == i)
				DrawArc(t.Pos, size * 0.56f, 0, Mathf.Tau, 40, new Color(1, 1, 1, 0.70f), 2.0f, true);
		}
	}

	private void DrawEnemies()
	{
		foreach (var e in _engine.Enemies)
		{
			float size = e.Radius * 2.2f;
			var r = new Rect2(e.Pos.X - size / 2f, e.Pos.Y - size / 2f, size, size);

			if (_enemy != null) DrawTextureRect(_enemy, r, false);
			else DrawCircle(e.Pos, e.Radius, Colors.OrangeRed);

			float hpPct = Mathf.Clamp(e.Hp / e.MaxHp, 0, 1);
			float barW = size;
			float barH = Mathf.Max(4, size * 0.10f);
			float barX = e.Pos.X - barW / 2f;
			float barY = r.Position.Y - barH - 2;

			DrawRect(new Rect2(barX, barY, barW, barH), new Color(0, 0, 0, 0.45f), true);
			DrawRect(new Rect2(barX, barY, barW * hpPct, barH), new Color(0.95f, 0.2f, 0.2f, 0.95f), true);
		}
	}

	private void DrawProjectiles()
	{
		foreach (var p in _engine.Projectiles)
		{
			Texture2D? tex = p.SourceType switch
			{
				TowerType.Archer => _projArrow,
				TowerType.Cannon => _projCannon,
				TowerType.Frost => _projFrost,
				_ => _projArrow
			};

			float size = Mathf.Min(_engine.WorldSize.X, _engine.WorldSize.Y) * 0.025f;
			var r = new Rect2(p.Pos.X - size / 2f, p.Pos.Y - size / 2f, size, size);

			if (tex != null) DrawTextureRect(tex, r, false);
			else DrawCircle(p.Pos, size * 0.30f, Colors.Yellow);
		}
	}
>>>>>>> parent of c1670f3 (Merge branch 'main' of https://github.com/Jakub-Chrzanowski/Tower-Defense-Godot)
}
