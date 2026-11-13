using UnityEngine;

public class VisionSensor : SensorBase
{
    [SerializeField] private float viewDistance = 20f;

    [SerializeField]
    [Range(1f, 180f)]
    private float fieldOfView = 90f;

    [SerializeField] private LayerMask targetMask;

    [SerializeField] private LayerMask obstacleMask;

    public override void Sense()
    {

        //Debug.Log("Vision Scan");

        bool sawTarget = false;
        blackboard.Set(AIKeys.Visibility, 0f);

        Collider[] hits = Physics.OverlapSphere(transform.position, viewDistance, targetMask);
        //Debug.Log($"Found {hits.Length} colliders.");

        foreach (Collider hit in hits)
        {
            Vector3 direction = (hit.transform.position - transform.position).normalized;

            float angle = Vector3.Angle(transform.forward, direction);

            if (angle > fieldOfView * 0.5f)
                continue;

            float distance = Vector3.Distance(transform.position, hit.transform.position);

            if (Physics.Raycast(transform.position, direction, distance, obstacleMask))
                continue;

            AISenseTarget target = hit.GetComponent<AISenseTarget>();

            if (target == null)
                continue;

            float strength = 1f - (distance / viewDistance);

            Stimulus stimulus = new Stimulus(
                target.subject,
                StimulusType.Vision,
                hit.transform.position,
                hit.transform,
                strength
            );

            sawTarget = true;

            bool wasSeeingTarget = blackboard.Get<bool>(AIKeys.CanSeeTarget);

            if (!wasSeeingTarget)
            {
                bool shouldRoar = distance <= 10f;
                blackboard.Set(AIKeys.ShouldRoar, shouldRoar);

                Debug.Log($"First spotted player. Distance: {distance}, ShouldRoar: {shouldRoar}");
            }

            blackboard.Set(AIKeys.Target, hit.transform);
            blackboard.Set(AIKeys.CanSeeTarget, true);
            blackboard.Set(AIKeys.Visibility, strength);
            blackboard.Set(AIKeys.LastSeenPosition, hit.transform.position);
            blackboard.Set(AIKeys.LastSeenTime, Time.time);


            memorySystem.ProcessStimulus(stimulus);
            Debug.Log($"Saw {target.subject}");

            Debug.DrawLine(
                transform.position,
                hit.transform.position,
                Color.green,
                0.25f
            );
        }

        if (!sawTarget)
        {
            blackboard.Set(AIKeys.CanSeeTarget, false);
            blackboard.Set(AIKeys.Visibility, 0f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, viewDistance);

        Vector3 left =
            Quaternion.Euler(0, -fieldOfView / 2f, 0) * transform.forward;

        Vector3 right =
            Quaternion.Euler(0, fieldOfView / 2f, 0) * transform.forward;

        Gizmos.color = Color.cyan;

        Gizmos.DrawRay(transform.position, left * viewDistance);
        Gizmos.DrawRay(transform.position, right * viewDistance);
    }
}