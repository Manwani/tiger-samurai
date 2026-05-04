using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LivesController : MonoBehaviour
{
    [SerializeField, Min(0)] private int startingLives = 3;
    [SerializeField] private Color livesTextColor = Color.white;
    [SerializeField] private Color gameOverTextColor = new(1f, 0.25f, 0.2f, 1f);
    [SerializeField, Min(1)] private int livesFontSize = 34;
    [SerializeField, Min(1)] private int gameOverFontSize = 96;
    [SerializeField] private Vector2 livesTextOffset = new(24f, -24f);
    [SerializeField] private Vector2 livesTextSize = new(320f, 72f);

    private Canvas canvas;
    private Text livesText;
    private Text gameOverText;
    private int currentLives;

    public int CurrentLives => currentLives;
    public int MaxLives => Mathf.Max(0, startingLives);
    public bool CanRestoreLife => !IsGameOver && currentLives < MaxLives;
    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        Time.timeScale = 1f;
        currentLives = MaxLives;
        EnsureUi();
        UpdateLivesText();
        SetGameOverVisible(false);
    }

    private void OnValidate()
    {
        startingLives = Mathf.Max(0, startingLives);
        livesFontSize = Mathf.Max(1, livesFontSize);
        gameOverFontSize = Mathf.Max(1, gameOverFontSize);
    }

    public void LoseLife()
    {
        if (IsGameOver)
        {
            return;
        }

        currentLives--;
        UpdateLivesText();

        if (currentLives <= 0)
        {
            ShowGameOver();
        }
    }

    public bool RestoreLife(int amount = 1)
    {
        if (!CanRestoreLife)
        {
            return false;
        }

        currentLives = Mathf.Min(MaxLives, currentLives + Mathf.Max(1, amount));
        UpdateLivesText();
        return true;
    }

    private void ShowGameOver()
    {
        IsGameOver = true;
        SetGameOverVisible(true);
        Time.timeScale = 0f;
    }

    private void EnsureUi()
    {
        EnsureCanvas();
        EnsureLivesText();
        EnsureGameOverText();
    }

    private void EnsureCanvas()
    {
        if (canvas != null)
        {
            return;
        }

        GameObject canvasObject = new("LivesCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();
    }

    private void EnsureLivesText()
    {
        if (livesText != null)
        {
            return;
        }

        GameObject textObject = new("LivesText");
        textObject.transform.SetParent(canvas.transform, false);

        livesText = textObject.AddComponent<Text>();
        livesText.font = GetReadableFont(livesFontSize);
        livesText.fontSize = livesFontSize;
        livesText.color = livesTextColor;
        livesText.alignment = TextAnchor.UpperLeft;
        livesText.raycastTarget = false;

        RectTransform rectTransform = livesText.rectTransform;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = livesTextOffset;
        rectTransform.sizeDelta = livesTextSize;
    }

    private void EnsureGameOverText()
    {
        if (gameOverText != null)
        {
            return;
        }

        GameObject textObject = new("GameOverText");
        textObject.transform.SetParent(canvas.transform, false);

        gameOverText = textObject.AddComponent<Text>();
        gameOverText.font = GetReadableFont(gameOverFontSize);
        gameOverText.fontSize = gameOverFontSize;
        gameOverText.color = gameOverTextColor;
        gameOverText.alignment = TextAnchor.MiddleCenter;
        gameOverText.raycastTarget = false;
        gameOverText.text = "GAME OVER";

        RectTransform rectTransform = gameOverText.rectTransform;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void UpdateLivesText()
    {
        EnsureLivesText();
        livesText.text = $"Lives: {Mathf.Max(0, currentLives)}";
        livesText.fontSize = livesFontSize;
        livesText.color = livesTextColor;
    }

    private void SetGameOverVisible(bool visible)
    {
        EnsureGameOverText();
        gameOverText.gameObject.SetActive(visible);
    }

    private static Font GetReadableFont(int fontSize)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            return font;
        }

        return Font.CreateDynamicFontFromOSFont("Arial", fontSize);
    }
}
