using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class HealthPickupSpawner : MonoBehaviour
{
    private const string TurkeyLegSpriteAssetPath = "Assets/Art/Items/turkeyLeg.png";

    [SerializeField] private Sprite healthPickupSprite;
    [SerializeField, Min(1)] private int spawnLifeThreshold = 3;
    [SerializeField, Range(0f, 1f)] private float spawnChanceOnLanding = 0.1f;
    [SerializeField, Min(0.1f)] private float pickupLifetimeSeconds = 5f;
    [SerializeField, Range(0.1f, 1f)] private float tileSizeMultiplier = 0.46f;
    [SerializeField] private Vector3 worldOffset = new(0f, 0.12f, 0f);
    [SerializeField] private int sortingOrder = 10;
    [SerializeField] private bool avoidCircleTiles = true;
    [SerializeField] private bool avoidEnemyTiles = true;
    [SerializeField] private bool avoidDodgeHazardTiles = true;
    [SerializeField] private Transform gameplayTilesRoot;
    [SerializeField] private Transform effectsRoot;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ParryPointTracker parryPointTracker;
    [SerializeField] private DodgeMechanic dodgeMechanic;
    [SerializeField] private LivesController livesController;

    private readonly List<SpriteRenderer> tileRenderers = new();
    private readonly List<SpriteRenderer> safeCandidateTiles = new();
    private HealthPickup activeHealthPickup;
    private PlayerController subscribedPlayerController;
    private bool hasLoggedMissingSprite;

    private void Start()
    {
        ResolveReferences();
        CollectTiles();
        SubscribeToPlayerLanding();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ResolveReferences();
        CollectTilesIfNeeded();
        SubscribeToPlayerLanding();
    }

    private void OnValidate()
    {
        spawnLifeThreshold = Mathf.Max(1, spawnLifeThreshold);
        spawnChanceOnLanding = Mathf.Clamp01(spawnChanceOnLanding);
        pickupLifetimeSeconds = Mathf.Max(0.1f, pickupLifetimeSeconds);
    }

    private void OnDestroy()
    {
        UnsubscribeFromPlayerLanding();
        ClearActivePickup();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerLanding();
    }

    public void Configure(
        Transform targetEffectsRoot,
        PlayerController targetPlayerController,
        LivesController targetLivesController)
    {
        Configure(null, targetEffectsRoot, targetPlayerController, null, null, targetLivesController);
    }

    public void Configure(
        Transform targetGameplayTilesRoot,
        Transform targetEffectsRoot,
        PlayerController targetPlayerController,
        ParryPointTracker targetParryPointTracker,
        DodgeMechanic targetDodgeMechanic,
        LivesController targetLivesController)
    {
        gameplayTilesRoot = targetGameplayTilesRoot != null ? targetGameplayTilesRoot : gameplayTilesRoot;
        effectsRoot = targetEffectsRoot;
        playerController = targetPlayerController;
        parryPointTracker = targetParryPointTracker;
        dodgeMechanic = targetDodgeMechanic;
        livesController = targetLivesController;

        ResolveReferences();
        CollectTiles();
        SubscribeToPlayerLanding();
    }

    public void TrySpawnForWave(IReadOnlyList<ParryCircleEncounter> activeEncounters)
    {
        _ = activeEncounters;
        TrySpawnRandomPickup();
    }

    public void ClearActivePickup()
    {
        if (activeHealthPickup != null)
        {
            Destroy(activeHealthPickup.gameObject);
        }

        activeHealthPickup = null;
    }

    public void ClearIfOnTile(Transform tileTransform)
    {
        if (activeHealthPickup != null && activeHealthPickup.TargetTile == tileTransform)
        {
            ClearActivePickup();
        }
    }

    public bool HasActivePickupOnTile(Transform tileTransform)
    {
        PruneActivePickup();
        return activeHealthPickup != null && activeHealthPickup.TargetTile == tileTransform;
    }

    private void HandlePlayerLanded(Transform landedTile)
    {
        if (landedTile == null)
        {
            return;
        }

        TrySpawnRandomPickup();
    }

    private void TrySpawnRandomPickup()
    {
        ResolveReferences();
        PruneActivePickup();

        if (activeHealthPickup != null || !CanSpawnPickup())
        {
            return;
        }

        if (Random.value > spawnChanceOnLanding)
        {
            return;
        }

        Sprite pickupSprite = ResolvePickupSprite();
        if (pickupSprite == null)
        {
            LogMissingSpriteOnce();
            return;
        }

        SpriteRenderer targetTile = GetRandomSafeTile();
        if (targetTile == null)
        {
            return;
        }

        SpawnPickupOnTile(targetTile.transform, pickupSprite);
    }

    private bool CanSpawnPickup()
    {
        return livesController != null &&
               livesController.CanRestoreLife &&
               livesController.CurrentLives < spawnLifeThreshold;
    }

    private SpriteRenderer GetRandomSafeTile()
    {
        CollectTilesIfNeeded();
        safeCandidateTiles.Clear();

        foreach (SpriteRenderer tileRenderer in tileRenderers)
        {
            if (tileRenderer == null || !IsSafeTile(tileRenderer.transform))
            {
                continue;
            }

            safeCandidateTiles.Add(tileRenderer);
        }

        if (safeCandidateTiles.Count == 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, safeCandidateTiles.Count);
        return safeCandidateTiles[randomIndex];
    }

    private bool IsSafeTile(Transform tileTransform)
    {
        if (tileTransform == null)
        {
            return false;
        }

        if (playerController != null &&
            playerController.CurrentTileTransform == tileTransform)
        {
            return false;
        }

        if (avoidCircleTiles &&
            parryPointTracker != null &&
            parryPointTracker.HasActiveEncounterOnTile(tileTransform))
        {
            return false;
        }

        if (avoidDodgeHazardTiles &&
            dodgeMechanic != null &&
            dodgeMechanic.HasActiveHazardOnTile(tileTransform))
        {
            return false;
        }

        return !avoidEnemyTiles || !HasEnemyOnTile(tileTransform);
    }

    private void SpawnPickupOnTile(Transform targetTile, Sprite pickupSprite)
    {
        SpriteRenderer tileRenderer = targetTile.GetComponent<SpriteRenderer>();
        Bounds tileBounds = tileRenderer != null
            ? tileRenderer.bounds
            : new Bounds(targetTile.position, Vector3.one);

        GameObject pickupObject = new("TurkeyLegHealthPickup");
        pickupObject.transform.SetParent(effectsRoot != null ? effectsRoot : transform, true);
        pickupObject.transform.position = tileBounds.center + worldOffset;

        GameObject visualObject = new("TurkeyLegVisual");
        visualObject.transform.SetParent(pickupObject.transform, false);

        SpriteRenderer pickupRenderer = visualObject.AddComponent<SpriteRenderer>();
        pickupRenderer.sprite = pickupSprite;
        pickupRenderer.sortingOrder = sortingOrder;
        ConfigurePickupVisual(pickupRenderer, visualObject.transform, tileBounds);

        HealthPickup pickup = pickupObject.AddComponent<HealthPickup>();
        activeHealthPickup = pickup;
        pickup.Initialize(targetTile, playerController, livesController, pickupLifetimeSeconds);
    }

    private void ResolveReferences()
    {
        if (gameplayTilesRoot == null)
        {
            gameplayTilesRoot = transform;
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (parryPointTracker == null)
        {
            parryPointTracker = FindFirstObjectByType<ParryPointTracker>();
        }

        if (dodgeMechanic == null)
        {
            dodgeMechanic = FindFirstObjectByType<DodgeMechanic>();
        }

        if (livesController == null)
        {
            livesController = FindFirstObjectByType<LivesController>();
        }
    }

    private void CollectTilesIfNeeded()
    {
        if (tileRenderers.Count == 0)
        {
            CollectTiles();
        }
    }

    private void CollectTiles()
    {
        tileRenderers.Clear();

        if (gameplayTilesRoot == null)
        {
            return;
        }

        foreach (Transform child in gameplayTilesRoot)
        {
            SpriteRenderer tileRenderer = child.GetComponent<SpriteRenderer>();
            if (tileRenderer != null)
            {
                tileRenderers.Add(tileRenderer);
            }
        }
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

    private void PruneActivePickup()
    {
        if (activeHealthPickup == null)
        {
            activeHealthPickup = null;
        }
    }

    private Sprite ResolvePickupSprite()
    {
        if (healthPickupSprite != null)
        {
            return healthPickupSprite;
        }

#if UNITY_EDITOR
        UnityEngine.Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(TurkeyLegSpriteAssetPath);
        foreach (UnityEngine.Object asset in assets)
        {
            if (asset is Sprite sprite)
            {
                healthPickupSprite = sprite;
                return healthPickupSprite;
            }
        }
#endif

        return null;
    }

    private void ConfigurePickupVisual(
        SpriteRenderer pickupRenderer,
        Transform visualTransform,
        Bounds tileBounds)
    {
        if (pickupRenderer.sprite == null)
        {
            return;
        }

        Vector2 spriteSize = pickupRenderer.sprite.bounds.size;
        float largestSpriteSize = Mathf.Max(spriteSize.x, spriteSize.y);
        float smallestTileSize = Mathf.Min(tileBounds.size.x, tileBounds.size.y);

        if (largestSpriteSize <= 0f || smallestTileSize <= 0f)
        {
            return;
        }

        float targetSize = smallestTileSize * tileSizeMultiplier;
        float visualScale = targetSize / largestSpriteSize;
        visualTransform.localScale = Vector3.one * visualScale;

        Vector3 spriteCenter = pickupRenderer.sprite.bounds.center;
        visualTransform.localPosition = new Vector3(
            -spriteCenter.x * visualScale,
            -spriteCenter.y * visualScale,
            0f);
    }

    private void LogMissingSpriteOnce()
    {
        if (hasLoggedMissingSprite)
        {
            return;
        }

        hasLoggedMissingSprite = true;
        Debug.LogWarning(
            $"HealthPickupSpawner could not find the turkey leg sprite at {TurkeyLegSpriteAssetPath}.",
            this);
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
}
