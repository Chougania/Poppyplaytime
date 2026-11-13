using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class RigidboyPlayerController : MonoBehaviour
{
    public bool canLook = true;
    public bool StartCrouched = false;
    public float bobSmoothSpeed = 8f;
    private float currentBob;
    public Transform grabPackManager;
    public float maxWeaponTilt = 8f;
    public float weaponTiltSpeed = 10f;


    public List<LaunchHand> allLaunchHands = new List<LaunchHand>();
    private int currentHandIndex = 0;
    private float scrollSwitchCooldown = 0.25f;
    private float lastScrollSwitchTime = 0f;
    public bool squeeze = false;

    private Coroutine footstepCoroutine;

    public Transform headCheck; 
    public float standUpCheckHeight = 1f;

    private float targetX;
    private float targetY;
    private float currentY;

    private bool unlockingCamera = false;
    private Quaternion unlockStartBodyRotation;
    private Quaternion unlockStartCameraRotation;
    private float unlockTimer = 0f;
    public float unlockDuration = 0.4f;

    public Vector2 squeezeCameraRotation = new Vector2(10f, 0f);
    public float squeezeCameraLerpSpeed = 5f;
    private Quaternion squeezeStartBodyRotation;
    private Quaternion squeezeStartCameraRotation;
    private bool lockCamera = false;
    public Transform squeezeCameraTarget;

    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float crouchMultiplier = 0.5f;

    public float sprintAcceleration = 2f;
    public float jumpForce = 10f;
    public float lookSpeedX = 2f;
    public float lookSpeedY = 2f;
    public Transform playerCamera;

    private Rigidbody rb;
    private float currentMoveSpeed;
    private float targetMoveSpeed;
    private float rotationX = 0f;

    public float squeezeSpeed = 2f;
    private bool wasSqueezing = false;

    public Animator playeranimations;
    public float groundCheckDistance = 0.3f;
    public LayerMask groundLayer;
    public bool isGrounded;

    public bool IsCrouched = false;

    public Animator croucher;
    private Vector3 moveDirection;

    public bool CanMove = true;


    public GameObject RedHand;
    public GameObject PurpleHand;
    public GameObject FlareHand;
    public GameObject conductiveHand;
    public GameObject BlueHand;
    public GameObject MagnetHand;
    public GameObject FireHand;
    public GameObject GreenHand;

    public string handtoSwitch;

    public LaunchHand purplelauncher;
    public LaunchHand redLauncher;

    public float groundCheckRadius;
    public Transform groundCheck;

    public CapsuleCollider playerstandingCollider;

    private Vector3 currentMoveDirection;
    public float airInputSmooth = 6f;

    public CablePhysics redcable;
    public CablePhysics purplecable;
    public CablePhysics pressurecable;
    public CablePhysics conductivecable;
    public CablePhysics magnetcable;
    public CablePhysics greencable;

    public LaunchHand magnetlaunch;
    public LaunchHand redlaunch;
    public LaunchHand purplelaunch;
    public LaunchHand pressurelaunch;
    public LaunchHand conductivelaunch;
    public LaunchHand greenlaunch;

    public AudioSource footstepSource;
    public AudioClip[] grassFootsteps;
    public AudioClip[] woodFootsteps;
    public AudioClip[] concreteFootsteps;
    public AudioClip[] MetalFootsteps;

    public AudioClip crouchsfx;
    public AudioClip jumpsfx;
    public AudioClip uncrouchsfx;


    private bool isPlayingFootsteps = false;



    public HandManager handmanager;

    public Image righthandIcon;


    public Color red;
    public Color purple;
    public Color grey;
    public Color yellow;
    public Color white;
    public Color green;


    // for mobile controls
    [HideInInspector] public Vector2 mobileMoveInput;
    [HideInInspector] public Vector2 mobileLookInput;
    [HideInInspector] public bool mobileJump;
    [HideInInspector] public bool mobileSprint;
    [HideInInspector] public bool mobileCrouch;

    private bool sprintToggled = false;
    private bool crouchToggled = false;

    private float lastTapTime = 0f;
    private float tapThreshold = 0.3f; 
    private bool sprinting = false;

    public MobileJoystick mobileJoystick;

    public MobileIcons mobileIcons;

    public GameObject Switcher;
    public SwitchMenu switchmenu;

    public GameObject redHandButton;
    public GameObject purpleHandButton;
    public GameObject flareHandButton;
    public GameObject conductiveHandButton;
    public GameObject magnetHandButton;
    public GameObject fireHandButton;
    public GameObject greenHandButton;

    public bool canSwitch = true;


    public void ToggleSprint()
    {
        sprintToggled = !sprintToggled;
    }

    public void ToggleCrouch()
    {
        crouchToggled = !crouchToggled;

        if (crouchToggled)
            sprintToggled = false;
    }

    public void MobileJump()
    {
        mobileJump = true;
    }

    private bool CanStandUp()
    {
        return !Physics.SphereCast(headCheck.position, 0.3f, Vector3.up, out _, standUpCheckHeight, groundLayer);
    }

    private void Start()
    {
        switchmenu = Switcher.GetComponent<SwitchMenu>();
        UpdateHandButtons();


        if (mobileIcons.isMobile)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        currentMoveSpeed = moveSpeed;
        targetMoveSpeed = moveSpeed;

        playeranimations.SetBool("walk", false);
        playeranimations.SetBool("switch", false);
        playeranimations.SetBool("jump", false);
        playeranimations.SetBool("crouch", false);



        if (handmanager.hasRedHand == false)
        {
            RedHand.SetActive(false);
        }
        if (handmanager.hasBlueHand == false)
        {
            BlueHand.SetActive(false);
        }
        if (handmanager.hasPurpleHand == false)
        {
            PurpleHand.SetActive(false);
        }
        if (handmanager.hasPressureHand == false)
        {
            FlareHand.SetActive(false);
        }
        if (handmanager.hasConductiveHand == false)
        {
            conductiveHand.SetActive(false);
        }
        if (handmanager.hasFireHand == false)
        {
            FireHand.SetActive(false);
        }
        if (handmanager.hasGreenHand == false)
        {
            GreenHand.SetActive(false);
        }

        InitializeStartingHand();


        if (StartCrouched)
        {
            EnterCrouch();

        }

    }

    void OnEnterSqueeze()
    {
        sprintToggled = false;
        targetMoveSpeed = squeezeSpeed;

        if (IsCrouched)
            ExitCrouch();

        playeranimations.speed = 1f;

        lockCamera = true;

        squeezeStartBodyRotation = transform.localRotation;
        squeezeStartCameraRotation = playerCamera.localRotation;

    }

    void OnExitSqueeze()
    {
        targetMoveSpeed = moveSpeed;

        unlockingCamera = true;
        lockCamera = false;

        unlockStartBodyRotation = transform.localRotation;
        unlockStartCameraRotation = playerCamera.localRotation;

        unlockTimer = 0f;
    }

    public void UpdateHandButtons()
    {
        if (redHandButton != null)
            redHandButton.SetActive(handmanager.hasRedHand);

        if (purpleHandButton != null)
            purpleHandButton.SetActive(handmanager.hasPurpleHand);

        if (flareHandButton != null)
            flareHandButton.SetActive(handmanager.hasPressureHand);

        if (conductiveHandButton != null)
            conductiveHandButton.SetActive(handmanager.hasConductiveHand);

        if (magnetHandButton != null)
            magnetHandButton.SetActive(handmanager.hasMagnetHand);

        if (fireHandButton != null)
            fireHandButton.SetActive(handmanager.hasFireHand);

        if (greenHandButton != null)
            greenHandButton.SetActive(handmanager.hasGreenHand);
    }

    string GetCurrentHand()
    {
        if (RedHand.activeSelf) return "red";
        if (PurpleHand.activeSelf) return "purple";
        if (FlareHand.activeSelf) return "flare";
        if (conductiveHand.activeSelf) return "conductive";
        if (MagnetHand.activeSelf) return "magnet";
        if (FireHand.activeSelf) return "fire";
        if (GreenHand.activeSelf) return "green";

        return "";
    }

    void CycleHand(int direction)
    {
        List<string> availableHands = new List<string>();

        if (handmanager.hasRedHand) availableHands.Add("red");
        if (handmanager.hasPurpleHand) availableHands.Add("purple");
        if (handmanager.hasPressureHand) availableHands.Add("flare");
        if (handmanager.hasConductiveHand) availableHands.Add("conductive");
        if (handmanager.hasMagnetHand) availableHands.Add("magnet");
        if (handmanager.hasFireHand) availableHands.Add("fire");
        if (handmanager.hasGreenHand) availableHands.Add("green");

        if (availableHands.Count == 0) return;

        string currentHand = GetCurrentHand();
        currentHandIndex = availableHands.IndexOf(currentHand);

        currentHandIndex += direction;

        if (currentHandIndex >= availableHands.Count)
            currentHandIndex = 0;
        if (currentHandIndex < 0)
            currentHandIndex = availableHands.Count - 1;

        handtoSwitch = availableHands[currentHandIndex];

        canSwitch = false;
        playeranimations.SetBool("switch", true);
        playeranimations.SetTrigger("Switch");
    }

    private void Update()
    {
        if (squeeze && !wasSqueezing)
        {
            OnEnterSqueeze();
        }
        else if (!squeeze && wasSqueezing)
        {
            OnExitSqueeze();
        }

        wasSqueezing = squeeze;

        if (canSwitch && !squeeze)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll != 0f && CanSwitchHands())
            {
                if (Time.time - lastScrollSwitchTime > scrollSwitchCooldown)
                {
                    if (scroll > 0f)
                        CycleHand(1);
                    else
                        CycleHand(-1);

                    lastScrollSwitchTime = Time.time;
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha1) && handmanager.hasRedHand)
            {
                if (redcable.isActive == false && purplecable.isActive == false && pressurecable.isActive == false && conductivecable.isActive == false && magnetcable.isActive == false && greencable.isActive == false)
                {
                    if (redlaunch.holdingbattery == false && purplelaunch.holdingbattery == false && pressurelaunch.holdingbattery == false && conductivelaunch.holdingbattery == false && greenlaunch.holdingbattery == false && magnetlaunch.holdingbattery == false)
                    {
                        canSwitch = false;
                        playeranimations.SetBool("switch", true);
                        handtoSwitch = "red";
                        playeranimations.SetTrigger("Switch");

                    }

                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) && handmanager.hasPurpleHand)
            {
                if (redcable.isActive == false && purplecable.isActive == false && pressurecable.isActive == false && conductivecable.isActive == false && magnetcable.isActive == false && greencable.isActive == false)
                {
                    if (redlaunch.holdingbattery == false && purplelaunch.holdingbattery == false && pressurelaunch.holdingbattery == false && conductivelaunch.holdingbattery == false && greenlaunch.holdingbattery == false && magnetlaunch.holdingbattery == false)
                    {
                        canSwitch = false;

                        playeranimations.SetBool("switch", true);
                        handtoSwitch = "purple";
                        playeranimations.SetTrigger("Switch");

                    }

                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha3) && handmanager.hasPressureHand)
            {
                if (redcable.isActive == false && purplecable.isActive == false && pressurecable.isActive == false && conductivecable.isActive == false && magnetcable.isActive == false && greencable.isActive == false)
                {
                    if (redlaunch.holdingbattery == false && purplelaunch.holdingbattery == false && pressurelaunch.holdingbattery == false && conductivelaunch.holdingbattery == false && greenlaunch.holdingbattery == false && magnetlaunch.holdingbattery == false)
                    {
                        canSwitch = false;

                        playeranimations.SetBool("switch", true);
                        handtoSwitch = "flare";
                        playeranimations.SetTrigger("Switch");

                    }

                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha4) && handmanager.hasConductiveHand)
            {
                if (redcable.isActive == false && purplecable.isActive == false && pressurecable.isActive == false && conductivecable.isActive == false && magnetcable.isActive == false && greencable.isActive == false)
                {
                    if (redlaunch.holdingbattery == false && purplelaunch.holdingbattery == false && pressurelaunch.holdingbattery == false && conductivelaunch.holdingbattery == false && greenlaunch.holdingbattery == false && magnetlaunch.holdingbattery == false)
                    {
                        canSwitch = false;

                        playeranimations.SetBool("switch", true);
                        playeranimations.SetTrigger("Switch");

                        handtoSwitch = "conductive";
                    }

                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha5) && handmanager.hasMagnetHand)
            {
                if (redcable.isActive == false && purplecable.isActive == false && pressurecable.isActive == false && conductivecable.isActive == false && magnetcable.isActive == false && greencable.isActive == false)
                {
                    if (redlaunch.holdingbattery == false && purplelaunch.holdingbattery == false && pressurelaunch.holdingbattery == false && conductivelaunch.holdingbattery == false && magnetlaunch.holdingbattery == false && greenlaunch.holdingbattery == false)
                    {
                        canSwitch = false;

                        playeranimations.SetBool("switch", true);
                        playeranimations.SetTrigger("Switch");

                        handtoSwitch = "magnet";
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.Alpha6) && handmanager.hasFireHand)
            {
                if (redcable.isActive == false && purplecable.isActive == false && pressurecable.isActive == false && conductivecable.isActive == false && magnetcable.isActive == false && greencable.isActive == false)
                {
                    if (redlaunch.holdingbattery == false && purplelaunch.holdingbattery == false && pressurelaunch.holdingbattery == false && conductivelaunch.holdingbattery == false && magnetlaunch.holdingbattery == false && greenlaunch.holdingbattery == false)
                    {
                        canSwitch = false;

                        playeranimations.SetBool("switch", true);
                        playeranimations.SetTrigger("Switch");

                        handtoSwitch = "fire";
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha7) && handmanager.hasGreenHand)
            {
                if (redcable.isActive == false && purplecable.isActive == false && pressurecable.isActive == false && conductivecable.isActive == false && magnetcable.isActive == false && greencable.isActive == false)
                {
                    if (redlaunch.holdingbattery == false && purplelaunch.holdingbattery == false && pressurelaunch.holdingbattery == false && conductivelaunch.holdingbattery == false && magnetlaunch.holdingbattery == false && greenlaunch.holdingbattery == false)
                    {
                        canSwitch = false;

                        playeranimations.SetBool("switch", true);
                        playeranimations.SetTrigger("Switch");

                        handtoSwitch = "green";
                    }
                }
            }
        }
       

        isGrounded = Physics.SphereCast(groundCheck.position, groundCheckRadius, Vector3.down, out RaycastHit hit, groundCheckRadius + 0.3f, groundLayer);

        float mouseX;
        float mouseY;

        if (mobileIcons.isMobile)
        {
            mouseX = mobileLookInput.x * lookSpeedX;
            mouseY = mobileLookInput.y * lookSpeedY;
        }
        else
        {
            mouseX = Input.GetAxis("Mouse X") * lookSpeedX;
            mouseY = Input.GetAxis("Mouse Y") * lookSpeedY;
        }

        if (!squeeze)
        {
            
            if (canLook)
            {
                targetX -= mouseY;
                targetX = Mathf.Clamp(targetX, -90f, 90f);
                targetY += mouseX;
            }

        }

        if (mobileIcons.isMobile)
        {
            rotationX = targetX;
            currentY = targetY;
        }
        else
        {
            rotationX = Mathf.Lerp(rotationX, targetX, 30f * Time.deltaTime);
            currentY = Mathf.Lerp(currentY, targetY, 30f * Time.deltaTime);
        }

        Quaternion normalBodyRotation = Quaternion.Euler(0f, currentY, 0f);
        Quaternion normalCameraRotation = Quaternion.Euler(rotationX, 0f, 0f);

        if (lockCamera)
        {
            Quaternion targetYaw = squeezeStartBodyRotation * Quaternion.Euler(0f, 35f, 0f);

            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                targetYaw,
                squeezeCameraLerpSpeed * Time.deltaTime
            );
        }
        else if (unlockingCamera)
        {
            unlockTimer += Time.deltaTime;
            float t = unlockTimer / unlockDuration;

            transform.localRotation = Quaternion.Slerp(
                unlockStartBodyRotation,
                normalBodyRotation,
                t
            );

            playerCamera.localRotation = Quaternion.Slerp(
                unlockStartCameraRotation,
                normalCameraRotation,
                t
            );

            if (t >= 1f)
            {
                unlockingCamera = false;
            }
        }
        else
        {
            playerCamera.localRotation = normalCameraRotation;
            transform.localRotation = normalBodyRotation;
        }


        float moveX;
        float moveZ;

        if (mobileIcons.isMobile)
        {
            if (mobileJoystick != null)
                mobileMoveInput = mobileJoystick.GetInput();

            moveX = mobileMoveInput.x;
            moveZ = mobileMoveInput.y;
        }
        else
        {
            moveX = Input.GetAxisRaw("Horizontal");
            moveZ = Input.GetAxisRaw("Vertical");
        }

        if (!CanMove)
        {
            moveX = 0f;
            moveZ = 0f;

            mobileMoveInput = Vector2.zero;
            currentMoveDirection = Vector3.zero;

            playeranimations.SetBool("walk", false);
        }

        if (squeeze)
        {
            moveX = 0f;
        }

        Vector3 rawDirection = transform.right * moveX + transform.forward * moveZ;

        currentMoveDirection = Vector3.MoveTowards(
            currentMoveDirection,
            rawDirection,
            (isGrounded ? 20f : airInputSmooth) * Time.deltaTime
        );

        moveDirection = currentMoveDirection.normalized;

        if (squeeze)
        {
            if (moveDirection.magnitude > 0.1f)
                playeranimations.speed = 1f;
            else
                playeranimations.speed = 0f;
        }

        if ((moveX != 0 || moveZ != 0))
        {
            playeranimations.SetBool("walk", true);

            if (footstepCoroutine == null)
            {
                footstepCoroutine = StartCoroutine(PlayFootsteps());
            }

            float moveAmount = new Vector2(moveX, moveZ).magnitude;

            float speedFactor = targetMoveSpeed / moveSpeed;

            float targetBob = moveAmount * speedFactor;

            currentBob = Mathf.Lerp(currentBob, targetBob, bobSmoothSpeed * Time.deltaTime);

            playeranimations.SetFloat("BobIntensity", currentBob);

        }
        else
        {
            playeranimations.SetBool("walk", false);

            if (footstepCoroutine != null)
            {
                StopCoroutine(footstepCoroutine);
                footstepCoroutine = null;
            }
        }

        float strafeAmount = Vector3.Dot(currentMoveDirection.normalized, transform.right);
        float targetTilt = -strafeAmount * maxWeaponTilt;

        Quaternion targetRotation = Quaternion.Euler(
            grabPackManager.localEulerAngles.x,
            grabPackManager.localEulerAngles.y,
            targetTilt
        );

        grabPackManager.localRotation = Quaternion.Slerp(
            grabPackManager.localRotation,
            targetRotation,
            weaponTiltSpeed * Time.deltaTime
        );

        if (isGrounded)
        {
            playeranimations.SetBool("jump", false);
        }
        else
        {
            playeranimations.SetBool("jump", true);
        }

        if (!IsCrouched)
        {
            if (!squeeze)
            {
                bool sprinting;

                if (mobileIcons.isMobile)
                {
                    if (mobileMoveInput.y > 0.9f)
                    {
                        if (Time.time - lastTapTime < tapThreshold)
                        {
                            sprinting = true;
                        }
                        else
                        {
                            sprinting = false;
                        }

                        lastTapTime = Time.time;
                    }
                    else
                    {
                        sprinting = false;
                    }
                }
                else
                {
                    sprinting = Input.GetKey(KeyCode.LeftShift);
                }


                if (sprinting)
                {
                    targetMoveSpeed = moveSpeed * sprintMultiplier;
                    playeranimations.speed = 1.6f;
                }
                else
                {
                    targetMoveSpeed = moveSpeed;
                    playeranimations.speed = 1f;
                }
            }
           
        }

        if (isGrounded)
        {
            bool crouchPressed;

            if (!squeeze)
            {
                if (mobileIcons.isMobile)
                {
                    crouchPressed = crouchToggled;
                }
                else
                {
                    crouchPressed = Input.GetKeyDown(KeyCode.LeftControl);
                }

                if (isGrounded)
                {
                    if (mobileIcons.isMobile)
                    {
                        if (crouchToggled && !IsCrouched)
                        {
                            EnterCrouch();
                        }
                        else if (!crouchToggled && IsCrouched)
                        {
                            if (CanStandUp())
                                ExitCrouch();
                        }
                    }
                    else
                    {
                        if (Input.GetKeyDown(KeyCode.LeftControl) && !IsCrouched)
                        {
                            EnterCrouch();
                        }
                        else if (Input.GetKeyUp(KeyCode.LeftControl) && IsCrouched)
                        {
                            if (CanStandUp())
                                ExitCrouch();
                        }
                    }
                }
            }


        }

        if (squeeze)
        {
            targetMoveSpeed = squeezeSpeed;
        }
        currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, targetMoveSpeed, sprintAcceleration * Time.deltaTime);

        bool jumpPressed = mobileIcons.isMobile ? mobileJump : Input.GetButtonDown("Jump");

        if (!IsCrouched && jumpPressed && isGrounded)
        {

            if (!squeeze)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                footstepSource.PlayOneShot(jumpsfx, 3.0f);
            }


        }

        mobileJump = false;
    }

    void EnterCrouch()
    {
        targetMoveSpeed = moveSpeed * crouchMultiplier;
        playeranimations.speed = 1f;

        IsCrouched = true;
        croucher.SetBool("Crouched", true);
        playeranimations.SetBool("crouch", true);
        playerstandingCollider.enabled = false;

        footstepSource.PlayOneShot(crouchsfx, 1.0f);
    }

    void ExitCrouch()
    {
        croucher.SetBool("Crouched", false);
        playeranimations.SetBool("crouch", false);
        IsCrouched = false;
        playerstandingCollider.enabled = true;

        footstepSource.PlayOneShot(uncrouchsfx, 1.0f);
    }


    private void FixedUpdate()
    {
        if (!CanMove)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        rb.linearVelocity = new Vector3(
            moveDirection.x * currentMoveSpeed,
            rb.linearVelocity.y,
            moveDirection.z * currentMoveSpeed
        );
    }

    private IEnumerator PlayFootsteps()
    {
        isPlayingFootsteps = true;

        while (moveDirection.magnitude > 0)
        {
            if (isGrounded)
            {
                float footstepInterval = IsCrouched ? 0.8f : (targetMoveSpeed > moveSpeed ? 0.3f : 0.5f);
                float volume = IsCrouched ? 0.5f : (targetMoveSpeed > moveSpeed ? 1.5f : 1.0f);

                AudioClip footstepClip = GetFootstepSound();
                if (footstepClip != null)
                {
                    footstepSource.PlayOneShot(footstepClip, volume);
                    if (!IsCrouched)
                    {
                        NoiseEmitter.EmitNoise(transform.position, 0.4f, transform);

                    }
                }

                yield return new WaitForSeconds(footstepInterval);
            }
            else
            {
                yield return null;
            }
        }

        isPlayingFootsteps = false;
    }

    private AudioClip GetFootstepSound()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            switch (hit.collider.tag)
            {
                case "Grass": return grassFootsteps[Random.Range(0, grassFootsteps.Length)];
                case "Wood": return woodFootsteps[Random.Range(0, woodFootsteps.Length)];
                case "Concrete": return concreteFootsteps[Random.Range(0, concreteFootsteps.Length)];
                case "Grab-able": return concreteFootsteps[Random.Range(0, concreteFootsteps.Length)];
                case "Metal": return MetalFootsteps[Random.Range(0, MetalFootsteps.Length)];

            }
        }
        return null;
    }

    public void SwitchHand()
    {
        playeranimations.speed = 1f;
        if (squeeze)
        {
            targetMoveSpeed = squeezeSpeed;
        }
        else if (IsCrouched)
        {
            targetMoveSpeed = moveSpeed * crouchMultiplier;
        }
        else
        {
            targetMoveSpeed = moveSpeed;
        }
        if (handtoSwitch == "red")
        {
            redhand();
        }
        if (handtoSwitch == "purple")
        {
            purplehand();
        }
        if (handtoSwitch == "flare")
        {
            flarehand();
        }
        if (handtoSwitch == "conductive")
        {
            conductivehand();
        }
        if (handtoSwitch == "magnet")
        {
            magnethand();
        }
        if (handtoSwitch == "fire")
        {
            firehand();
        }
        if (handtoSwitch == "green")
        {
            greenhand();
        }
    }

    public void redhand()
    {
        RedHand.SetActive(true);
        PurpleHand.SetActive(false);
        FlareHand.SetActive(false);
        conductiveHand.SetActive(false);
        MagnetHand.SetActive(false);
        FireHand.SetActive(false);
        GreenHand.SetActive(false);

    }

    public void purplehand()
    {
        RedHand.SetActive(false);
        PurpleHand.SetActive(true);
        FlareHand.SetActive(false);
        conductiveHand.SetActive(false);
        MagnetHand.SetActive(false);
        FireHand.SetActive(false);
        GreenHand.SetActive(false);

    }

    public void flarehand()
    {
        RedHand.SetActive(false);
        PurpleHand.SetActive(false);
        FlareHand.SetActive(true);
        conductiveHand.SetActive(false);
        MagnetHand.SetActive(false);
        FireHand.SetActive(false);
        GreenHand.SetActive(false);

    }

    public void conductivehand()
    {
        RedHand.SetActive(false);
        PurpleHand.SetActive(false);
        FlareHand.SetActive(false);
        conductiveHand.SetActive(true);
        MagnetHand.SetActive(false);
        FireHand.SetActive(false);
        GreenHand.SetActive(false);

    }
    public void magnethand()
    {
        RedHand.SetActive(false);
        PurpleHand.SetActive(false);
        FlareHand.SetActive(false);
        conductiveHand.SetActive(false);
        FireHand.SetActive(false);
        MagnetHand.SetActive(true);
        GreenHand.SetActive(false);

    }

    public void firehand()
    {
        RedHand.SetActive(false);
        PurpleHand.SetActive(false);
        FlareHand.SetActive(false);
        conductiveHand.SetActive(false);
        MagnetHand.SetActive(false);
        FireHand.SetActive(true);
        GreenHand.SetActive(false);

    }

    public void greenhand()
    {
        RedHand.SetActive(false);
        PurpleHand.SetActive(false);
        FlareHand.SetActive(false);
        conductiveHand.SetActive(false);
        MagnetHand.SetActive(false);
        FireHand.SetActive(false);
        GreenHand.SetActive(true);

    }

    private void InitializeStartingHand()
    {
        RedHand.SetActive(false);
        PurpleHand.SetActive(false);
        FlareHand.SetActive(false);
        conductiveHand.SetActive(false);
        MagnetHand.SetActive(false);
        FireHand.SetActive(false);
        GreenHand.SetActive(false);

        BlueHand.SetActive(handmanager.hasBlueHand);

        if (handmanager.hasRedHand)
        {
            redhand();
        }
        else if (handmanager.hasPurpleHand)
        {
            purplehand();
        }
        else if (handmanager.hasPressureHand)
        {
            flarehand();
        }
        else if (handmanager.hasConductiveHand)
        {
            conductivehand();
        }
        else if (handmanager.hasMagnetHand)
        {
            magnethand();
        }
        else if (handmanager.hasFireHand)
        {
            firehand();
        }
        else if (handmanager.hasGreenHand)
        {
            greenhand();
        }
    }

    public void StopFootsteps()
    {
        if (footstepCoroutine != null)
        {
            StopCoroutine(footstepCoroutine);
            footstepCoroutine = null;
        }

        isPlayingFootsteps = false;

        if (footstepSource.isPlaying)
        {
            footstepSource.Stop();
        }
    }

    public void MobileSwitchRed()
    {
        if (!handmanager.hasRedHand) return;
        if (!CanSwitchHands()) return;

        if (!RedHand.activeSelf)
        {
            handtoSwitch = "red";
            TriggerSwitch();
            canSwitch = false;
            righthandIcon.color = red;
            switchmenu.closed();

        }
    }

    public void MobileSwitchPurple()
    {
        if (!handmanager.hasPurpleHand) return;
        if (!CanSwitchHands()) return;

        if (!PurpleHand.activeSelf)
        {
            handtoSwitch = "purple";
            TriggerSwitch();
            canSwitch = false;
            righthandIcon.color = purple;
            switchmenu.closed();

        }
    }

    public void MobileSwitchFlare()
    {
        if (!handmanager.hasPressureHand) return;
        if (!CanSwitchHands()) return;

        if (!FlareHand.activeSelf)
        {
            handtoSwitch = "flare";
            TriggerSwitch();
            canSwitch = false;
            righthandIcon.color = grey;
            switchmenu.closed();

        }
    }

    public void MobileSwitchConductive()
    {
        if (!handmanager.hasConductiveHand) return;
        if (!CanSwitchHands()) return;

        if (!conductiveHand.activeSelf)
        {
            handtoSwitch = "conductive";
            TriggerSwitch();
            canSwitch = false;
            righthandIcon.color = yellow;
            switchmenu.closed();
        }
    }

    public void MobileSwitchMagnet()
    {
        if (!handmanager.hasMagnetHand) return;
        if (!CanSwitchHands()) return;

        if (!MagnetHand.activeSelf)
        {
            handtoSwitch = "magnet";
            TriggerSwitch();
            canSwitch = false;
            righthandIcon.color = grey;
            switchmenu.closed();
        }
    }

    public void MobileSwitchFire()
    {
        if (!handmanager.hasFireHand) return;
        if (!CanSwitchHands()) return;

        if (!FireHand.activeSelf)
        {
            handtoSwitch = "fire";
            TriggerSwitch();
            canSwitch = false;
            righthandIcon.color = red;
            switchmenu.closed();
        }
    }

    public void MobileSwitchGreen()
    {
        if (!handmanager.hasGreenHand) return;
        if (!CanSwitchHands()) return;

        if (!GreenHand.activeSelf)
        {
            handtoSwitch = "green";
            TriggerSwitch();
            canSwitch = false;
            righthandIcon.color = green;
            switchmenu.closed();
        }
    }

    bool CanSwitchHands()
    {
        if (redcable.isActive || purplecable.isActive || pressurecable.isActive || conductivecable.isActive || magnetcable.isActive || greencable.isActive)
            return false;

        if (redlaunch.holdingbattery || purplelaunch.holdingbattery || pressurelaunch.holdingbattery || conductivelaunch.holdingbattery || magnetlaunch.holdingbattery || greenlaunch.holdingbattery)
            return false;


        return true;
    }

    void TriggerSwitch()
    {
        playeranimations.SetBool("switch", true);
        playeranimations.SetTrigger("Switch");
    }


    LaunchHand GetActiveHand(string side)
    {
        foreach (LaunchHand hand in allLaunchHands)
        {
            if (hand.Hand == side && hand.gameObject.activeInHierarchy)
                return hand;
        }
        return null;
    }



    public void MobileFireDownLeft()
    {
        LaunchHand hand = GetActiveHand("Left");
        if (hand != null)
            hand.UIButtonDown();
    }

    public void MobileFireUpLeft()
    {
        LaunchHand hand = GetActiveHand("Left");
        if (hand != null)
            hand.UIButtonUp();
    }

    public void MobileFireDownRight()
    {
        LaunchHand hand = GetActiveHand("Right");
        if (hand != null)
            hand.UIButtonDown();
    }

    public void MobileFireUpRight()
    {
        LaunchHand hand = GetActiveHand("Right");
        if (hand != null)
            hand.UIButtonUp();
    }
}
