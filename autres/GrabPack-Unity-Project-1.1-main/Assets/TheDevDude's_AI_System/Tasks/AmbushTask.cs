using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AmbushTask : AITask
{
    private NavMeshAgent agent;

    [SerializeField] private float waitTime = 3f;
    [SerializeField] private float hearingIgnoreTime = 2f;

    private float timer;

    [SerializeField] private float maxAmbushDistance = 20f;
    [SerializeField] private float minDistanceFromPlayer = 8f;

    private AmbushPoint currentPoint;
    private bool waiting;
    [SerializeField] private LayerMask obstacleMask;


    [SerializeField] private float turnSpeed = 360f;
    private Transform player;

    public bool SkipAmbushPointSelection { get; set; }
    public AudioSource AudioSource;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public override bool CanInterrupt => false;

    public override void Begin()
    {
        Debug.Log($"SkipAmbushPointSelection = {SkipAmbushPointSelection}");

        if (SkipAmbushPointSelection)
        {
            SkipAmbushPointSelection = false;

            waiting = true;

            agent.ResetPath();
            agent.isStopped = true;

            return;
        }

        base.Begin();

        timer = 0f;
        AudioSource.Stop();
        brain.Blackboard.Set(AIKeys.IgnoreHearingUntil, Time.time + hearingIgnoreTime);

        waiting = false;

        AmbushPoint[] points = FindObjectsByType<AmbushPoint>(
            FindObjectsSortMode.None);

        Vector3 referencePosition;

        if (brain.Blackboard.Get<bool>(AIKeys.CanSeeTarget))
        {
            referencePosition = brain.Blackboard.Get<Vector3>(AIKeys.LastSeenPosition);
        }
        else
        {
            referencePosition = player.position;
        }
        List<AmbushPoint> candidates = new();
        foreach (AmbushPoint point in points)
        {


            float distance = Vector3.Distance(
                transform.position,
                point.transform.position);

            if (distance > maxAmbushDistance)
                continue;

            float playerDistance = Vector3.Distance(referencePosition, point.transform.position);

            if (playerDistance < minDistanceFromPlayer)
                continue;

            if (!Physics.Linecast(
                    referencePosition,
                    point.transform.position,
                    obstacleMask))
            {
                continue;
            }

            candidates.Add(point);
        }

        if (candidates.Count == 0)
        {
            brain.Blackboard.Set(AIKeys.IsAmbushing, false);
            Complete();
            Debug.Log($"Ambush candidates: {candidates.Count}");

            return;
        }

        candidates.Sort((a, b) =>
        {
            float da = Vector3.Distance(referencePosition, a.transform.position);
            float db = Vector3.Distance(referencePosition, b.transform.position);

            return db.CompareTo(da);
        });
        int maxChoices = Mathf.Min(3, candidates.Count);
        currentPoint = candidates[Random.Range(0, maxChoices)];

        agent.isStopped = false;
        agent.SetDestination(currentPoint.transform.position);

        brain.Blackboard.Set(AIKeys.HeardNoise, false);
        brain.Blackboard.Set(AIKeys.LastNoiseTime, 0f);
        brain.Blackboard.Set(AIKeys.NoiseVolume, 0f);
        brain.Blackboard.Set(AIKeys.LastNoisePosition, Vector3.zero);
    }

    public override void Tick()
    {
        if (!waiting)
        {
            if (agent.pathPending)
                return;

            if (agent.remainingDistance > agent.stoppingDistance + 0.2f)
                return;

            waiting = true;

            timer = 0f;

            agent.ResetPath();
            agent.isStopped = true;

            return;
        }

        timer += Time.deltaTime;

        if (timer >= waitTime)
        {
            brain.Blackboard.Set(AIKeys.IsAmbushing, false);
            Complete();
        }

        Transform target = brain.Blackboard.Get<Transform>(AIKeys.Target);

        if (brain.Blackboard.Get<bool>(AIKeys.CanSeeTarget))
        {
            brain.Blackboard.Set(AIKeys.IsAmbushing, false);
            Complete();
            return;
        }


        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                desiredRotation,
                turnSpeed * Time.deltaTime);
        }


    }

    public override void End()
    {
        base.End();

        brain.Blackboard.Set(AIKeys.IsAmbushing, false);
        agent.isStopped = false;

        waiting = false;
        currentPoint = null;
    }
}