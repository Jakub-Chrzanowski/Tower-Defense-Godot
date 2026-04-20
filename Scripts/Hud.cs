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

		var vBack     = GetNodeOrNull<Button>("Overlays/Victory/Back");
		var dBack     = GetNodeOrNull<Button>("Overlays/Defeat/Back");
		var vRestart  = GetNodeOrNull<Button>("Overlays/Victory/Restart");
		var dRestart  = GetNodeOrNull<Button>("Overlays/Defeat/Restart");

		if (archer == null || cannon == null || frost == null || startWave == null || upgrade == null || sell == null || menuBtn == null ||
			vBack == null || dBack == null || vRestart == null || dRestart == null)
		{
			GD.PushError("HUD: Brakuje któregoś z przycisków. Sprawdź Game.tscn i nazwy nodów.");
			return;
		}

		archer.Pressed += () => _gc?.SetSelectedTower(TowerType.Archer);
		cannon.Pressed += () => _gc?.SetSelectedTower(TowerType.Cannon);
		frost.Pressed  += () => _gc?.SetSelectedTower(TowerType.Frost);

		startWave.Pressed += () => _gc?.StartWave();
		upgrade.Pressed   += () => _gc?.UpgradeSelected();
		sell.Pressed      += () => _gc?.SellSelected();
		menuBtn.Pressed   += () => _gc?.BackToMenu();

		vBack.Pressed     += () => _gc?.BackToMenu();
		dBack.Pressed     += () => _gc?.BackToMenu();

		vRestart.Pressed  += () => _gc?.RestartLevel();
		dRestart.Pressed  += () => _gc?.RestartLevel();
	}

	public void Refresh(GameEngine e)
	{
		SetHearts(e.Lives);

		var waveLabel = GetNodeOrNull<Label>("TopBar/WaveLabel");
		if (waveLabel != null)
			waveLabel.Text = "WAVE 1/1";

		var coinsLabel = GetNodeOrNull<Label>("TopBar/CoinsLabel");
		if (coinsLabel != null)
			coinsLabel.Text = e.Coins.ToString();

		Mark("BottomBar/Archer", e.SelectedTowerType == TowerType.Archer);
		Mark("BottomBar/Cannon", e.SelectedTowerType == TowerType.Cannon);
		Mark("BottomBar/Frost", e.SelectedTowerType == TowerType.Frost);

		var startWave = GetNodeOrNull<Button>("BottomBar/StartWave");
		if (startWave != null)
			startWave.Disabled = e.WaveRunning || e.Victory || e.Defeat;

		var upgrade = GetNodeOrNull<Button>("BottomBar/Upgrade");
		if (upgrade != null)
			upgrade.Disabled = e.SelectedTowerIndex is null || e.WaveRunning || e.Victory || e.Defeat;

		var sell = GetNodeOrNull<Button>("BottomBar/Sell");
		if (sell != null)
			sell.Disabled = e.SelectedTowerIndex is null || e.WaveRunning || e.Victory || e.Defeat;

		var victory = GetNodeOrNull<Control>("Overlays/Victory");
		if (victory != null)
			victory.Visible = e.Victory;

		var defeat = GetNodeOrNull<Control>("Overlays/Defeat");
		if (defeat != null)
			defeat.Visible = e.Defeat;
	}

	private void SetHearts(int lives)
	{
		for (int i = 1; i <= 3; i++)
		{
			var heart = GetNodeOrNull<TextureRect>($"TopBar/LivesIcons/Heart{i}");
			if (heart != null)
				heart.Visible = i <= lives;
		}
	}

	private void Mark(string path, bool active)
	{
		var btn = GetNodeOrNull<Button>(path);
		if (btn != null)
			btn.Modulate = active ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.65f);
	}

	public void ShowVictory()
	{
		var victory = GetNodeOrNull<Control>("Overlays/Victory");
		if (victory != null) victory.Visible = true;
	}

	public void ShowDefeat()
	{
		var defeat = GetNodeOrNull<Control>("Overlays/Defeat");
		if (defeat != null) defeat.Visible = true;
	}
}
