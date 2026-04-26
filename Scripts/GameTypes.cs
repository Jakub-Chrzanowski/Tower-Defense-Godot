using Godot;

/// <summary>Typ wieży wybierany przez gracza.</summary>
public enum TowerType
{
	/// <summary>Szybkostrzelna wieża łucznicza — niskie obrażenia, wysoka kadencja.</summary>
	Archer,
	/// <summary>Wieża armatnia — wysokie obrażenia, efekt odłamkowy (splash).</summary>
	Cannon,
	/// <summary>Wieża mroźna — spowalnia trafione cele.</summary>
	Frost
}

/// <summary>Typ wroga poruszającego się po ścieżce.</summary>
public enum EnemyType
{
	/// <summary>Standardowy wróg — zrównoważone statystyki.</summary>
	Grunt,
	/// <summary>Bardzo szybki, ale kruchy.</summary>
	Fast,
	/// <summary>Powolny, bardzo wytrzymały.</summary>
	Tank,
	/// <summary>Leci po linii prostej do końca ścieżki, ignoruje splash.</summary>
	Flying,
	/// <summary>Boss — pojawia się jako ostatni; dotarcie do celu kończy grę natychmiastową przegraną.</summary>
	Boss
}

/// <summary>
/// Reprezentuje pojedynczego wroga na planszy.
/// Przechowuje pozycję, statystyki oraz stan ruchu po ścieżce.
/// </summary>
public sealed class Enemy
{
	/// <summary>Typ wroga determinujący wygląd i zachowanie.</summary>
	public EnemyType Type;
	/// <summary>Aktualne punkty życia.</summary>
	public float Hp;
	/// <summary>Maksymalne punkty życia (używane do rysowania paska HP).</summary>
	public float MaxHp;
	/// <summary>Prędkość ruchu w pikselach na sekundę.</summary>
	public float Speed;
	/// <summary>Aktualna pozycja w pikselach przestrzeni świata.</summary>
	public Vector2 Pos;
	/// <summary>Indeks bieżącego segmentu ścieżki.</summary>
	public int Segment;
	/// <summary>Czy wróg dotarł do końca ścieżki i powinien zostać usunięty.</summary>
	public bool ReachedEnd;
	/// <summary>Promień kolizji używany do rysowania i trafień splash.</summary>
	public float Radius;
	/// <summary>Nagroda w monetach przyznawana po zabiciu.</summary>
	public int Reward;
	/// <summary>Mnożnik prędkości pod wpływem spowolnienia (1.0 = brak efektu).</summary>
	public float SlowMultiplier = 1.0f;
	/// <summary>Pozostały czas spowolnienia w sekundach.</summary>
	public float SlowTimeLeft   = 0.0f;
	/// <summary>Czy ten wróg jest bossem — wpływa na zachowanie po dotarciu do końca.</summary>
	public bool IsBoss          = false;
}

/// <summary>
/// Reprezentuje postawioną wieżę na jednym z podkładek planszy.
/// </summary>
public sealed class Tower
{
	/// <summary>Typ wieży określający mechanikę strzelania.</summary>
	public TowerType Type;
	/// <summary>Pozycja środka wieży w pikselach.</summary>
	public Vector2 Pos;
	/// <summary>Poziom ulepszenia (1–3).</summary>
	public int Level;
	/// <summary>Zasięg strzału w pikselach.</summary>
	public float Range;
	/// <summary>Obrażenia zadawane celowi przy trafieniu.</summary>
	public float Damage;
	/// <summary>Czas między strzałami w sekundach.</summary>
	public float FireInterval;
	/// <summary>Pozostały czas do następnego strzału.</summary>
	public float Cooldown;
	/// <summary>Promień eksplozji odłamkowej (tylko <see cref="TowerType.Cannon"/>).</summary>
	public float SplashRadius;
	/// <summary>Mnożnik spowolnienia nakładany na cel (tylko <see cref="TowerType.Frost"/>).</summary>
	public float SlowMultiplier;
	/// <summary>Czas trwania spowolnienia w sekundach (tylko <see cref="TowerType.Frost"/>).</summary>
	public float SlowDuration;
}

/// <summary>
/// Pocisk wystrzelony przez wieżę — porusza się po linii prostej przez krótki czas.
/// </summary>
public sealed class Projectile
{
	/// <summary>Aktualna pozycja pocisku w pikselach.</summary>
	public Vector2 Pos;
	/// <summary>Wektor prędkości (kierunek × prędkość) w pikselach na sekundę.</summary>
	public Vector2 Vel;
	/// <summary>Pozostały czas życia pocisku w sekundach.</summary>
	public float TimeLeft;
	/// <summary>Typ wieży, która wystrzeliła pocisk — używany do wyboru tekstury.</summary>
	public TowerType SourceType;
}

/// <summary>
/// Podkładka na planszy, na której można postawić wieżę.
/// </summary>
public sealed class Pad
{
	/// <summary>Pozycja środka podkładki w przestrzeni znormalizowanej (0–1).</summary>
	public Vector2 CenterNorm;
	/// <summary>Rozmiar podkładki w pikselach.</summary>
	public float SizePx;
	/// <summary>Czy na podkładce stoi już wieża.</summary>
	public bool HasTower;
}

/// <summary>
/// Definicja mapy — ścieżka wrogów i rozmieszczenie podkładek.
/// </summary>
public sealed class MapDef
{
	/// <summary>Punkty kontrolne ścieżki w przestrzeni znormalizowanej (0–1).</summary>
	public required Vector2[] Path;
	/// <summary>Definicje podkładek na tej mapie.</summary>
	public required PadDef[]  Pads;
	/// <summary>Wyświetlana nazwa mapy (np. "Easy").</summary>
	public required string    Name;
}

/// <summary>
/// Niezmienna definicja pojedynczej podkładki używana przy inicjalizacji mapy.
/// </summary>
/// <param name="CenterNorm">Pozycja środka w przestrzeni znormalizowanej.</param>
/// <param name="SizePx">Rozmiar w pikselach.</param>
public readonly record struct PadDef(Vector2 CenterNorm, float SizePx);
