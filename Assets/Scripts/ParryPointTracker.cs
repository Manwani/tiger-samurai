using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class ParryPointTracker : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip parrySuccessClip;
    [SerializeField] private GameObject parrySuccessEffectPrefab;
    [SerializeField] private ParryScreenFeedback parryScreenFeedback;
    [SerializeField, Range(0f, 1f)] private float parryWindowMinNormalizedSize = 0.35f;
    [SerializeField, Range(0f, 1f)] private float parryWindowMaxNormalizedSize = 0.55f;
    [SerializeField, Min(0.01f)] private float parryPitchMin = 0.9f;
    [SerializeField, Min(0.01f)] private float parryPitchMax = 1.1f;
    [SerializeField] private bool logParryAttempts = true;
    [SerializeField] private int parryPoints;

    private readonly List<ShrinkingCircle> activeCircles = new();

    public int ParryPoints => parryPoints;

    private void Start()
    {
        if (playerController == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();
            }
        }

        if (playerController == null)
        {
            Debug.LogError("ParryPointTracker needs a PlayerController reference.", this);
            enabled = false;
            return;
        }

        EnsureAudioSource();
        PreloadParrySound();
        ResolveParryScreenFeedback();
        NormalizeParryWindow();
        NormalizePitchRange();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
        {
            return;
        }

        TryParryCurrentTile();
    }

    public void RegisterCircle(ShrinkingCircle circle)
    {
        if (circle == null || activeCircles.Contains(circle))
        {
            return;
        }

        activeCircles.Add(circle);
    }

    public void UnregisterCircle(ShrinkingCircle circle)
    {
        if (circle == null)
        {
            return;
        }

        activeCircles.Remove(circle);
    }

    private void OnValidate()
    {
        EnsureAudioSource();
        NormalizeParryWindow();
        NormalizePitchRange();
    }

    private void TryParryCurrentTile()
    {
        Transform playerTile = playerController.CurrentTileTransform;
        if (playerTile == null)
        {
            LogParryEvent("Parry miss. The player is not on a valid tile.");
            return;
        }

        ShrinkingCircle bestCircle = FindBestCircleOnTile(playerTile);
        if (bestCircle == null)
        {
            LogParryEvent("Parry miss. There is no active circle on the player's tile.");
            return;
        }

        if (!IsInsideParryWindow(bestCircle.NormalizedSize))
        {
            LogParryEvent($"Parry miss. Circle size {bestCircle.NormalizedSize:F2} is outside the parry window.");
            return;
        }

        Vector3 parryEffectPosition = bestCircle.transform.position;

        parryPoints++;
        bestCircle.Consume();
        PlayParrySuccessSound();
        SpawnParrySuccessEffect(parryEffectPosition);
        PlayParrySuccessScreenFeedback();
        LogParryEvent($"Parry! Points: {parryPoints}");
    }

    private ShrinkingCircle FindBestCircleOnTile(Transform tileTransform)
    {
        ShrinkingCircle bestCircle = null;
        float bestDistanceToWindowCenter = float.MaxValue;
        float targetWindowCenter = (parryWindowMinNormalizedSize + parryWindowMaxNormalizedSize) * 0.5f;

        for (int i = activeCircles.Count - 1; i >= 0; i--)
        {
            ShrinkingCircle circle = activeCircles[i];
            if (circle == null)
            {
                activeCircles.RemoveAt(i);
                continue;
            }

            if (circle.TargetTile != tileTransform)
            {
                continue;
            }

            float distanceToWindowCenter = Mathf.Abs(circle.NormalizedSize - targetWindowCenter);
            if (distanceToWindowCenter < bestDistanceToWindowCenter)
            {
                bestDistanceToWindowCenter = distanceToWindowCenter;
                bestCircle = circle;
            }
        }

        return bestCircle;
    }

    private bool IsInsideParryWindow(float normalizedSize)
    {
        return normalizedSize >= parryWindowMinNormalizedSize &&
               normalizedSize <= parryWindowMaxNormalizedSize;
    }

    private void NormalizeParryWindow()
    {
        if (parryWindowMaxNormalizedSize < parryWindowMinNormalizedSize)
        {
            parryWindowMaxNormalizedSize = parryWindowMinNormalizedSize;
        }
    }

    private void NormalizePitchRange()
    {
        if (parryPitchMax < parryPitchMin)
        {
            parryPitchMax = parryPitchMin;
        }
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.pitch = 1f;
    }

    private void PlayParrySuccessSound()
    {
        if (audioSource == null || parrySuccessClip == null)
        {
            return;
        }

        NormalizePitchRange();
        audioSource.pitch = Random.Range(parryPitchMin, parryPitchMax);
        audioSource.PlayOneShot(parrySuccessClip);
    }

    private void SpawnParrySuccessEffect(Vector3 position)
    {
        if (parrySuccessEffectPrefab == null)
        {
            return;
        }

        Instantiate(parrySuccessEffectPrefab, position, parrySuccessEffectPrefab.transform.rotation);
    }

    private void PlayParrySuccessScreenFeedback()
    {
        ResolveParryScreenFeedback();
        parryScreenFeedback?.PlayParryFeedback();
    }

    private void PreloadParrySound()
    {
        if (parrySuccessClip == null)
        {
            return;
        }

        parrySuccessClip.LoadAudioData();
    }

    private void ResolveParryScreenFeedback()
    {
        if (parryScreenFeedback != null)
        {
            return;
        }

        parryScreenFeedback = FindFirstObjectByType<ParryScreenFeedback>();
        if (parryScreenFeedback != null)
        {
            return;
        }

        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            targetCamera = FindFirstObjectByType<Camera>();
        }

        if (targetCamera == null)
        {
            return;
        }

        parryScreenFeedback = targetCamera.GetComponent<ParryScreenFeedback>();
        if (parryScreenFeedback == null)
        {
            parryScreenFeedback = targetCamera.gameObject.AddComponent<ParryScreenFeedback>();
        }
    }

    private void LogParryEvent(string message)
    {
        if (!logParryAttempts)
        {
            return;
        }

        Debug.Log(message, this);
    }
}
