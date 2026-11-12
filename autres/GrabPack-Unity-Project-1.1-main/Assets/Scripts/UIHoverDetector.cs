using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class UIHoverText : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TMP_Text targetText; 
    [TextArea] public string handName;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetText != null)
            targetText.text = handName;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetText != null)
            targetText.text = "";
    }
}