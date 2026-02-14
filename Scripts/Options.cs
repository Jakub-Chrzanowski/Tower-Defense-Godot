using Godot;

public partial class Options : Control
{
    private const string SettingsPath = "user://settings.cfg";

    public override void _Ready()
    {
        var back = GetNode<Button>("UI/Back");
        back.Pressed += () => SceneNav.GoTo(GetTree(), ScenePaths.MainMenu);

        var music = GetNode<HSlider>("UI/MusicSlider");
        var showFps = GetNode<CheckBox>("UI/ShowFps");
        LoadSettings(music, showFps);

        music.ValueChanged += _ => SaveSettings(music, showFps);
        showFps.Toggled += _ => SaveSettings(music, showFps);
    }

    private static void LoadSettings(HSlider music, CheckBox showFps)
    {
        var cfg = new ConfigFile();
        if (cfg.Load(SettingsPath) != Error.Ok) return;

        music.Value = (double)cfg.GetValue("audio", "music", 70.0);
        showFps.ButtonPressed = (bool)cfg.GetValue("ui", "show_fps", false);
    }

    private static void SaveSettings(HSlider music, CheckBox showFps)
    {
        var cfg = new ConfigFile();
        cfg.SetValue("audio", "music", music.Value);
        cfg.SetValue("ui", "show_fps", showFps.ButtonPressed);
        cfg.Save(SettingsPath);
    }
}
