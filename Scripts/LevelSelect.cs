using Godot;

public partial class LevelSelect : Control
{
	public override void _Ready()
	{
		GetNode<Button>("Panel/Maps/Easy").Pressed   += () => StartMap(0);
		GetNode<Button>("Panel/Maps/Medium").Pressed += () => StartMap(1);
		GetNode<Button>("Panel/Maps/Hard").Pressed   += () => StartMap(2);
		GetNode<Button>("Panel/Back").Pressed        += () => SceneNav.GoTo(GetTree(), ScenePaths.MainMenu);
	}

	private void StartMap(int mapId)
	{
		GameSession.StartFresh();
		GameSession.CurrentMap = mapId;
		SceneNav.GoTo(GetTree(), ScenePaths.Game);
	}
}
