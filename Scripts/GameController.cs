using Godot;

public partial class GameController : Node2D
{
	private readonly GameEngine _engine = new();

	private Texture2D? _enemy;
	private Texture2D? _tower;
	private Texture2D? _proj;

	private Hud? _hud;

	public override void _Ready()
	{
		_hud = GetNode<Hud>("../HUD");
		_hud.Bind(this);

		_engine.OnHudChanged += () => _hud.Refresh(_engine);
		_engine.OnVictory += () => _hud.ShowVictory();
		_engine.OnDefeat += () => _hud.ShowDefeat();

		_enemy = GD.Load<Texture2D>("res://assets/sprites/enemy_grunt.png");
		_tower = GD.Load<Texture2D>("res://assets/sprites/tower_archer.png");
		_proj  = GD.Load<Texture2D>("res://assets/sprites/proj_arrow.png");

		_engine.SetWorldSize(GetViewportRect().Size);
		_engine.Reset();

		_hud.Refresh(_engine);
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
			var pos = ToLocal(mb.Position);
			_engine.TryPlaceTower(pos);
		}
	}

	public void StartWave() => _engine.StartWave();

	public void RestartLevel()
	{
		_engine.Reset();
		_hud?.Refresh(_engine);
	}

	public void BackToMenu() => SceneNav.GoTo(GetTree(), ScenePaths.MainMenu);

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

		var width = Mathf.Min(_engine.WorldSize.X, _engine.WorldSize.Y) * 0.06f;
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

			var fill = pad.HasTower ? new Color(0,0,0,0.15f) : new Color(0.2f,1f,0.2f,0.18f);
			var stroke = pad.HasTower ? new Color(0,0,0,0.25f) : new Color(0.2f,1f,0.2f,0.35f);

			DrawRect(r, fill, true);
			DrawRect(r, stroke, false, 2.0f);
		}
	}

	private void DrawTowers()
	{
		float size = Mathf.Min(_engine.WorldSize.X, _engine.WorldSize.Y) * 0.10f;
		foreach (var t in _engine.Towers)
		{
			var r = new Rect2(t.Pos.X - size/2f, t.Pos.Y - size/2f, size, size);
			if (_tower != null) DrawTextureRect(_tower, r, false);
			else DrawCircle(t.Pos, size * 0.35f, Colors.SteelBlue);
		}
	}

	private void DrawEnemies()
	{
		foreach (var e in _engine.Enemies)
		{
			float size = e.Radius * 2.2f;
			var r = new Rect2(e.Pos.X - size/2f, e.Pos.Y - size/2f, size, size);

			if (_enemy != null) DrawTextureRect(_enemy, r, false);
			else DrawCircle(e.Pos, e.Radius, Colors.OrangeRed);

			
			float hpPct = Mathf.Clamp(e.Hp / e.MaxHp, 0, 1);
			float barW = size;
			float barH = Mathf.Max(4, size * 0.10f);
			float barX = e.Pos.X - barW/2f;
			float barY = r.Position.Y - barH - 2;

			DrawRect(new Rect2(barX, barY, barW, barH), new Color(0,0,0,0.45f), true);
			DrawRect(new Rect2(barX, barY, barW*hpPct, barH), new Color(0.95f,0.2f,0.2f,0.95f), true);
		}
	}

	private void DrawProjectiles()
	{
		foreach (var p in _engine.Projectiles)
		{
			float size = Mathf.Min(_engine.WorldSize.X, _engine.WorldSize.Y) * 0.025f;
			var r = new Rect2(p.Pos.X - size/2f, p.Pos.Y - size/2f, size, size);

			if (_proj != null) DrawTextureRect(_proj, r, false);
			else DrawCircle(p.Pos, size*0.3f, Colors.Yellow);
		}
	}
}
	
