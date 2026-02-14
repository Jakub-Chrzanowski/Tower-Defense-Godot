using Godot;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		var start = GetNode<TextureButton>("UI/Buttons/StartButton");
		var options = GetNode<TextureButton>("UI/Buttons/OptionsButton");
		var exit = GetNode<TextureButton>("UI/Buttons/ExitButton");

		start.Pressed += () => SceneNav.GoTo(GetTree(), ScenePaths.LevelSelect);
		options.Pressed += () => SceneNav.GoTo(GetTree(), ScenePaths.Options);
		exit.Pressed += () => GetTree().Quit();
	}
}
