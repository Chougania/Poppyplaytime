using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CodeShowerUI : MonoBehaviour
{
	[Header("UI Elements")]
	public GameObject codeWindow;

	public TMP_Text codeTextDisplay;

	public Button closeButton;

	private void Start()
	{
		if (codeWindow != null)
		{
			codeWindow.SetActive(value: false);
		}
		if (closeButton != null)
		{
			closeButton.onClick.AddListener(HideCode);
		}
	}

	public void ShowCode(string code)
	{
		if (codeWindow != null && codeTextDisplay != null)
		{
			codeWindow.SetActive(value: true);
			codeTextDisplay.text = code;
		}
	}

	public void HideCode()
	{
		if (codeWindow != null)
		{
			codeWindow.SetActive(value: false);
		}
	}

	public void CopyToClipboard()
	{
		GUIUtility.systemCopyBuffer = codeTextDisplay.text;
		Debug.Log("Code copied to clipboard!");
	}
}

