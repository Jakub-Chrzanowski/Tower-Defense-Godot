using Godot;

public partial class Options : Control
{
	private CheckButton? _fpsToggle;

	public override void _Ready()
	{
		GetNode<Button>("Panel/Back").Pressed += () => SceneNav.GoTo(GetTree(), ScenePaths.MainMenu);

		_fpsToggle = GetNodeOrNull<CheckButton>("Panel/FpsToggle");
		if (_fpsToggle != null)
		{
			_fpsToggle.ButtonPressed = GameSettings.ShowFps;
			_fpsToggle.Toggled += (on) =>
			{
				GameSettings.ShowFps = on;
			};
		}
	}
}
