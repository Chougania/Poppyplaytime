using UnityEngine;

/// <summary>
/// Local prototype catalogue for AI monster appearances. The host's selected skin
/// is copied into a NetworkVariable by HuggyHunterAI so every client sees the same look.
/// </summary>
public static class PoppyMonsterShop
{
    private const string CreditsKey = "Poppy.Shop.Credits";
    private const string SelectedKey = "Poppy.Shop.MonsterSkin";
    private const string UnlockedPrefix = "Poppy.Shop.MonsterSkin.";

    private static readonly string[] Names =
    {
        "CLASSIC HUGGY", "NIGHTMARE", "TOXIC EXPERIMENT", "FROSTBITE",
        "SHADOW HUGGY", "MOLTEN PROTOTYPE"
    };

    private static readonly Color32[] Colors =
    {
        new Color32(35, 105, 220, 255),
        new Color32(72, 12, 24, 255),
        new Color32(85, 210, 55, 255),
        new Color32(95, 225, 245, 255),
        new Color32(35, 25, 55, 255),
        new Color32(245, 90, 25, 255)
    };

    private static readonly int[] Prices = { 0, 180, 220, 240, 0, 0 };
    private static readonly int[] TicketPrices = { 0, 0, 0, 0, 1, 2 };

    public static int SkinCount => Names.Length;
    public static int SelectedSkin => Mathf.Clamp(PlayerPrefs.GetInt(SelectedKey, 0), 0, SkinCount - 1);
    public static string GetName(int index) => Names[Mathf.Clamp(index, 0, Names.Length - 1)];
    public static Color GetColor(int index) => Colors[Mathf.Clamp(index, 0, Colors.Length - 1)];
    public static int GetPrice(int index) => Prices[Mathf.Clamp(index, 0, Prices.Length - 1)];
    public static int GetTicketPrice(int index) => TicketPrices[Mathf.Clamp(index, 0, TicketPrices.Length - 1)];

    public static bool IsUnlocked(int index)
    {
        return index >= 0 && index < SkinCount &&
            (index == 0 || PlayerPrefs.GetInt(UnlockedPrefix + index, 0) == 1);
    }

    public static bool BuyOrSelect(int index)
    {
        if (index < 0 || index >= SkinCount)
            return false;

        if (!IsUnlocked(index))
        {
            int ticketPrice = GetTicketPrice(index);
            if (ticketPrice > 0)
            {
                if (!PoppyShop.SpendTickets(ticketPrice))
                    return false;
            }
            else
            {
                int price = GetPrice(index);
                int credits = PlayerPrefs.GetInt(CreditsKey, 350);
                if (credits < price)
                    return false;
                PlayerPrefs.SetInt(CreditsKey, credits - price);
            }
            PlayerPrefs.SetInt(UnlockedPrefix + index, 1);
        }

        PlayerPrefs.SetInt(SelectedKey, index);
        PlayerPrefs.Save();
        return true;
    }

    public static void Apply(GameObject modelRoot, int skinIndex)
    {
        if (modelRoot == null)
            return;

        Color color = GetColor(skinIndex);
        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        MaterialPropertyBlock block = new MaterialPropertyBlock();

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(block);
        }
    }
}
