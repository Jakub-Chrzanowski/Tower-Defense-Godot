using Godot;

public partial class Hud : Control
{
	private GameController? _gc;

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

		var vNextMap  = GetNodeOrNull<Button>("Overlays/Victory/NextMap");
		var vRestart  = GetNodeOrNull<Button>("Overlays/Victory/Restart");
		var dRestart  = GetNodeOrNull<Button>("Overlays/Defeat/Restart");
		var vBack     = GetNodeOrNull<Button>("Overlays/Victory/Back");
		var dBack     = GetNodeOrNull<Button>("Overlays/Defeat/Back");

		if (archer == null || cannon == null || frost == null || startWave == null ||
		    upgrade == null || sell == null || menuBtn == null)
		{
			GD.PushError("HUD: Brakuje któregoś z przycisków. Sprawdź Game.tscn.");
			return;
		}

		archer.Pressed    += () => _gc?.SetSelectedTower(TowerType.Archer);
		cannon.Pressed    += () => _gc?.SetSelectedTower(TowerType.Cannon);
		frost.Pressed     += () => _gc?.SetSelectedTower(TowerType.Frost);
		startWave.Pressed += () => _gc?.StartWave();
		upgrade.Pressed   += () => _gc?.UpgradeSelected();
		sell.Pressed      += () => _gc?.SellSelected();
		menuBtn.Pressed   += () => _gc?.BackToMenu();

		// Victory overlay: NextMap (jeśli istnieje) lub Back
		if (vNextMap != null) vNextMap.Pressed += () => _gc?.GoToNextMap();
		if (vRestart != null) vRestart.Pressed += () => _gc?.RestartLevel();
		if (vBack    != null) vBack.Pressed    += () => _gc?.BackToMenu();

		// Defeat overlay
		if (dRestart != null) dRestart.Pressed += () => _gc?.RestartLevel();
		if (dBack    != null) dBack.Pressed    += () => _gc?.BackToMenu();
	}

	public void Refresh(GameEngine e)
	{
		SetHearts(e.Lives);

		var mapNames = new[] { "EASY", "MEDIUM", "HARD" };
		var waveLabel = GetNodeOrNull<Label>("TopBar/WaveLabel");
		if (waveLabel != null)
			waveLabel.Text = mapNames[Mathf.Clamp(GameSession.CurrentMap, 0, mapNames.Length - 1)];

		var coinsLabel = GetNodeOrNull<Label>("TopBar/CoinsLabel");
		if (coinsLabel != null) coinsLabel.Text = e.Coins.ToString();

		Mark("BottomBar/Archer", e.SelectedTowerType == TowerType.Archer);
		Mark("BottomBar/Cannon", e.SelectedTowerType == TowerType.Cannon);
		Mark("BottomBar/Frost",  e.SelectedTowerType == TowerType.Frost);

		var upgrade = GetNodeOrNull<Button>("BottomBar/Upgrade");
		if (upgrade != null) upgrade.Disabled = e.SelectedTowerIndex is null || e.WaveRunning || e.Victory || e.Defeat;

		var sell = GetNodeOrNull<Button>("BottomBar/Sell");
		if (sell != null) sell.Disabled = e.SelectedTowerIndex is null || e.WaveRunning || e.Victory || e.Defeat;

		var startWave = GetNodeOrNull<Button>("BottomBar/StartWave");
		if (startWave != null) startWave.Disabled = e.WaveRunning || e.Victory || e.Defeat;

		// Overlaye – odświeżane przez ShowVictory/ShowDefeat/Hide*
	}

	public void ShowVictory(bool isLastMap = false)
	{
		var victory = GetNodeOrNull<Control>("Overlays/Victory");
		if (victory == null) return;
		victory.Visible = true;

		// Pokaż/ukryj przycisk NextMap zależnie od tego czy to ostatnia mapa
		var nextMap = GetNodeOrNull<Button>("Overlays/Victory/NextMap");
		if (nextMap != null) nextMap.Visible = !isLastMap;

		// Jeśli to ostatnia mapa, zmień tekst
		var label = GetNodeOrNull<Label>("Overlays/Victory/Label");
		if (label != null)
			label.Text = isLastMap ? "GRATULACJE!\nUkończyłeś wszystkie mapy!" : "WYGRANA!\nCzas na następną mapę!";
	}

	public void HideVictory()
	{
		var victory = GetNodeOrNull<Control>("Overlays/Victory");
		if (victory != null) victory.Visible = false;
	}

	public void ShowDefeat()
	{
		var defeat = GetNodeOrNull<Control>("Overlays/Defeat");
		if (defeat != null) defeat.Visible = true;
	}

	public void HideDefeat()
	{
		var defeat = GetNodeOrNull<Control>("Overlays/Defeat");
		if (defeat != null) defeat.Visible = false;
	}

	private void SetHearts(int lives)
	{
		for (int i = 1; i <= 3; i++)
		{
			var heart = GetNodeOrNull<TextureRect>($"TopBar/LivesIcons/Heart{i}");
			if (heart != null) heart.Visible = i <= lives;
		}
	}

	private void Mark(string path, bool active)
	{
		var btn = GetNodeOrNull<Button>(path);
		if (btn != null)
			btn.Modulate = active ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.65f);
	}
}
