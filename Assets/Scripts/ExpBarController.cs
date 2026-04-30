using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ExpBarController : MonoBehaviour
{
    [SerializeField] private Slider expBarSlider;
    [SerializeField, Min(1)] private int parriesForFullBar = 10;
    [SerializeField] private Color fillColor = new(1f, 0.88f, 0.35f, 1f);
    [SerializeField, Min(0f)] private float fillAnimationDuration = 0.18f;
    [SerializeField, Min(0)] private int sparkCount = 8;
    [SerializeField] private Color sparkColor = new(1f, 0.96f, 0.55f, 0.95f);
    [SerializeField, Min(1f)] private float sparkSize = 10f;
    [SerializeField, Min(0.01f)] private float sparkLifetime = 0.35f;
    [SerializeField, Min(0f)] private float sparkTravel = 42f;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private TileCircleSpawner tileCircleSpawner;
    [SerializeField] private bool spawnEnemyWhenFull = true;
    [SerializeField] private bool stopCirclesWhenFull = true;
    [SerializeField] private bool logEvents = true;

    private int currentParryExp;
    private bool hasTriggeredFullBarReward;
    private Coroutine fillCoroutine;
    private static Sprite runtimeSparkSprite;

    public int CurrentParryExp => currentParryExp;
    public int ParriesForFullBar => Mathf.Max(1, parriesForFullBar);
    public float Progress => Mathf.Clamp01(currentParryExp / (float)ParriesForFullBar);

    private void Start()
    {
        SetupBar();
        ResolveRewardReferences();
        UpdateBar();
    }

    private void OnDisable()
    {
        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
            fillCoroutine = null;
        }
    }

    private void OnValidate()
    {
        parriesForFullBar = Mathf.Max(1, parriesForFullBar);
        SetFillColor();
    }

    public void AddParryExp(int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        currentParryExp += amount;
        AnimateBarTo(Progress);
        SpawnSparkBurst(Progress);
        TryTriggerFullBarReward();
    }

    public void ResetBar()
    {
        currentParryExp = 0;
        hasTriggeredFullBarReward = false;
        StopFillAnimation();
        UpdateBar();
    }

    private void SetupBar()
    {
        if (expBarSlider == null)
        {
            expBarSlider = GetComponent<Slider>();
        }

        if (expBarSlider == null)
        {
            GameObject expBarObject = GameObject.Find("ExpBar");
            if (expBarObject != null)
            {
                expBarSlider = expBarObject.GetComponent<Slider>();
            }
        }

        if (expBarSlider == null)
        {
            return;
        }

        expBarSlider.minValue = 0f;
        expBarSlider.maxValue = 1f;
        expBarSlider.wholeNumbers = false;
        expBarSlider.interactable = false;
        SetFillColor();
    }

    private void UpdateBar()
    {
        if (expBarSlider == null)
        {
            SetupBar();
        }

        if (expBarSlider == null)
        {
            return;
        }

        expBarSlider.value = Progress;
        SetFillColor();
    }

    private void AnimateBarTo(float targetProgress)
    {
        if (expBarSlider == null)
        {
            SetupBar();
        }

        if (expBarSlider == null)
        {
            return;
        }

        StopFillAnimation();

        if (fillAnimationDuration <= 0f)
        {
            expBarSlider.value = targetProgress;
            SetFillColor();
            return;
        }

        fillCoroutine = StartCoroutine(FillRoutine(expBarSlider.value, targetProgress));
    }

    private IEnumerator FillRoutine(float startProgress, float targetProgress)
    {
        float elapsed = 0f;

        while (elapsed < fillAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fillAnimationDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            expBarSlider.value = Mathf.Lerp(startProgress, targetProgress, easedT);
            SetFillColor();
            yield return null;
        }

        expBarSlider.value = targetProgress;
        SetFillColor();
        fillCoroutine = null;
    }

    private void StopFillAnimation()
    {
        if (fillCoroutine == null)
        {
            return;
        }

        StopCoroutine(fillCoroutine);
        fillCoroutine = null;
    }

    private void SpawnSparkBurst(float targetProgress)
    {
        if (sparkCount <= 0 || sparkLifetime <= 0f)
        {
            return;
        }

        if (expBarSlider == null)
        {
            SetupBar();
        }

        if (expBarSlider == null)
        {
            return;
        }

        RectTransform barRect = expBarSlider.GetComponent<RectTransform>();
        if (barRect == null)
        {
            return;
        }

        Vector2 spawnPosition = GetFillEdgePosition(barRect, targetProgress);

        for (int i = 0; i < sparkCount; i++)
        {
            CreateSpark(barRect, spawnPosition);
        }
    }

    private Vector2 GetFillEdgePosition(RectTransform barRect, float progress)
    {
        Rect rect = barRect.rect;
        float x = Mathf.Lerp(rect.xMin, rect.xMax, Mathf.Clamp01(progress));
        return new Vector2(x, rect.center.y);
    }

    private void CreateSpark(RectTransform parent, Vector2 startPosition)
    {
        GameObject sparkObject = new("ExpSpark");
        RectTransform sparkRect = sparkObject.AddComponent<RectTransform>();
        sparkRect.SetParent(parent, false);
        sparkRect.anchorMin = new Vector2(0.5f, 0.5f);
        sparkRect.anchorMax = new Vector2(0.5f, 0.5f);
        sparkRect.pivot = new Vector2(0.5f, 0.5f);

        float startSize = Random.Range(sparkSize * 0.65f, sparkSize * 1.15f);
        sparkRect.sizeDelta = Vector2.one * startSize;
        sparkRect.anchoredPosition = startPosition;

        Image sparkImage = sparkObject.AddComponent<Image>();
        sparkImage.sprite = GetRuntimeSparkSprite();
        sparkImage.raycastTarget = false;
        sparkImage.color = sparkColor;

        Vector2 endPosition = startPosition + new Vector2(
            Random.Range(-sparkTravel, sparkTravel),
            Random.Range(sparkTravel * 0.35f, sparkTravel));

        StartCoroutine(SparkRoutine(sparkRect, sparkImage, startPosition, endPosition, startSize));
    }

    private IEnumerator SparkRoutine(
        RectTransform sparkRect,
        Image sparkImage,
        Vector2 startPosition,
        Vector2 endPosition,
        float startSize)
    {
        float elapsed = 0f;

        while (elapsed < sparkLifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / sparkLifetime);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            sparkRect.anchoredPosition = Vector2.Lerp(startPosition, endPosition, easedT);
            sparkRect.sizeDelta = Vector2.one * Mathf.Lerp(startSize, startSize * 0.2f, t);

            Color color = sparkColor;
            color.a = Mathf.Lerp(sparkColor.a, 0f, t);
            sparkImage.color = color;

            yield return null;
        }

        if (sparkRect != null)
        {
            Destroy(sparkRect.gameObject);
        }
    }

    private static Sprite GetRuntimeSparkSprite()
    {
        if (runtimeSparkSprite != null)
        {
            return runtimeSparkSprite;
        }

        runtimeSparkSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);

        runtimeSparkSprite.name = "RuntimeExpSpark";
        return runtimeSparkSprite;
    }

    private void SetFillColor()
    {
        if (expBarSlider == null || expBarSlider.fillRect == null)
        {
            return;
        }

        Image fillImage = expBarSlider.fillRect.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.color = fillColor;
        }
    }

    private void ResolveRewardReferences()
    {
        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }

        if (tileCircleSpawner == null)
        {
            tileCircleSpawner = FindFirstObjectByType<TileCircleSpawner>();
        }
    }

    private void TryTriggerFullBarReward()
    {
        if (hasTriggeredFullBarReward || currentParryExp < ParriesForFullBar)
        {
            return;
        }

        hasTriggeredFullBarReward = true;
        ResolveRewardReferences();

        if (stopCirclesWhenFull && tileCircleSpawner != null)
        {
            tileCircleSpawner.StopSpawningAndClearActiveCircle();
        }

        if (!spawnEnemyWhenFull)
        {
            return;
        }

        if (enemySpawner == null)
        {
            Debug.LogWarning("EXP bar filled, but no EnemySpawner was found to spawn an enemy.", this);
            return;
        }

        enemySpawner.SpawnEnemy();
        LogEvent("EXP bar full. Enemy spawned and circle spawning stopped.");
    }

    private void LogEvent(string message)
    {
        if (!logEvents)
        {
            return;
        }

        Debug.Log(message, this);
    }
}
