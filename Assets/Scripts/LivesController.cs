using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LivesController : MonoBehaviour
{
    [SerializeField, Min(0)] private int startingLives = 300;
    [SerializeField] private Color livesTextColor = Color.white;
    [SerializeField] private Color gameOverTextColor = new(1f, 0.25f, 0.2f, 1f);
    [SerializeField, Min(1)] private int livesFontSize = 34;
    [SerializeField, Min(1)] private int gameOverFontSize = 96;
    [SerializeField] private Vector2 livesTextOffset = new(24f, -24f);
    [SerializeField] private Vector2 livesTextSize = new(320f, 72f);
    [SerializeField] private string playAgainButtonLabel = "PLAY AGAIN";
    [SerializeField, Min(1)] private int playAgainButtonFontSize = 34;
    [SerializeField] private Vector2 playAgainButtonOffset = new(0f, -145f);
    [SerializeField] private Vector2 playAgainButtonSize = new(320f, 78f);
    [SerializeField] private Color playAgainButtonColor = new(1f, 0.25f, 0.2f, 0.95f);
    [SerializeField] private Color playAgainButtonTextColor = Color.white;
    [SerializeField] private PlayerDamageFeedback playerDamageFeedback;
    [SerializeField] private bool logLifeLossEvents = true;

    private Canvas canvas;
    private Text livesText;
    private Text gameOverText;
    private Button playAgainButton;
    private Text playAgainButtonText;
    private int currentLives;
    private bool isRestarting;

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
        playAgainButtonFontSize = Mathf.Max(1, playAgainButtonFontSize);
    }

    private void Update()
    {
        if (!IsGameOver || isRestarting)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            RestartGame();
        }
    }

    public void LoseLife(string damageReason = null)
    {
        if (IsGameOver)
        {
            return;
        }

        currentLives--;
        PlayDamageFeedback();
        UpdateLivesText();
        LogLifeLoss(damageReason);

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

    private void PlayDamageFeedback()
    {
        ResolvePlayerDamageFeedback();
        playerDamageFeedback?.PlayDamageFlash();
    }

    private void EnsureUi()
    {
        EnsureCanvas();
        EnsureLivesText();
        EnsureGameOverText();
        EnsurePlayAgainButton();
    }

    private void ResolvePlayerDamageFeedback()
    {
        if (playerDamageFeedback != null)
        {
            return;
        }

        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null)
        {
            return;
        }

        playerDamageFeedback = playerController.GetComponent<PlayerDamageFeedback>();
        if (playerDamageFeedback == null)
        {
            playerDamageFeedback = playerController.gameObject.AddComponent<PlayerDamageFeedback>();
        }
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

    private void EnsurePlayAgainButton()
    {
        if (playAgainButton != null)
        {
            return;
        }

        GameObject buttonObject = new("PlayAgainButton");
        buttonObject.transform.SetParent(canvas.transform, false);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = playAgainButtonColor;

        playAgainButton = buttonObject.AddComponent<Button>();
        playAgainButton.targetGraphic = buttonImage;
        ColorBlock buttonColors = playAgainButton.colors;
        buttonColors.normalColor = playAgainButtonColor;
        buttonColors.highlightedColor = Color.Lerp(playAgainButtonColor, Color.white, 0.2f);
        buttonColors.selectedColor = buttonColors.highlightedColor;
        buttonColors.pressedColor = Color.Lerp(playAgainButtonColor, Color.black, 0.18f);
        buttonColors.disabledColor = new Color(playAgainButtonColor.r, playAgainButtonColor.g, playAgainButtonColor.b, 0.4f);
        playAgainButton.colors = buttonColors;
        playAgainButton.onClick.AddListener(RestartGame);

        RectTransform buttonRect = playAgainButton.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = playAgainButtonOffset;
        buttonRect.sizeDelta = playAgainButtonSize;

        GameObject textObject = new("Text");
        textObject.transform.SetParent(buttonObject.transform, false);

        playAgainButtonText = textObject.AddComponent<Text>();
        playAgainButtonText.font = GetReadableFont(playAgainButtonFontSize);
        playAgainButtonText.fontSize = playAgainButtonFontSize;
        playAgainButtonText.color = playAgainButtonTextColor;
        playAgainButtonText.alignment = TextAnchor.MiddleCenter;
        playAgainButtonText.raycastTarget = false;
        playAgainButtonText.text = playAgainButtonLabel;

        RectTransform textRect = playAgainButtonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
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
        EnsurePlayAgainButton();
        gameOverText.gameObject.SetActive(visible);
        playAgainButton.gameObject.SetActive(visible);
    }

    private void RestartGame()
    {
        if (isRestarting)
        {
            return;
        }

        isRestarting = true;
        Time.timeScale = 1f;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

        SceneManager.LoadScene(activeScene.name);
    }

    private void LogLifeLoss(string damageReason)
    {
        if (!logLifeLossEvents)
        {
            return;
        }

        string reasonText = string.IsNullOrWhiteSpace(damageReason)
            ? "unknown damage source"
            : damageReason;

        Debug.Log($"Life lost from {reasonText}. Lives remaining: {Mathf.Max(0, currentLives)}", this);
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
