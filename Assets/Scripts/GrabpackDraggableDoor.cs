using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Porte-herse contrôlée au Grabpack : accroche la porte avec une main, MAINTIENS le clic pour la faire descendre,
/// refais la même chose pour la remonter. Seuls les survivants peuvent la manœuvrer : le monstre (joueur ou IA)
/// ne peut pas l'ouvrir et ne passe pas tant qu'elle est fermée (collider + obstacle de NavMesh).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GrabpackDraggableDoor : NetworkBehaviour
{
	[Header("Door Movement")]
	[Tooltip("Course totale de la porte vers le bas (en mètres).")]
	public float maxPullDistance = 3f;

	[Tooltip("Vitesse volontairement lente de descente / remontée tant que la main reste accrochée (mètres par seconde).")]
	public float moveSpeed = 0.7f;

	[Tooltip("Obsolète (ancien système à la souris), conservé pour ne pas casser les scènes.")]
	public float pullSpeed = 10f;

	[Tooltip("Obsolète (ancien système à la souris), conservé pour ne pas casser les scènes.")]
	public float doorDamping = 5f;

	[Header("Monstre")]
	[Tooltip("Bloque le NavMesh de Huggy IA tant que la porte est fermée.")]
	public bool blockMonsterNavigation = true;

	// 0 = porte levée (passage fermé), 1 = porte complètement descendue (passage libre)
	private readonly NetworkVariable<float> netOpen = new NetworkVariable<float>(0f);

	private Rigidbody doorRigidbody;
	private NavMeshObstacle obstacle;
	private Vector3 startingPosition;
	private float shown;
	private float lastShown;
	private bool localHolding;
	private float nextSoundTime;

	// état serveur
	private readonly HashSet<ulong> holders = new HashSet<ulong>();
	private int direction = 1;
	private int lastDirection = -1;

	public bool IsBlocking
	{
		get { return shown < 0.85f; }
	}

	private void Awake()
	{
		doorRigidbody = GetComponent<Rigidbody>();
		startingPosition = transform.position;
		doorRigidbody.useGravity = false;
		doorRigidbody.isKinematic = true;
		doorRigidbody.constraints = RigidbodyConstraints.FreezeAll;
		BuildObstacle();
	}

	private void BuildObstacle()
	{
		if (!blockMonsterNavigation)
			return;

		Collider[] colliders = GetComponentsInChildren<Collider>();
		if (colliders.Length == 0)
			return;

		Bounds b = colliders[0].bounds;
		for (int i = 1; i < colliders.Length; i++)
			b.Encapsulate(colliders[i].bounds);

		obstacle = gameObject.GetComponent<NavMeshObstacle>();
		if (obstacle == null)
			obstacle = gameObject.AddComponent<NavMeshObstacle>();

		Vector3 ls = transform.lossyScale;
		obstacle.shape = NavMeshObstacleShape.Box;
		obstacle.center = transform.InverseTransformPoint(b.center);
		obstacle.size = new Vector3(
			b.size.x / Mathf.Max(0.0001f, Mathf.Abs(ls.x)),
			b.size.y / Mathf.Max(0.0001f, Mathf.Abs(ls.y)),
			b.size.z / Mathf.Max(0.0001f, Mathf.Abs(ls.z)));
		obstacle.carving = true;
		obstacle.carveOnlyStationary = true;
		obstacle.enabled = true;
	}

	public override void OnNetworkSpawn()
	{
		shown = netOpen.Value;
		lastShown = shown;
		if (IsServer && NetworkManager.Singleton != null)
			NetworkManager.Singleton.OnClientDisconnectCallback += OnClientGone;
	}

	public override void OnNetworkDespawn()
	{
		if (IsServer && NetworkManager.Singleton != null)
			NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientGone;
	}

	private void OnClientGone(ulong clientId)
	{
		holders.Remove(clientId);
	}

	// ------------------------------------------------------------------ API appelée par le Grabpack
	// StartGrabInteraction est appelée à chaque image tant que le clic est maintenu : seul le premier appel compte.
	public void StartGrabInteraction()
	{
		if (localHolding)
			return;
		if (!LocalPlayerCanOperate())
			return;

		localHolding = true;
		SendHolding(true);
	}

	public void StopGrabInteraction()
	{
		if (!localHolding)
			return;

		localHolding = false;
		SendHolding(false);
	}

	private void SendHolding(bool holding)
	{
		NetworkManager nm = NetworkManager.Singleton;
		if (nm == null || !nm.IsListening || !IsSpawned)
			return;

		if (IsServer)
			ApplyHolding(holding, nm.LocalClientId);
		else
			SetHoldingServerRpc(holding);
	}

	private static bool IsMonster(DbUnityMovment mv)
	{
		return mv != null && mv.monsterModelContainer != null && mv.monsterModelContainer.activeSelf;
	}

	private static bool LocalPlayerCanOperate()
	{
		NetworkManager nm = NetworkManager.Singleton;
		if (nm == null || nm.LocalClient == null || nm.LocalClient.PlayerObject == null)
			return false;

		return !IsMonster(nm.LocalClient.PlayerObject.GetComponent<DbUnityMovment>());
	}

	[ServerRpc(RequireOwnership = false)]
	private void SetHoldingServerRpc(bool holding, ServerRpcParams rpcParams = default(ServerRpcParams))
	{
		ApplyHolding(holding, rpcParams.Receive.SenderClientId);
	}

	// ------------------------------------------------------------------ serveur
	private void ApplyHolding(bool holding, ulong clientId)
	{
		if (!holding)
		{
			holders.Remove(clientId);
			return;
		}

		NetworkClient client;
		if (NetworkManager.Singleton == null || !NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out client) || client.PlayerObject == null)
			return;

		// le monstre ne peut jamais ouvrir ni fermer la porte
		if (IsMonster(client.PlayerObject.GetComponent<DbUnityMovment>()))
			return;

		if (Vector3.Distance(client.PlayerObject.transform.position, transform.position) > 30f)
			return;

		if (holders.Count == 0)
		{
			// nouvelle prise : on inverse le sens (descendre puis remonter), sauf aux extrémités
			float open = netOpen.Value;
			if (open <= 0.001f)
				direction = 1;
			else if (open >= 0.999f)
				direction = -1;
			else
				direction = -lastDirection;
			lastDirection = direction;
		}
		holders.Add(clientId);
	}

	private void Update()
	{
		if (IsServer && IsSpawned && holders.Count > 0 && maxPullDistance > 0.01f)
		{
			float next = Mathf.Clamp01(netOpen.Value + direction * (moveSpeed / maxPullDistance) * Time.deltaTime);
			if (!Mathf.Approximately(next, netOpen.Value))
				netOpen.Value = next;
		}

		float target = IsSpawned ? netOpen.Value : shown;
		shown = IsServer ? target : Mathf.MoveTowards(shown, target, Time.deltaTime * Mathf.Max(0.5f, moveSpeed / Mathf.Max(0.01f, maxPullDistance)) * 3f);

		if (obstacle != null)
			obstacle.enabled = IsBlocking;

		PlayMovementSound();
	}

	private void PlayMovementSound()
	{
		float delta = Mathf.Abs(shown - lastShown);
		if (delta > 0.0005f && Time.time >= nextSoundTime)
		{
			nextSoundTime = Time.time + 0.5f;
			ProceduralAudio.PlayAt(ProceduralAudio.Whoosh, transform.position, 0.35f);
		}
		bool reachedEnd = (lastShown > 0.001f && shown <= 0.001f) || (lastShown < 0.999f && shown >= 0.999f);
		if (reachedEnd)
			ProceduralAudio.PlayAt(ProceduralAudio.Thud, transform.position, 0.6f);
		lastShown = shown;
	}

	private void FixedUpdate()
	{
		Vector3 target = startingPosition + Vector3.down * (maxPullDistance * shown);
		doorRigidbody.MovePosition(target);
	}
}
