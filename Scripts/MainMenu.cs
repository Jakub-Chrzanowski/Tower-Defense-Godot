using Godot;
using System;

public partial class MainMenu : Control
{
	public override void _Ready()
	{
		var start   = FindButton("StartButton");
		var options = FindButton("OptionsButton");
		var exit    = FindButton("ExitButton");

		// Zamiast przechodzić do LevelSelect – startujemy sesję od Easy
		start.Pressed += () =>
		{
			GameSession.StartFresh();
			SceneNav.GoTo(GetTree(), ScenePaths.Game);
		};
		options.Pressed += () => SceneNav.GoTo(GetTree(), ScenePaths.Options);
		exit.Pressed    += () => GetTree().Quit();
	}

	private BaseButton FindButton(string name)
	{
		var b = GetNodeOrNull<BaseButton>($"UI/Buttons/{name}");
		if (b != null) return b;
		b = FindChild(name, recursive: true, owned: false) as BaseButton;
		if (b != null) return b;
		throw new Exception($"Nie znaleziono przycisku: {name}");
	}
}
