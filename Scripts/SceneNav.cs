using Godot;

public static class ScenePaths
{
	public const string MainMenu = "res://Scenes/MainMenu.tscn";
<<<<<<< HEAD
	public const string Options = "res://Scenes/Options.tscn";
	public const string LevelSelect = "res://Scenes/LevelSelect.tscn";
	public const string Game = "res://Scenes/Game.tscn";
=======
	public const string Options  = "res://Scenes/Options.tscn";
	public const string Game     = "res://Scenes/Game.tscn";
>>>>>>> parent of c1670f3 (Merge branch 'main' of https://github.com/Jakub-Chrzanowski/Tower-Defense-Godot)
}

public static class SceneNav
{
<<<<<<< HEAD
	public static void GoTo(SceneTree tree, string scenePath) => tree.ChangeSceneToFile(scenePath);
}

public static class GameSession
{
	public static int SelectedMapId { get; set; } = 0;
=======
	public static void GoTo(SceneTree tree, string scenePath)
	{
		tree.ChangeSceneToFile(scenePath);
	}
>>>>>>> parent of c1670f3 (Merge branch 'main' of https://github.com/Jakub-Chrzanowski/Tower-Defense-Godot)
}
