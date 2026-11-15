using UnityEngine;

// Carton dans lequel un survivant est enfermé après 3 coups de Huggy. C'est un objet physique léger qui suit le joueur ;
// les autres survivants peuvent l'accrocher au Grabpack pour le tirer, ou le libérer en maintenant F.
public class BoxedPlayerBox : MonoBehaviour
{
	public DbUnityMovment Owner { get; private set; }

	private BoxCollider boxCollider;

	public static GameObject Create(DbUnityMovment owner)
	{
		GameObject root = new GameObject("Boxed_" + owner.name);
		int grabLayer = LayerMask.NameToLayer("Grabable");
		if (grabLayer >= 0)
		{
			root.layer = grabLayer;
		}
		BoxedPlayerBox box = root.AddComponent<BoxedPlayerBox>();
		box.Owner = owner;

		Shader shader = Shader.Find("Standard");
		Material cardboard = new Material(shader);
		cardboard.color = new Color(0.66f, 0.47f, 0.27f);
		Material tape = new Material(shader);
		tape.color = new Color(0.82f, 0.7f, 0.45f);
		Material dark = new Material(shader);
		dark.color = new Color(0.42f, 0.29f, 0.15f);

		Part(root.transform, "Body", new Vector3(0f, 0.5f, 0f), new Vector3(1.05f, 1.0f, 1.05f), cardboard);
		Part(root.transform, "Lid", new Vector3(0f, 1.02f, 0f), new Vector3(1.1f, 0.05f, 1.1f), dark);
		Part(root.transform, "TapeTop", new Vector3(0f, 1.06f, 0f), new Vector3(0.14f, 0.012f, 1.1f), tape);
		Part(root.transform, "TapeFront", new Vector3(0f, 0.55f, 0.53f), new Vector3(0.14f, 0.9f, 0.012f), tape);

		box.boxCollider = root.AddComponent<BoxCollider>();
		box.boxCollider.center = new Vector3(0f, 0.5f, 0f);
		box.boxCollider.size = new Vector3(1.05f, 1.0f, 1.05f);

		// le carton ne doit pas coincer les joueurs : il ignore toutes les capsules de personnages
		foreach (CharacterController cc in Object.FindObjectsOfType<CharacterController>())
		{
			Physics.IgnoreCollision(box.boxCollider, cc, true);
		}
		box.Follow();
		return root;
	}

	private static void Part(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
	{
		GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
		g.name = name;
		Destroy(g.GetComponent<Collider>());
		g.transform.SetParent(parent, false);
		g.transform.localPosition = pos;
		g.transform.localScale = scale;
		g.GetComponent<Renderer>().sharedMaterial = mat;
	}

	private void Follow()
	{
		if (Owner == null)
		{
			return;
		}
		transform.position = Owner.transform.position;
		transform.rotation = Quaternion.Euler(0f, Owner.transform.eulerAngles.y, 0f);
	}

	private void LateUpdate()
	{
		if (Owner == null)
		{
			Destroy(gameObject);
			return;
		}
		Follow();
	}
}
