using Godot;

/// <summary>
/// Stałe ze ścieżkami do plików scen Godota.
/// </summary>
public static class ScenePaths
{
    /// <summary>Scena menu głównego.</summary>
    public const string MainMenu    = "res://Scenes/MainMenu.tscn";
    /// <summary>Scena opcji.</summary>
    public const string Options     = "res://Scenes/Options.tscn";
    /// <summary>Scena wyboru mapy.</summary>
    public const string LevelSelect = "res://Scenes/LevelSelect.tscn";
    /// <summary>Scena rozgrywki.</summary>
    public const string Game        = "res://Scenes/Game.tscn";
}

/// <summary>
/// Pomocnicza klasa do przełączania scen w drzewie Godota.
/// </summary>
public static class SceneNav
{
    /// <summary>
    /// Natychmiast przełącza aktywną scenę na podaną ścieżkę.
    /// </summary>
    /// <param name="tree">Aktywne drzewo sceny.</param>
    /// <param name="scenePath">Ścieżka zasobu docelowej sceny (np. <see cref="ScenePaths.Game"/>).</param>
    public static void GoTo(SceneTree tree, string scenePath) => tree.ChangeSceneToFile(scenePath);
}

/// <summary>
/// Globalny stan sesji gry — przechowuje dane między scenami.
/// </summary>
public static class GameSession
{
    /// <summary>Łączna liczba dostępnych map.</summary>
    public const int MapCount = 3;

    /// <summary>Indeks aktualnie wybranej mapy (0 = Easy, 1 = Medium, 2 = Hard).</summary>
    public static int CurrentMap { get; set; } = 0;

    /// <summary>
    /// Monety przenoszone między mapami.
    /// Zapisywane przez <see cref="GameEngine.SaveCoinsToSession"/> po wygranej fali.
    /// </summary>
    public static int PersistentCoins { get; set; } = 120;

    /// <summary>
    /// Resetuje sesję do stanu początkowego — mapa Easy, 120 monet.
    /// </summary>
    public static void StartFresh()
    {
        CurrentMap      = 0;
        PersistentCoins = 120;
    }

    /// <summary>
    /// Przechodzi do następnej mapy.
    /// </summary>
    /// <returns><c>true</c> jeśli następna mapa istnieje, <c>false</c> po ostatniej mapie.</returns>
    public static bool AdvanceMap()
    {
        CurrentMap++;
        return CurrentMap < MapCount;
    }

    /// <summary>Czy aktualna mapa jest ostatnią dostępną.</summary>
    public static bool IsLastMap => CurrentMap >= MapCount - 1;

    /// <summary>Alias dla <see cref="CurrentMap"/> — kompatybilność wsteczna.</summary>
    public static int SelectedMapId => CurrentMap;
}
