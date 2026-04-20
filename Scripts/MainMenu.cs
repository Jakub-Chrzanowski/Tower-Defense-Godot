using Godot;
using System;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		GD.Print("MainMenu READY");

		var start   = GetNodeOrNull<BaseButton>("Start");
		var options = GetNodeOrNull<BaseButton>("Options");
		var exit    = GetNodeOrNull<BaseButton>("Exit");

		start.Pressed   += () => SceneNav.GoTo(GetTree(), ScenePaths.Game);
		options.Pressed += () => SceneNav.GoTo(GetTree(), ScenePaths.Options);
		exit.Pressed    += () => GetTree().Quit();
	}

  
}
