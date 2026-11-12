using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Server-authoritative Huggy hunter. Add this to a registered NetworkObject prefab
/// that also has a server-authoritative NetworkTransform and a baked NavMesh nearby.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class HuggyHunterAI : NetworkBehaviour
{
    public enum HunterState : byte { Patrol, Chase, Search, Attack }

    [Header("Detection")]
    [Min(1f)] public float visionRange = 18f;
    [Range(10f, 180f)] public float visionAngle = 110f;
    public LayerMask obstructionMask = ~0;
    [Min(0.1f)] public float targetRefreshRate = 0.2f;
    [Min(0.1f)] public float loseTargetAfter = 3f;

    [Header("Movement")]
    [Min(0.1f)] public float patrolSpeed = 2.2f;
    [Min(0.1f)] public float chaseSpeed = 5.2f;
    [Min(0.1f)] public float searchSpeed = 3f;
    [Min(1f)] public float patrolRadius = 12f;
    [Min(0f)] public float patrolPause = 1.5f;

    [Header("Attack")]
    [Min(0.5f)] public float attackDistance = 1.8f;
    [Min(0.1f)] public float attackCooldown = 4f;
    [Min(0f)] public float attackWindup = 1.35f;
    public AudioSource audioSource;
    public AudioClip alertSound;
    public Animator animator;

    private NavMeshAgent agent;
    private readonly NetworkVariable<byte> networkState =
        new NetworkVariable<byte>((byte)HunterState.Patrol,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> networkSpeed =
        new NetworkVariable<float>(0f,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private DbUnityMovment target;
    private Vector3 lastKnownPosition;
    private float targetRefreshTimer;
    private float lostSightTimer;
    private float nextAttackTime;
    private float patrolWaitTimer;
    private bool attackInProgress;
    private bool hasAlerted;

    private HunterState State
    {
        get => (HunterState)networkState.Value;
        set => networkState.Value = (byte)value;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public override void OnNetworkSpawn()
    {
        networkState.OnValueChanged += OnStateChanged;

        if (!IsServer)
        {
            if (agent != null)
                agent.enabled = false;
            return;
        }

        if (agent == null || !agent.isOnNavMesh)
        {
            Debug.LogError("HuggyHunterAI: the enemy must spawn on a baked NavMesh.", this);
            enabled = false;
            return;
        }

        agent.speed = patrolSpeed;
        ChoosePatrolDestination();
    }

    public override void OnNetworkDespawn()
    {
        networkState.OnValueChanged -= OnStateChanged;
    }

    private void Update()
    {
        UpdateAnimator();

        if (!IsServer || !IsSpawned || !enabled || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        targetRefreshTimer -= Time.deltaTime;
        if (targetRefreshTimer <= 0f)
        {
            targetRefreshTimer = Mathf.Max(0.1f, targetRefreshRate);
            RefreshTarget();
        }

        switch (State)
        {
            case HunterState.Patrol:
                UpdatePatrol();
                break;
            case HunterState.Chase:
                UpdateChase();
                break;
            case HunterState.Search:
                UpdateSearch();
                break;
            case HunterState.Attack:
                agent.isStopped = true;
                break;
        }

        networkSpeed.Value = agent.velocity.magnitude;
    }

    private void RefreshTarget()
    {
        DbUnityMovment nearest = null;
        float nearestDistance = visionRange;

        DbUnityMovment[] players = FindObjectsOfType<DbUnityMovment>();
        for (int i = 0; i < players.Length; i++)
        {
            DbUnityMovment candidate = players[i];
            if (candidate == null || !candidate.IsSpawned ||
                candidate.survivorModelContainer == null ||
                !candidate.survivorModelContainer.activeInHierarchy)
                continue;

            float distance = Vector3.Distance(transform.position, candidate.transform.position);
            if (distance < nearestDistance && CanSee(candidate))
            {
                nearest = candidate;
                nearestDistance = distance;
            }
        }

        if (nearest != null)
        {
            target = nearest;
            lastKnownPosition = target.transform.position;
            lostSightTimer = 0f;

            if (State != HunterState.Attack)
            {
                State = HunterState.Chase;
                agent.speed = chaseSpeed;
                agent.isStopped = false;

                if (!hasAlerted && audioSource != null && alertSound != null)
                {
                    audioSource.PlayOneShot(alertSound);
                    hasAlerted = true;
                }
            }
        }
        else if (target != null && State == HunterState.Chase)
        {
            lostSightTimer += targetRefreshRate;
            lastKnownPosition = target.transform.position;

            if (lostSightTimer >= loseTargetAfter)
            {
                target = null;
                State = HunterState.Search;
                agent.speed = searchSpeed;
                agent.isStopped = false;
                agent.SetDestination(lastKnownPosition);
            }
        }
    }

    private bool CanSee(DbUnityMovment candidate)
    {
        Vector3 origin = transform.position + Vector3.up * 1.6f;
        Vector3 destination = candidate.transform.position + Vector3.up * 1.0f;
        Vector3 direction = destination - origin;
        float distance = direction.magnitude;

        if (distance > visionRange || Vector3.Angle(transform.forward, direction) > visionAngle * 0.5f)
            return false;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit,
            distance, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            DbUnityMovment hitPlayer = hit.collider.GetComponentInParent<DbUnityMovment>();
            return hitPlayer == candidate;
        }

        return true;
    }

    private void UpdatePatrol()
    {
        if (target != null)
            return;

        if (agent.pathPending)
            return;

        if (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.4f))
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= patrolPause)
            {
                patrolWaitTimer = 0f;
                ChoosePatrolDestination();
            }
        }
    }

    private void UpdateChase()
    {
        if (target == null || !target.IsSpawned)
        {
            State = HunterState.Search;
            agent.speed = searchSpeed;
            agent.SetDestination(lastKnownPosition);
            return;
        }

        lastKnownPosition = target.transform.position;
        agent.isStopped = false;
        agent.speed = chaseSpeed;
        agent.SetDestination(lastKnownPosition);

        if (Vector3.Distance(transform.position, target.transform.position) <= attackDistance &&
            Time.time >= nextAttackTime && !attackInProgress)
        {
            StartCoroutine(AttackTarget(target));
        }
    }

    private void UpdateSearch()
    {
        if (target != null)
        {
            State = HunterState.Chase;
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            patrolWaitTimer += Time.deltaTime;
            if (patrolWaitTimer >= 2.5f)
            {
                patrolWaitTimer = 0f;
                State = HunterState.Patrol;
                agent.speed = patrolSpeed;
                hasAlerted = false;
                ChoosePatrolDestination();
            }
        }
    }

    private void ChoosePatrolDestination()
    {
        if (!IsServer || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * patrolRadius;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    private IEnumerator AttackTarget(DbUnityMovment victim)
    {
        attackInProgress = true;
        nextAttackTime = Time.time + attackCooldown;
        State = HunterState.Attack;
        agent.isStopped = true;

        ulong victimId = victim.OwnerClientId;
        PlayJumpscareClientRpc(victimId);

        yield return new WaitForSeconds(attackWindup);

        if (victim != null && victim.IsSpawned &&
            Vector3.Distance(transform.position, victim.transform.position) <= attackDistance + 0.75f)
        {
            GameManager manager = FindObjectOfType<GameManager>();
            if (manager != null && manager.lobbySpawnPoint != null)
                manager.ExecuteTargetedTeleport(victimId, manager.lobbySpawnPoint.position,
                    isMonster: false, isSurvivor: false);
        }

        target = null;
        lostSightTimer = 0f;
        attackInProgress = false;
        agent.isStopped = false;
        State = HunterState.Search;
        agent.speed = searchSpeed;
        agent.SetDestination(lastKnownPosition);
    }

    [ClientRpc]
    private void PlayJumpscareClientRpc(ulong victimClientId)
    {
        if (animator != null)
            animator.SetTrigger("Jumpscare");

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.LocalClientId == victimClientId &&
            Camera.main != null)
        {
            Camera.main.transform.LookAt(transform.position + Vector3.up * 1.6f);
        }
    }

    private void OnStateChanged(byte previous, byte current)
    {
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (animator == null)
            return;

        animator.SetFloat("Speed", networkSpeed.Value);
        animator.SetBool("IsMoving", networkSpeed.Value > 0.12f);
        animator.SetBool("IsChasing", networkState.Value == (byte)HunterState.Chase);
    }
}
