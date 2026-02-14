using Godot;

public partial class Hud : Control
{
	private GameController? _gc;

	public void Bind(GameController gc)
	{
		_gc = gc;

	   
		var startWave = GetNodeOrNull<Button>("BottomBar/StartWave");
		var menuBtn   = GetNodeOrNull<TextureButton>("Menu");

		var vBack     = GetNodeOrNull<Button>("Overlays/Victory/Back");
		var dBack     = GetNodeOrNull<Button>("Overlays/Defeat/Back");
		var vRestart  = GetNodeOrNull<Button>("Overlays/Victory/Restart");
		var dRestart  = GetNodeOrNull<Button>("Overlays/Defeat/Restart");

		if (startWave == null) { GD.PushError("HUD: Nie znaleziono BottomBar/StartWave (sprawdź nazwy nodów w Game.tscn)."); return; }
		if (menuBtn   == null) { GD.PushError("HUD: Nie znaleziono TopBar/Menu."); return; }
		if (vBack     == null) { GD.PushError("HUD: Nie znaleziono Overlays/Victory/Back."); return; }
		if (dBack     == null) { GD.PushError("HUD: Nie znaleziono Overlays/Defeat/Back."); return; }
		if (vRestart  == null) { GD.PushError("HUD: Nie znaleziono Overlays/Victory/Restart."); return; }
		if (dRestart  == null) { GD.PushError("HUD: Nie znaleziono Overlays/Defeat/Restart."); return; }

		startWave.Pressed += () => _gc?.StartWave();
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
		if (waveLabel != null) waveLabel.Text = "WAVE 1/1";

		var startWave = GetNodeOrNull<Button>("BottomBar/StartWave");
		if (startWave != null) startWave.Disabled = e.WaveRunning || e.Victory || e.Defeat;

		var victory = GetNodeOrNull<Control>("Overlays/Victory");
		if (victory != null) victory.Visible = e.Victory;

		var defeat = GetNodeOrNull<Control>("Overlays/Defeat");
		if (defeat != null) defeat.Visible = e.Defeat;
	}

	private void SetHearts(int lives)
	{
		for (int i = 1; i <= 3; i++)
		{
			var heart = GetNodeOrNull<TextureRect>($"Heart{i}");
			if (heart != null) heart.Visible = i <= lives;
		}
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
