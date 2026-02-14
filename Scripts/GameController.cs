using Godot;

public partial class GameController : Node2D
{
	private readonly GameEngine _engine = new();

	private Window? _window;

	private Texture2D? _enemyGrunt;
	private Texture2D? _enemyFast;
	private Texture2D? _enemyTank;
	private Texture2D? _enemyFlying;
	private Texture2D? _enemyBoss;

	private Texture2D? _towerArcher;
	private Texture2D? _towerCannon;
	private Texture2D? _towerFrost;

	private Texture2D? _projArrow;
	private Texture2D? _projCannon;
	private Texture2D? _projFrost;

	private Hud? _hud;

	public override void _Ready()
	{
		_hud = GetNode<Hud>("../HUD");
		_hud.Bind(this);

		_engine.OnHudChanged += () => _hud.Refresh(_engine);
		_engine.OnWaveChanged += _ => _hud.Refresh(_engine);
		_engine.OnVictory += () => _hud.ShowVictory();
		_engine.OnDefeat += () => _hud.ShowDefeat();

		LoadTextures();

		_engine.LoadMap(GameSession.SelectedMapId);
		_engine.SetWorldSize(GetViewportRect().Size);
		_engine.Reset();
		_hud.Refresh(_engine);
		_window = GetWindow(); 
_window.SizeChanged += OnWindowSizeChanged;
OnWindowSizeChanged(); 

	}
	
	private void OnWindowSizeChanged()
{
	_engine.SetWorldSize(GetViewportRect().Size);
	QueueRedraw();
}



	public override void _Process(double delta)
	{
		_engine.Update((float)delta);
		QueueRedraw();
	}

	public override void _UnhandledInput(InputEvent e)
	{
		if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			var pos = mb.Position; // Node2D at (0,0)

			if (!_engine.TrySelectTower(pos))
				_engine.TryPlaceTower(pos);
		}
	}

	public void SetSelectedTower(TowerType type)
	{
		_engine.SelectedTowerType = type;
		_hud?.Refresh(_engine);
	}

	public void StartWave() => _engine.StartWave();
	public void UpgradeSelected() => _engine.UpgradeSelectedTower();
	public void SellSelected() => _engine.SellSelectedTower();
	public void BackToMenu() => SceneNav.GoTo(GetTree(), ScenePaths.MainMenu);

	private void LoadTextures()
	{
		_enemyGrunt = GD.Load<Texture2D>("res://assets/sprites/enemy_grunt.png");
		_enemyFast = GD.Load<Texture2D>("res://assets/sprites/enemy_fast.png");
		_enemyTank = GD.Load<Texture2D>("res://assets/sprites/enemy_tank.png");
		_enemyFlying = GD.Load<Texture2D>("res://assets/sprites/enemy_flying.png");
		_enemyBoss = GD.Load<Texture2D>("res://assets/sprites/enemy_boss.png");

		_towerArcher = GD.Load<Texture2D>("res://assets/sprites/tower_archer.png");
		_towerCannon = GD.Load<Texture2D>("res://assets/sprites/tower_cannon.png");
		_towerFrost = GD.Load<Texture2D>("res://assets/sprites/tower_frost.png");

		_projArrow = GD.Load<Texture2D>("res://assets/sprites/proj_arrow.png");
		_projCannon = GD.Load<Texture2D>("res://assets/sprites/proj_cannon.png");
		_projFrost = GD.Load<Texture2D>("res://assets/sprites/proj_frost.png");
	}

	public override void _Draw()
	{
		DrawPath();
		DrawPads();
		DrawTowers();
		DrawEnemies();
		DrawProjectiles();
	}

	private void DrawPath()
	{
		var pts = _engine.PathPx;
		if (pts.Count < 2) return;

		float width = Mathf.Min(_engine.WorldSize.X, _engine.WorldSize.Y) * 0.06f;

		DrawPolyline(pts.ToArray(), new Color(0.08f, 0.08f, 0.08f, 0.85f), width);
		DrawPolyline(pts.ToArray(), new Color(0.95f, 0.95f, 0.95f, 0.85f), width * 0.18f);
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

	private void DrawTowers()
	{
		float size = Mathf.Min(_engine.WorldSize.X, _engine.WorldSize.Y) * 0.10f;

		for (int i = 0; i < _engine.Towers.Count; i++)
		{
			var t = _engine.Towers[i];
			var tex = t.Type switch
			{
				TowerType.Archer => _towerArcher,
				TowerType.Cannon => _towerCannon,
				TowerType.Frost => _towerFrost,
				_ => _towerArcher
			};

			var r = new Rect2(t.Pos.X - size / 2f, t.Pos.Y - size / 2f, size, size);

			if (tex != null) DrawTextureRect(tex, r, false);
			else DrawCircle(t.Pos, size * 0.35f, Colors.SteelBlue);

			if (_engine.SelectedTowerIndex is int sel && sel == i)
				DrawArc(t.Pos, size * 0.55f, 0, Mathf.Tau, 48, new Color(1, 1, 1, 0.75f), 2.0f, true);
		}
	}

	private void DrawEnemies()
	{
		foreach (var e in _engine.Enemies)
		{
			Texture2D? tex = e.Type switch
			{
				EnemyType.Grunt => _enemyGrunt,
				EnemyType.Fast => _enemyFast,
				EnemyType.Tank => _enemyTank,
				EnemyType.Flying => _enemyFlying,
				EnemyType.Boss => _enemyBoss,
				_ => _enemyGrunt
			};

			float size = e.Radius * 2.2f;
			var r = new Rect2(e.Pos.X - size / 2f, e.Pos.Y - size / 2f, size, size);

			if (tex != null) DrawTextureRect(tex, r, false);
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
			Texture2D? tex = _engine.SelectedTowerType switch
			{
				TowerType.Archer => _projArrow,
				TowerType.Cannon => _projCannon,
				TowerType.Frost => _projFrost,
				_ => _projArrow
			};

			float size = Mathf.Min(_engine.WorldSize.X, _engine.WorldSize.Y) * 0.025f;
			var r = new Rect2(p.Pos.X - size / 2f, p.Pos.Y - size / 2f, size, size);

			if (tex != null) DrawTextureRect(tex, r, false);
			else DrawCircle(p.Pos, size * 0.3f, Colors.Yellow);
		}
	}
	public override void _ExitTree()
{
	if (_window != null)
		_window.SizeChanged -= OnWindowSizeChanged;
}

}
