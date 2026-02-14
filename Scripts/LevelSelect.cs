using Godot;

public partial class LevelSelect : Control
{
	public override void _Ready()
	{
		GetNode<Button>("UI/Back").Pressed += () => SceneNav.GoTo(GetTree(), ScenePaths.MainMenu);

		GetNode<Button>("UI/Easy").Pressed += () => StartMap(0);
		GetNode<Button>("UI/Medium").Pressed += () => StartMap(1);
		GetNode<Button>("UI/Hard").Pressed += () => StartMap(2);
	}

	private void StartMap(int mapId)
	{
		GameSession.SelectedMapId = mapId;
		SceneNav.GoTo(GetTree(), ScenePaths.Game);
	}
}
