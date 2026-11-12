using Unity.Netcode;
using UnityEngine;

public enum GrabpackAbility
{
    Blue = 0,
    Red = 1,
    Green = 2,
    Magnet = 3,
    Flare = 4,
    Blacklight = 5
}

[DisallowMultipleComponent]
public class GrabpackProgression : NetworkBehaviour
{
    private const string SaveKey = "Poppy.Grabpack.UnlockedMask";
    private const int StartingMask = (1 << (int)GrabpackAbility.Blue) | (1 << (int)GrabpackAbility.Red);

    [Header("Optional hand models - assign in the player prefab")]
    public GameObject greenHandVisual;
    public GameObject magnetHandVisual;
    public GameObject flareHandVisual;
    public GameObject blacklightHandVisual;

    [Header("Puzzle interaction")]
    [Min(1f)] public float interactionDistance = 4f;
    public LayerMask puzzleLayer = ~0;
    public KeyCode interactKey = KeyCode.E;
    public Transform viewOrigin;

    private readonly NetworkVariable<int> unlockedMask = new NetworkVariable<int>(
        StartingMask, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int UnlockedMask => unlockedMask.Value;

    public override void OnNetworkSpawn()
    {
        unlockedMask.OnValueChanged += OnUnlockedMaskChanged;
        ApplyHandVisuals();

        if (IsOwner)
        {
            int savedMask = PlayerPrefs.GetInt(SaveKey, StartingMask) | StartingMask;
            if (IsServer)
                unlockedMask.Value = savedMask;
            else
                RestoreProgressServerRpc(savedMask);
        }
    }

    public override void OnNetworkDespawn()
    {
        unlockedMask.OnValueChanged -= OnUnlockedMaskChanged;
    }

    private void Update()
    {
        if (!IsSpawned || !IsOwner || !Input.GetKeyDown(interactKey))
            return;

        Transform origin = viewOrigin != null ? viewOrigin : (Camera.main != null ? Camera.main.transform : null);
        if (origin == null)
            return;

        if (Physics.Raycast(origin.position, origin.forward, out RaycastHit hit,
            interactionDistance, puzzleLayer, QueryTriggerInteraction.Collide))
        {
            GrabpackPuzzleInteractable puzzle = hit.collider.GetComponentInParent<GrabpackPuzzleInteractable>();
            if (puzzle != null)
            {
                if (HasAbility(puzzle.requiredAbility))
                    puzzle.TryActivateServerRpc();
                else
                    Debug.Log("GrabPack: cette main n'est pas encore débloquée : " + puzzle.requiredAbility, puzzle);
            }
        }
    }

    public bool HasAbility(GrabpackAbility ability)
    {
        int bit = 1 << (int)ability;
        return (unlockedMask.Value & bit) != 0;
    }

    public bool UnlockAbility(GrabpackAbility ability)
    {
        if (!IsServer)
            return false;

        int bit = 1 << (int)ability;
        if ((unlockedMask.Value & bit) != 0)
            return false;

        unlockedMask.Value |= bit;
        return true;
    }

    [ServerRpc]
    private void RestoreProgressServerRpc(int savedMask)
    {
        unlockedMask.Value |= (savedMask | StartingMask);
    }

    private void OnUnlockedMaskChanged(int previous, int current)
    {
        ApplyHandVisuals();

        if (IsOwner)
        {
            PlayerPrefs.SetInt(SaveKey, current);
            PlayerPrefs.Save();
        }
    }

    private void ApplyHandVisuals()
    {
        SetVisual(greenHandVisual, GrabpackAbility.Green);
        SetVisual(magnetHandVisual, GrabpackAbility.Magnet);
        SetVisual(flareHandVisual, GrabpackAbility.Flare);
        SetVisual(blacklightHandVisual, GrabpackAbility.Blacklight);
    }

    private void SetVisual(GameObject visual, GrabpackAbility ability)
    {
        if (visual != null)
            visual.SetActive(HasAbility(ability));
    }
}
