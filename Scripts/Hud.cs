using Godot;

/// <summary>
/// Kontroluje interfejs użytkownika podczas rozgrywki.
/// Binduje przyciski do akcji <see cref="GameController"/> i odświeża
/// wyświetlane dane (życia, monety, nazwa mapy, stany przycisków).
/// </summary>
public partial class Hud : Control
{
	private GameController? _gc;

	/// <summary>
	/// Podłącza HUD do kontrolera gry i rejestruje wszystkie callbacki przycisków.
	/// Musi być wywołane raz po załadowaniu sceny.
	/// </summary>
	/// <param name="gc">Aktywny <see cref="GameController"/> dla bieżącej rozgrywki.</param>
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
	}

	/// <summary>
	/// Odświeża wszystkie elementy HUD na podstawie aktualnego stanu silnika gry.
	/// Wywoływane przy każdej zmianie stanu (<c>OnHudChanged</c>).
	/// </summary>
	/// <param name="e">Aktualny stan <see cref="GameEngine"/>.</param>
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

	/// <summary>
	/// Wyświetla nakładkę zwycięstwa.
	/// </summary>
	/// <param name="isLastMap">Jeśli <c>true</c>, pokazuje ekran gratulacji zamiast przycisku "Następna mapa".</param>
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

	/// <summary>Ukrywa nakładkę zwycięstwa.</summary>
	public void HideVictory() { var v = GetNodeOrNull<Control>("Overlays/Victory"); if (v != null) v.Visible = false; }

	/// <summary>Wyświetla nakładkę przegranej.</summary>
	public void ShowDefeat()  { var d = GetNodeOrNull<Control>("Overlays/Defeat");  if (d != null) d.Visible = true;  }

	/// <summary>Ukrywa nakładkę przegranej.</summary>
	public void HideDefeat()  { var d = GetNodeOrNull<Control>("Overlays/Defeat");  if (d != null) d.Visible = false; }

	/// <summary>
	/// Aktualizuje widoczność ikon serc na podstawie liczby pozostałych żyć.
	/// </summary>
	/// <param name="lives">Liczba żyć (0–3).</param>
	private void SetHearts(int lives)
	{
		for (int i = 1; i <= 3; i++)
		{
			var h = GetNodeOrNull<TextureRect>($"TopBar/LivesIcons/Heart{i}");
			if (h != null) h.Visible = i <= lives;
		}
	}

	/// <summary>
	/// Podświetla lub przyciemnia przycisk wieży zależnie od tego, czy jest aktywnie wybrany.
	/// </summary>
	/// <param name="path">Ścieżka węzła przycisku w drzewie sceny.</param>
	/// <param name="active">Czy przycisk ma być w stanie aktywnym.</param>
	private void Mark(string path, bool active)
	{
		var btn = GetNodeOrNull<Button>(path);
		if (btn != null) btn.Modulate = active ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.65f);
	}
}
