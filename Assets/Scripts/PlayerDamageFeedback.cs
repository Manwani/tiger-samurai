using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerDamageFeedback : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Color flashColor = new(1f, 0.25f, 0.18f, 1f);
    [SerializeField, Min(0.01f)] private float flashDuration = 0.08f;
    [SerializeField, Min(1)] private int flashCount = 2;
    [SerializeField, Min(0f)] private float timeBetweenFlashes = 0.04f;

    private Coroutine flashCoroutine;
    private Color restoreColor;

    private void Awake()
    {
        ResolveTargetRenderer();
    }

    private void OnValidate()
    {
        flashDuration = Mathf.Max(0.01f, flashDuration);
        flashCount = Mathf.Max(1, flashCount);
        timeBetweenFlashes = Mathf.Max(0f, timeBetweenFlashes);
    }

    public void PlayDamageFlash()
    {
        if (!ResolveTargetRenderer())
        {
            return;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            targetRenderer.color = restoreColor;
        }
        else
        {
            restoreColor = targetRenderer.color;
        }

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            targetRenderer.color = flashColor;
            yield return new WaitForSecondsRealtime(flashDuration);

            targetRenderer.color = restoreColor;

            if (timeBetweenFlashes > 0f && i < flashCount - 1)
            {
                yield return new WaitForSecondsRealtime(timeBetweenFlashes);
            }
        }

        targetRenderer.color = restoreColor;
        flashCoroutine = null;
    }

    private bool ResolveTargetRenderer()
    {
        if (targetRenderer != null)
        {
            return true;
        }

        targetRenderer = GetComponent<SpriteRenderer>();
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        return targetRenderer != null;
    }
}
