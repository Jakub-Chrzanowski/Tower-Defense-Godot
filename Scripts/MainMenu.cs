using Godot;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("UI/Start").Pressed += () => SceneNav.GoTo(GetTree(), ScenePaths.LevelSelect);
        GetNode<Button>("UI/Options").Pressed += () => SceneNav.GoTo(GetTree(), ScenePaths.Options);
        GetNode<Button>("UI/Exit").Pressed += () => GetTree().Quit();
    }
}
