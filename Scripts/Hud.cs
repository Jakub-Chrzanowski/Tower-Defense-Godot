using Godot;

public partial class Hud : Control
{
	private GameController? _gc;

	private Control?     _bossBarRoot;
	private ProgressBar? _bossProgressBar;
	private Label?       _bossLabel;
	private Label?       _fpsLabel;

	public void Bind(GameController gc)
	{
		_gc = gc;

		var archer    = GetNodeOrNull<Button>("BottomBar/Archer");
		var cannon    = GetNodeOrNull<Button>("BottomBar/Cannon");
		var frost     = GetNodeOrNull<Button>("BottomBar/Frost");
		var startWave = GetNodeOrNull<Button>("BottomBar/StartWave");
		var upgrade   = GetNodeOrNull<Button>("BottomBar/Upgrade");
		var sell      = GetNodeOrNull<Button>("BottomBar/Sell");
		var menuBtn   = GetNodeOrNull<TextureButton>("Menu");

		var vNextMap = GetNodeOrNull<Button>("Overlays/Victory/NextMap");
		var vRestart = GetNodeOrNull<Button>("Overlays/Victory/Restart");
		var dRestart = GetNodeOrNull<Button>("Overlays/Defeat/Restart");
		var vBack    = GetNodeOrNull<Button>("Overlays/Victory/Back");
		var dBack    = GetNodeOrNull<Button>("Overlays/Defeat/Back");

		if (archer == null || cannon == null || frost == null || startWave == null ||
		    upgrade == null || sell == null || menuBtn == null)
		{
			GD.PushError("HUD: Brakuje przycisków. Sprawdź Game.tscn.");
			return;
		}

		archer.Pressed    += () => _gc?.SetSelectedTower(TowerType.Archer);
		cannon.Pressed    += () => _gc?.SetSelectedTower(TowerType.Cannon);
		frost.Pressed     += () => _gc?.SetSelectedTower(TowerType.Frost);
		startWave.Pressed += () => _gc?.StartWave();
		upgrade.Pressed   += () => _gc?.UpgradeSelected();
		sell.Pressed      += () => _gc?.SellSelected();
		menuBtn.Pressed   += () => _gc?.BackToMenu();

		if (vNextMap != null) vNextMap.Pressed += () => _gc?.GoToNextMap();
		if (vRestart != null) vRestart.Pressed += () => _gc?.RestartLevel();
		if (vBack    != null) vBack.Pressed    += () => _gc?.BackToMenu();
		if (dRestart != null) dRestart.Pressed += () => _gc?.RestartLevel();
		if (dBack    != null) dBack.Pressed    += () => _gc?.BackToMenu();

		// Boss bar – węzły z sceny
		_bossBarRoot     = GetNodeOrNull<Control>("BossBar");
		_bossProgressBar = GetNodeOrNull<ProgressBar>("BossBar/VBox/Bar");
		_bossLabel       = GetNodeOrNull<Label>("BossBar/VBox/Label");
		if (_bossBarRoot != null) _bossBarRoot.Visible = false;

		// FPS label – węzeł z sceny
		_fpsLabel = GetNodeOrNull<Label>("TopBar/FpsLabel");
		if (_fpsLabel != null) _fpsLabel.Visible = false;
	}

	// ── Boss bar ──────────────────────────────────────────────────────
	public void ShowBossBar(Enemy? boss)
	{
		if (_bossBarRoot == null || boss == null) return;
		_bossBarRoot.Visible = true;
		RefreshBossBar(boss);
	}

	public void RefreshBossBar(Enemy? boss)
	{
		if (_bossBarRoot == null || boss == null || !_bossBarRoot.Visible) return;
		float pct = Mathf.Clamp(boss.Hp / boss.MaxHp, 0f, 1f);
		if (_bossProgressBar != null) _bossProgressBar.Value = pct;
		if (_bossLabel != null)
			_bossLabel.Text = $"☠ BOSS  {Mathf.Max(0, (int)boss.Hp)} / {(int)boss.MaxHp}";
	}

	public void HideBossBar()
	{
		if (_bossBarRoot != null) _bossBarRoot.Visible = false;
	}

	// ── FPS ───────────────────────────────────────────────────────────
	public void RefreshFps()
	{
		if (_fpsLabel == null) return;
		bool show = GameSettings.ShowFps;
		_fpsLabel.Visible = show;
		if (show) _fpsLabel.Text = $"FPS: {Engine.GetFramesPerSecond()}";
	}

	// ── Główny Refresh ────────────────────────────────────────────────
	public void Refresh(GameEngine e)
	{
		SetHearts(e.Lives);

		var mapNames  = new[] { "EASY", "MEDIUM", "HARD" };
		var waveLabel = GetNodeOrNull<Label>("TopBar/WaveLabel");
		if (waveLabel != null)
			waveLabel.Text = mapNames[Mathf.Clamp(GameSession.CurrentMap, 0, mapNames.Length - 1)];

		var coinsLabel = GetNodeOrNull<Label>("TopBar/CoinsLabel");
		if (coinsLabel != null) coinsLabel.Text = e.Coins.ToString();

		Mark("BottomBar/Archer", e.SelectedTowerType == TowerType.Archer);
		Mark("BottomBar/Cannon", e.SelectedTowerType == TowerType.Cannon);
		Mark("BottomBar/Frost",  e.SelectedTowerType == TowerType.Frost);

		var upgrade   = GetNodeOrNull<Button>("BottomBar/Upgrade");
		if (upgrade   != null) upgrade.Disabled   = e.SelectedTowerIndex is null || e.WaveRunning || e.Victory || e.Defeat;

		var sell      = GetNodeOrNull<Button>("BottomBar/Sell");
		if (sell      != null) sell.Disabled      = e.SelectedTowerIndex is null || e.WaveRunning || e.Victory || e.Defeat;

		var startWave = GetNodeOrNull<Button>("BottomBar/StartWave");
		if (startWave != null) startWave.Disabled = e.WaveRunning || e.Victory || e.Defeat;
	}

	public void ShowVictory(bool isLastMap = false)
	{
		var victory = GetNodeOrNull<Control>("Overlays/Victory");
		if (victory == null) return;
		victory.Visible = true;

		var nextMap = GetNodeOrNull<Button>("Overlays/Victory/NextMap");
		if (nextMap != null) nextMap.Visible = !isLastMap;

		var label = GetNodeOrNull<Label>("Overlays/Victory/Label");
		if (label != null)
			label.Text = isLastMap
				? "GRATULACJE!\nUkończyłeś wszystkie mapy!"
				: "WYGRANA!\nCzas na następną mapę!";
	}

	public void HideVictory() { var v = GetNodeOrNull<Control>("Overlays/Victory"); if (v != null) v.Visible = false; }
	public void ShowDefeat()  { var d = GetNodeOrNull<Control>("Overlays/Defeat");  if (d != null) d.Visible = true;  }
	public void HideDefeat()  { var d = GetNodeOrNull<Control>("Overlays/Defeat");  if (d != null) d.Visible = false; }

	private void SetHearts(int lives)
	{
		for (int i = 1; i <= 3; i++)
		{
			var h = GetNodeOrNull<TextureRect>($"TopBar/LivesIcons/Heart{i}");
			if (h != null) h.Visible = i <= lives;
		}
	}

	private void Mark(string path, bool active)
	{
		var btn = GetNodeOrNull<Button>(path);
		if (btn != null) btn.Modulate = active ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.65f);
	}
}
