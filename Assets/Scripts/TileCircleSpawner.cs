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

    [SerializeField] private Transform gameplayTilesRoot;
    [SerializeField] private Transform effectsRoot;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ParryPointTracker parryPointTracker;
    [SerializeField] private SpawnMode spawnMode = SpawnMode.RandomTile;
    [SerializeField] private bool avoidPlayerTile = true;
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
    private Material runtimeLineMaterial;
    private SpriteRenderer startingPlayerTile;
    private ParryCircleEncounter activeEncounter;
    private Coroutine spawnLoopCoroutine;
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

        runtimeLineMaterial = CreateRuntimeLineMaterial();
        if (runtimeLineMaterial == null)
        {
            Debug.LogError("TileCircleSpawner could not create a line material.", this);
            enabled = false;
            return;
        }

        spawnLoopCoroutine = StartCoroutine(SpawnLoop());
    }

    private void OnValidate()
    {
        NormalizeEncounterSettings();
    }

    private void OnDestroy()
    {
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
            if (activeEncounter != null)
            {
                yield return null;
                continue;
            }

            if (!SpawnCircle())
            {
                yield return null;
                continue;
            }

            while (activeEncounter != null)
            {
                yield return null;
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

        if (activeEncounter != null)
        {
            Destroy(activeEncounter.gameObject);
            activeEncounter = null;
        }

        enabled = false;
    }

    private bool SpawnCircle()
    {
        SpriteRenderer tile = GetSpawnTile();
        if (tile == null)
        {
            return false;
        }

        ParryCircleEncounter.Settings encounterSettings = ChooseEncounterSettings();
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
        activeEncounter = encounter;
        return true;
    }

    private bool UsesStartingPlayerTile()
    {
        return spawnMode == SpawnMode.StartingPlayerTile;
    }

    private ParryCircleEncounter.Settings ChooseEncounterSettings()
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

        float roll = Random.value;
        if (roll < blackChance)
        {
            return blackCircleSettings;
        }

        roll -= blackChance;
        if (roll < blueChance)
        {
            return blueCircleSettings;
        }

        roll -= blueChance;
        if (roll < redChance)
        {
            return redCircleSettings;
        }

        return whiteCircleSettings;
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
