using TMPro;
using UnityEngine;

public class BattleCountdownUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panelRoot;
    public TMP_Text titleText;
    public TMP_Text countdownText;

    [Header("文案")]
    public string defaultTitle = "无线电塔已被摧毁";
    public string countdownFormat = "{0} 秒后你将失去前线连接";

    private void Awake()
    {
        Hide();
    }

    public void Show(string title, float seconds)
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(title) ? defaultTitle : title;

        SetRemaining(seconds);
    }

    public void SetRemaining(float seconds)
    {
        int displaySeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));

        if (countdownText != null)
            countdownText.text = string.Format(countdownFormat, displaySeconds);
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}