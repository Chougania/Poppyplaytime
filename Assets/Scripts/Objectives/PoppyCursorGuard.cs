using Unity.Netcode;
using UnityEngine;

// Garde-fou du curseur : il est toujours visible et libre quand une interface (menu, salon, pause, boutique) est ouverte,
// quand aucune partie n'est lancée ou quand le joueur est éliminé. Il est verrouillé uniquement en plein jeu.
public class PoppyCursorGuard : MonoBehaviour
{
	private bool wasFree = true;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void Boot()
	{
		if (FindObjectOfType<PoppyCursorGuard>() != null)
		{
			return;
		}
		GameObject go = new GameObject("PoppyCursorGuard");
		DontDestroyOnLoad(go);
		go.AddComponent<PoppyCursorGuard>();
	}

	private static bool LocalPlayerNeedsCursor()
	{
		NetworkManager nm = NetworkManager.Singleton;
		if (nm == null || !nm.IsListening || nm.LocalClient == null || nm.LocalClient.PlayerObject == null)
		{
			return true;
		}
		GameManager gm = FindObjectOfType<GameManager>();
		return gm != null && gm.IsClientEliminated(nm.LocalClientId);
	}

	private void LateUpdate()
	{
		bool free = PoppyUIInputGate.IsBlocked || LocalPlayerNeedsCursor();
		if (free)
		{
			if (!Cursor.visible || Cursor.lockState != CursorLockMode.None)
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
		}
		else if (wasFree)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		else if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
		{
			// après Échap dans l'éditeur : un clic reprend le contrôle
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
		}
		wasFree = free;
	}
}
