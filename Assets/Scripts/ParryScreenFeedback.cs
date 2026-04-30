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
    [SerializeField, Min(0f)] private float zoomAmount = 0.25f;
    [SerializeField, Min(0f)] private float zoomInDuration = 0.045f;
    [SerializeField, Min(0f)] private float zoomOutDuration = 0.12f;
    [SerializeField, Min(1)] private int maxZoomStreak = 5;
    [SerializeField, Range(0f, 1f)] private float zoomFocusStrength = 1f;

    private Image flashImage;
    private Camera targetCamera;
    private Vector3 baseLocalPosition;
    private Vector3 shakeOffset;
    private Vector3 zoomFocusOffset;
    private float baseOrthographicSize;
    private float baseFieldOfView;
    private int zoomStreakCount;
    private Coroutine shakeCoroutine;
    private Coroutine flashCoroutine;
    private Coroutine zoomCoroutine;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        if (targetCamera != null)
        {
            baseOrthographicSize = targetCamera.orthographicSize;
            baseFieldOfView = targetCamera.fieldOfView;
        }

        baseLocalPosition = transform.localPosition;
        EnsureFlashOverlay();
        SetFlashAlpha(0f);
    }

    private void OnDisable()
    {
        StopActiveFeedback();
        transform.localPosition = baseLocalPosition;
        ResetZoom();
        SetFlashAlpha(0f);
    }

    public void PlayParryFeedback()
    {
        PlayParryFeedback(transform.position);
    }

    public void PlayParryFeedback(Vector3 focusWorldPosition)
    {
        EnsureFlashOverlay();
        zoomStreakCount = Mathf.Min(Mathf.Max(1, maxZoomStreak), zoomStreakCount + 1);
        StopActiveFeedback(false);

        SetFlashAlpha(0f);

        shakeCoroutine = StartCoroutine(ShakeRoutine());
        flashCoroutine = StartCoroutine(FlashRoutine());
        zoomCoroutine = StartCoroutine(ZoomRoutine(focusWorldPosition));
    }

    public void ResetParryZoomStreak()
    {
        zoomStreakCount = 0;
    }

    private IEnumerator ShakeRoutine()
    {
        float duration = Mathf.Max(0.0001f, shakeDuration);

        if (shakeStrength <= 0f)
        {
            shakeCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentStrength = Mathf.Lerp(shakeStrength, 0f, t);
            Vector2 randomOffset = Random.insideUnitCircle * currentStrength;

            shakeOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);
            ApplyCameraPosition();
            yield return null;
        }

        shakeOffset = Vector3.zero;
        ApplyCameraPosition();
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

    private IEnumerator ZoomRoutine(Vector3 focusWorldPosition)
    {
        if (targetCamera == null || zoomAmount <= 0f)
        {
            zoomCoroutine = null;
            yield break;
        }

        float startZoom = GetCurrentZoomValue();
        Vector3 startFocusOffset = zoomFocusOffset;
        int clampedStreak = Mathf.Clamp(zoomStreakCount, 1, Mathf.Max(1, maxZoomStreak));
        float streakPercent = clampedStreak / (float)Mathf.Max(1, maxZoomStreak);
        float punchZoom = Mathf.Max(0.01f, GetBaseZoomValue() - (zoomAmount * clampedStreak));
        Vector3 punchFocusOffset = GetFocusOffset(focusWorldPosition) * streakPercent * zoomFocusStrength;

        yield return AnimateZoomAndFocus(startZoom, punchZoom, startFocusOffset, punchFocusOffset, zoomInDuration);
        yield return AnimateZoomAndFocus(punchZoom, GetBaseZoomValue(), punchFocusOffset, Vector3.zero, zoomOutDuration);

        ResetZoom();
        zoomFocusOffset = Vector3.zero;
        ApplyCameraPosition();
        zoomCoroutine = null;
    }

    private IEnumerator AnimateZoomAndFocus(
        float startZoom,
        float endZoom,
        Vector3 startFocusOffset,
        Vector3 endFocusOffset,
        float duration)
    {
        if (duration <= 0f)
        {
            SetZoomValue(endZoom);
            zoomFocusOffset = endFocusOffset;
            ApplyCameraPosition();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            SetZoomValue(Mathf.Lerp(startZoom, endZoom, easedT));
            zoomFocusOffset = Vector3.Lerp(startFocusOffset, endFocusOffset, easedT);
            ApplyCameraPosition();
            yield return null;
        }

        SetZoomValue(endZoom);
        zoomFocusOffset = endFocusOffset;
        ApplyCameraPosition();
    }

    private float GetCurrentZoomValue()
    {
        if (targetCamera == null)
        {
            return 0f;
        }

        return targetCamera.orthographic
            ? targetCamera.orthographicSize
            : targetCamera.fieldOfView;
    }

    private float GetBaseZoomValue()
    {
        if (targetCamera == null)
        {
            return 0f;
        }

        return targetCamera.orthographic
            ? baseOrthographicSize
            : baseFieldOfView;
    }

    private void SetZoomValue(float value)
    {
        if (targetCamera == null)
        {
            return;
        }

        if (targetCamera.orthographic)
        {
            targetCamera.orthographicSize = Mathf.Max(0.01f, value);
            return;
        }

        targetCamera.fieldOfView = Mathf.Clamp(value, 1f, 179f);
    }

    private void ResetZoom()
    {
        if (targetCamera == null)
        {
            return;
        }

        if (targetCamera.orthographic)
        {
            targetCamera.orthographicSize = baseOrthographicSize;
            return;
        }

        targetCamera.fieldOfView = baseFieldOfView;
    }

    private Vector3 GetFocusOffset(Vector3 focusWorldPosition)
    {
        Vector3 focusLocalPosition = transform.parent != null
            ? transform.parent.InverseTransformPoint(focusWorldPosition)
            : focusWorldPosition;

        Vector3 focusOffset = focusLocalPosition - baseLocalPosition;
        focusOffset.z = 0f;
        return focusOffset;
    }

    private void ApplyCameraPosition()
    {
        transform.localPosition = baseLocalPosition + zoomFocusOffset + shakeOffset;
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

    private void StopActiveFeedback(bool resetZoomAndPosition = true)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        shakeOffset = Vector3.zero;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
            zoomCoroutine = null;
        }

        if (resetZoomAndPosition)
        {
            zoomFocusOffset = Vector3.zero;
            ResetZoom();
        }

        ApplyCameraPosition();
    }
}
