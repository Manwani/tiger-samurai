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
    [SerializeField] private Vector3 parryPromptOffset = new(0f, 0.95f, 0f);
    [SerializeField] private Color parryPromptColor = Color.white;
    [SerializeField, Min(0.01f)] private float parryPromptCharacterSize = 0.18f;
    [SerializeField] private int parryPromptFontSize = 64;
    [SerializeField, Min(0.01f)] private float parryPitchMin = 0.9f;
    [SerializeField, Min(0.01f)] private float parryPitchMax = 1.1f;
    [SerializeField] private bool logParryAttempts = true;
    [SerializeField] private int parryPoints;

    private readonly List<ParryCircleEncounter> activeCircles = new();
    private TextMesh parryPromptText;

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
        EnsureParryPrompt();
        NormalizePitchRange();
    }

    private void Update()
    {
        UpdateParryPrompt();

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
        {
            return;
        }

        TryParryCurrentTile();
    }

    public void RegisterCircle(ParryCircleEncounter circle)
    {
        if (circle == null || activeCircles.Contains(circle))
        {
            return;
        }

        activeCircles.Add(circle);
    }

    public void UnregisterCircle(ParryCircleEncounter circle)
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
        NormalizePitchRange();
    }

    private void OnDisable()
    {
        SetParryPromptVisible(false);
    }

    private void TryParryCurrentTile()
    {
        Transform playerTile = playerController.CurrentTileTransform;
        if (playerTile == null)
        {
            LogParryEvent("Parry miss. The player is not on a valid tile.");
            return;
        }

        ParryCircleEncounter bestCircle = FindBestCircleOnTile(playerTile);
        if (bestCircle == null)
        {
            LogParryEvent("Parry miss. There is no active circle on the player's tile.");
            return;
        }

        if (!bestCircle.CanAttemptCurrentStage)
        {
            return;
        }

        if (!bestCircle.IsInsideCurrentParryWindow())
        {
            if (bestCircle.HandleMissedParryAttempt())
            {
                LogParryEvent($"Parry miss. Circle size {bestCircle.NormalizedSize:F2} is outside the parry window, so it sped up.");
            }

            return;
        }

        Vector3 parryEffectPosition = bestCircle.transform.position;
        bool completedEncounter;

        if (!bestCircle.TryResolveParry(out completedEncounter))
        {
            LogParryEvent("Parry miss. The encounter could not accept that input.");
            return;
        }

        parryPoints++;
        PlayParrySuccessSound();
        SpawnParrySuccessEffect(parryEffectPosition);
        PlayParrySuccessScreenFeedback();
        LogParryEvent(
            completedEncounter
                ? $"Parry! {bestCircle.DifficultyLabel} circle completed. Points: {parryPoints}"
                : $"Parry! {bestCircle.DifficultyLabel} circle advanced. Points: {parryPoints}");
    }

    public bool HasMovementLockingEncounter()
    {
        for (int i = activeCircles.Count - 1; i >= 0; i--)
        {
            ParryCircleEncounter circle = activeCircles[i];
            if (circle == null)
            {
                activeCircles.RemoveAt(i);
                continue;
            }

            if (circle.IsLockingPlayer)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasActiveEncounterOnTile(Transform tileTransform)
    {
        for (int i = activeCircles.Count - 1; i >= 0; i--)
        {
            ParryCircleEncounter circle = activeCircles[i];
            if (circle == null)
            {
                activeCircles.RemoveAt(i);
                continue;
            }

            if (circle.IsOnTile(tileTransform))
            {
                return true;
            }
        }

        return false;
    }

    private ParryCircleEncounter FindBestCircleOnTile(Transform tileTransform)
    {
        ParryCircleEncounter engagedCircle = null;
        ParryCircleEncounter bestCircle = null;
        float bestDistanceToWindowCenter = float.MaxValue;

        for (int i = activeCircles.Count - 1; i >= 0; i--)
        {
            ParryCircleEncounter circle = activeCircles[i];
            if (circle == null)
            {
                activeCircles.RemoveAt(i);
                continue;
            }

            if (!circle.IsOnTile(tileTransform))
            {
                continue;
            }

            if (circle.IsEngaged)
            {
                engagedCircle = circle;
                break;
            }

            float distanceToWindowCenter = circle.GetDistanceToWindowCenter();
            if (distanceToWindowCenter < bestDistanceToWindowCenter)
            {
                bestDistanceToWindowCenter = distanceToWindowCenter;
                bestCircle = circle;
            }
        }

        return engagedCircle != null ? engagedCircle : bestCircle;
    }

    private void EnsureParryPrompt()
    {
        if (parryPromptText != null || playerController == null)
        {
            return;
        }

        Transform playerTransform = playerController.transform;
        Transform existingPrompt = playerTransform.Find("ParryPrompt");
        if (existingPrompt != null)
        {
            parryPromptText = existingPrompt.GetComponent<TextMesh>();
        }

        if (parryPromptText == null)
        {
            GameObject promptObject = new("ParryPrompt");
            promptObject.transform.SetParent(playerTransform, false);
            parryPromptText = promptObject.AddComponent<TextMesh>();
        }

        Transform promptTransform = parryPromptText.transform;
        promptTransform.localPosition = parryPromptOffset;
        promptTransform.localRotation = Quaternion.identity;

        parryPromptText.text = "P";
        parryPromptText.anchor = TextAnchor.MiddleCenter;
        parryPromptText.alignment = TextAlignment.Center;
        parryPromptText.characterSize = parryPromptCharacterSize;
        parryPromptText.fontSize = Mathf.Max(1, parryPromptFontSize);
        parryPromptText.color = parryPromptColor;

        MeshRenderer promptRenderer = parryPromptText.GetComponent<MeshRenderer>();
        if (promptRenderer != null)
        {
            promptRenderer.sortingOrder = 100;
        }

        SetParryPromptVisible(false);
    }

    private void UpdateParryPrompt()
    {
        EnsureParryPrompt();
        if (parryPromptText == null || playerController == null)
        {
            return;
        }

        parryPromptText.transform.localPosition = parryPromptOffset;
        parryPromptText.color = parryPromptColor;
        parryPromptText.characterSize = parryPromptCharacterSize;
        parryPromptText.fontSize = Mathf.Max(1, parryPromptFontSize);

        Transform playerTile = playerController.CurrentTileTransform;
        if (playerTile == null)
        {
            SetParryPromptVisible(false);
            return;
        }

        ParryCircleEncounter bestCircle = FindBestCircleOnTile(playerTile);
        bool shouldShowPrompt = bestCircle != null &&
                                bestCircle.CanAttemptCurrentStage &&
                                bestCircle.IsEngaged &&
                                bestCircle.IsInsideCurrentParryWindow();

        SetParryPromptVisible(shouldShowPrompt);
    }

    private void SetParryPromptVisible(bool visible)
    {
        if (parryPromptText == null)
        {
            return;
        }

        if (parryPromptText.gameObject.activeSelf != visible)
        {
            parryPromptText.gameObject.SetActive(visible);
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
