using Godot;

public static class ScenePaths
{
	public const string MainMenu = "res://Scenes/MainMenu.tscn";
	public const string Options  = "res://Scenes/Options.tscn";
	public const string Game     = "res://Scenes/Game.tscn";
}

public static class SceneNav
{
	public static void GoTo(SceneTree tree, string scenePath)
	{
		tree.ChangeSceneToFile(scenePath);
	}
}
