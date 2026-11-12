using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Single source of truth for gameplay input ownership.
/// Menus, pause panels, shops and lobby screens block movement and GrabPack input.
/// </summary>
public static class PoppyUIInputGate
{
    private static int blockingOwners;

    public static bool IsBlocked => blockingOwners > 0;

    public static void SetBlocked(bool blocked)
    {
        blockingOwners = blocked ? Mathf.Max(0, blockingOwners + 1) : Mathf.Max(0, blockingOwners - 1);
    }

    public static void Clear()
    {
        blockingOwners = 0;
    }

    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }

    public static bool ShouldBlockGameplayMouse()
    {
        return IsBlocked || IsPointerOverUI() || Cursor.lockState != CursorLockMode.Locked;
    }
}
