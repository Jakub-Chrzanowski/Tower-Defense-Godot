using Godot;

public partial class Options : Control
{
    public override void _Ready()
    {
        GetNode<Button>("Panel/Back").Pressed += () => SceneNav.GoTo(GetTree(), ScenePaths.MainMenu);
    }
}
