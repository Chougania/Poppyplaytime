using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class PrefabCatalogEntry
{
    public GameObject prefab;
    public Sprite icon;
}

public class PrefabCatalogUI : MonoBehaviour
{
    public RuntimePrefabPlacer placer;

    public GameObject buttonTemplate;
    public Transform content;

    public GameObject panel;

    public PrefabCatalogEntry[] prefabs;

    void Start()
    {
        buttonTemplate.SetActive(false);
        PopulateCatalog();

        panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            bool open = !panel.activeSelf;

            panel.SetActive(open);

            placer.enabled = !open;
        }
    }

    void PopulateCatalog()
    {
        for (int i = 0; i < prefabs.Length; i++)
        {
            PrefabCatalogEntry entry = prefabs[i];

            GameObject buttonObj = Instantiate(buttonTemplate, content);
            buttonObj.SetActive(true);

            Button btn = buttonObj.GetComponent<Button>();

            TMP_Text label = buttonObj.GetComponentInChildren<TMP_Text>();
            Image icon = buttonObj.GetComponentInChildren<Image>();

            label.text = entry.prefab.name;

            if (icon != null && entry.icon != null)
                icon.sprite = entry.icon;

            int index = i;

            btn.onClick.AddListener(() =>
            {
                placer.SetPrefab(index);
            });
        }
    }
}