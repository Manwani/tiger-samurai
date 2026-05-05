using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TileCircleSpawner : MonoBehaviour
{
    private enum SpawnMode
    {
        RandomTile,
        StartingPlayerTile
    }

    private enum DebugSpawnFilter
    {
        All,
        WhiteOnly,
        RedOnly,
        BlueOnly,
        BlackOnly
    }

    private enum SpawnTriggerMode
    {
        PlayerLandingChance,
        TimedWaves
    }

    [SerializeField] private Transform gameplayTilesRoot;
    [SerializeField] private Transform effectsRoot;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ParryPointTracker parryPointTracker;
    [SerializeField] private LivesController livesController;
    [SerializeField] private HealthPickupSpawner healthPickupSpawner;
    [SerializeField] private DodgeMechanic dodgeMechanic;
    [SerializeField] private bool circleSpawningEnabled;
    [SerializeField] private SpawnTriggerMode spawnTriggerMode = SpawnTriggerMode.PlayerLandingChance;
    [SerializeField, Range(0f, 1f)] private float circleSpawnChanceOnLanding = 0.6f;
    [SerializeField] private SpawnMode spawnMode = SpawnMode.RandomTile;
    [SerializeField] private bool avoidPlayerTile = true;
    [SerializeField, Min(1)] private int maxActiveCircles = 2;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField, Min(0f)] private float circlePaddingPixels = 500f;
    [SerializeField, Range(0.1f, 1.5f)] private float circleSizeMultiplier = 1f;
    [SerializeField, Range(0.1f, 1f)] private float ellipseHeightScale = 0.426f;
    [SerializeField, Range(8, 64)] private int circleSegments = 64;
    [SerializeField] private int sortingOrder = 0;
    [SerializeField, Range(0f, 1f)] private float redCircleChance = 0.6f;
    [SerializeField, Range(0f, 1f)] private float blueCircleChance = 0.2f;
    [SerializeField, Range(0f, 1f)] private float blackCircleChance = 0.1f;
    [SerializeField] private DebugSpawnFilter debugSpawnFilter = DebugSpawnFilter.All;
    [SerializeField] private ParryCircleEncounter.Settings whiteCircleSettings = ParryCircleEncounter.Settings.CreateWhiteDefaults();
    [SerializeField] private ParryCircleEncounter.Settings redCircleSettings = ParryCircleEncounter.Settings.CreateRedDefaults();
    [SerializeField] private ParryCircleEncounter.Settings blueCircleSettings = ParryCircleEncounter.Settings.CreateBlueDefaults();
    [SerializeField] private ParryCircleEncounter.Settings blackCircleSettings = ParryCircleEncounter.Settings.CreateBlackDefaults();

    private readonly List<SpriteRenderer> tileRenderers = new();
    private readonly List<SpriteRenderer> candidateTiles = new();
    private readonly List<ParryCircleEncounter> activeEncounters = new();
    private readonly List<ParryCircleEncounter.Settings> candidateSettings = new();
    private readonly List<float> candidateSettingWeights = new();
    private readonly List<string> waveDifficultyLabels = new();
    private Material runtimeLineMaterial;
    private SpriteRenderer startingPlayerTile;
    private Coroutine spawnLoopCoroutine;
    private PlayerController subscribedPlayerController;
    private bool hasStoppedSpawning;

    private void Start()
    {
        if (gameplayTilesRoot == null)
        {
            gameplayTilesRoot = transform;
        }

        if (effectsRoot == null)
        {
            effectsRoot = CreateEffectsRoot();
        }

        NormalizeEncounterSettings();

        if (!TryCollectTiles())
        {
            enabled = false;
            return;
        }

        if (!TryFindPlayerController())
        {
            enabled = false;
            return;
        }

        if (!TryFindParryPointTracker())
        {
            enabled = false;
            return;
        }

        ResolveLivesController();

        runtimeLineMaterial = CreateRuntimeLineMaterial();
        if (runtimeLineMaterial == null)
        {
            Debug.LogError("TileCircleSpawner could not create a line material.", this);
            enabled = false;
            return;
        }

        SubscribeToPlayerLanding();
        ResolveDodgeMechanic();
        ResolveHealthPickupSpawner();

        if (circleSpawningEnabled && spawnTriggerMode == SpawnTriggerMode.TimedWaves)
        {
            spawnLoopCoroutine = StartCoroutine(SpawnLoop());
        }
    }

    private void OnValidate()
    {
        maxActiveCircles = Mathf.Max(1, maxActiveCircles);
        circleSpawnChanceOnLanding = Mathf.Clamp01(circleSpawnChanceOnLanding);
        NormalizeEncounterSettings();
    }

    private void OnDestroy()
    {
        UnsubscribeFromPlayerLanding();
        healthPickupSpawner?.ClearActivePickup();

        if (runtimeLineMaterial != null)
        {
            Destroy(runtimeLineMaterial);
        }
    }

    private IEnumerator SpawnLoop()
    {
        yield return null;

        while (enabled && !hasStoppedSpawning)
        {
            PruneInactiveEncounters();
            if (activeEncounters.Count > 0)
            {
                yield return null;
                continue;
            }

            int spawnedCount = SpawnCircleWave();
            if (spawnedCount == 0)
            {
                yield return null;
                continue;
            }

            while (enabled && !hasStoppedSpawning)
            {
                PruneInactiveEncounters();
                if (activeEncounters.Count == 0)
                {
                    break;
                }

                ParryCircleEncounter chosenEncounter = FindEngagedEncounter();
                if (chosenEncounter != null && activeEncounters.Count > 1)
                {
                    ClearUnchosenEncounters(chosenEncounter);
                    spawnedCount = activeEncounters.Count;
                }

                if (activeEncounters.Count < spawnedCount)
                {
                    ClearActiveEncounters();
                    break;
                }

                yield return null;
            }

            if (!enabled || hasStoppedSpawning)
            {
                yield break;
            }

            yield return new WaitForSeconds(Mathf.Max(0.05f, spawnInterval));
        }
    }

    public void StopSpawningAndClearActiveCircle()
    {
        hasStoppedSpawning = true;

        if (spawnLoopCoroutine != null)
        {
            StopCoroutine(spawnLoopCoroutine);
            spawnLoopCoroutine = null;
        }

        ClearActiveEncounters();

        enabled = false;
    }

    private int SpawnCircleWave()
    {
        if (!circleSpawningEnabled)
        {
            return 0;
        }

        int circlesToSpawn = Mathf.Max(1, maxActiveCircles);
        int spawnedCount = 0;
        waveDifficultyLabels.Clear();

        for (int i = 0; i < circlesToSpawn; i++)
        {
            if (!SpawnCircle(waveDifficultyLabels))
            {
                break;
            }

            spawnedCount++;
        }

        return spawnedCount;
    }

    private ParryCircleEncounter FindEngagedEncounter()
    {
        for (int i = activeEncounters.Count - 1; i >= 0; i--)
        {
            ParryCircleEncounter encounter = activeEncounters[i];
            if (encounter != null && encounter.IsEngaged)
            {
                return encounter;
            }
        }

        return null;
    }

    private void HandlePlayerLanded(Transform landedTile)
    {
        if (!circleSpawningEnabled ||
            spawnTriggerMode != SpawnTriggerMode.PlayerLandingChance ||
            hasStoppedSpawning ||
            landedTile == null)
        {
            return;
        }

        ResolveLivesController();
        if (livesController != null && livesController.IsGameOver)
        {
            return;
        }

        PruneInactiveEncounters();
        if (activeEncounters.Count > 0)
        {
            return;
        }

        if (Random.value > circleSpawnChanceOnLanding)
        {
            return;
        }

        SpriteRenderer tileRenderer = landedTile.GetComponent<SpriteRenderer>();
        if (tileRenderer == null ||
            (parryPointTracker != null && parryPointTracker.HasActiveEncounterOnTile(landedTile)) ||
            (healthPickupSpawner != null && healthPickupSpawner.HasActivePickupOnTile(landedTile)) ||
            HasEnemyOnTile(landedTile))
        {
            return;
        }

        SpawnCircleOnTile(tileRenderer, null, true);
    }

    private void ClearActiveEncounters()
    {
        healthPickupSpawner?.ClearActivePickup();

        for (int i = activeEncounters.Count - 1; i >= 0; i--)
        {
            ParryCircleEncounter encounter = activeEncounters[i];
            if (encounter != null)
            {
                Destroy(encounter.gameObject);
            }
        }

        activeEncounters.Clear();
    }

    private void ClearUnchosenEncounters(ParryCircleEncounter chosenEncounter)
    {
        for (int i = activeEncounters.Count - 1; i >= 0; i--)
        {
            ParryCircleEncounter encounter = activeEncounters[i];
            if (encounter == null)
            {
                activeEncounters.RemoveAt(i);
                continue;
            }

            if (encounter == chosenEncounter)
            {
                continue;
            }

            healthPickupSpawner?.ClearIfOnTile(encounter.TargetTile);
            Destroy(encounter.gameObject);
            activeEncounters.RemoveAt(i);
        }
    }

    private void PruneInactiveEncounters()
    {
        for (int i = activeEncounters.Count - 1; i >= 0; i--)
        {
            if (activeEncounters[i] == null)
            {
                activeEncounters.RemoveAt(i);
            }
        }
    }

    private bool SpawnCircle(List<string> usedDifficultyLabels)
    {
        SpriteRenderer tile = GetSpawnTile();
        if (tile == null)
        {
            return false;
        }

        return SpawnCircleOnTile(tile, usedDifficultyLabels, false);
    }

    private bool SpawnCircleOnTile(
        SpriteRenderer tile,
        List<string> usedDifficultyLabels,
        bool startIfPlayerIsOnTile)
    {
        if (!circleSpawningEnabled)
        {
            return false;
        }

        ParryCircleEncounter.Settings encounterSettings = ChooseEncounterSettings(usedDifficultyLabels);
        if (encounterSettings == null)
        {
            return false;
        }

        Bounds tileBounds = tile.bounds;
        float radius = (Mathf.Min(tileBounds.size.x, tileBounds.size.y) * 0.5f * circleSizeMultiplier) +
                       GetPaddingInWorldUnits(tile, circlePaddingPixels);

        GameObject circleObject = new("TileCircle");
        Transform circleTransform = circleObject.transform;
        circleTransform.SetParent(effectsRoot, true);
        circleTransform.position = tileBounds.center;

        LineRenderer lineRenderer = circleObject.AddComponent<LineRenderer>();
        ConfigureLineRenderer(lineRenderer, encounterSettings.CircleColor);
        DrawUnitEllipse(lineRenderer);

        ParryCircleEncounter encounter = circleObject.AddComponent<ParryCircleEncounter>();
        encounter.Initialize(lineRenderer, radius, tile.transform, playerController, parryPointTracker, encounterSettings);
        encounter.Resolved += HandleEncounterResolved;
        activeEncounters.Add(encounter);
        usedDifficultyLabels?.Add(GetDifficultyKey(encounterSettings));

        if (startIfPlayerIsOnTile)
        {
            encounter.StartIfPlayerIsOnTargetTile();
        }

        return true;
    }

    private void HandleEncounterResolved(ParryCircleEncounter encounter, bool completed)
    {
        if (encounter != null)
        {
            encounter.Resolved -= HandleEncounterResolved;
        }

        if (completed)
        {
            return;
        }

        ResolveLivesController();
        string difficultyLabel = encounter != null && !string.IsNullOrWhiteSpace(encounter.DifficultyLabel)
            ? encounter.DifficultyLabel
            : "unknown";
        string tileName = encounter != null && encounter.TargetTile != null
            ? encounter.TargetTile.name
            : "unknown tile";

        livesController?.LoseLife($"failed {difficultyLabel} parry circle on {tileName}");
    }

    private bool UsesStartingPlayerTile()
    {
        return spawnMode == SpawnMode.StartingPlayerTile;
    }

    private ParryCircleEncounter.Settings ChooseEncounterSettings(List<string> usedDifficultyLabels)
    {
        if (redCircleSettings == null)
        {
            redCircleSettings = ParryCircleEncounter.Settings.CreateRedDefaults();
        }

        if (blueCircleSettings == null)
        {
            blueCircleSettings = ParryCircleEncounter.Settings.CreateBlueDefaults();
        }

        if (blackCircleSettings == null)
        {
            blackCircleSettings = ParryCircleEncounter.Settings.CreateBlackDefaults();
        }

        if (whiteCircleSettings == null)
        {
            whiteCircleSettings = ParryCircleEncounter.Settings.CreateWhiteDefaults();
        }

        ParryCircleEncounter.Settings filteredSettings = GetDebugFilteredSettings();
        if (filteredSettings != null)
        {
            return HasUsedDifficultyLabel(filteredSettings, usedDifficultyLabels) ? null : filteredSettings;
        }

        float redChance = Mathf.Clamp01(redCircleChance);
        float blueChance = Mathf.Clamp01(blueCircleChance);
        float blackChance = Mathf.Clamp01(blackCircleChance);
        float totalSpecialChance = redChance + blueChance + blackChance;

        if (totalSpecialChance > 1f)
        {
            float normalizationFactor = 1f / totalSpecialChance;
            redChance *= normalizationFactor;
            blueChance *= normalizationFactor;
            blackChance *= normalizationFactor;
        }

        float whiteChance = Mathf.Max(0f, 1f - (redChance + blueChance + blackChance));
        candidateSettings.Clear();
        candidateSettingWeights.Clear();

        AddEncounterCandidate(blackCircleSettings, blackChance, usedDifficultyLabels);
        AddEncounterCandidate(blueCircleSettings, blueChance, usedDifficultyLabels);
        AddEncounterCandidate(redCircleSettings, redChance, usedDifficultyLabels);
        AddEncounterCandidate(whiteCircleSettings, whiteChance, usedDifficultyLabels);

        return ChooseWeightedCandidate();
    }

    private ParryCircleEncounter.Settings GetDebugFilteredSettings()
    {
        switch (debugSpawnFilter)
        {
            case DebugSpawnFilter.WhiteOnly:
                return whiteCircleSettings;
            case DebugSpawnFilter.RedOnly:
                return redCircleSettings;
            case DebugSpawnFilter.BlueOnly:
                return blueCircleSettings;
            case DebugSpawnFilter.BlackOnly:
                return blackCircleSettings;
            default:
                return null;
        }
    }

    private void AddEncounterCandidate(
        ParryCircleEncounter.Settings settings,
        float weight,
        List<string> usedDifficultyLabels)
    {
        if (settings == null || HasUsedDifficultyLabel(settings, usedDifficultyLabels))
        {
            return;
        }

        candidateSettings.Add(settings);
        candidateSettingWeights.Add(Mathf.Max(0f, weight));
    }

    private ParryCircleEncounter.Settings ChooseWeightedCandidate()
    {
        if (candidateSettings.Count == 0)
        {
            return null;
        }

        float totalWeight = 0f;
        for (int i = 0; i < candidateSettingWeights.Count; i++)
        {
            totalWeight += candidateSettingWeights[i];
        }

        if (totalWeight <= 0f)
        {
            int randomIndex = Random.Range(0, candidateSettings.Count);
            return candidateSettings[randomIndex];
        }

        float roll = Random.value * totalWeight;
        for (int i = 0; i < candidateSettings.Count; i++)
        {
            roll -= candidateSettingWeights[i];
            if (roll <= 0f)
            {
                return candidateSettings[i];
            }
        }

        return candidateSettings[candidateSettings.Count - 1];
    }

    private static bool HasUsedDifficultyLabel(
        ParryCircleEncounter.Settings settings,
        List<string> usedDifficultyLabels)
    {
        return usedDifficultyLabels != null &&
               usedDifficultyLabels.Contains(GetDifficultyKey(settings));
    }

    private static string GetDifficultyKey(ParryCircleEncounter.Settings settings)
    {
        if (settings == null || string.IsNullOrWhiteSpace(settings.Label))
        {
            return string.Empty;
        }

        return settings.Label.Trim().ToLowerInvariant();
    }

    private SpriteRenderer GetSpawnTile()
    {
        if (UsesStartingPlayerTile())
        {
            SpriteRenderer startingTile = GetStartingPlayerTile();
            if (startingTile == null)
            {
                return null;
            }

            if (parryPointTracker != null && parryPointTracker.HasActiveEncounterOnTile(startingTile.transform))
            {
                return null;
            }

            if (HasEnemyOnTile(startingTile.transform))
            {
                return null;
            }

            return startingTile;
        }

        return GetRandomTile();
    }

    private SpriteRenderer GetRandomTile()
    {
        candidateTiles.Clear();

        Transform playerTile = avoidPlayerTile && playerController != null
            ? playerController.CurrentTileTransform
            : null;

        foreach (SpriteRenderer tileRenderer in tileRenderers)
        {
            if (tileRenderer == null)
            {
                continue;
            }

            if (tileRenderer.transform == playerTile)
            {
                continue;
            }

            if (parryPointTracker != null && parryPointTracker.HasActiveEncounterOnTile(tileRenderer.transform))
            {
                continue;
            }

            if (HasEnemyOnTile(tileRenderer.transform))
            {
                continue;
            }

            candidateTiles.Add(tileRenderer);
        }

        if (candidateTiles.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, candidateTiles.Count);
        return candidateTiles[randomIndex];
    }

    private static bool HasEnemyOnTile(Transform tileTransform)
    {
        if (tileTransform == null)
        {
            return false;
        }

        EnemyEncounter[] enemyEncounters = FindObjectsByType<EnemyEncounter>(FindObjectsSortMode.None);
        foreach (EnemyEncounter enemyEncounter in enemyEncounters)
        {
            if (enemyEncounter != null && enemyEncounter.IsOnTile(tileTransform))
            {
                return true;
            }
        }

        return false;
    }

    private static float GetPaddingInWorldUnits(SpriteRenderer tile, float paddingPixels)
    {
        if (tile.sprite == null || tile.sprite.pixelsPerUnit <= 0f || paddingPixels <= 0f)
        {
            return 0f;
        }

        float smallestScale = Mathf.Min(
            Mathf.Abs(tile.transform.lossyScale.x),
            Mathf.Abs(tile.transform.lossyScale.y));

        return (paddingPixels / tile.sprite.pixelsPerUnit) * smallestScale;
    }

    private bool TryFindPlayerController()
    {
        if (playerController == null && playerTransform != null)
        {
            playerController = playerTransform.GetComponent<PlayerController>();
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (playerController != null)
        {
            playerTransform = playerController.transform;
            return true;
        }

        Debug.LogError("TileCircleSpawner needs a PlayerController reference.", this);
        return false;
    }

    private bool TryFindParryPointTracker()
    {
        if (parryPointTracker != null)
        {
            return true;
        }

        parryPointTracker = GetComponentInParent<ParryPointTracker>();
        if (parryPointTracker == null)
        {
            parryPointTracker = FindFirstObjectByType<ParryPointTracker>();
        }

        if (parryPointTracker == null)
        {
            Debug.LogError("TileCircleSpawner needs a ParryPointTracker reference.", this);
            return false;
        }

        return true;
    }

    private void ResolveLivesController()
    {
        if (livesController != null)
        {
            return;
        }

        livesController = FindFirstObjectByType<LivesController>();
        if (livesController != null)
        {
            return;
        }

        GameObject livesObject = new("LivesController");
        livesController = livesObject.AddComponent<LivesController>();
    }

    private void ResolveHealthPickupSpawner()
    {
        if (healthPickupSpawner == null)
        {
            healthPickupSpawner = GetComponent<HealthPickupSpawner>();
        }

        if (healthPickupSpawner == null)
        {
            healthPickupSpawner = gameObject.AddComponent<HealthPickupSpawner>();
        }

        healthPickupSpawner.Configure(
            gameplayTilesRoot,
            effectsRoot,
            playerController,
            parryPointTracker,
            dodgeMechanic,
            livesController);
    }

    private void SubscribeToPlayerLanding()
    {
        if (subscribedPlayerController == playerController)
        {
            return;
        }

        UnsubscribeFromPlayerLanding();

        if (playerController == null)
        {
            return;
        }

        playerController.LandedOnTile += HandlePlayerLanded;
        subscribedPlayerController = playerController;
    }

    private void UnsubscribeFromPlayerLanding()
    {
        if (subscribedPlayerController == null)
        {
            return;
        }

        subscribedPlayerController.LandedOnTile -= HandlePlayerLanded;
        subscribedPlayerController = null;
    }

    private void ResolveDodgeMechanic()
    {
        if (dodgeMechanic == null)
        {
            dodgeMechanic = GetComponent<DodgeMechanic>();
        }

        if (dodgeMechanic == null)
        {
            dodgeMechanic = gameObject.AddComponent<DodgeMechanic>();
        }

        dodgeMechanic.Configure(effectsRoot, playerController, parryPointTracker, livesController);
    }

    private SpriteRenderer GetStartingPlayerTile()
    {
        if (startingPlayerTile != null)
        {
            return startingPlayerTile;
        }

        if (playerTransform == null)
        {
            return null;
        }

        Vector3 playerPosition = playerTransform.position;
        float closestDistance = float.MaxValue;

        foreach (SpriteRenderer tileRenderer in tileRenderers)
        {
            float distance = (tileRenderer.bounds.center - playerPosition).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                startingPlayerTile = tileRenderer;
            }
        }

        return startingPlayerTile;
    }

    private bool TryCollectTiles()
    {
        tileRenderers.Clear();

        foreach (Transform child in gameplayTilesRoot)
        {
            SpriteRenderer tileRenderer = child.GetComponent<SpriteRenderer>();
            if (tileRenderer != null)
            {
                tileRenderers.Add(tileRenderer);
            }
        }

        if (tileRenderers.Count == 0)
        {
            Debug.LogError("TileCircleSpawner could not find any tile SpriteRenderers.", this);
            return false;
        }

        return true;
    }

    private Transform CreateEffectsRoot()
    {
        Transform parent = gameplayTilesRoot.parent;
        if (parent != null)
        {
            Transform existingRoot = parent.Find("TileCircleEffects");
            if (existingRoot != null)
            {
                return existingRoot;
            }
        }

        GameObject effectsObject = new("TileCircleEffects");
        Transform newRoot = effectsObject.transform;

        if (parent != null)
        {
            newRoot.SetParent(parent, false);
        }

        return newRoot;
    }

    private Material CreateRuntimeLineMaterial()
    {
        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null)
        {
            return null;
        }

        Material material = new(lineShader)
        {
            name = "RuntimeTileCircleMaterial",
            hideFlags = HideFlags.DontSave
        };

        return material;
    }

    private void ConfigureLineRenderer(LineRenderer lineRenderer, Color encounterColor)
    {
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = Mathf.Max(8, circleSegments);
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.sharedMaterial = runtimeLineMaterial;
        lineRenderer.sortingOrder = sortingOrder;
        lineRenderer.startColor = encounterColor;
        lineRenderer.endColor = encounterColor;
    }

    private void DrawUnitEllipse(LineRenderer lineRenderer)
    {
        int segmentCount = lineRenderer.positionCount;

        // The encounter script scales this unit ellipse up to the tile size.
        for (int i = 0; i < segmentCount; i++)
        {
            float angle = (i / (float)segmentCount) * Mathf.PI * 2f;
            Vector3 point = new(Mathf.Cos(angle), Mathf.Sin(angle) * ellipseHeightScale, 0f);

            lineRenderer.SetPosition(i, point);
        }
    }

    private void NormalizeEncounterSettings()
    {
        if (whiteCircleSettings == null)
        {
            whiteCircleSettings = ParryCircleEncounter.Settings.CreateWhiteDefaults();
        }

        if (redCircleSettings == null)
        {
            redCircleSettings = ParryCircleEncounter.Settings.CreateRedDefaults();
        }

        if (blueCircleSettings == null)
        {
            blueCircleSettings = ParryCircleEncounter.Settings.CreateBlueDefaults();
        }

        if (blackCircleSettings == null)
        {
            blackCircleSettings = ParryCircleEncounter.Settings.CreateBlackDefaults();
        }

        whiteCircleSettings.Normalize();
        redCircleSettings.Normalize();
        blueCircleSettings.Normalize();
        blackCircleSettings.Normalize();
    }
}
