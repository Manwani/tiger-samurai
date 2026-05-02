using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ParryCircleEncounter : MonoBehaviour
{
    private enum EncounterPhase
    {
        WaitingForPlayer,
        Shrinking,
        Expanding
    }

    [System.Serializable]
    public class Settings
    {
        private const float DefaultParryWindowDurationSeconds = 0.2f;
        private const float DefaultExpandDurationSeconds = 0.12f;

        [System.Serializable]
        public class StageProfile
        {
            [SerializeField, Min(0.01f)] private float shrinkDuration = 1f;
            [SerializeField, Min(0.01f)] private float parryWindowDurationSeconds = DefaultParryWindowDurationSeconds;
            [SerializeField, Min(0f)] private float expandDuration = DefaultExpandDurationSeconds;

            public float ShrinkDuration => Mathf.Max(0.01f, shrinkDuration);
            public float ParryWindowDurationSeconds => Mathf.Max(0.01f, parryWindowDurationSeconds);
            public float ExpandDuration => Mathf.Max(0f, expandDuration);

            public static StageProfile Create(float shrinkDuration, float parryWindowDurationSeconds, float expandDuration)
            {
                return new StageProfile
                {
                    shrinkDuration = shrinkDuration,
                    parryWindowDurationSeconds = parryWindowDurationSeconds,
                    expandDuration = expandDuration
                };
            }

            public StageProfile CreateCopy()
            {
                return new StageProfile
                {
                    shrinkDuration = shrinkDuration,
                    parryWindowDurationSeconds = parryWindowDurationSeconds,
                    expandDuration = expandDuration
                };
            }

            public void Normalize(float fallbackShrinkDuration, float fallbackWindowDuration)
            {
                shrinkDuration = shrinkDuration <= 0f
                    ? Mathf.Max(0.01f, fallbackShrinkDuration)
                    : Mathf.Max(0.01f, shrinkDuration);
                parryWindowDurationSeconds = parryWindowDurationSeconds <= 0f
                    ? Mathf.Max(0.01f, fallbackWindowDuration)
                    : Mathf.Max(0.01f, parryWindowDurationSeconds);
                expandDuration = Mathf.Max(0f, expandDuration);
            }
        }

        [SerializeField] private string label = "White";
        [SerializeField] private Color circleColor = new(1f, 1f, 1f, 0.9f);
        [SerializeField, Min(1)] private int requiredParries = 1;
        [SerializeField, Min(0.01f)] private float shrinkDuration = 1.25f;
        [SerializeField, Range(0f, 1f)] private float parryWindowMinNormalizedSize = 0.35f;
        [SerializeField, Range(0f, 1f)] private float parryWindowMaxNormalizedSize = 0.55f;
        [SerializeField, Min(0.01f)] private float parryWindowDurationSeconds = DefaultParryWindowDurationSeconds;
        [SerializeField, Range(0.1f, 1f)] private float stageSpeedMultiplier = 1f;
        [SerializeField, Range(0f, 0.15f)] private float windowTightenPerStage = 0f;
        [SerializeField, Min(1f)] private float missedParryShrinkMultiplier = 4f;
        [SerializeField] private bool lockPlayerOnTile;
        [SerializeField] private StageProfile[] stageProfiles;

        public string Label => label;
        public Color CircleColor => circleColor;
        public int RequiredParries => Mathf.Max(1, stageProfiles != null && stageProfiles.Length > 0 ? stageProfiles.Length : requiredParries);
        public float MissedParryShrinkMultiplier => Mathf.Max(1f, missedParryShrinkMultiplier);
        public bool LockPlayerOnTile => lockPlayerOnTile;

        public static Settings CreateWhiteDefaults()
        {
            return new Settings
            {
                parryWindowDurationSeconds = 0.3f,
                stageProfiles = new[]
                {
                    StageProfile.Create(1.56f, 0.3f, 0f)
                }
            };
        }

        public static Settings CreateRedDefaults()
        {
            return new Settings
            {
                label = "Red",
                circleColor = new Color(1f, 0.26f, 0.26f, 0.95f),
                requiredParries = 3,
                shrinkDuration = 0.9f,
                parryWindowMinNormalizedSize = 0.4f,
                parryWindowMaxNormalizedSize = 0.52f,
                stageSpeedMultiplier = 0.82f,
                windowTightenPerStage = 0.03f,
                missedParryShrinkMultiplier = 5f,
                lockPlayerOnTile = true,
                stageProfiles = new[]
                {
                    StageProfile.Create(1.05f, 0.22f, 0f),
                    StageProfile.Create(1.32f, 0.24f, 0.18f),
                    StageProfile.Create(0.74f, 0.18f, 0.1f)
                }
            };
        }

        public static Settings CreateBlueDefaults()
        {
            return new Settings
            {
                label = "Blue",
                circleColor = new Color(0.28f, 0.62f, 1f, 0.95f),
                requiredParries = 5,
                shrinkDuration = 0.82f,
                parryWindowMinNormalizedSize = 0.4f,
                parryWindowMaxNormalizedSize = 0.53f,
                stageSpeedMultiplier = 0.93f,
                windowTightenPerStage = 0.01f,
                missedParryShrinkMultiplier = 6f,
                lockPlayerOnTile = true,
                stageProfiles = new[]
                {
                    StageProfile.Create(1.02f, 0.22f, 0f),
                    StageProfile.Create(0.82f, 0.2f, 0.13f),
                    StageProfile.Create(1.08f, 0.21f, 0.16f),
                    StageProfile.Create(0.76f, 0.18f, 0.11f),
                    StageProfile.Create(0.64f, 0.17f, 0.09f)
                }
            };
        }

        public static Settings CreateBlackDefaults()
        {
            return new Settings
            {
                label = "Black",
                circleColor = new Color(0.1f, 0.1f, 0.12f, 0.98f),
                requiredParries = 7,
                shrinkDuration = 0.72f,
                parryWindowMinNormalizedSize = 0.42f,
                parryWindowMaxNormalizedSize = 0.52f,
                stageSpeedMultiplier = 0.94f,
                windowTightenPerStage = 0.008f,
                missedParryShrinkMultiplier = 7f,
                lockPlayerOnTile = true,
                stageProfiles = new[]
                {
                    StageProfile.Create(0.86f, 0.19f, 0f),
                    StageProfile.Create(0.72f, 0.18f, 0.08f),
                    StageProfile.Create(0.62f, 0.18f, 0.07f),
                    StageProfile.Create(0.76f, 0.17f, 0.07f),
                    StageProfile.Create(0.58f, 0.16f, 0.06f),
                    StageProfile.Create(0.68f, 0.16f, 0.06f),
                    StageProfile.Create(0.52f, 0.15f, 0.05f)
                }
            };
        }

        public Settings CreateCopy()
        {
            return new Settings
            {
                label = label,
                circleColor = circleColor,
                requiredParries = requiredParries,
                shrinkDuration = shrinkDuration,
                parryWindowMinNormalizedSize = parryWindowMinNormalizedSize,
                parryWindowMaxNormalizedSize = parryWindowMaxNormalizedSize,
                parryWindowDurationSeconds = parryWindowDurationSeconds,
                stageSpeedMultiplier = stageSpeedMultiplier,
                windowTightenPerStage = windowTightenPerStage,
                missedParryShrinkMultiplier = missedParryShrinkMultiplier,
                lockPlayerOnTile = lockPlayerOnTile,
                stageProfiles = CloneStageProfiles(stageProfiles)
            };
        }

        public void Normalize()
        {
            requiredParries = Mathf.Max(1, requiredParries);
            shrinkDuration = Mathf.Max(0.01f, shrinkDuration);
            missedParryShrinkMultiplier = Mathf.Max(1f, missedParryShrinkMultiplier);
            parryWindowMinNormalizedSize = Mathf.Clamp01(parryWindowMinNormalizedSize);
            parryWindowMaxNormalizedSize = Mathf.Clamp01(parryWindowMaxNormalizedSize);
            parryWindowDurationSeconds = parryWindowDurationSeconds <= 0f
                ? DefaultParryWindowDurationSeconds
                : Mathf.Max(0.01f, parryWindowDurationSeconds);

            if (parryWindowMaxNormalizedSize < parryWindowMinNormalizedSize)
            {
                parryWindowMaxNormalizedSize = parryWindowMinNormalizedSize;
            }

            EnsureStageProfiles();
            requiredParries = Mathf.Max(1, stageProfiles.Length);
        }

        public float GetStageDuration(int stageIndex)
        {
            return GetStageProfile(stageIndex).ShrinkDuration;
        }

        public float GetStageExpandDuration(int stageIndex)
        {
            return GetStageProfile(stageIndex).ExpandDuration;
        }

        public void GetStageWindow(int stageIndex, float stageDuration, out float min, out float max)
        {
            float tightenAmount = Mathf.Max(0f, windowTightenPerStage) * Mathf.Max(0, stageIndex);
            float centeredMin = Mathf.Clamp01(parryWindowMinNormalizedSize + tightenAmount);
            float centeredMax = Mathf.Clamp01(parryWindowMaxNormalizedSize - tightenAmount);

            if (centeredMax < centeredMin)
            {
                centeredMax = centeredMin;
            }

            float center = (centeredMin + centeredMax) * 0.5f;
            float windowDuration = GetStageProfile(stageIndex).ParryWindowDurationSeconds;
            float normalizedWindowWidth = Mathf.Clamp01(windowDuration / Mathf.Max(0.01f, stageDuration));

            min = Mathf.Clamp01(center - normalizedWindowWidth * 0.5f);
            max = Mathf.Clamp01(center + normalizedWindowWidth * 0.5f);

            if (max < min)
            {
                max = min;
            }
        }

        private void EnsureStageProfiles()
        {
            if (stageProfiles == null || stageProfiles.Length == 0)
            {
                stageProfiles = BuildDefaultStageProfiles();
            }

            if (stageProfiles == null || stageProfiles.Length == 0)
            {
                stageProfiles = BuildLegacyStageProfiles(requiredParries);
            }

            for (int i = 0; i < stageProfiles.Length; i++)
            {
                if (stageProfiles[i] == null)
                {
                    stageProfiles[i] = new StageProfile();
                }

                stageProfiles[i].Normalize(GetLegacyStageDuration(i), parryWindowDurationSeconds);
            }
        }

        private StageProfile GetStageProfile(int stageIndex)
        {
            if (stageProfiles == null || stageProfiles.Length == 0)
            {
                stageProfiles = BuildLegacyStageProfiles(requiredParries);
            }

            int clampedIndex = Mathf.Clamp(stageIndex, 0, stageProfiles.Length - 1);
            return stageProfiles[clampedIndex];
        }

        private StageProfile[] BuildDefaultStageProfiles()
        {
            string normalizedLabel = string.IsNullOrWhiteSpace(label)
                ? "white"
                : label.Trim().ToLowerInvariant();

            if (normalizedLabel == "white" && requiredParries == 1)
            {
                return CloneStageProfiles(CreateWhiteDefaults().stageProfiles);
            }

            if (normalizedLabel == "red" && requiredParries == 3)
            {
                return CloneStageProfiles(CreateRedDefaults().stageProfiles);
            }

            if (normalizedLabel == "blue" && requiredParries == 5)
            {
                return CloneStageProfiles(CreateBlueDefaults().stageProfiles);
            }

            if (normalizedLabel == "black" && requiredParries == 7)
            {
                return CloneStageProfiles(CreateBlackDefaults().stageProfiles);
            }

            return BuildLegacyStageProfiles(requiredParries);
        }

        private StageProfile[] BuildLegacyStageProfiles(int stageCount)
        {
            int count = Mathf.Max(1, stageCount);
            StageProfile[] fallbackProfiles = new StageProfile[count];

            for (int i = 0; i < count; i++)
            {
                fallbackProfiles[i] = StageProfile.Create(
                    GetLegacyStageDuration(i),
                    parryWindowDurationSeconds,
                    i == 0 ? 0f : DefaultExpandDurationSeconds);
            }

            return fallbackProfiles;
        }

        private float GetLegacyStageDuration(int stageIndex)
        {
            float duration = shrinkDuration * Mathf.Pow(Mathf.Max(0.1f, stageSpeedMultiplier), Mathf.Max(0, stageIndex));
            duration *= 1.25f;
            return Mathf.Max(0.01f, duration);
        }

        private static StageProfile[] CloneStageProfiles(StageProfile[] sourceProfiles)
        {
            if (sourceProfiles == null || sourceProfiles.Length == 0)
            {
                return null;
            }

            StageProfile[] clonedProfiles = new StageProfile[sourceProfiles.Length];
            for (int i = 0; i < sourceProfiles.Length; i++)
            {
                clonedProfiles[i] = sourceProfiles[i]?.CreateCopy();
            }

            return clonedProfiles;
        }
    }

    private LineRenderer lineRenderer;
    private PlayerController playerController;
    private ParryPointTracker parryPointTracker;
    private Settings settings;
    private float startRadius;
    private float phaseElapsed;
    private float currentStageDuration;
    private float currentStageWindowMin;
    private float currentStageWindowMax;
    private float currentExpandDuration;
    private float expandStartNormalizedSize;
    private float expandStartAlpha;
    private int currentStageIndex;
    private bool encounterStarted;
    private bool controlsLockedByEncounter;
    private bool hasBeenResolved;
    private bool stageAttemptUsed;
    private EncounterPhase currentPhase = EncounterPhase.WaitingForPlayer;

    public Transform TargetTile { get; private set; }
    public float NormalizedSize => startRadius <= 0f ? 0f : transform.localScale.x / startRadius;
    public bool CanAttemptCurrentStage => !hasBeenResolved && !stageAttemptUsed && currentPhase != EncounterPhase.Expanding;
    public bool IsEngaged => encounterStarted;
    public bool IsLockingPlayer => controlsLockedByEncounter;
    public string DifficultyLabel => settings != null ? settings.Label : string.Empty;

    public event Action<ParryCircleEncounter, bool> Resolved;

    public void Initialize(
        LineRenderer targetLineRenderer,
        float radius,
        Transform targetTile,
        PlayerController controller,
        ParryPointTracker pointTracker,
        Settings sourceSettings)
    {
        lineRenderer = targetLineRenderer;
        startRadius = Mathf.Max(0.001f, radius);
        TargetTile = targetTile;
        playerController = controller;
        parryPointTracker = pointTracker;
        settings = sourceSettings != null ? sourceSettings.CreateCopy() : new Settings();
        settings.Normalize();

        if (lineRenderer == null || TargetTile == null || playerController == null || parryPointTracker == null)
        {
            Debug.LogError("ParryCircleEncounter is missing required references.", this);
            Destroy(gameObject);
            return;
        }

        playerController.LandedOnTile += HandlePlayerLanded;
        parryPointTracker.RegisterCircle(this);

        PrepareStage(0);
        currentPhase = EncounterPhase.WaitingForPlayer;
        ApplyNormalizedSize(1f);
        ApplyColor(settings.CircleColor);
    }

    private void Update()
    {
        if (hasBeenResolved || !encounterStarted)
        {
            return;
        }

        if (currentPhase == EncounterPhase.Expanding)
        {
            UpdateExpansion();
            return;
        }

        if (currentPhase == EncounterPhase.Shrinking)
        {
            UpdateShrink();
        }
    }

    private void OnDestroy()
    {
        if (playerController != null)
        {
            playerController.LandedOnTile -= HandlePlayerLanded;
        }

        if (controlsLockedByEncounter && playerController != null)
        {
            playerController.SetControlsLocked(false);
            controlsLockedByEncounter = false;
        }

        parryPointTracker?.UnregisterCircle(this);
    }

    public bool IsOnTile(Transform tile)
    {
        return TargetTile == tile;
    }

    public float GetDistanceToWindowCenter()
    {
        float windowCenter = (currentStageWindowMin + currentStageWindowMax) * 0.5f;
        return Mathf.Abs(NormalizedSize - windowCenter);
    }

    public bool IsInsideCurrentParryWindow()
    {
        float normalizedSize = NormalizedSize;
        return normalizedSize >= currentStageWindowMin &&
               normalizedSize <= currentStageWindowMax;
    }

    public bool HandleMissedParryAttempt()
    {
        if (hasBeenResolved || stageAttemptUsed || currentPhase == EncounterPhase.Expanding)
        {
            return false;
        }

        StartEncounter();
        stageAttemptUsed = true;
        SpeedUpCurrentStageFailure();
        return true;
    }

    public bool TryResolveParry(out bool completedEncounter)
    {
        completedEncounter = false;

        if (hasBeenResolved || stageAttemptUsed || currentPhase == EncounterPhase.Expanding)
        {
            return false;
        }

        StartEncounter();
        stageAttemptUsed = true;
        if (!IsInsideCurrentParryWindow())
        {
            return false;
        }

        int nextStageIndex = currentStageIndex + 1;
        if (nextStageIndex >= settings.RequiredParries)
        {
            completedEncounter = true;
            CompleteEncounter();
            return true;
        }

        BeginExpansionIntoStage(nextStageIndex);
        return true;
    }

    private void HandlePlayerLanded(Transform landedTile)
    {
        if (hasBeenResolved || landedTile != TargetTile)
        {
            return;
        }

        StartEncounter();
    }

    private void StartEncounter()
    {
        if (encounterStarted)
        {
            return;
        }

        encounterStarted = true;

        if (settings.LockPlayerOnTile && !controlsLockedByEncounter)
        {
            playerController.SetControlsLocked(true);
            controlsLockedByEncounter = true;
        }

        BeginShrinkPhase();
    }

    private void PrepareStage(int stageIndex)
    {
        currentStageIndex = stageIndex;
        phaseElapsed = 0f;
        stageAttemptUsed = false;
        currentStageDuration = settings.GetStageDuration(stageIndex);
        settings.GetStageWindow(stageIndex, currentStageDuration, out currentStageWindowMin, out currentStageWindowMax);
    }

    private void BeginShrinkPhase()
    {
        currentPhase = EncounterPhase.Shrinking;
        phaseElapsed = 0f;
        ApplyNormalizedSize(1f);
        ApplyColor(settings.CircleColor);
    }

    private void BeginExpansionIntoStage(int nextStageIndex)
    {
        float currentAlpha = GetCurrentAlpha();
        float currentNormalizedSize = Mathf.Clamp01(NormalizedSize);

        PrepareStage(nextStageIndex);
        currentPhase = EncounterPhase.Expanding;
        currentExpandDuration = settings.GetStageExpandDuration(nextStageIndex);
        expandStartNormalizedSize = currentNormalizedSize;
        expandStartAlpha = currentAlpha;

        if (currentExpandDuration <= 0.0001f || currentNormalizedSize >= 0.999f)
        {
            BeginShrinkPhase();
        }
    }

    private void UpdateShrink()
    {
        phaseElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(phaseElapsed / currentStageDuration);
        float normalizedSize = Mathf.Lerp(1f, 0f, t);

        ApplyNormalizedSize(normalizedSize);

        Color currentColor = settings.CircleColor;
        currentColor.a = Mathf.Lerp(settings.CircleColor.a, 0f, t);
        ApplyColor(currentColor);

        if (t >= 1f)
        {
            FailEncounter();
        }
    }

    private void UpdateExpansion()
    {
        phaseElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(phaseElapsed / currentExpandDuration);
        float normalizedSize = Mathf.Lerp(expandStartNormalizedSize, 1f, t);

        ApplyNormalizedSize(normalizedSize);

        Color currentColor = settings.CircleColor;
        currentColor.a = Mathf.Lerp(expandStartAlpha, settings.CircleColor.a, t);
        ApplyColor(currentColor);

        if (t >= 1f)
        {
            BeginShrinkPhase();
        }
    }

    private void SpeedUpCurrentStageFailure()
    {
        float remainingDuration = Mathf.Max(0.01f, currentStageDuration - phaseElapsed);
        float acceleratedRemainingDuration = remainingDuration / settings.MissedParryShrinkMultiplier;
        currentStageDuration = phaseElapsed + Mathf.Max(0.05f, acceleratedRemainingDuration);
    }

    private void CompleteEncounter()
    {
        ResolveEncounter(true);
    }

    private void FailEncounter()
    {
        ResolveEncounter(false);
    }

    private void ResolveEncounter(bool completed)
    {
        if (hasBeenResolved)
        {
            return;
        }

        hasBeenResolved = true;
        Resolved?.Invoke(this, completed);
        Destroy(gameObject);
    }

    private void ApplyNormalizedSize(float normalizedSize)
    {
        float radius = Mathf.Clamp01(normalizedSize) * startRadius;
        transform.localScale = Vector3.one * radius;
    }

    private float GetCurrentAlpha()
    {
        return lineRenderer != null ? lineRenderer.startColor.a : settings.CircleColor.a;
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
