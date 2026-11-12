using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SmilingCritterAI : MonoBehaviour
{


    float returnTimer = 0f;
    public float maxReturnTime = 10f;

    public float moveSpeed = 5f;
    public float rotationSpeed = 6f;

    public float viewDistance = 15f;
    public float viewAngle = 60f;
    public LayerMask obstacleMask;

    public LayerMask groundMask;


    public float climbUpForce = 10f;
    public float stickForce = 20f;
    public float wallCheckDistance = 1.2f;

    private Rigidbody rb;
    private Transform player;

    private Vector3 homePosition;


    private float loseSightTimer = 0f;
    public float loseSightDelay = 1.5f;

    private bool isClimbing = false;

    public float friendlyStopDistance = 2f;

    public enum State { Chase, Return, Jumpscare, Friendly }
    public State currentState = State.Chase;

    public GameObject jumpscareCamera;
    public Animator enemyAnimator;

    private bool hasJumpscared = false;

    private AudioSource ambientSource;

    void Start()
    {
        ambientSource = GetComponent<AudioSource>();

        rb = GetComponent<Rigidbody>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        homePosition = transform.position;

        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        if (currentState == State.Jumpscare)
            return; 

        if (player == null) return;

        bool canSeePlayer = CanSeePlayer();

        if (currentState == State.Chase)
        {
            if (canSeePlayer)
            {
                loseSightTimer = 0f;
            }
            else
            {
                loseSightTimer += Time.fixedDeltaTime;
                if (loseSightTimer > loseSightDelay)
                {
                    currentState = State.Return;
                    returnTimer = 0f;
                }
            }
        }



        switch (currentState)
        {
            case State.Chase:
                Move((player.position - transform.position).normalized);
                break;

            case State.Friendly:
                {
                    Vector3 toPlayer = player.position - transform.position;
                    float distance = toPlayer.magnitude;

                    if (distance > friendlyStopDistance)
                    {
                        Move(toPlayer.normalized);
                    }
                    else
                    {
                        // Stop moving but still face player
                        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

                        Vector3 lookDir = toPlayer;
                        lookDir.y = 0f;

                        if (lookDir.sqrMagnitude > 0.001f)
                        {
                            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
                        }
                    }

                    break;
                }

            case State.Return:
                
                

                returnTimer += Time.fixedDeltaTime;

                if (returnTimer > maxReturnTime)
                {
                    Destroy(gameObject);
                    return;
                }

                Vector3 dir = homePosition - transform.position;

                if (dir.magnitude > 1f)
                    Move(dir.normalized);
                else
                    Destroy(gameObject);

                break;
        }


        UpdateAnimation();
    }

    bool CanSeePlayer()
    {
        Vector3 dirToPlayer = player.position - transform.position;
        float distance = dirToPlayer.magnitude;

        if (distance > viewDistance)
            return false;

        dirToPlayer.Normalize();

        float angle = Vector3.Angle(transform.forward, dirToPlayer);
        if (angle > viewAngle / 2f)
            return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.5f;

        if (Physics.Raycast(eyePos, dirToPlayer, distance, obstacleMask))
            return false;

        return true;
    }

    void Move(Vector3 direction)
    {
        RaycastHit wallHit;
        bool wallAhead = IsWallAhead(direction, out wallHit);

        if (wallAhead)
        {
            StartClimbing(direction, wallHit);
            return;
        }

        StopClimbing();

        Vector3 flatDir = direction;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatDir);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        Vector3 velocity = flatDir * moveSpeed;
        velocity.y = rb.linearVelocity.y;

        rb.linearVelocity = velocity;
    }

    void StartClimbing(Vector3 direction, RaycastHit wallHit)
    {
        isClimbing = true;

        rb.useGravity = false;


        Quaternion wallRotation = Quaternion.LookRotation(-wallHit.normal);
        rb.rotation = Quaternion.Slerp(rb.rotation, wallRotation, 10f * Time.fixedDeltaTime);


        Vector3 climb = Vector3.up * climbUpForce;
        Vector3 stick = -wallHit.normal * stickForce;

        rb.linearVelocity = climb + stick;
    }

    void StopClimbing()
    {
        if (!isClimbing) return;

        isClimbing = false;
        rb.useGravity = true;
    }

    bool IsWallAhead(Vector3 direction, out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        float width = 0.4f;

        Vector3[] offsets = new Vector3[]
        {
            Vector3.zero,
            transform.right * width,
            -transform.right * width
        };

        foreach (var offset in offsets)
        {
            if (Physics.Raycast(origin + offset, direction, out hit, wallCheckDistance, groundMask))
            {
                return true;
            }
        }

        hit = new RaycastHit();
        return false;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (hasJumpscared) return;

        if (currentState == State.Friendly)
            return;

        if (other.CompareTag("Player"))
        {
            GameObject playerobj = other.gameObject;

            hasJumpscared = true;
            currentState = State.Jumpscare;

            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;

            if (playerobj != null)
                playerobj.SetActive(false);

            if (jumpscareCamera != null)
                jumpscareCamera.SetActive(true);
                

            Vector3 lookDir = (player.position - transform.position);
            lookDir.y = 0;
            transform.rotation = Quaternion.LookRotation(lookDir);

            ambientSource.enabled = false;

            Camera jumpscarecamera = jumpscareCamera.GetComponent<Camera>();

            SettingsManager.Instance.SetActiveCamera(jumpscarecamera);


            if (enemyAnimator != null)
            {
                enemyAnimator.SetBool("moving", false);
                enemyAnimator.SetTrigger("scare");
            }
        }
    }

    public void BecomeFriendly()
    {
        currentState = State.Friendly;

        loseSightTimer = 0f;
    }

    void UpdateAnimation()
    {
        if (enemyAnimator == null) return;

        Vector3 horizontalVel = rb.linearVelocity;
        horizontalVel.y = 0f;

        bool isMoving = horizontalVel.magnitude > 0.1f;

        enemyAnimator.SetBool("moving", isMoving);
    }
}