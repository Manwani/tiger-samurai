using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySequencePopup : MonoBehaviour
{
    [SerializeField] private float tokenSpacing = 0.48f;
    [SerializeField] private Vector2 tokenDisplaySize = new(0.42f, 0.42f);
    [SerializeField] private Vector2 backgroundPadding = new(0.24f, 0.18f);
    [SerializeField] private float backgroundHeight = 0.62f;
    [SerializeField] private float currentTokenPulseScale = 1.12f;
    [SerializeField] private float currentTokenPulseSpeed = 6f;
    [SerializeField] private float feedbackFlashDuration = 0.18f;
    [SerializeField] private Color backgroundColor = new(0.08f, 0.08f, 0.12f, 0.82f);
    [SerializeField] private Color upcomingTokenColor = new(1f, 1f, 1f, 0.45f);
    [SerializeField] private Color currentTokenColor = Color.white;
    [SerializeField] private Color completedTokenColor = new(0.55f, 1f, 0.7f, 1f);
    [SerializeField] private Color failureFlashColor = new(1f, 0.38f, 0.38f, 1f);
    [SerializeField] private Color successFlashColor = new(0.5f, 1f, 0.68f, 1f);

    private static Sprite runtimeSquareSprite;

    private readonly List<SpriteRenderer> tokenRenderers = new();
    private readonly List<Vector3> tokenBaseScales = new();

    private Transform followTarget;
    private Vector3 worldOffset;
    private SpriteRenderer backgroundRenderer;
    private Sprite[] currentSprites = System.Array.Empty<Sprite>();
    private int currentIndex;
    private int popupSortingOrder;
    private float flashTimer;
    private Color flashColor;

    public void Initialize(Transform target, Vector3 offset, int sortingOrder)
    {
        followTarget = target;
        worldOffset = offset;
        popupSortingOrder = sortingOrder;

        EnsureBackground();
        UpdatePosition();
    }

    public void SetSequence(Sprite[] sprites, int activeIndex)
    {
        if (sprites == null || sprites.Length == 0)
        {
            return;
        }

        currentSprites = (Sprite[])sprites.Clone();
        currentIndex = Mathf.Clamp(activeIndex, 0, currentSprites.Length);

        EnsureTokenCount(currentSprites.Length);
        LayoutTokens();
        RefreshVisuals();
    }

    public void FlashFailure()
    {
        flashColor = failureFlashColor;
        flashTimer = feedbackFlashDuration;
        RefreshVisuals();
    }

    public void FlashSuccess()
    {
        flashColor = successFlashColor;
        flashTimer = feedbackFlashDuration;
        RefreshVisuals();
    }

    private void Update()
    {
        UpdatePosition();

        if (flashTimer > 0f)
        {
            flashTimer = Mathf.Max(0f, flashTimer - Time.deltaTime);
        }

        RefreshVisuals();
    }

    private void UpdatePosition()
    {
        if (followTarget == null)
        {
            return;
        }

        transform.position = followTarget.position + worldOffset;
    }

    private void EnsureBackground()
    {
        if (backgroundRenderer != null)
        {
            return;
        }

        GameObject backgroundObject = new("Background");
        Transform backgroundTransform = backgroundObject.transform;
        backgroundTransform.SetParent(transform, false);

        backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = GetRuntimeSquareSprite();
        backgroundRenderer.sortingOrder = popupSortingOrder - 1;
    }

    private void EnsureTokenCount(int requiredCount)
    {
        while (tokenRenderers.Count < requiredCount)
        {
            CreateTokenObject(tokenRenderers.Count);
        }
    }

    private void CreateTokenObject(int tokenIndex)
    {
        GameObject tokenObject = new($"Token{tokenIndex + 1}");
        Transform tokenTransform = tokenObject.transform;
        tokenTransform.SetParent(transform, false);

        SpriteRenderer renderer = tokenObject.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = popupSortingOrder;

        tokenRenderers.Add(renderer);
        tokenBaseScales.Add(Vector3.one);
    }

    private void LayoutTokens()
    {
        float totalWidth = (currentSprites.Length - 1) * tokenSpacing;
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < tokenRenderers.Count; i++)
        {
            SpriteRenderer renderer = tokenRenderers[i];
            bool isActiveToken = i < currentSprites.Length;

            renderer.gameObject.SetActive(isActiveToken);
            if (!isActiveToken)
            {
                continue;
            }

            renderer.sprite = currentSprites[i];
            renderer.transform.localPosition = new Vector3(startX + (i * tokenSpacing), 0f, 0f);
            renderer.transform.localScale = GetBaseScale(currentSprites[i]);
            tokenBaseScales[i] = renderer.transform.localScale;
        }

        if (backgroundRenderer != null)
        {
            float backgroundWidth = Mathf.Max(0.72f, totalWidth + tokenDisplaySize.x + backgroundPadding.x * 2f);
            backgroundRenderer.transform.localScale = new Vector3(backgroundWidth, backgroundHeight, 1f);
            backgroundRenderer.color = backgroundColor;
        }
    }

    private void RefreshVisuals()
    {
        if (tokenRenderers.Count == 0 || currentSprites.Length == 0)
        {
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.time * currentTokenPulseSpeed) * (currentTokenPulseScale - 1f);
        float flashWeight = feedbackFlashDuration <= 0f
            ? 0f
            : Mathf.Clamp01(flashTimer / feedbackFlashDuration);

        if (backgroundRenderer != null)
        {
            backgroundRenderer.color = Color.Lerp(backgroundColor, flashColor, flashWeight * 0.55f);
        }

        for (int i = 0; i < currentSprites.Length; i++)
        {
            SpriteRenderer renderer = tokenRenderers[i];

            Color baseColor = GetBaseTokenColor(i);
            Color displayColor = Color.Lerp(baseColor, flashColor, flashWeight);

            renderer.color = displayColor;
            renderer.sortingOrder = popupSortingOrder;

            bool isCurrentToken = i == currentIndex && currentIndex < currentSprites.Length;
            float scale = isCurrentToken ? pulse : 1f;
            renderer.transform.localScale = tokenBaseScales[i] * scale;
        }
    }

    private Color GetBaseTokenColor(int tokenIndex)
    {
        if (tokenIndex < currentIndex)
        {
            return completedTokenColor;
        }

        if (tokenIndex == currentIndex && currentIndex < currentSprites.Length)
        {
            return currentTokenColor;
        }

        return upcomingTokenColor;
    }

    private Vector3 GetBaseScale(Sprite sprite)
    {
        if (sprite == null)
        {
            return Vector3.one;
        }

        Vector2 spriteSize = sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return Vector3.one;
        }

        float widthScale = tokenDisplaySize.x / spriteSize.x;
        float heightScale = tokenDisplaySize.y / spriteSize.y;
        float scale = Mathf.Min(widthScale, heightScale);
        return Vector3.one * scale;
    }

    private static Sprite GetRuntimeSquareSprite()
    {
        if (runtimeSquareSprite != null)
        {
            return runtimeSquareSprite;
        }

        runtimeSquareSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);

        runtimeSquareSprite.name = "RuntimePopupSquare";
        return runtimeSquareSprite;
    }
}
