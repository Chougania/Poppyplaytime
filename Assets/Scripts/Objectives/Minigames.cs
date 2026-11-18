using UnityEngine;

// Système de minijeux : chaque minijeu est un objet autonome (Tick = logique, Draw = affichage OnGUI).
// Pour en ajouter un, crée une classe qui hérite de Minigame et lance-la depuis ObjectiveWorld.
public abstract class Minigame
{
	public bool Finished;
	public bool Success;

	private static Texture2D white;

	protected static Texture2D White
	{
		get
		{
			if (white == null)
			{
				white = new Texture2D(1, 1);
				white.SetPixel(0, 0, Color.white);
				white.Apply();
			}
			return white;
		}
	}

	public abstract void Tick(float deltaTime);

	public abstract void Draw();

	protected static void Fill(Rect r, Color c)
	{
		Color old = GUI.color;
		GUI.color = c;
		GUI.DrawTexture(r, White);
		GUI.color = old;
	}

	protected static GUIStyle Text(int size, Color color, TextAnchor anchor)
	{
		GUIStyle s = new GUIStyle(GUI.skin.label);
		s.fontSize = size;
		s.fontStyle = FontStyle.Bold;
		s.alignment = anchor;
		s.normal.textColor = color;
		return s;
	}
}

// Test de timing : une aiguille fait le tour du cadran, appuie sur [Q] dans la zone verte.
// Réussi = bonus de progression ; raté = la réparation recule et le générateur fait du bruit.
public class SkillCheckMinigame : Minigame
{
	public const KeyCode Key = KeyCode.Q;

	private const float Period = 1.3f;
	private float t;
	private readonly float zoneStart;
	private readonly float zoneSize = 0.16f;

	public SkillCheckMinigame()
	{
		zoneStart = Random.Range(0.3f, 0.82f);
	}

	public override void Tick(float deltaTime)
	{
		t += deltaTime / Period;
		if (Input.GetKeyDown(Key))
		{
			Finished = true;
			Success = t >= zoneStart && t <= zoneStart + zoneSize;
		}
		else if (t >= 1f)
		{
			Finished = true;
			Success = false;
		}
	}

	public override void Draw()
	{
		Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.42f);
		float radius = 84f;
		Matrix4x4 saved = GUI.matrix;

		for (int i = 0; i < 60; i++)
		{
			float f = i / 60f;
			bool inZone = f >= zoneStart && f <= zoneStart + zoneSize;
			GUI.matrix = saved;
			GUIUtility.RotateAroundPivot(f * 360f - 90f, center);
			Fill(new Rect(center.x + radius - 6f, center.y - 3f, 12f, 6f), inZone ? new Color(0.25f, 1f, 0.35f) : new Color(1f, 1f, 1f, 0.55f));
		}

		GUI.matrix = saved;
		GUIUtility.RotateAroundPivot(t * 360f - 90f, center);
		Fill(new Rect(center.x, center.y - 2f, radius, 4f), new Color(1f, 0.2f, 0.2f));
		GUI.matrix = saved;

		GUI.Label(new Rect(center.x - 200f, center.y + radius + 14f, 400f, 30f), "TEST DE RÉPARATION  -  appuie sur [Q] dans la zone verte", Text(18, Color.white, TextAnchor.MiddleCenter));
	}
}

// Séquence de disjoncteur : recopie la suite de flèches avant la fin du temps.
// Les générateurs pairs (2 et 4) restent bloqués à 50 % tant que la séquence n'est pas réussie.
public class ArrowSequenceMinigame : Minigame
{
	private readonly KeyCode[] sequence;
	private int position;
	private float timeLeft = 7f;
	private const float TotalTime = 7f;

	public ArrowSequenceMinigame(int length)
	{
		KeyCode[] pool = { KeyCode.LeftArrow, KeyCode.UpArrow, KeyCode.RightArrow, KeyCode.DownArrow };
		sequence = new KeyCode[length];
		for (int i = 0; i < length; i++)
		{
			sequence[i] = pool[Random.Range(0, pool.Length)];
		}
	}

	private static string Glyph(KeyCode k)
	{
		switch (k)
		{
		case KeyCode.LeftArrow: return "←";
		case KeyCode.UpArrow: return "↑";
		case KeyCode.RightArrow: return "→";
		default: return "↓";
		}
	}

	public override void Tick(float deltaTime)
	{
		timeLeft -= deltaTime;
		if (timeLeft <= 0f)
		{
			Finished = true;
			Success = false;
			return;
		}
		KeyCode[] pool = { KeyCode.LeftArrow, KeyCode.UpArrow, KeyCode.RightArrow, KeyCode.DownArrow };
		for (int i = 0; i < pool.Length; i++)
		{
			if (!Input.GetKeyDown(pool[i]))
			{
				continue;
			}
			if (pool[i] == sequence[position])
			{
				position++;
				if (position >= sequence.Length)
				{
					Finished = true;
					Success = true;
				}
			}
			else
			{
				Finished = true;
				Success = false;
			}
			return;
		}
	}

	public override void Draw()
	{
		float w = 520f;
		float h = 170f;
		Rect box = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.3f, w, h);
		Fill(box, new Color(0.04f, 0.05f, 0.07f, 0.92f));
		Fill(new Rect(box.x, box.y, w, 4f), new Color(1f, 0.82f, 0.2f));

		GUI.Label(new Rect(box.x, box.y + 12f, w, 30f), "DISJONCTEUR  -  recopie la séquence avec les flèches", Text(18, Color.white, TextAnchor.MiddleCenter));

		float cell = w / (sequence.Length + 1f);
		for (int i = 0; i < sequence.Length; i++)
		{
			Color c = i < position ? new Color(0.25f, 1f, 0.35f) : (i == position ? new Color(1f, 0.82f, 0.2f) : new Color(1f, 1f, 1f, 0.55f));
			GUI.Label(new Rect(box.x + cell * (i + 0.5f), box.y + 50f, cell, 60f), Glyph(sequence[i]), Text(44, c, TextAnchor.MiddleCenter));
		}

		float ratio = Mathf.Clamp01(timeLeft / TotalTime);
		Fill(new Rect(box.x + 20f, box.y + h - 26f, w - 40f, 10f), new Color(0f, 0f, 0f, 0.6f));
		Fill(new Rect(box.x + 20f, box.y + h - 26f, (w - 40f) * ratio, 10f), Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.25f, 1f, 0.35f), ratio));
	}
}
