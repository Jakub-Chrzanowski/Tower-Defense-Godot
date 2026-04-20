using Godot;

public partial class Enemy : Node2D
{
	public int Hp;
	public int Reward;
	public float Speed;

	public void Setup(int wave)
	{
		Hp = 10 + wave * 3;
		Speed = 60 + wave * 2;
		Reward = 5 + wave;

		// sprite zależny od typu
		var sprite = GetNodeOrNull<Sprite2D>("Sprite2D");

		if (sprite != null)
		{
			if (wave % 5 == 0)
				sprite.Texture = GD.Load<Texture2D>("res://assets/sprites/enemy_tank.png");
			else if (wave % 2 == 0)
				sprite.Texture = GD.Load<Texture2D>("res://assets/sprites/enemy_fast.png");
			else
				sprite.Texture = GD.Load<Texture2D>("res://assets/sprites/enemy_grunt.png");
		}
	}

	public override void _Process(double delta)
	{
		Position += new Vector2(Speed * (float)delta, 0);
	}

	public void TakeDamage(int dmg)
	{
		Hp -= dmg;

		if (Hp <= 0)
		{
			GetParent<GameController>()
				.OnEnemyKilled(this);

			QueueFree();
		}
	}
}
