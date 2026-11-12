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
    private readonly NetworkVariable<int> networkMonsterSkin =
        new NetworkVariable<int>(0,
            NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float baseVisionRange;
    private float baseChaseSpeed;
    private float baseAttackCooldown;
    private float baseLoseTargetAfter;
    private DbUnityMovment target;
    private Vector3 lastKnownPosition;
    private float targetRefreshTimer;
    private float lostSightTimer;
    private float nextAttackTime;
    private float patrolWaitTimer;
    private bool attackInProgress;
    private bool hasAlerted;
    // RaycastNonAlloc avoids a new array allocation every vision check.
    private readonly RaycastHit[] visionHits = new RaycastHit[32];

    private HunterState State
    {
        get => (HunterState)networkState.Value;
        set => networkState.Value = (byte)value;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        baseVisionRange = visionRange;
        baseChaseSpeed = chaseSpeed;
        baseAttackCooldown = attackCooldown;
        baseLoseTargetAfter = loseTargetAfter;
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public override void OnNetworkSpawn()
    {
        networkState.OnValueChanged += OnStateChanged;
        networkMonsterSkin.OnValueChanged += OnMonsterSkinChanged;

        if (!IsServer)
        {
            ApplyMonsterSkin(networkMonsterSkin.Value);
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

        ApplyDifficultyPreset();
        networkMonsterSkin.Value = PoppyMonsterShop.SelectedSkin;
        ApplyMonsterSkin(networkMonsterSkin.Value);
        agent.speed = patrolSpeed;
        ChoosePatrolDestination();
    }

    public override void OnNetworkDespawn()
    {
        networkState.OnValueChanged -= OnStateChanged;
        networkMonsterSkin.OnValueChanged -= OnMonsterSkinChanged;
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
        GameManager gameManager = FindObjectOfType<GameManager>();
        for (int i = 0; i < players.Length; i++)
        {
            DbUnityMovment candidate = players[i];
            if (candidate == null || !candidate.IsSpawned ||
                candidate.survivorModelContainer == null ||
                !candidate.survivorModelContainer.activeInHierarchy ||
                (gameManager != null && gameManager.IsClientEliminated(candidate.OwnerClientId)))
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
            // Keep the position from the last successful sighting. Updating it
            // here would let Huggy track a player perfectly through walls.
            lostSightTimer += targetRefreshRate;

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

        int hitCount = Physics.RaycastNonAlloc(origin, direction.normalized, visionHits, distance,
            obstructionMask, QueryTriggerInteraction.Ignore);

        // RaycastNonAlloc does not guarantee hit ordering: inspect every hit and
        // decide visibility from the closest collider that is not part of Huggy.
        float nearestDistance = float.MaxValue;
        DbUnityMovment firstVisiblePlayer = null;
        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = visionHits[i].collider;
            Transform hitTransform = hitCollider != null ? hitCollider.transform : null;
            if (hitTransform == null || hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;

            if (visionHits[i].distance >= nearestDistance)
                continue;

            nearestDistance = visionHits[i].distance;
            firstVisiblePlayer = hitCollider.GetComponentInParent<DbUnityMovment>();
        }

        return nearestDistance == float.MaxValue || firstVisiblePlayer == candidate;
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

        agent.isStopped = false;
        if (lostSightTimer <= 0f)
        {
            lastKnownPosition = target.transform.position;
            agent.speed = chaseSpeed;
            agent.SetDestination(lastKnownPosition);
        }
        else
        {
            agent.speed = searchSpeed;
            agent.SetDestination(lastKnownPosition);
        }

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

    private void ApplyDifficultyPreset()
    {
        // 0 = Easy, 1 = Normal, 2 = Nightmare. Values are selected by the host.
        int difficulty = Mathf.Clamp(PlayerPrefs.GetInt("Poppy.AIDifficulty", 1), 0, 2);
        float[] vision = { 0.78f, 1f, 1.22f };
        float[] speed = { 0.82f, 1f, 1.18f };
        float[] cooldown = { 1.35f, 1f, 0.78f };
        float[] memory = { 1.4f, 1f, 0.78f };

        visionRange = baseVisionRange * vision[difficulty];
        chaseSpeed = baseChaseSpeed * speed[difficulty];
        attackCooldown = baseAttackCooldown * cooldown[difficulty];
        loseTargetAfter = baseLoseTargetAfter * memory[difficulty];
    }

    private void OnMonsterSkinChanged(int previous, int current)
    {
        ApplyMonsterSkin(current);
    }

    private void ApplyMonsterSkin(int skinIndex)
    {
        PoppyMonsterShop.Apply(gameObject, skinIndex);
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.12f, 0.08f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = new Color(1f, 0.65f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        Vector3 left = Quaternion.Euler(0f, -visionAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, visionAngle * 0.5f, 0f) * transform.forward;
        Gizmos.color = new Color(1f, 0.12f, 0.08f, 0.45f);
        Gizmos.DrawLine(transform.position, transform.position + left * visionRange);
        Gizmos.DrawLine(transform.position, transform.position + right * visionRange);
    }
}
