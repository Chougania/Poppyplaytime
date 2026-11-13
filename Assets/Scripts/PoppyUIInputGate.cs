using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Central gate preventing gameplay input while any UI layer owns the cursor.</summary>
public static class PoppyUIInputGate
{
    private static bool menuOpen;
    private static bool lobbyOpen;
    private static bool pauseOpen;
    private static bool shopOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        menuOpen = false;
        lobbyOpen = false;
        pauseOpen = false;
        shopOpen = false;
    }

    public static bool IsBlocked => menuOpen || lobbyOpen || pauseOpen || shopOpen;
    public static bool IsLobbyOpen => lobbyOpen;

    // The lobby is a playable staging area: movement is enabled there,
    // while UI pointer clicks are still protected from gameplay actions.
    public static bool IsGameplayBlocked =>
        menuOpen || pauseOpen || shopOpen || IsPointerOverUI();

    public static void SetMenuOpen(bool value) { menuOpen = value; }
    public static void SetLobbyOpen(bool value) { lobbyOpen = value; }
    public static void SetPauseOpen(bool value) { pauseOpen = value; }
    public static void SetShopOpen(bool value) { shopOpen = value; }

    public static void Clear()
    {
        menuOpen = false;
        lobbyOpen = false;
        pauseOpen = false;
        shopOpen = false;
    }

    public static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    public static bool ShouldBlockGameplayMouse()
    {
        return menuOpen || lobbyOpen || pauseOpen || shopOpen || IsPointerOverUI();
    }
}
