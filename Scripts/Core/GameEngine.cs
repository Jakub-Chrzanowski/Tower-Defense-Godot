public class GameEngine
{
	public int Gold = 150;
	public int Lives = 10;
	public int WaveIndex = 0;
	public bool WaveRunning = false;

	public void StartWave()
	{
		WaveRunning = true;
	}

	public void EndWave()
	{
		WaveRunning = false;

		// nagroda za falę
		Gold += 50 + WaveIndex * 10;

		WaveIndex++;
	}

	public void EnemyKilled(int reward)
	{
		Gold += reward;
	}

	public void EnemyLeak()
	{
		Lives--;
	}
}
