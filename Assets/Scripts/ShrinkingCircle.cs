using UnityEngine;

public class ShrinkingCircle : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private float startRadius;
    private float duration;
    private float elapsed;
    private Color baseColor;
    private ParryPointTracker parryPointTracker;
    private bool hasBeenConsumed;

    public Transform TargetTile { get; private set; }
    public float NormalizedSize => startRadius <= 0f ? 0f : transform.localScale.x / startRadius;

    public void Initialize(
        LineRenderer targetLineRenderer,
        float radius,
        float lifetime,
        Color color,
        Transform targetTile,
        ParryPointTracker pointTracker)
    {
        lineRenderer = targetLineRenderer;
        startRadius = Mathf.Max(0.001f, radius);
        duration = Mathf.Max(0.01f, lifetime);
        baseColor = color;
        TargetTile = targetTile;
        parryPointTracker = pointTracker;

        transform.localScale = Vector3.one * startRadius;
        ApplyColor(baseColor);

        parryPointTracker?.RegisterCircle(this);
    }

    private void Update()
    {
        if (hasBeenConsumed)
        {
            return;
        }

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float scale = Mathf.Lerp(startRadius, 0f, t);

        transform.localScale = Vector3.one * scale;

        Color currentColor = baseColor;
        currentColor.a = Mathf.Lerp(baseColor.a, 0f, t);
        ApplyColor(currentColor);

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        parryPointTracker?.UnregisterCircle(this);
    }

    public void Consume()
    {
        if (hasBeenConsumed)
        {
            return;
        }

        hasBeenConsumed = true;
        Destroy(gameObject);
    }

    private void ApplyColor(Color color)
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}
