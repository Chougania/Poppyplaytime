using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class DbUnityMovment : NetworkBehaviour
{
	[Header("Single Prefab Mesh Swapping")]
	public GameObject survivorModelContainer;

	public GameObject monsterModelContainer;

	public GameObject deadModelContainer;

	[Header("Hider Mechanics to Disable")]
	public MonoBehaviour grabpackScript;

	public GameObject grabpackPhysicalModel;

	[Header("Monster Attack Settings")]
	public float killDistance = 3f;

	public Transform attackRaycastOrigin;

	public BoxCollider attackCubeCollider;

	public LayerMask survivorLayer;

	[Header("Movement Speeds")]
	public float walkSpeed = 4f;

	public float sprintSpeed = 6f;

	public float crouchSpeed = 2f;

	public float monsterSpeed = 5.5f;

	public float jumpHeight = 1.8f;

	public float gravity = 9.81f;

	[Header("Survivor Stamina")]
	[Min(0.1f)] public float maxStamina = 5f;
	[Min(0f)] public float staminaDrainPerSecond = 1f;
	[Min(0f)] public float staminaRecoveryPerSecond = 0.8f;
	[Min(0f)] public float staminaRecoveryDelay = 1.25f;
	[Range(0f, 1f)] public float exhaustedThreshold = 0.08f;

	private float _stamina;
	private float _lastSprintTime;
	private bool _wasExhausted;

	public float StaminaNormalized => maxStamina <= 0f ? 0f : Mathf.Clamp01(_stamina / maxStamina);
	public bool IsExhausted => _wasExhausted;

	[Header("Camera Settings")]
	public Transform cameraTransform;

	public Vector3 cameraOffset = new Vector3(0.58f, 1.18f, -1.97f);

	public float mouseSensitivity = 2f;

	public float crouchOffsetMultiplier = 0.5f;

	public float heightSmoothTime = 0.12f;

	private CharacterController _controller;

	private Animator _animator;

	private float _verticalVelocity;

	private float _verticalRotation;

	private Vector3 _currentCameraOffset;

	private Vector3 _offsetVelocity;

	private bool _isLockedInAttack;

	private bool _isLocalVictimFrozen;

	public NetworkVariable<Vector3> NetPosition = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	public NetworkVariable<Quaternion> NetRotation = new NetworkVariable<Quaternion>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	public NetworkVariable<float> NetMoveSpeed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	public NetworkVariable<bool> IsCrouching = new NetworkVariable<bool>(value: false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	public NetworkVariable<bool> IsSprinting = new NetworkVariable<bool>(value: false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	public NetworkVariable<bool> IsWalking = new NetworkVariable<bool>(value: false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

	private NetworkVariable<bool> _netIsLockedInAttack = new NetworkVariable<bool>(value: false);

	private NetworkVariable<int> _skinColorIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	private void Awake()
	{
		_controller = GetComponent<CharacterController>();
		_animator = GetComponentInChildren<Animator>();
	}

	public override void OnNetworkSpawn()
	{
		_skinColorIndex.OnValueChanged += OnSkinColorChanged;
		ApplySkinColor(_skinColorIndex.Value);
		if (IsOwner)
			RequestSkinColorServerRpc(PoppyShop.SelectedColor);
		if (monsterModelContainer != null && monsterModelContainer.activeSelf)
		{
			_animator = monsterModelContainer.GetComponent<Animator>() ?? monsterModelContainer.GetComponentInChildren<Animator>();
		}
		else if (survivorModelContainer != null && survivorModelContainer.activeSelf)
		{
			_animator = survivorModelContainer.GetComponent<Animator>() ?? survivorModelContainer.GetComponentInChildren<Animator>();
		}
		if (IsOwner)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			_currentCameraOffset = cameraOffset;
			_stamina = Mathf.Max(0.1f, maxStamina);
			if (cameraTransform != null)
			{
				cameraTransform.localPosition = cameraOffset;
				if (cameraTransform.TryGetComponent<Camera>(out var component))
				{
					component.enabled = true;
				}
				if (cameraTransform.TryGetComponent<AudioListener>(out var component2))
				{
					component2.enabled = true;
				}
			}
			if (survivorModelContainer != null && survivorModelContainer.activeSelf)
			{
				SetGrabpackStatus(enableGrabpack: true);
			}
			else
			{
				SetGrabpackStatus(enableGrabpack: false);
			}
		}
		else
		{
			if (cameraTransform != null)
			{
				if (cameraTransform.TryGetComponent<Camera>(out var component3))
				{
					component3.enabled = false;
				}
				if (cameraTransform.TryGetComponent<AudioListener>(out var component4))
				{
					component4.enabled = false;
				}
			}
			if (_controller != null)
			{
				_controller.enabled = false;
			}
			SetGrabpackStatus(enableGrabpack: false);
		}
		if (monsterModelContainer != null && monsterModelContainer.activeSelf)
		{
			gameObject.layer = LayerMask.NameToLayer("Default");
		}
		else if (survivorModelContainer != null && survivorModelContainer.activeSelf)
		{
			gameObject.layer = LayerMask.NameToLayer("Player");
		}
		if (attackCubeCollider != null)
		{
			attackCubeCollider.enabled = false;
			attackCubeCollider.isTrigger = true;
		}
		if (deadModelContainer != null)
		{
			deadModelContainer.SetActive(value: false);
		}
	}


	public override void OnNetworkDespawn()
	{
		_skinColorIndex.OnValueChanged -= OnSkinColorChanged;
	}

	[ServerRpc]
	private void RequestSkinColorServerRpc(int colorIndex)
	{
		if (colorIndex >= 0 && colorIndex < PoppyShop.ColorCount)
			_skinColorIndex.Value = colorIndex;
	}

	private void OnSkinColorChanged(int previous, int current)
	{
		ApplySkinColor(current);
	}

	public void RefreshSkinColor()
	{
		ApplySkinColor(_skinColorIndex.Value);
	}

	private void ApplySkinColor(int colorIndex)
	{
		if (survivorModelContainer != null)
			PoppyShop.ApplyColor(survivorModelContainer, colorIndex);
		if (monsterModelContainer != null)
			PoppyShop.ApplyColor(monsterModelContainer, colorIndex);
	}

	private void Update()
	{
		if (!IsOwner)
		{
			if (!_netIsLockedInAttack.Value)
			{
				transform.position = Vector3.Lerp(transform.position, NetPosition.Value, Time.deltaTime * 25f);
				transform.rotation = Quaternion.Slerp(transform.rotation, NetRotation.Value, Time.deltaTime * 25f);
				UpdateAnimatorProperties(NetMoveSpeed.Value, IsCrouching.Value, IsSprinting.Value, IsWalking.Value);
			}
		}
		else if (_netIsLockedInAttack.Value || _isLockedInAttack || _isLocalVictimFrozen)
		{
			if (_controller != null && _controller.enabled)
			{
				_controller.Move(Vector3.zero);
			}
			UpdateAnimatorProperties(0f, IsCrouching.Value, sprint: false, walk: false);
		}
		else
		{
			HandleCameraPosition();
			HandleRotation();
			HandleMovement();
			HandleMonsterAttack();
		}
	}

	private void HandleCameraPosition()
	{
		if (!(cameraTransform == null))
		{
			Vector3 target = cameraOffset;
			if (IsCrouching.Value)
			{
				target.y *= crouchOffsetMultiplier;
			}
			_currentCameraOffset = Vector3.SmoothDamp(_currentCameraOffset, target, ref _offsetVelocity, heightSmoothTime);
			cameraTransform.localPosition = _currentCameraOffset;
		}
	}

	private void HandleRotation()
	{
		float num = Input.GetAxis("Mouse X") * mouseSensitivity;
		transform.Rotate(Vector3.up * num);
		if (cameraTransform != null)
		{
			_verticalRotation -= Input.GetAxis("Mouse Y") * mouseSensitivity;
			_verticalRotation = Mathf.Clamp(_verticalRotation, -60f, 60f);
			cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
		}
		NetRotation.Value = transform.rotation;
	}

\tprivate void HandleMovement()
	{
		float axis = Input.GetAxis("Horizontal");
		float axis2 = Input.GetAxis("Vertical");
		bool wantsSprint = Input.GetKey(KeyCode.LeftShift) && axis2 > 0.1f;
		bool crouching = Input.GetKey(KeyCode.LeftControl);
		bool isMonster = monsterModelContainer != null && monsterModelContainer.activeSelf;
		float inputMagnitude = new Vector2(axis, axis2).magnitude;

		if (!isMonster)
		{
			if (wantsSprint && !crouching && _stamina > maxStamina * exhaustedThreshold)
			{
				_stamina = Mathf.Max(0f, _stamina - staminaDrainPerSecond * Time.deltaTime);
				_lastSprintTime = Time.time;
				if (_stamina <= maxStamina * exhaustedThreshold)
					_wasExhausted = true;
			}
			else if (Time.time >= _lastSprintTime + staminaRecoveryDelay)
			{
				float recovery = crouching ? staminaRecoveryPerSecond * 1.35f : staminaRecoveryPerSecond;
				_stamina = Mathf.Min(maxStamina, _stamina + recovery * Time.deltaTime);
				if (_stamina > maxStamina * 0.25f)
					_wasExhausted = false;
			}
		}
		else
		{
			_stamina = maxStamina;
			_wasExhausted = false;
		}

		bool sprinting = !isMonster && wantsSprint && !crouching &&
			_stamina > maxStamina * exhaustedThreshold && !_wasExhausted;
		float speed = isMonster ? monsterSpeed : (crouching ? crouchSpeed : (sprinting ? sprintSpeed : walkSpeed));

		Vector3 direction = transform.right * axis + transform.forward * axis2;
		if (direction.magnitude > 1f)
			direction.Normalize();

		if (_controller.enabled && _controller.isGrounded)
		{
			_verticalVelocity = -2f;
			if (Input.GetButtonDown("Jump") && !isMonster)
			{
				_verticalVelocity = Mathf.Sqrt(jumpHeight * 2f * gravity);
				JumpServerRpc();
			}
		}
		else
		{
			_verticalVelocity -= gravity * Time.deltaTime;
		}

		Vector3 velocity = direction * speed;
		velocity.y = _verticalVelocity;
		if (_controller.enabled)
			_controller.Move(velocity * Time.deltaTime);

		float animationSpeed = inputMagnitude > 0.05f ? speed : 0f;
		NetPosition.Value = transform.position;
		NetMoveSpeed.Value = animationSpeed;
		IsCrouching.Value = crouching;
		IsSprinting.Value = sprinting && animationSpeed > walkSpeed;
		IsWalking.Value = animationSpeed > 0.1f && !sprinting && !crouching;
		UpdateAnimatorProperties(animationSpeed, crouching, IsSprinting.Value, IsWalking.Value);
	}

	private void UpdateAnimatorProperties(float speed, bool crouch, bool sprint, bool walk)
	{
		if (!(_animator == null))
		{
			_animator.SetFloat("Speed", speed);
			_animator.SetBool("IsCrouching", crouch);
			_animator.SetBool("IsWalking", walk);
			_animator.SetBool("IsSprinting", sprint);
		}
	}

	private void HandleMonsterAttack()
	{
		if (monsterModelContainer != null && monsterModelContainer.activeSelf && Input.GetMouseButtonDown(0) && !_isLockedInAttack && !_netIsLockedInAttack.Value)
		{
			RequestAttackVisualServerRpc();
			StartCoroutine(ActivateAttackCubeRoutine());
		}
	}

	[ServerRpc]
	private void RequestAttackVisualServerRpc()
	{
		TriggerMonsterAttackAnimationClientRpc();
	}

	[ClientRpc]
	private void TriggerMonsterAttackAnimationClientRpc()
	{
		if (_animator != null)
		{
			_animator.SetTrigger("Jumpscare");
		}
	}

	private IEnumerator ActivateAttackCubeRoutine()
	{
		if (attackCubeCollider == null)
		{
			yield break;
		}
		attackCubeCollider.enabled = true;
		Vector3 center = attackCubeCollider.transform.TransformPoint(attackCubeCollider.center);
		Vector3 halfExtents = Vector3.Scale(attackCubeCollider.size, attackCubeCollider.transform.lossyScale) / 2f;
		Quaternion rotation = attackCubeCollider.transform.rotation;
		Collider[] array = Physics.OverlapBox(center, halfExtents, rotation, survivorLayer);
		for (int i = 0; i < array.Length; i++)
		{
			PlayerHitbox component = array[i].GetComponent<PlayerHitbox>();
			if (component != null && component.networkObject != null && component.networkObject.OwnerClientId != OwnerClientId)
			{
				RequestKillTargetServerRpc(component.networkObject.OwnerClientId, component.networkObject.NetworkObjectId);
				attackCubeCollider.enabled = false;
				yield break;
			}
		}
		yield return new WaitForSeconds(0.5f);
		attackCubeCollider.enabled = false;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!(monsterModelContainer == null) && monsterModelContainer.activeSelf && IsOwner && !(attackCubeCollider == null) && attackCubeCollider.enabled)
		{
			PlayerHitbox component = other.GetComponent<PlayerHitbox>();
			if (component != null && component.networkObject != null && component.networkObject.OwnerClientId != OwnerClientId && !_isLockedInAttack && !_netIsLockedInAttack.Value)
			{
				RequestKillTargetServerRpc(component.networkObject.OwnerClientId, component.networkObject.NetworkObjectId);
				attackCubeCollider.enabled = false;
			}
		}
	}

	[ServerRpc]
	private void RequestKillTargetServerRpc(ulong victimClientId, ulong victimNetObjectId)
	{
		if (!IsServer || NetworkManager.Singleton == null ||
			monsterModelContainer == null || !monsterModelContainer.activeSelf ||
			_isLockedInAttack || _netIsLockedInAttack.Value)
			return;

		if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(victimNetObjectId, out NetworkObject victimObject) ||
			victimObject == null || victimObject.OwnerClientId != victimClientId ||
			victimClientId == OwnerClientId)
			return;

		DbUnityMovment victimMovement = victimObject.GetComponent<DbUnityMovment>();
		if (victimMovement == null || victimMovement.survivorModelContainer == null ||
			!victimMovement.survivorModelContainer.activeSelf)
			return;

		float allowedDistance = Mathf.Max(0.5f, killDistance);
		if ((victimObject.transform.position - transform.position).sqrMagnitude > allowedDistance * allowedDistance)
			return;

		if (attackCubeCollider != null)
		{
			Vector3 attackCenter = attackCubeCollider.transform.TransformPoint(attackCubeCollider.center);
			Vector3 attackSize = Vector3.Scale(attackCubeCollider.size, attackCubeCollider.transform.lossyScale);
			Bounds attackBounds = new Bounds(attackCenter, attackSize);

			Collider[] victimColliders = victimObject.GetComponentsInChildren<Collider>();
			bool hasUsableCollider = false;
			bool intersectsAttack = false;

			for (int i = 0; i < victimColliders.Length; i++)
			{
				Collider victimCollider = victimColliders[i];
				if (victimCollider == null || !victimCollider.enabled || victimCollider.isTrigger)
					continue;

				hasUsableCollider = true;
				if (attackBounds.Intersects(victimCollider.bounds))
				{
					intersectsAttack = true;
					break;
				}
			}

			if (hasUsableCollider && !intersectsAttack)
				return;
		}

		StartCoroutine(ServerKillSequence(victimClientId, victimNetObjectId));
	}

	private IEnumerator ServerKillSequence(ulong victimId, ulong victimNetObjectId)
	{
		_isLockedInAttack = true;
		_netIsLockedInAttack.Value = true;
		TriggerJumpscareClientRpc(victimId, victimNetObjectId);
		yield return new WaitForSeconds(2.5f);
		GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
		if (gameManager != null && gameManager.lobbySpawnPoint != null)
		{
			gameManager.ExecuteTargetedTeleport(victimId, gameManager.lobbySpawnPoint.position, isMonster: false, isSurvivor: true);
		}
		_isLockedInAttack = false;
		_netIsLockedInAttack.Value = false;
	}

	[ClientRpc]
	private void TriggerJumpscareClientRpc(ulong victimId, ulong victimNetObjectId)
	{
		if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null &&
			NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(victimNetObjectId, out var value))
		{
			DbUnityMovment component = value.GetComponent<DbUnityMovment>();
			if (component != null)
			{
				if (component.grabpackScript != null && component.grabpackScript is GrabpackController grabpackController)
				{
					grabpackController.ResetHandsBeforeDeath();
				}
				if (component.survivorModelContainer != null)
				{
					component.survivorModelContainer.SetActive(value: false);
				}
				if (component.deadModelContainer != null)
				{
					component.deadModelContainer.SetActive(value: true);
				}
				component.SetGrabpackStatus(enableGrabpack: false);
				component.StartCoroutine(component.LocalVictimFreezeAndTeleportGuardRoutine());
			}
		}
		if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == victimId && Camera.main != null)
		{
			Camera.main.transform.LookAt(transform.position + Vector3.up * 1.5f);
		}
	}

	private IEnumerator LocalVictimFreezeAndTeleportGuardRoutine()
	{
		_isLocalVictimFrozen = true;
		if (_controller != null)
		{
			_controller.enabled = false;
		}
		yield return new WaitForSeconds(2.8f);
		if (IsOwner)
		{
			if (survivorModelContainer != null)
			{
				survivorModelContainer.SetActive(value: true);
			}
			if (deadModelContainer != null)
			{
				deadModelContainer.SetActive(value: false);
			}
			SetGrabpackStatus(enableGrabpack: true);
		}
		if (_controller != null)
		{
			_controller.enabled = true;
		}
		_isLocalVictimFrozen = false;
	}

	public void SetEliminatedState(bool eliminated, bool enableGrabpack)
	{
		if (!IsOwner)
			return;

		_isLocalVictimFrozen = eliminated;
		if (_controller != null)
			_controller.enabled = !eliminated;

		SetGrabpackStatus(!eliminated && enableGrabpack);
		if (eliminated)
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;
		}
		else
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
	}

	public void SetGrabpackStatus(bool enableGrabpack)
	{
		if (grabpackScript != null)
		{
			grabpackScript.enabled = enableGrabpack;
		}
		if (grabpackPhysicalModel != null)
		{
			grabpackPhysicalModel.SetActive(enableGrabpack);
		}
	}

	[ServerRpc]
	private void JumpServerRpc()
	{
		JumpClientRpc();
	}

	[ClientRpc]
	private void JumpClientRpc()
	{
			GetComponent<DbDPlayerAnimation>()?.PlayJump();
	}
}

