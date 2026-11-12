using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GrabpackColorManager : NetworkBehaviour
{
	[Serializable]
	public class HandReferences
	{
		public string handName = "Hand";

		public Renderer handRenderer;

		public int materialIndex;

		[Header("Scene Object Hierarchy Names")]
		[Tooltip("The exact name of the UI Button GameObject in your scene hierarchy.")]
		public string leftArrowButtonName = "LeftArrowButton";

		[Tooltip("The exact name of the UI Button GameObject in your scene hierarchy.")]
		public string rightArrowButtonName = "RightArrowButton";

		[Tooltip("Optional: The exact name of the TextMeshPro text display between arrows.")]
		public string colorStatusTextName = "ColorText";

		[HideInInspector]
		public int currentColorIndex;
	}

	[Serializable]
	public class ColorData
	{
		public string colorName;

		public Color colorValue;
	}

	[Header("Hand Mesh Setup")]
	public HandReferences leftHandSetup;

	public HandReferences rightHandSetup;

	[Header("Color Palette Configuration")]
	public ColorData[] colorPalette = new ColorData[6]
	{
		new ColorData
		{
			colorName = "Red",
			colorValue = Color.red
		},
		new ColorData
		{
			colorName = "Green",
			colorValue = Color.green
		},
		new ColorData
		{
			colorName = "Blue",
			colorValue = Color.blue
		},
		new ColorData
		{
			colorName = "Yellow",
			colorValue = Color.yellow
		},
		new ColorData
		{
			colorName = "Cyan",
			colorValue = Color.cyan
		},
		new ColorData
		{
			colorName = "Magenta",
			colorValue = Color.magenta
		}
	};

	[Header("UI Interaction Settings")]
	[Tooltip("The exact name of your main customization canvas panel in the scene.")]
	public string customizationCanvasName = "CustomizationCanvas";

	private Material leftHandMaterialInstance;

	private Material rightHandMaterialInstance;

	private bool isMenuOpen;

	private CharacterController characterController;

	private Rigidbody playerRigidbody;

	private MonoBehaviour customMovementScript;

	private MonoBehaviour mouseLookScript;

	public override void OnNetworkSpawn()
	{
		leftHandMaterialInstance = GetHandMaterial(leftHandSetup, "left");
		rightHandMaterialInstance = GetHandMaterial(rightHandSetup, "right");

		if (!IsOwner)
			return;

		if (leftHandSetup != null)
			FindAndHookUpUI(isLeftHand: true, leftHandSetup);
		if (rightHandSetup != null)
			FindAndHookUpUI(isLeftHand: false, rightHandSetup);

		CacheMovementComponents();
	}

	private Material GetHandMaterial(HandReferences setup, string side)
	{
		if (setup == null || setup.handRenderer == null)
			return null;

		Material[] materials = setup.handRenderer.materials;
		if (setup.materialIndex < 0 || setup.materialIndex >= materials.Length)
		{
			Debug.LogWarning($"GrabpackColorManager: Invalid {side} hand material index.", this);
			return null;
		}

		return materials[setup.materialIndex];
	}

	private void Update()
	{
		if (!IsOwner)
		{
			return;
		}
		if (string.IsNullOrEmpty(customizationCanvasName))
			return;

		GameObject gameObject = GameObject.Find(customizationCanvasName);
		if (gameObject != null)
		{
			bool activeInHierarchy = gameObject.activeInHierarchy;
			if (activeInHierarchy != isMenuOpen)
			{
				isMenuOpen = activeInHierarchy;
				SetPlayerMovementState(!isMenuOpen);
			}
		}
	}

	private void SetPlayerMovementState(bool canMove)
	{
		if (characterController != null)
		{
			characterController.enabled = canMove;
		}
		if (playerRigidbody != null)
		{
			playerRigidbody.velocity = Vector3.zero;
			playerRigidbody.angularVelocity = Vector3.zero;
			playerRigidbody.isKinematic = !canMove;
		}
		if (customMovementScript != null)
		{
			customMovementScript.enabled = canMove;
		}
		if (mouseLookScript != null)
		{
			mouseLookScript.enabled = canMove;
		}
		if (!canMove)
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

	private void CacheMovementComponents()
	{
		characterController = GetComponent<CharacterController>();
		playerRigidbody = GetComponent<Rigidbody>();
		MonoBehaviour[] componentsInChildren = GetComponentsInChildren<MonoBehaviour>();
		foreach (MonoBehaviour monoBehaviour in componentsInChildren)
		{
			string text = monoBehaviour.GetType().Name.ToLower();
			if (text.Contains("movement") || text.Contains("walk") || text.Contains("locomotion"))
			{
				customMovementScript = monoBehaviour;
			}
			if (text.Contains("look") || text.Contains("camera") || text.Contains("mouse"))
			{
				mouseLookScript = monoBehaviour;
			}
		}
	}

	private void FindAndHookUpUI(bool isLeftHand, HandReferences setup)
	{
		GameObject gameObject = GameObject.Find(setup.leftArrowButtonName);
		if (gameObject != null)
		{
			Button component = gameObject.GetComponent<Button>();
			if (component != null)
			{
				component.onClick.AddListener(() =>
				{
					LocalCycleColor(isLeftHand, -1);
				});
			}
		}
		GameObject gameObject2 = GameObject.Find(setup.rightArrowButtonName);
		if (gameObject2 != null)
		{
			Button component2 = gameObject2.GetComponent<Button>();
			if (component2 != null)
			{
				component2.onClick.AddListener(() =>
				{
					LocalCycleColor(isLeftHand, 1);
				});
			}
		}
		UpdateSceneTextDisplay(isLeftHand, setup.currentColorIndex);
	}

	private void LocalCycleColor(bool isLeftHand, int direction)
	{
		HandReferences handReferences = (isLeftHand ? leftHandSetup : rightHandSetup);
		int num = handReferences.currentColorIndex + direction;
		if (num >= colorPalette.Length)
		{
			num = 0;
		}
		if (num < 0)
		{
			num = colorPalette.Length - 1;
		}
		handReferences.currentColorIndex = num;
		RequestHandColorChangeServerRpc(isLeftHand, num);
	}

	[ServerRpc]
	public void RequestHandColorChangeServerRpc(bool isLeftHand, int colorIndex)
	{
		if (colorPalette != null && colorIndex >= 0 && colorIndex < colorPalette.Length &&
			colorPalette[colorIndex] != null)
		{
			UpdateHandColorClientRpc(isLeftHand, colorIndex);
		}
	}

	[ClientRpc]
	private void UpdateHandColorClientRpc(bool isLeftHand, int colorIndex)
	{
		if (colorPalette == null || colorIndex < 0 || colorIndex >= colorPalette.Length ||
			colorPalette[colorIndex] == null)
			return;

		Color colorValue = colorPalette[colorIndex].colorValue;
		if (isLeftHand && leftHandMaterialInstance != null)
		{
			leftHandMaterialInstance.color = colorValue;
			if (IsOwner)
			{
				UpdateSceneTextDisplay(isLeftHand: true, colorIndex);
			}
		}
		else if (!isLeftHand && rightHandMaterialInstance != null)
		{
			rightHandMaterialInstance.color = colorValue;
			if (IsOwner)
			{
				UpdateSceneTextDisplay(isLeftHand: false, colorIndex);
			}
		}
	}

	private void UpdateSceneTextDisplay(bool isLeftHand, int colorIndex)
	{
		HandReferences handReferences = (isLeftHand ? leftHandSetup : rightHandSetup);
		if (handReferences == null || colorPalette == null || colorIndex < 0 ||
			colorIndex >= colorPalette.Length || colorPalette[colorIndex] == null ||
			string.IsNullOrEmpty(handReferences.colorStatusTextName))
		{
			return;
		}
		GameObject gameObject = GameObject.Find(handReferences.colorStatusTextName);
		if (gameObject != null)
		{
			TextMeshProUGUI component = gameObject.GetComponent<TextMeshProUGUI>();
			if (component != null)
			{
				component.text = colorPalette[colorIndex].colorName;
			}
		}
	}

	private new void OnDestroy()
	{
		if (leftHandMaterialInstance != null)
		{
			UnityEngine.Object.Destroy(leftHandMaterialInstance);
		}
		if (rightHandMaterialInstance != null)
		{
			UnityEngine.Object.Destroy(rightHandMaterialInstance);
		}
	}
}

