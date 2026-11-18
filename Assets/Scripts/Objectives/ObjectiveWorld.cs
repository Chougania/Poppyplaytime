using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Côté local (tous les joueurs) : fabrique les générateurs et la sortie, gère "maintenir E pour réparer",
// affiche l'objectif, les bruits (pour le monstre) et la direction de la sortie.
public class ObjectiveWorld : MonoBehaviour
{
	public static ObjectiveWorld Instance;

	private class Gen
	{
		public GameObject root;
		public Renderer lamp;
		public Light light;
		public AudioSource hum;
	}

	private struct Ping
	{
		public Vector3 pos;
		public float until;
	}

	private GameManager gm;
	private readonly List<Gen> gens = new List<Gen>();
	private readonly List<Ping> pings = new List<Ping>();
	private GameObject exitRoot;
	private Renderer exitGlow;
	private Light exitLight;
	private int builtTotal;
	private float buildRetryAt;
	private float repairAcc;
	private float repairSendAt;
	private bool escapeSent;
	private GUIStyle big;
	private GUIStyle small;
	private Shader shader;

	public void Init(GameManager g)
	{
		gm = g;
		Instance = this;
	}

	private void OnDestroy()
	{
		CancelGame();
		Clear();
		if (Instance == this)
		{
			Instance = null;
		}
	}

	public void AddPing(Vector3 pos)
	{
		Ping p = new Ping { pos = pos, until = Time.time + 4f };
		pings.Add(p);
		ProceduralAudio.Play2D(ProceduralAudio.Alarm, 0.35f);
	}

	// ------------------------------------------------------------------ placement (serveur)
	public static void ComputePlaces(Transform[] survivorSpawns, Transform monsterSpawn, Transform fallback, int count, out Vector3[] gens, out Vector3 exit)
	{
		gens = new Vector3[count];
		List<Transform> named = new List<Transform>();
		Transform exitNamed = null;
		foreach (Transform t in Object.FindObjectsOfType<Transform>())
		{
			if (t.name.StartsWith("GeneratorSpawn"))
			{
				named.Add(t);
			}
			else if (t.name == "ExitSpawn")
			{
				exitNamed = t;
			}
		}
		// GeneratorSpawn1, 2, 3, 4 : l'ordre doit suivre les noms pour que "générateur N" corresponde à la porte N
		named.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
		List<Vector3> bases = new List<Vector3>();
		if (survivorSpawns != null)
		{
			for (int i = 0; i < survivorSpawns.Length; i++)
			{
				if (survivorSpawns[i] != null)
				{
					bases.Add(survivorSpawns[i].position);
				}
			}
		}
		if (bases.Count == 0)
		{
			bases.Add(fallback.position);
		}
		Vector3 monsterPos = monsterSpawn != null ? monsterSpawn.position : fallback.position;

		for (int g = 0; g < count; g++)
		{
			if (g < named.Count)
			{
				gens[g] = named[g].position;
				continue;
			}
			gens[g] = FindSpot(bases, monsterPos, gens, g, 7f);
		}
		if (exitNamed != null)
		{
			exit = exitNamed.position;
			return;
		}
		Vector3 best = bases[0];
		float bestScore = -1f;
		for (int i = 0; i < 30; i++)
		{
			Vector3 c = FindSpot(bases, monsterPos, gens, count, 6f);
			float score = Vector3.Distance(c, monsterPos);
			if (score > bestScore)
			{
				bestScore = score;
				best = c;
			}
		}
		exit = best;
	}

	private static Vector3 FindSpot(List<Vector3> bases, Vector3 monsterPos, Vector3[] placed, int placedCount, float minGap)
	{
		Vector3 fallback = bases[Random.Range(0, bases.Count)] + new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
		for (int tries = 0; tries < 80; tries++)
		{
			Vector3 b = bases[Random.Range(0, bases.Count)];
			Vector2 c = Random.insideUnitCircle.normalized * Random.Range(6f, 24f);
			Vector3 origin = new Vector3(b.x + c.x, b.y + 1.5f, b.z + c.y);
			RaycastHit hit;
			if (!Physics.Raycast(origin, Vector3.down, out hit, 6f, ~0, QueryTriggerInteraction.Ignore) || hit.normal.y < 0.75f)
			{
				continue;
			}
			Vector3 p = hit.point;
			if (Physics.CheckSphere(p + Vector3.up * 1.1f, 0.7f, ~0, QueryTriggerInteraction.Ignore))
			{
				continue;
			}
			bool ok = Vector3.Distance(p, monsterPos) > 8f;
			for (int i = 0; i < placedCount && ok; i++)
			{
				if (Vector3.Distance(p, placed[i]) < minGap)
				{
					ok = false;
				}
			}
			if (ok)
			{
				return p;
			}
		}
		return fallback;
	}

	// ------------------------------------------------------------------ props locaux
	private Material Mat(Color c, bool glow)
	{
		if (shader == null)
		{
			shader = Shader.Find("Standard");
			if (shader == null)
			{
				shader = Shader.Find("Sprites/Default");
			}
		}
		Material m = new Material(shader);
		m.color = c;
		if (glow && m.HasProperty("_EmissionColor"))
		{
			m.EnableKeyword("_EMISSION");
			m.SetColor("_EmissionColor", c * 2f);
		}
		return m;
	}

	private GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collider)
	{
		GameObject g = GameObject.CreatePrimitive(t);
		if (!collider)
		{
			Destroy(g.GetComponent<Collider>());
		}
		g.transform.SetParent(parent, false);
		g.transform.localPosition = pos;
		g.transform.localScale = scale;
		g.GetComponent<Renderer>().sharedMaterial = m;
		return g;
	}

	private void Clear()
	{
		for (int i = 0; i < gens.Count; i++)
		{
			if (gens[i].root != null)
			{
				Destroy(gens[i].root);
			}
		}
		gens.Clear();
		if (exitRoot != null)
		{
			Destroy(exitRoot);
		}
		exitRoot = null;
		builtTotal = 0;
	}

	private void Build()
	{
		// Ne marque le monde comme construit qu'une fois TOUS les objets créés.
		// Sinon une exception (ex. police Unity) laissait builtTotal renseigné et
		// empêchait toute nouvelle tentative : les générateurs restaient absents.
		Clear();
		int targetTotal = gm != null ? gm.GenTotal : 0;
		if (targetTotal <= 0)
			return;

		try
		{
			for (int i = 0; i < targetTotal; i++)
			{
				Gen g = new Gen();
				g.root = new GameObject("Generator" + i);
				gens.Add(g); // enregistré immédiatement pour pouvoir nettoyer en cas d'erreur
				g.root.transform.position = gm.GenPos(i);
				Transform t = g.root.transform;
				Prim(PrimitiveType.Cube, t, new Vector3(0f, 0.55f, 0f), new Vector3(1.3f, 1.1f, 0.8f), Mat(new Color(0.18f, 0.2f, 0.22f), false), true);
				Prim(PrimitiveType.Cube, t, new Vector3(0f, 1.18f, 0f), new Vector3(1.0f, 0.12f, 0.6f), Mat(new Color(0.4f, 0.08f, 0.08f), false), false);
				Prim(PrimitiveType.Cylinder, t, new Vector3(0.45f, 0.5f, 0.45f), new Vector3(0.2f, 0.5f, 0.2f), Mat(new Color(0.5f, 0.45f, 0.1f), false), false);

				GameObject label = new GameObject("Label");
				label.transform.SetParent(t, false);
				label.transform.localPosition = new Vector3(0f, 2.2f, 0f);
				TextMesh text = label.AddComponent<TextMesh>();
				text.text = "GÉNÉRATEUR " + (i + 1);
				text.fontSize = 64;
				text.characterSize = 0.08f;
				text.anchor = TextAnchor.MiddleCenter;
				text.alignment = TextAlignment.Center;
				text.color = Color.white;
				// Arial.ttf n'est plus une police intégrée valide dans les versions récentes.
				Font runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
				if (runtimeFont != null)
				{
					text.font = runtimeFont;
					MeshRenderer labelRenderer = label.GetComponent<MeshRenderer>();
					if (labelRenderer != null && runtimeFont.material != null)
						labelRenderer.sharedMaterial = runtimeFont.material;
					label.AddComponent<FaceCameraLabel>();
				}
				else
				{
					// La signalétique ne doit jamais empêcher la création du générateur.
					Destroy(label);
				}

				GameObject lamp = Prim(PrimitiveType.Sphere, t, new Vector3(0f, 1.45f, 0f), Vector3.one * 0.3f, Mat(Color.red, true), false);
				g.lamp = lamp.GetComponent<Renderer>();
				GameObject lg = new GameObject("Light");
				lg.transform.SetParent(t, false);
				lg.transform.localPosition = new Vector3(0f, 1.6f, 0f);
				g.light = lg.AddComponent<Light>();
				g.light.range = 7f;
				g.light.intensity = 1.4f;
				g.light.color = Color.red;
			}

			exitRoot = new GameObject("ExitGate");
			exitRoot.transform.position = gm.ExitPos;
			Transform e = exitRoot.transform;
			Material frame = Mat(new Color(0.12f, 0.12f, 0.14f), false);
			Prim(PrimitiveType.Cube, e, new Vector3(-1.2f, 1.4f, 0f), new Vector3(0.3f, 2.8f, 0.4f), frame, false);
			Prim(PrimitiveType.Cube, e, new Vector3(1.2f, 1.4f, 0f), new Vector3(0.3f, 2.8f, 0.4f), frame, false);
			Prim(PrimitiveType.Cube, e, new Vector3(0f, 2.85f, 0f), new Vector3(2.7f, 0.3f, 0.4f), frame, false);
			GameObject glow = Prim(PrimitiveType.Cube, e, new Vector3(0f, 1.4f, 0f), new Vector3(2.1f, 2.6f, 0.06f), Mat(Color.red, true), false);
			exitGlow = glow.GetComponent<Renderer>();
			GameObject el = new GameObject("ExitLight");
			el.transform.SetParent(e, false);
			el.transform.localPosition = new Vector3(0f, 1.6f, 1.2f);
			exitLight = el.AddComponent<Light>();
			exitLight.range = 9f;
			exitLight.intensity = 1.8f;

			builtTotal = targetTotal;
			buildRetryAt = 0f;
			Debug.Log("[ObjectiveWorld] " + builtTotal + " générateur(s) et la sortie ont été créés.");
		}
		catch (System.Exception ex)
		{
			Clear();
			builtTotal = 0;
			buildRetryAt = Time.time + 2f;
			Debug.LogError("[ObjectiveWorld] Échec de création des générateurs. Nouvelle tentative dans 2 secondes. " + ex);
		}
	}

	private static DbUnityMovment LocalPlayer()
	{
		NetworkManager nm = NetworkManager.Singleton;
		if (nm == null || nm.LocalClient == null || nm.LocalClient.PlayerObject == null)
		{
			return null;
		}
		return nm.LocalClient.PlayerObject.GetComponent<DbUnityMovment>();
	}

	private static bool IsSurvivor(DbUnityMovment mv)
	{
		return mv != null && mv.survivorModelContainer != null && mv.survivorModelContainer.activeSelf &&
		       (mv.monsterModelContainer == null || !mv.monsterModelContainer.activeSelf);
	}

	private static bool IsMonster(DbUnityMovment mv)
	{
		return mv != null && mv.monsterModelContainer != null && mv.monsterModelContainer.activeSelf;
	}

	// ------------------------------------------------------------------ libération des alliés encartonnés
	private Minigame activeGame;
	private int activeGameGen;
	private int activeGameKind;
	private float nextSkillCheckAt;

	private void StartGame(int generator, int kind)
	{
		activeGame = kind == 1 ? (Minigame)new ArrowSequenceMinigame(5) : new SkillCheckMinigame();
		activeGameGen = generator;
		activeGameKind = kind;
		if (kind == 1)
			PoppyUIInputGate.SetMinigameOpen(true);
	}

	private void CancelGame()
	{
		if (activeGame == null)
			return;
		activeGame = null;
		PoppyUIInputGate.SetMinigameOpen(false);
	}

	private void StepGame(int nearGenerator)
	{
		if (activeGame == null)
			return;
		activeGame.Tick(Time.deltaTime);
		if (activeGame.Finished)
		{
			bool ok = activeGame.Success;
			int gen = activeGameGen;
			int kind = activeGameKind;
			CancelGame();
			gm.GeneratorMinigameServerRpc(gen, kind, ok);
			ProceduralAudio.Play2D(ok ? ProceduralAudio.Success : ProceduralAudio.Buzz, 0.7f);
		}
		else if (nearGenerator != activeGameGen)
		{
			CancelGame(); // le joueur s'est éloigné du générateur
		}
	}

	private float freeAcc;
	private float freeSendAt;
	private float freeShown;
	private DbUnityMovment[] playersCache = new DbUnityMovment[0];
	private float playersCacheAt;

	private DbUnityMovment NearestBoxedAlly(DbUnityMovment me, float maxDistance)
	{
		if (Time.time > playersCacheAt)
		{
			playersCache = FindObjectsOfType<DbUnityMovment>();
			playersCacheAt = Time.time + 0.5f;
		}
		DbUnityMovment best = null;
		float bestDist = maxDistance;
		for (int i = 0; i < playersCache.Length; i++)
		{
			DbUnityMovment p = playersCache[i];
			if (p == null || p == me || !p.IsBoxed)
			{
				continue;
			}
			float d = Vector3.Distance(me.transform.position, p.transform.position);
			if (d < bestDist)
			{
				bestDist = d;
				best = p;
			}
		}
		return best;
	}

	private void UpdateFreeing()
	{
		DbUnityMovment me = LocalPlayer();
		if (!gm.IsGameStarted || !IsSurvivor(me) || me.IsBoxed)
		{
			freeAcc = 0f;
			freeShown = 0f;
			return;
		}
		DbUnityMovment ally = NearestBoxedAlly(me, 3.2f);
		if (ally != null && Input.GetKey(KeyCode.F) && !PoppyUIInputGate.IsGameplayBlocked)
		{
			freeAcc += Time.deltaTime;
			freeShown = Mathf.Min(1f, freeShown + Time.deltaTime / 3f);
			if (Time.time >= freeSendAt)
			{
				gm.FreeBoxedServerRpc(ally.OwnerClientId, freeAcc);
				freeAcc = 0f;
				freeSendAt = Time.time + 0.3f;
			}
		}
		else
		{
			freeAcc = 0f;
			freeShown = Mathf.Max(0f, freeShown - Time.deltaTime);
		}
	}

	private void OnGUIFreeing()
	{
		DbUnityMovment me = LocalPlayer();
		if (!gm.IsGameStarted || !IsSurvivor(me) || me.IsBoxed)
		{
			return;
		}
		for (int i = 0; i < playersCache.Length; i++)
		{
			DbUnityMovment p = playersCache[i];
			if (p != null && p != me && p.IsBoxed)
			{
				Marker(p.transform.position, "ENCARTONNÉ  " + Mathf.RoundToInt(Vector3.Distance(me.transform.position, p.transform.position)) + " m", new Color(1f, 0.8f, 0.3f));
			}
		}
		if (NearestBoxedAlly(me, 3.2f) != null)
		{
			big.normal.textColor = Color.white;
			string bar = freeShown > 0.01f ? "  " + Mathf.RoundToInt(freeShown * 100f) + " %" : "";
			GUI.Label(new Rect(0f, Screen.height * 0.68f, Screen.width, 40f), "Maintiens [F] pour libérer ton allié" + bar, big);
		}
	}

	private void Update()
	{
		if (gm == null)
		{
			return;
		}
		UpdateFreeing();
		int total = gm.GenTotal;
		if (total == 0)
		{
			if (builtTotal != 0)
			{
				Clear();
			}
			escapeSent = false;
			return;
		}
		if ((builtTotal != total || gens.Count != total || exitRoot == null) && Time.time >= buildRetryAt)
		{
			Build();
			escapeSent = false;
		}
		for (int i = 0; i < gens.Count; i++)
		{
			float p = gm.GenProgress(i);
			Color c = p >= 1f ? new Color(0.2f, 1f, 0.3f) : Color.Lerp(new Color(1f, 0.15f, 0.1f), new Color(1f, 0.85f, 0.1f), p);
			if (gens[i].lamp != null && gens[i].lamp.sharedMaterial != null)
			{
				gens[i].lamp.sharedMaterial.color = c;
				if (gens[i].lamp.sharedMaterial.HasProperty("_EmissionColor"))
					gens[i].lamp.sharedMaterial.SetColor("_EmissionColor", c * 2f);
			}
			if (gens[i].light != null) gens[i].light.color = c;
			if (p >= 1f && gens[i].hum == null)
			{
				gens[i].hum = ProceduralAudio.Loop(gens[i].root, ProceduralAudio.Hum, 0.35f, 1f, 18f);
			}
		}
		if (exitGlow != null && exitGlow.sharedMaterial != null)
		{
			Color c = gm.ExitOpen ? new Color(0.2f, 1f, 0.35f) : new Color(0.9f, 0.1f, 0.1f);
			exitGlow.sharedMaterial.color = c;
			exitGlow.sharedMaterial.SetColor("_EmissionColor", c * 2f);
			if (exitLight != null) exitLight.color = c;
		}

		DbUnityMovment me = LocalPlayer();
		if (!IsSurvivor(me))
		{
			repairAcc = 0f;
			CancelGame();
			return;
		}
		Vector3 pos = me.transform.position;
		int near = -1;
		float nearDist = 3.2f;
		for (int i = 0; i < gens.Count; i++)
		{
			float d = Vector3.Distance(pos, gm.GenPos(i));
			if (d < nearDist && gm.GenProgress(i) < 1f)
			{
				near = i;
				nearDist = d;
			}
		}
		StepGame(near);
		bool needsSequence = near >= 0 && GameManager.GeneratorNeedsSequence(near) && gm.GenProgress(near) >= 0.499f && !gm.GenSolved(near);
		if (activeGame != null && activeGameKind == 1)
		{
			repairAcc = 0f;
		}
		else if (needsSequence)
		{
			repairAcc = 0f;
			if (Input.GetKeyDown(KeyCode.F) && activeGame == null && !PoppyUIInputGate.IsGameplayBlocked)
				StartGame(near, 1);
		}
		else if (near >= 0 && Input.GetKey(KeyCode.F) && !PoppyUIInputGate.IsGameplayBlocked)
		{
			repairAcc += Time.deltaTime;
			if (Time.time >= repairSendAt)
			{
				gm.RepairGeneratorServerRpc(near, repairAcc);
				repairAcc = 0f;
				repairSendAt = Time.time + 0.3f;
			}
			// test de timing aléatoire pendant la réparation
			if (activeGame == null)
			{
				if (nextSkillCheckAt <= 0f)
					nextSkillCheckAt = Time.time + Random.Range(5f, 9f);
				else if (Time.time >= nextSkillCheckAt)
				{
					nextSkillCheckAt = 0f;
					StartGame(near, 0);
				}
			}
		}
		else
		{
			repairAcc = 0f;
		}
		if (gm.ExitOpen && !escapeSent && Vector3.Distance(pos, gm.ExitPos) < 2.6f)
		{
			escapeSent = true;
			gm.EscapeServerRpc();
		}
	}

	// ------------------------------------------------------------------ affichage
	private void EnsureStyles()
	{
		if (big != null)
		{
			return;
		}
		big = new GUIStyle(GUI.skin.label);
		big.fontSize = 26;
		big.fontStyle = FontStyle.Bold;
		big.alignment = TextAnchor.MiddleCenter;
		small = new GUIStyle(GUI.skin.label);
		small.fontSize = 18;
		small.fontStyle = FontStyle.Bold;
		small.alignment = TextAnchor.MiddleCenter;
	}

	private void Marker(Vector3 world, string label, Color color)
	{
		Camera cam = Camera.main;
		if (cam == null)
		{
			return;
		}
		Vector3 sp = cam.WorldToScreenPoint(world + Vector3.up * 1.5f);
		bool behind = sp.z < 0f;
		float x = Mathf.Clamp(behind ? Screen.width - sp.x : sp.x, 60f, Screen.width - 60f);
		float y = Mathf.Clamp(behind ? 80f : sp.y, 80f, Screen.height - 80f);
		small.normal.textColor = color;
		GUI.Label(new Rect(x - 120f, Screen.height - y - 15f, 240f, 30f), label, small);
	}

	// Chronomètre de la manche, en haut de l'écran.
	private void DrawTimer()
	{
		if (!gm.IsGameStarted)
		{
			return;
		}
		int left = Mathf.Max(0, gm.RoundTimeLeft);
		bool urgent = left <= 60;
		GUIStyle style = new GUIStyle(big);
		style.fontSize = 36;
		float pulse = urgent ? 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 4f)) : 1f;
		style.normal.textColor = urgent ? new Color(1f, 0.25f * pulse, 0.2f * pulse) : Color.white;
		string text = (left / 60).ToString("00") + ":" + (left % 60).ToString("00");
		GUI.Label(new Rect(0f, 10f, Screen.width, 44f), text, style);
	}

	private void OnGUI()
	{
		if (gm == null)
		{
			return;
		}
		EnsureStyles();
		// Le chronomètre est maintenant affiché par PoppyLobbyUI sur chaque instance.
		OnGUIFreeing();
		if (activeGame != null)
			activeGame.Draw();
		if (gm.GenTotal == 0)
		{
			return;
		}
		DbUnityMovment me = LocalPlayer();
		string obj = "Générateurs : " + gm.GenDone + " / " + gm.GenTotal + (gm.ExitOpen ? "   -   SORTIE OUVERTE !" : "");
		big.normal.textColor = gm.ExitOpen ? new Color(0.3f, 1f, 0.4f) : Color.white;
		GUI.Label(new Rect(0f, 56f, Screen.width, 40f), obj, big);

		if (IsSurvivor(me))
		{
			Vector3 pos = me.transform.position;
			for (int i = 0; i < gens.Count; i++)
			{
				if (gm.GenProgress(i) < 1f && Vector3.Distance(pos, gm.GenPos(i)) < 3.2f)
				{
					big.normal.textColor = Color.white;
					bool seq = GameManager.GeneratorNeedsSequence(i) && gm.GenProgress(i) >= 0.499f && !gm.GenSolved(i);
					string prompt = seq ? "Appuie sur [F] pour brancher le disjoncteur" : "Maintiens [F] pour réparer  " + Mathf.RoundToInt(gm.GenProgress(i) * 100f) + " %";
					GUI.Label(new Rect(0f, Screen.height * 0.62f, Screen.width, 40f), prompt, big);
					break;
				}
			}
			if (gm.ExitOpen)
			{
				Marker(gm.ExitPos, "SORTIE  " + Mathf.RoundToInt(Vector3.Distance(pos, gm.ExitPos)) + " m", new Color(0.3f, 1f, 0.4f));
			}
			else
			{
				for (int i = 0; i < gens.Count; i++)
				{
					if (gm.GenProgress(i) < 1f && Vector3.Distance(pos, gm.GenPos(i)) < 40f)
					{
						Marker(gm.GenPos(i), "GÉNÉRATEUR " + Mathf.RoundToInt(gm.GenProgress(i) * 100f) + " %", new Color(1f, 0.85f, 0.2f));
					}
				}
			}
		}
		else if (IsMonster(me))
		{
			for (int i = pings.Count - 1; i >= 0; i--)
			{
				if (Time.time > pings[i].until)
				{
					pings.RemoveAt(i);
					continue;
				}
				Marker(pings[i].pos, "BRUIT  " + Mathf.RoundToInt(Vector3.Distance(me.transform.position, pings[i].pos)) + " m", new Color(1f, 0.3f, 0.25f));
			}
		}
	}
}

// Fait toujours face à la caméra (étiquettes 3D).
public class FaceCameraLabel : MonoBehaviour
{
	private void LateUpdate()
	{
		Camera cam = Camera.main;
		if (cam != null)
		{
			transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, Vector3.up);
		}
	}
}
