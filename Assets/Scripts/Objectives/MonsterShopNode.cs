using UnityEngine;

// Node de la boutique des monstres : place-le dans ta scène. Le monstre est affiché en temps réel à cet endroit,
// avec changement de skin (flèches, A/D), rotation à la souris, zoom à la molette et lumière qui prend la couleur du skin.
public class MonsterShopNode : MonoBehaviour
{
	[Header("Placement (optionnel : sinon ce node lui-même)")]
	[Tooltip("Où se tient le monstre (ses pieds). Il regarde dans la direction avant (bleu) de cet objet.")]
	public Transform monsterAnchor;

	[Tooltip("Position/orientation de la caméra de la boutique. Si vide, une caméra par défaut est placée devant le monstre.")]
	public Transform cameraAnchor;

	[Header("Comportement")]
	public bool playIdleAnimation = true;

	[Min(0f)] public float autoRotateSpeed = 0f;

	[Min(0f)] public float zoomRange = 2.5f;

	[Header("Décor généré automatiquement")]
	public bool buildPedestal = true;

	[Min(0.5f)] public float pedestalRadius = 1.3f;

	public float defaultCameraDistance = 4.2f;

	public float defaultCameraHeight = 1.7f;

	private Light skinLight;
	private Renderer ringRenderer;
	private float pulse;
	private bool built;

	// Compatible avec une hiérarchie faite à la main : si les champs sont vides, on cherche les enfants par leur nom
	// (ex. "ShopCameraAnchor", "CameraAnchor" pour la caméra ; "MonsterAnchor" pour le monstre).
	private Transform FindChildByName(string contains)
	{
		foreach (Transform t in GetComponentsInChildren<Transform>(true))
		{
			if (t != transform && t.name.IndexOf(contains, System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return t;
			}
		}
		return null;
	}

	public Transform MonsterAnchor
	{
		get
		{
			if (monsterAnchor == null)
			{
				monsterAnchor = FindChildByName("MonsterAnchor");
			}
			return monsterAnchor != null ? monsterAnchor : transform;
		}
	}

	public Transform CameraAnchor
	{
		get
		{
			if (cameraAnchor == null)
			{
				cameraAnchor = FindChildByName("Camera");
			}
			return cameraAnchor;
		}
	}

	public void GetDefaultCamera(out Vector3 position, out Quaternion rotation)
	{
		Transform a = MonsterAnchor;
		position = a.position + a.forward * defaultCameraDistance + Vector3.up * defaultCameraHeight;
		Vector3 target = a.position + Vector3.up * 1.4f;
		rotation = Quaternion.LookRotation(target - position, Vector3.up);
	}

	private void Awake()
	{
		if (buildPedestal)
		{
			BuildPedestal();
		}
	}

	private void BuildPedestal()
	{
		if (built)
		{
			return;
		}
		built = true;
		Transform a = MonsterAnchor;
		GameObject root = new GameObject("MonsterShopNode_Pedestal");
		root.transform.SetParent(transform, false);
		root.transform.position = a.position;
		root.transform.rotation = Quaternion.Euler(0f, a.eulerAngles.y, 0f);

		GameObject baseGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		baseGo.name = "Base";
		Destroy(baseGo.GetComponent<Collider>());
		baseGo.transform.SetParent(root.transform, false);
		baseGo.transform.localPosition = new Vector3(0f, -0.06f, 0f);
		baseGo.transform.localScale = new Vector3(pedestalRadius * 2f, 0.06f, pedestalRadius * 2f);
		Material dark = new Material(Shader.Find("Standard"));
		dark.color = new Color(0.03f, 0.035f, 0.04f);
		baseGo.GetComponent<Renderer>().sharedMaterial = dark;

		GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		ring.name = "Ring";
		Destroy(ring.GetComponent<Collider>());
		ring.transform.SetParent(root.transform, false);
		ring.transform.localPosition = new Vector3(0f, 0.002f, 0f);
		ring.transform.localScale = new Vector3(pedestalRadius * 1.85f, 0.004f, pedestalRadius * 1.85f);
		Material glow = new Material(Shader.Find("Sprites/Default"));
		glow.color = new Color(0.2f, 0.5f, 1f);
		ringRenderer = ring.GetComponent<Renderer>();
		ringRenderer.sharedMaterial = glow;

		GameObject lg = new GameObject("SkinLight");
		lg.transform.SetParent(root.transform, false);
		lg.transform.localPosition = new Vector3(0f, 3f, 1.4f);
		skinLight = lg.AddComponent<Light>();
		skinLight.type = LightType.Point;
		skinLight.range = 9f;
		skinLight.intensity = 2.2f;
		skinLight.color = glow.color;
	}

	// Appelé par le menu à chaque changement de skin : la lumière et l'anneau prennent la couleur du skin.
	public void SetSkinColor(Color c)
	{
		if (ringRenderer != null)
		{
			ringRenderer.sharedMaterial.color = c;
		}
		if (skinLight != null)
		{
			skinLight.color = c;
		}
	}

	public void Pulse()
	{
		pulse = 0.5f;
	}

	private void Update()
	{
		if (skinLight == null)
		{
			return;
		}
		if (pulse > 0f)
		{
			pulse -= Time.unscaledDeltaTime;
		}
		skinLight.intensity = 2.2f + Mathf.Max(0f, pulse) * 8f;
	}

#if UNITY_EDITOR
	private void OnDrawGizmos()
	{
		Transform a = MonsterAnchor;
		Gizmos.color = new Color(1f, 0.3f, 0.3f);
		Gizmos.DrawWireSphere(a.position + Vector3.up * 0.05f, pedestalRadius);
		Gizmos.DrawRay(a.position + Vector3.up * 1f, a.forward * 1.5f);
		Gizmos.DrawWireCube(a.position + Vector3.up * 1f, new Vector3(0.9f, 2f, 0.9f));
		Vector3 camPos;
		Quaternion camRot;
		if (cameraAnchor != null)
		{
			camPos = cameraAnchor.position;
			camRot = cameraAnchor.rotation;
		}
		else
		{
			GetDefaultCamera(out camPos, out camRot);
		}
		Gizmos.color = new Color(0.3f, 0.8f, 1f);
		Gizmos.DrawWireSphere(camPos, 0.2f);
		Gizmos.DrawRay(camPos, camRot * Vector3.forward * 1.5f);
		Gizmos.DrawLine(camPos, a.position + Vector3.up * 1.4f);
		UnityEditor.Handles.Label(a.position + Vector3.up * 2.4f, "Monstre (boutique)");
		UnityEditor.Handles.Label(camPos + Vector3.up * 0.3f, "Caméra boutique monstres");
	}
#endif
}
