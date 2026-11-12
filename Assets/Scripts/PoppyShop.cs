using UnityEngine;

/// <summary>
/// Local cosmetic shop data. Purchases are stored per device; match rewards are granted by the server.
/// Cosmetic ownership is intentionally separate from gameplay stats.
/// </summary>
public static class PoppyShop
{
    private const string CreditsKey = "Poppy.Shop.Credits";
    private const string SelectedKey = "Poppy.Shop.SelectedColor";
    private const string UnlockedPrefix = "Poppy.Shop.Color.";

    private static readonly string[] Names =
    {
        "CLASSIC BLUE", "TOY RED", "FACTORY YELLOW", "TOXIC GREEN",
        "MIDNIGHT PURPLE", "FROST CYAN", "BLOOD ORANGE", "NEON PINK"
    };

    private static readonly Color32[] Colors =
    {
        new Color32(35, 105, 220, 255),
        new Color32(220, 45, 55, 255),
        new Color32(245, 185, 35, 255),
        new Color32(55, 190, 105, 255),
        new Color32(105, 65, 180, 255),
        new Color32(55, 210, 220, 255),
        new Color32(230, 85, 35, 255),
        new Color32(235, 75, 165, 255)
    };

    private static readonly int[] Prices = { 0, 0, 0, 120, 160, 180, 220, 260 };

    public static int ColorCount => Names.Length;
    public static int SelectedColor => Mathf.Clamp(PlayerPrefs.GetInt(SelectedKey, 0), 0, ColorCount - 1);
    public static int Credits
    {
        get
        {
            if (!PlayerPrefs.HasKey(CreditsKey))
                PlayerPrefs.SetInt(CreditsKey, 350);
            return PlayerPrefs.GetInt(CreditsKey, 350);
        }
    }

    public static string GetColorName(int index) => Names[Mathf.Clamp(index, 0, Names.Length - 1)];
    public static Color GetColor(int index) => Colors[Mathf.Clamp(index, 0, Colors.Length - 1)];
    public static int GetPrice(int index) => Prices[Mathf.Clamp(index, 0, Prices.Length - 1)];
    public static bool IsUnlocked(int index) =>
        index >= 0 && index < ColorCount &&
        (index < 3 || PlayerPrefs.GetInt(UnlockedPrefix + index, 0) == 1);

    public static bool BuyOrSelect(int index)
    {
        if (index < 0 || index >= ColorCount)
            return false;

        if (!IsUnlocked(index))
        {
            int price = GetPrice(index);
            if (Credits < price)
                return false;

            PlayerPrefs.SetInt(CreditsKey, Credits - price);
            PlayerPrefs.SetInt(UnlockedPrefix + index, 1);
        }

        PlayerPrefs.SetInt(SelectedKey, index);
        PlayerPrefs.Save();
        return true;
    }

    public static void AddCredits(int amount)
    {
        if (amount <= 0)
            return;

        PlayerPrefs.SetInt(CreditsKey, Credits + amount);
        PlayerPrefs.Save();
    }

    public static void ApplyColor(GameObject modelRoot, int colorIndex)
    {
        if (modelRoot == null)
            return;

        Color color = GetColor(colorIndex);
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
