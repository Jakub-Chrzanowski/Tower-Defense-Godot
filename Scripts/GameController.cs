using Godot;
using System.Collections.Generic;

public partial class GameController : Node2D
{
	private readonly GameEngine _engine = new();

	private Texture2D? _enemyGrunt, _enemyFast, _enemyTank, _enemyFlying;
	private Texture2D? _towerArcher, _towerCannon, _towerFrost;
	private Texture2D? _projArrow, _projCannon, _projFrost;
	private Texture2D? _coin;

	private Hud? _hud;
	private readonly Dictionary<string, AudioStream?> _audioCache = new();

	// Fade
	private ColorRect? _fadeRect;
	private float _fadeTimer, _fadeDuration = 0.6f;
	private bool _fadingOut, _fadingIn;
	private System.Action? _onFadeOutDone;

	private Label? _fpsLabel;

	public override void _Ready()
	{
		_hud = GetNodeOrNull<Hud>("../HUD");
		if (_hud == null) { GD.PushError("GameController: brak ../HUD"); return; }

		// Fade rect
		_fadeRect = new ColorRect
		{
			Color       = new Color(0, 0, 0, 0),
			MouseFilter = Control.MouseFilterEnum.Ignore,
			ZIndex      = 100
		};
		_fadeRect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		GetParent().AddChild(_fadeRect);

		// FPS label - dodajemy do HUD obok monetek, nie tutaj
		// (tworzone w Hud.BuildFpsLabel)

		_hud.Bind(this);

		_engine.OnHudChanged    += () => _hud?.Refresh(_engine);
		_engine.OnVictory       += OnVictory;
		_engine.OnDefeat        += () => { _hud?.ShowDefeat(); PlaySfx("res://assets/sfx/sfx_defeat.wav", -4); };
		_engine.OnBossSpawned   += () => { _hud?.ShowBossBar(_engine.Boss); PlaySfx("res://assets/sfx/sfx_wave.wav", -4); };
		_engine.OnBossDefeated  += () => { _hud?.HideBossBar(); PlaySfx("res://assets/sfx/sfx_victory.wav", -6); };
		_engine.OnTowerBuilt    += () => PlaySfx("res://assets/sfx/sfx_build.wav",   -8);
		_engine.OnTowerUpgraded += () => PlaySfx("res://assets/sfx/sfx_upgrade.wav", -7);
		_engine.OnTowerSold     += () => PlaySfx("res://assets/sfx/sfx_sell.wav",    -8);
		_engine.OnLeak          += () => PlaySfx("res://assets/sfx/sfx_leak.wav",    -6);
		_engine.OnShot          += () => PlaySfx("res://assets/sfx/sfx_shot.wav",   -14);
		_engine.OnEnemyKilled   += () => PlaySfx("res://assets/sfx/sfx_kill.wav",   -10);
		_engine.OnWaveReward    += () => PlaySfx("res://assets/sfx/sfx_wave.wav",    -8);

		LoadTextures();
		_engine.LoadMap(GameSession.CurrentMap);
		_engine.SetWorldSize(GetViewportRect().Size);
		_engine.Reset();
		_hud.Refresh(_engine);
		StartFadeIn();
	}

	// ── Victory / next map ────────────────────────────────────────────
	private void OnVictory()
	{
		PlaySfx("res://assets/sfx/sfx_victory.wav", -4);
		_hud?.ShowVictory(isLastMap: GameSession.IsLastMap);
	}

	public void GoToNextMap()
	{
		GameSession.AdvanceMap();
		FadeOutThen(() => {
			_engine.LoadMap(GameSession.CurrentMap);
			_engine.SetWorldSize(GetViewportRect().Size);
			_engine.Reset();
			_hud?.HideVictory();
			_hud?.HideBossBar();
			_hud?.Refresh(_engine);
			QueueRedraw();
			StartFadeIn();
		});
	}

	// ── Fade ─────────────────────────────────────────────────────────
	private void FadeOutThen(System.Action cb) { _onFadeOutDone = cb; _fadingOut = true; _fadingIn = false; _fadeTimer = 0f; }
	private void StartFadeIn() { _fadingIn = true; _fadingOut = false; _fadeTimer = 0f; if (_fadeRect != null) _fadeRect.Color = new Color(0, 0, 0, 1); }

	private void UpdateFade(float dt)
	{
		if (_fadeRect == null) return;
		if (_fadingOut)
		{
			_fadeTimer += dt;
			float t = Mathf.Clamp(_fadeTimer / _fadeDuration, 0, 1);
			_fadeRect.Color = new Color(0, 0, 0, t);
			if (t >= 1f) { _fadingOut = false; _onFadeOutDone?.Invoke(); _onFadeOutDone = null; }
		}
		else if (_fadingIn)
		{
			_fadeTimer += dt;
			float t = Mathf.Clamp(_fadeTimer / _fadeDuration, 0, 1);
			_fadeRect.Color = new Color(0, 0, 0, 1f - t);
			if (t >= 1f) { _fadingIn = false; _fadeRect.Color = new Color(0, 0, 0, 0); }
		}
	}

	// ── Process ───────────────────────────────────────────────────────
	public override void _Notification(int what)
	{
		if (what == 1008) { _engine.SetWorldSize(GetViewportRect().Size); QueueRedraw(); }
	}

	public override void _Process(double delta)
	{
		_engine.Update((float)delta);
		UpdateFade((float)delta);
		UpdateFpsLabel();

		// Odśwież boss bar jeśli boss żyje
		if (_engine.Boss != null && !_engine.BossDefeated)
			_hud?.RefreshBossBar(_engine.Boss);

		QueueRedraw();
	}

	private void UpdateFpsLabel()
	{
		_hud?.RefreshFps();
	}

	// ── Input ─────────────────────────────────────────────────────────
	public override void _UnhandledInput(InputEvent e)
	{
		if (e is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
		{
			var pos = ToLocal(mb.Position);
			if (!_engine.TrySelectTower(pos)) _engine.TryPlaceTower(pos);
		}
	}

	// ── Publiczne akcje HUD ───────────────────────────────────────────
	public void SetSelectedTower(TowerType type) { _engine.SelectedTowerType = type; _hud?.Refresh(_engine); PlaySfx("res://assets/sfx/sfx_click.wav", -12); }
	public void StartWave()       { _engine.StartWave(); PlaySfx("res://assets/sfx/sfx_click.wav", -12); }
	public void UpgradeSelected() { if (_engine.UpgradeSelectedTower()) PlaySfx("res://assets/sfx/sfx_upgrade.wav", -7); }
	public void SellSelected()    { if (_engine.SellSelectedTower())    PlaySfx("res://assets/sfx/sfx_sell.wav",    -8); }

	public void RestartLevel()
	{
		GameSession.StartFresh();
		_engine.LoadMap(GameSession.CurrentMap);
		_engine.SetWorldSize(GetViewportRect().Size);
		_engine.Reset();
		_hud?.HideVictory(); _hud?.HideDefeat(); _hud?.HideBossBar();
		_hud?.Refresh(_engine);
		PlaySfx("res://assets/sfx/sfx_click.wav", -12);
		StartFadeIn();
	}

	public void BackToMenu()    { PlaySfx("res://assets/sfx/sfx_click.wav", -12); FadeOutThen(() => SceneNav.GoTo(GetTree(), ScenePaths.MainMenu)); }
	public void BackToMapSelect() => BackToMenu();

	// ── Tekstury / audio ──────────────────────────────────────────────
	private void LoadTextures()
	{
		_enemyGrunt  = GD.Load<Texture2D>("res://assets/sprites/enemy_grunt.png");
		_enemyFast   = GD.Load<Texture2D>("res://assets/sprites/enemy_fast.png");
		_enemyTank   = GD.Load<Texture2D>("res://assets/sprites/enemy_tank.png");
		_enemyFlying = GD.Load<Texture2D>("res://assets/sprites/enemy_flying.png");
		_towerArcher = GD.Load<Texture2D>("res://assets/sprites/tower_archer.png");
		_towerCannon = GD.Load<Texture2D>("res://assets/sprites/tower_cannon.png");
		_towerFrost  = GD.Load<Texture2D>("res://assets/sprites/tower_frost.png");
		_projArrow   = GD.Load<Texture2D>("res://assets/sprites/proj_arrow.png");
		_projCannon  = GD.Load<Texture2D>("res://assets/sprites/proj_cannon.png");
		_projFrost   = GD.Load<Texture2D>("res://assets/sprites/proj_frost.png");
		_coin        = GD.Load<Texture2D>("res://assets/sprites/coin.png");
	}

	private AudioStream? GetAudio(string path)
	{
		if (_audioCache.TryGetValue(path, out var c)) return c;
		var s = GD.Load<AudioStream>(path); _audioCache[path] = s; return s;
	}

	private void PlaySfx(string path, float vol = -8)
	{
		var s = GetAudio(path); if (s == null) return;
		var p = new AudioStreamPlayer { Stream = s, VolumeDb = vol, Bus = "Master" };
		AddChild(p); p.Finished += () => p.QueueFree(); p.Play();
	}

	// ── Rysowanie ─────────────────────────────────────────────────────
	public override void _Draw()
	{
		DrawPath(); DrawPads(); DrawSelectedRange();
		DrawTowers(); DrawEnemies(); DrawProjectiles(); DrawCoins();
	}

	private static Vector2[] ToArr(IReadOnlyList<Vector2> l)
	{
		var a = new Vector2[l.Count]; for (int i = 0; i < l.Count; i++) a[i] = l[i]; return a;
	}

	private void DrawPath()
	{
		var pts = _engine.PathPx; if (pts.Count < 2) return;
		float w = Mathf.Min(_engine.WorldSize.X, _engine.WorldSize.Y) * 0.06f;
		DrawPolyline(ToArr(pts), new Color(0.08f, 0.08f, 0.08f, 0.85f), w);
		DrawPolyline(ToArr(pts), new Color(0.95f, 0.95f, 0.95f, 0.85f), w * 0.18f);
	}

	private void DrawPads()
	{
		foreach (var pad in _engine.Pads)
		{
			var c    = new Vector2(pad.CenterNorm.X * _engine.WorldSize.X, pad.CenterNorm.Y * _engine.WorldSize.Y);
			float h  = pad.SizePx / 2f;
			var r    = new Rect2(c.X - h, c.Y - h, pad.SizePx, pad.SizePx);
			DrawRect(r, pad.HasTower ? new Color(0,0,0,0.15f) : new Color(0.2f,1f,0.2f,0.18f), true);
			DrawRect(r, pad.HasTower ? new Color(0,0,0,0.25f) : new Color(0.2f,1f,0.2f,0.35f), false, 2f);
		}
	}

	private void DrawSelectedRange()
	{
		if (_engine.SelectedTowerIndex is not int idx) return;
		if (idx < 0 || idx >= _engine.Towers.Count) return;
		DrawArc(_engine.Towers[idx].Pos, _engine.Towers[idx].Range, 0, Mathf.Tau, 64, new Color(1,1,1,0.20f), 2f, true);
	}

	private void DrawTowers()
	{
		float size = Mathf.Min(_engine.WorldSize.X, _engine.WorldSize.Y) * 0.10f;
		for (int i = 0; i < _engine.Towers.Count; i++)
		{
			var t   = _engine.Towers[i];
			Texture2D? tex = t.Type switch { TowerType.Cannon => _towerCannon, TowerType.Frost => _towerFrost, _ => _towerArcher };
			var r = new Rect2(t.Pos.X - size/2f, t.Pos.Y - size/2f, size, size);
			if (tex != null) DrawTextureRect(tex, r, false); else DrawCircle(t.Pos, size*0.35f, Colors.SteelBlue);
			if (_engine.SelectedTowerIndex == i)
				DrawArc(t.Pos, size*0.56f, 0, Mathf.Tau, 40, new Color(1,1,1,0.70f), 2f, true);
		}
	}

	private void DrawEnemies()
	{
		foreach (var e in _engine.Enemies)
		{
			float size = e.Radius * 2.2f;
			var r = new Rect2(e.Pos.X - size/2f, e.Pos.Y - size/2f, size, size);

			if (e.Type == EnemyType.Boss)
			{
				DrawBoss(e, size, r);
				continue;
			}

			Texture2D? tex = e.Type switch {
				EnemyType.Fast   => _enemyFast,
				EnemyType.Tank   => _enemyTank,
				EnemyType.Flying => _enemyFlying,
				_                => _enemyGrunt
			};
			Color fallback = e.Type switch {
				EnemyType.Fast   => Colors.Yellow,
				EnemyType.Tank   => Colors.SaddleBrown,
				EnemyType.Flying => Colors.CornflowerBlue,
				_                => Colors.OrangeRed
			};

			if (tex != null) DrawTextureRect(tex, r, false); else DrawCircle(e.Pos, e.Radius, fallback);
			if (e.Type == EnemyType.Flying)
				DrawArc(e.Pos, e.Radius*1.35f, 0, Mathf.Tau, 32, new Color(0.4f,0.8f,1f,0.55f), 1.5f, true);

			DrawEnemyHpBar(e, size, r, e.Type switch {
				EnemyType.Fast   => new Color(1.0f, 0.85f, 0.0f, 0.95f),
				EnemyType.Tank   => new Color(0.6f, 0.3f,  0.1f, 0.95f),
				EnemyType.Flying => new Color(0.3f, 0.6f,  1.0f, 0.95f),
				_                => new Color(0.95f,0.2f,  0.2f, 0.95f)
			});
		}
	}

	private void DrawBoss(Enemy e, float size, Rect2 r)
	{
		// Wielki czerwono-czarny krąg z pulsującą aurą
		float pulse = 1f + 0.08f * Mathf.Sin((float)Time.GetTicksMsec() / 250f);
		DrawCircle(e.Pos, e.Radius * 1.55f * pulse, new Color(0.6f, 0f, 0f, 0.35f));
		DrawCircle(e.Pos, e.Radius, new Color(0.15f, 0f, 0f));
		DrawArc(e.Pos, e.Radius, 0, Mathf.Tau, 48, new Color(1f, 0.1f, 0.1f), 3f, true);

		// Pasek HP bossa – duży, nad głową
		float hpPct = Mathf.Clamp(e.Hp / e.MaxHp, 0, 1);
		float barW  = size * 1.6f;
		float barH  = Mathf.Max(6f, size * 0.13f);
		float barX  = e.Pos.X - barW / 2f;
		float barY  = r.Position.Y - barH - 6f;
		DrawRect(new Rect2(barX, barY, barW, barH), new Color(0,0,0,0.6f), true);
		DrawRect(new Rect2(barX, barY, barW * hpPct, barH), new Color(1f, 0.15f, 0.15f), true);
		DrawRect(new Rect2(barX, barY, barW, barH), new Color(1f, 0.4f, 0.4f, 0.8f), false, 1.5f);
	}

	private void DrawEnemyHpBar(Enemy e, float size, Rect2 r, Color hpColor)
	{
		float hpPct = Mathf.Clamp(e.Hp / e.MaxHp, 0, 1);
		float barW  = size, barH = Mathf.Max(4f, size * 0.10f);
		float barX  = e.Pos.X - barW / 2f;
		float barY  = r.Position.Y - barH - 2f;
		DrawRect(new Rect2(barX, barY, barW, barH),        new Color(0, 0, 0, 0.45f), true);
		DrawRect(new Rect2(barX, barY, barW * hpPct, barH), hpColor,                   true);
	}

	private void DrawProjectiles()
	{
		float size = Mathf.Min(_engine.WorldSize.X, _engine.WorldSize.Y) * 0.025f;
		foreach (var p in _engine.Projectiles)
		{
			Texture2D? tex = p.SourceType switch { TowerType.Cannon => _projCannon, TowerType.Frost => _projFrost, _ => _projArrow };
			var r = new Rect2(p.Pos.X - size/2f, p.Pos.Y - size/2f, size, size);
			if (tex != null) DrawTextureRect(tex, r, false); else DrawCircle(p.Pos, size*0.30f, Colors.Yellow);
		}
	}

	private void DrawCoins()
	{
		if (_coin == null) return;
		DrawTextureRect(_coin, new Rect2(18, 62, 18, 18), false);
	}
}
