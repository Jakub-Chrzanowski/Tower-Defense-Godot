using Godot;
using System.Collections.Generic;

public partial class GameController : Node2D
{
	GameEngine engine = new();

	PackedScene enemyScene =
		GD.Load<PackedScene>("res://Scenes/Enemy.tscn");

	List<Enemy> alive = new();

	public async void StartWave()
	{
		if (engine.WaveRunning)
			return;

		engine.StartWave();

		int count = 5 + engine.WaveIndex * 2;

		for (int i = 0; i < count; i++)
		{
			SpawnEnemy();

			await ToSignal(
				GetTree().CreateTimer(0.8f),
				"timeout");
		}

		// czekaj aż wszyscy umrą
		while (alive.Count > 0)
		{
			await ToSignal(
				GetTree().CreateTimer(0.5f),
				"timeout");
		}

		engine.EndWave();

		GD.Print("Wave: " + engine.WaveIndex +
				 " Gold: " + engine.Gold);
	}

	void SpawnEnemy()
	{
		var e = enemyScene.Instantiate<Enemy>();

		e.Setup(engine.WaveIndex);

		AddChild(e);
		alive.Add(e);
	}

	public void OnEnemyKilled(Enemy e)
	{
		engine.EnemyKilled(e.Reward);
		alive.Remove(e);

		// SFX kill
		var sfx = GetNodeOrNull<AudioStreamPlayer>("SfxKill");
		sfx?.Play();
	}
	private void _on_start_wave_button_pressed()
	{
		StartWave();
	}	
}
