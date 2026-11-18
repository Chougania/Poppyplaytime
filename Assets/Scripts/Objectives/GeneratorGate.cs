using UnityEngine;
using UnityEngine.AI;

// Porte verrouillée ouverte par un générateur : donne accès à une nouvelle zone.
// Elle s'ouvre (en s'enfonçant dans le sol ou en se levant) quand le générateur choisi est réparé.
// Aucun réseau à configurer : l'état vient de la progression des générateurs déjà synchronisée par le GameManager.
public class GeneratorGate : MonoBehaviour
{
	[Header("Quel générateur l'ouvre ?")]
	[Range(1, 4)]
	public int generatorNumber = 1;

	[Tooltip("Coché : la porte s'ouvre quand AU MOINS 'requiredCount' générateurs (n'importe lesquels) sont réparés.")]
	public bool useCount;

	[Min(1)] public int requiredCount = 1;

	[Header("Mouvement")]
	[Min(0.5f)] public float openDistance = 3f;

	[Min(0.1f)] public float speed = 1.2f;

	[Tooltip("Coché : la porte s'enfonce dans le sol. Décoché : elle se lève.")]
	public bool slideDown = true;

	[Header("Affichage")]
	public bool showLockedHint = true;

	private Vector3 closedPosition;
	private float openAmount;
	private GameManager gm;
	private Collider[] colliders;
	private NavMeshObstacle obstacle;
	private float lastAmount;
	private GUIStyle hintStyle;

	private void Awake()
	{
		closedPosition = transform.position;
		colliders = GetComponentsInChildren<Collider>();
		BuildObstacle();
	}

	private void BuildObstacle()
	{
		if (colliders.Length == 0)
		{
			return;
		}
		Bounds b = colliders[0].bounds;
		for (int i = 1; i < colliders.Length; i++)
		{
			b.Encapsulate(colliders[i].bounds);
		}
		obstacle = gameObject.GetComponent<NavMeshObstacle>();
		if (obstacle == null)
		{
			obstacle = gameObject.AddComponent<NavMeshObstacle>();
		}
		Vector3 ls = transform.lossyScale;
		obstacle.shape = NavMeshObstacleShape.Box;
		obstacle.center = transform.InverseTransformPoint(b.center);
		obstacle.size = new Vector3(
			b.size.x / Mathf.Max(0.0001f, Mathf.Abs(ls.x)),
			b.size.y / Mathf.Max(0.0001f, Mathf.Abs(ls.y)),
			b.size.z / Mathf.Max(0.0001f, Mathf.Abs(ls.z)));
		obstacle.carving = true;
		obstacle.carveOnlyStationary = true;
	}

	public bool IsLocked
	{
		get { return openAmount < 0.9f; }
	}

	private bool ShouldBeOpen()
	{
		if (gm == null || gm.GenTotal <= 0)
		{
			return false; // hors manche : tout reste verrouillé
		}
		if (useCount)
		{
			return gm.GenDone >= Mathf.Min(requiredCount, gm.GenTotal);
		}
		// En solo il n'y a que 2 générateurs : les portes 3 et 4 s'ouvrent avec le dernier générateur en jeu.
		int index = Mathf.Min(generatorNumber - 1, gm.GenTotal - 1);
		return gm.GenProgress(index) >= 1f;
	}

	private void Update()
	{
		if (gm == null)
		{
			gm = FindObjectOfType<GameManager>();
		}
		float target = ShouldBeOpen() ? 1f : 0f;
		openAmount = Mathf.MoveTowards(openAmount, target, Time.deltaTime * speed / Mathf.Max(0.01f, openDistance));
		Vector3 dir = slideDown ? Vector3.down : Vector3.up;
		transform.position = closedPosition + dir * (openDistance * openAmount);

		bool locked = IsLocked;
		for (int i = 0; i < colliders.Length; i++)
		{
			if (colliders[i] != null)
			{
				colliders[i].enabled = locked;
			}
		}
		if (obstacle != null)
		{
			obstacle.enabled = locked;
		}

		if (Mathf.Abs(openAmount - lastAmount) > 0.0005f && Mathf.Approximately(Mathf.Round(lastAmount * 100f) % 25f, 0f))
		{
			ProceduralAudio.PlayAt(ProceduralAudio.Whoosh, transform.position, 0.3f);
		}
		if ((lastAmount < 0.999f && openAmount >= 0.999f) || (lastAmount > 0.001f && openAmount <= 0.001f))
		{
			ProceduralAudio.PlayAt(ProceduralAudio.Thud, transform.position, 0.6f);
		}
		lastAmount = openAmount;
	}

	private void OnGUI()
	{
		if (!showLockedHint || !IsLocked || gm == null || gm.GenTotal <= 0)
		{
			return;
		}
		Camera cam = Camera.main;
		if (cam == null)
		{
			return;
		}
		Vector3 anchor = closedPosition + Vector3.up * 1.6f;
		if (Vector3.Distance(cam.transform.position, anchor) > 9f)
		{
			return;
		}
		Vector3 sp = cam.WorldToScreenPoint(anchor);
		if (sp.z <= 0f)
		{
			return;
		}
		if (hintStyle == null)
		{
			hintStyle = new GUIStyle(GUI.skin.label);
			hintStyle.fontSize = 20;
			hintStyle.fontStyle = FontStyle.Bold;
			hintStyle.alignment = TextAnchor.MiddleCenter;
		}
		hintStyle.normal.textColor = new Color(1f, 0.35f, 0.3f);
		string need = useCount
			? "répare " + Mathf.Min(requiredCount, gm.GenTotal) + " générateur(s)"
			: "répare le générateur " + (Mathf.Min(generatorNumber, gm.GenTotal));
		GUI.Label(new Rect(sp.x - 160f, Screen.height - sp.y - 15f, 320f, 30f), "VERROUILLÉ  -  " + need, hintStyle);
	}
}
