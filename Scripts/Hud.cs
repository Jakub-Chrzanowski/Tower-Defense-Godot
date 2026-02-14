using Godot;

public partial class Hud : Control
{
    private GameController? _gc;

    public void Bind(GameController gc)
    {
        _gc = gc;

        GetNode<Button>("BottomBar/Archer").Pressed += () => _gc.SetSelectedTower(TowerType.Archer);
        GetNode<Button>("BottomBar/Cannon").Pressed += () => _gc.SetSelectedTower(TowerType.Cannon);
        GetNode<Button>("BottomBar/Frost").Pressed += () => _gc.SetSelectedTower(TowerType.Frost);

        GetNode<Button>("BottomBar/StartWave").Pressed += () => _gc.StartWave();
        GetNode<Button>("BottomBar/Upgrade").Pressed += () => _gc.UpgradeSelected();
        GetNode<Button>("BottomBar/Sell").Pressed += () => _gc.SellSelected();

        GetNode<Button>("TopBar/Menu").Pressed += () => _gc.BackToMenu();

        GetNode<Button>("Overlays/Victory/Back").Pressed += () => _gc.BackToMenu();
        GetNode<Button>("Overlays/Defeat/Back").Pressed += () => _gc.BackToMenu();
    }

    public void Refresh(GameEngine e)
    {
        GetNode<Label>("TopBar/WaveLabel").Text = $"WAVE {e.WaveIndex + 1}/{MapDefs.Maps[GameSession.SelectedMapId].Waves.Count}";
        GetNode<Label>("TopBar/CoinsLabel").Text = e.Coins.ToString();
        GetNode<Label>("TopBar/LivesLabel").Text = e.Lives.ToString();

        Mark("BottomBar/Archer", e.SelectedTowerType == TowerType.Archer);
        Mark("BottomBar/Cannon", e.SelectedTowerType == TowerType.Cannon);
        Mark("BottomBar/Frost", e.SelectedTowerType == TowerType.Frost);

        GetNode<Button>("BottomBar/StartWave").Disabled = e.WaveRunning || e.Victory || e.Defeat;
        GetNode<Button>("BottomBar/Upgrade").Disabled = (e.SelectedTowerIndex is null) || e.WaveRunning || e.Victory || e.Defeat;
        GetNode<Button>("BottomBar/Sell").Disabled = (e.SelectedTowerIndex is null) || e.WaveRunning || e.Victory || e.Defeat;

        GetNode<Control>("Overlays/Victory").Visible = e.Victory;
        GetNode<Control>("Overlays/Defeat").Visible = e.Defeat;
    }

    private void Mark(string nodePath, bool on)
    {
        var b = GetNode<Button>(nodePath);
        b.Modulate = on ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0.65f);
    }

    public void ShowVictory() => GetNode<Control>("Overlays/Victory").Visible = true;
    public void ShowDefeat() => GetNode<Control>("Overlays/Defeat").Visible = true;
}
