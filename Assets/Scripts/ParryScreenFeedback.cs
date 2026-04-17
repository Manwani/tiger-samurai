using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ParryScreenFeedback : MonoBehaviour
{
    [SerializeField, Min(0f)] private float shakeDuration = 0.1f;
    [SerializeField, Min(0f)] private float shakeStrength = 0.08f;
    [SerializeField, Min(0f)] private float flashDuration = 0.12f;
    [SerializeField, Range(0f, 1f)] private float flashAlpha = 0.18f;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private int flashSortingOrder = 500;

    private Image flashImage;
    private Vector3 baseLocalPosition;
    private Coroutine shakeCoroutine;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        EnsureFlashOverlay();
        SetFlashAlpha(0f);
    }

    private void OnDisable()
    {
        StopActiveFeedback();
        transform.localPosition = baseLocalPosition;
        SetFlashAlpha(0f);
    }

    public void PlayParryFeedback()
    {
        EnsureFlashOverlay();
        bool wasShaking = shakeCoroutine != null;
        StopActiveFeedback();

        if (wasShaking)
        {
            transform.localPosition = baseLocalPosition;
        }

        baseLocalPosition = transform.localPosition;
        SetFlashAlpha(0f);

        shakeCoroutine = StartCoroutine(ShakeRoutine());
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float duration = Mathf.Max(0.0001f, shakeDuration);

        if (shakeStrength <= 0f)
        {
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentStrength = Mathf.Lerp(shakeStrength, 0f, t);
            Vector2 offset = Random.insideUnitCircle * currentStrength;

            transform.localPosition = baseLocalPosition + new Vector3(offset.x, offset.y, 0f);
            yield return null;
        }

        transform.localPosition = baseLocalPosition;
        shakeCoroutine = null;
    }

    private IEnumerator FlashRoutine()
    {
        float duration = Mathf.Max(0.0001f, flashDuration);
        float elapsed = 0f;

        SetFlashAlpha(flashAlpha);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetFlashAlpha(Mathf.Lerp(flashAlpha, 0f, t));
            yield return null;
        }

        SetFlashAlpha(0f);
        flashCoroutine = null;
    }

    private void EnsureFlashOverlay()
    {
        if (flashImage != null)
        {
            return;
        }

        Transform existingCanvas = transform.Find("ParryFlashCanvas");
        if (existingCanvas != null)
        {
            flashImage = existingCanvas.GetComponentInChildren<Image>(true);
            if (flashImage != null)
            {
                return;
            }
        }

        // Build a tiny full-screen overlay in code so the flash works without extra scene setup.
        GameObject canvasObject = new("ParryFlashCanvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = flashSortingOrder;

        GameObject imageObject = new("ParryFlashImage");
        imageObject.transform.SetParent(canvasObject.transform, false);

        flashImage = imageObject.AddComponent<Image>();
        flashImage.raycastTarget = false;

        RectTransform imageRect = flashImage.rectTransform;
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        SetFlashAlpha(0f);
    }

    private void SetFlashAlpha(float alpha)
    {
        if (flashImage == null)
        {
            return;
        }

        Color color = flashColor;
        color.a = alpha;
        flashImage.color = color;
    }

    private void StopActiveFeedback()
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
    }
}
