using Godot;

public static class ScenePaths
{
	public const string MainMenu   = "res://Scenes/MainMenu.tscn";
	public const string Options    = "res://Scenes/Options.tscn";
	public const string LevelSelect = "res://Scenes/LevelSelect.tscn";
	public const string Game       = "res://Scenes/Game.tscn";
}

public static class SceneNav
{
	public static void GoTo(SceneTree tree, string scenePath) => tree.ChangeSceneToFile(scenePath);
}

public static class GameSession
{
	public const int MapCount = 3;

	// Aktualna mapa (0=Easy, 1=Medium, 2=Hard)
	public static int CurrentMap { get; set; } = 0;

	// Pieniądze przenoszone między mapami
	public static int PersistentCoins { get; set; } = 120;

	// Wejście do gry – reset całej sesji i start od Easy
	public static void StartFresh()
	{
		CurrentMap = 0;
		PersistentCoins = 120;
	}

	// Przejście do następnej mapy; zwraca false jeśli nie ma więcej map
	public static bool AdvanceMap()
	{
		CurrentMap++;
		return CurrentMap < MapCount;
	}

	public static bool IsLastMap => CurrentMap >= MapCount - 1;

	// Kompatybilność wsteczna (używane w Hud do etykiety)
	public static int SelectedMapId => CurrentMap;
}


public static class GameSettings
{
	public static bool ShowFps { get; set; } = false;
}
