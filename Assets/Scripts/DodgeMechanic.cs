using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public class DodgeMechanic : MonoBehaviour
{
    [SerializeField] private Transform effectsRoot;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private ParryPointTracker parryPointTracker;
    [SerializeField] private HealthPickupSpawner healthPickupSpawner;
    [SerializeField] private LivesController livesController;
    [SerializeField] private bool spawnOnEveryLanding = true;
    [SerializeField, Range(0f, 1f)] private float spawnChance = 1f;
    [SerializeField] private bool skipCircleTiles = true;
    [SerializeField] private bool skipEnemyTiles = true;
    [SerializeField] private bool skipHealthPickupTiles = true;
    [SerializeField, Min(0f)] private float warningDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float collapseDuration = 0.28f;
    [SerializeField, Min(0f)] private float lingerDuration = 0.08f;
    [SerializeField, Min(0.001f)] private float lineWidth = 0.08f;
    [SerializeField, Range(-0.5f, 0.45f)] private float edgeInsetPercent = 0.06f;
    [SerializeField] private float zOffset = -0.25f;
    [SerializeField] private Color warningColor = new(1f, 0.88f, 0.25f, 0.95f);
    [SerializeField] private Color collapseColor = new(1f, 0.12f, 0.08f, 1f);
    [SerializeField] private int sortingOrder = 35;
    [SerializeField, Min(1)] private int damage = 1;

    private readonly List<DodgeHazard> activeHazards = new();
    private Material runtimeLineMaterial;
    private PlayerController subscribedPlayerController;

    public void Configure(
        Transform targetEffectsRoot,
        PlayerController targetPlayerController,
        ParryPointTracker targetParryPointTracker,
        LivesController targetLivesController)
    {
        effectsRoot = targetEffectsRoot;
        playerController = targetPlayerController;
        parryPointTracker = targetParryPointTracker;
        livesController = targetLivesController;

        ResolveReferences();
        EnsureRuntimeLineMaterial();
        SubscribeToPlayerLanding();
    }

    private void Start()
    {
        ResolveReferences();
        EnsureRuntimeLineMaterial();
        SubscribeToPlayerLanding();
    }

    private void OnValidate()
    {
        spawnChance = Mathf.Clamp01(spawnChance);
        warningDuration = Mathf.Max(0f, warningDuration);
        collapseDuration = Mathf.Max(0.01f, collapseDuration);
        lingerDuration = Mathf.Max(0f, lingerDuration);
        lineWidth = Mathf.Max(0.001f, lineWidth);
        edgeInsetPercent = Mathf.Clamp(edgeInsetPercent, -0.5f, 0.45f);
        damage = Mathf.Max(1, damage);
    }

    private void OnDestroy()
    {
        UnsubscribeFromPlayerLanding();
        ClearActiveHazards();

        if (runtimeLineMaterial != null)
        {
            Destroy(runtimeLineMaterial);
        }
    }

    private void HandlePlayerLanded(Transform landedTile)
    {
        if (!spawnOnEveryLanding || landedTile == null)
        {
            return;
        }

        ResolveReferences();
        if (livesController != null && livesController.IsGameOver)
        {
            return;
        }

        if (Random.value > spawnChance)
        {
            return;
        }

        if (ShouldSkipTile(landedTile))
        {
            return;
        }

        SpawnHazard(landedTile);
    }

    private bool ShouldSkipTile(Transform tile)
    {
        if (skipCircleTiles &&
            parryPointTracker != null &&
            parryPointTracker.HasActiveEncounterOnTile(tile))
        {
            return true;
        }

        if (skipHealthPickupTiles &&
            healthPickupSpawner != null &&
            healthPickupSpawner.HasActivePickupOnTile(tile))
        {
            return true;
        }

        return skipEnemyTiles && HasEnemyOnTile(tile);
    }

    private void SpawnHazard(Transform targetTile)
    {
        if (!EnsureRuntimeLineMaterial())
        {
            return;
        }

        PruneInactiveHazards();
        ClearHazardOnTile(targetTile);

        GameObject hazardObject = new("DodgeHazard");
        Transform hazardTransform = hazardObject.transform;
        hazardTransform.SetParent(effectsRoot != null ? effectsRoot : transform, true);
        hazardTransform.position = targetTile.position;

        DodgeHazard.LineDirection direction = Random.value < 0.5f
            ? DodgeHazard.LineDirection.Horizontal
            : DodgeHazard.LineDirection.Vertical;

        DodgeHazard hazard = hazardObject.AddComponent<DodgeHazard>();
        activeHazards.Add(hazard);
        hazard.Initialize(
            targetTile,
            playerController,
            livesController,
            runtimeLineMaterial,
            direction,
            warningDuration,
            collapseDuration,
            lingerDuration,
            lineWidth,
            edgeInsetPercent,
            zOffset,
            warningColor,
            collapseColor,
            sortingOrder,
            damage);
    }

    private void ClearHazardOnTile(Transform tile)
    {
        for (int i = activeHazards.Count - 1; i >= 0; i--)
        {
            DodgeHazard hazard = activeHazards[i];
            if (hazard == null)
            {
                activeHazards.RemoveAt(i);
                continue;
            }

            if (hazard.TargetTile == tile)
            {
                Destroy(hazard.gameObject);
                activeHazards.RemoveAt(i);
            }
        }
    }

    private void ClearActiveHazards()
    {
        for (int i = activeHazards.Count - 1; i >= 0; i--)
        {
            DodgeHazard hazard = activeHazards[i];
            if (hazard != null)
            {
                Destroy(hazard.gameObject);
            }
        }

        activeHazards.Clear();
    }

    private void PruneInactiveHazards()
    {
        for (int i = activeHazards.Count - 1; i >= 0; i--)
        {
            if (activeHazards[i] == null)
            {
                activeHazards.RemoveAt(i);
            }
        }
    }

    private void ResolveReferences()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (parryPointTracker == null)
        {
            parryPointTracker = FindFirstObjectByType<ParryPointTracker>();
        }

        if (healthPickupSpawner == null)
        {
            healthPickupSpawner = FindFirstObjectByType<HealthPickupSpawner>();
        }

        if (livesController == null)
        {
            livesController = FindFirstObjectByType<LivesController>();
        }

        if (effectsRoot == null)
        {
            effectsRoot = CreateEffectsRoot();
        }
    }

    private Transform CreateEffectsRoot()
    {
        Transform parent = playerController != null ? playerController.transform.parent : transform;
        if (parent != null)
        {
            Transform existingRoot = parent.Find("DodgeHazards");
            if (existingRoot != null)
            {
                return existingRoot;
            }
        }

        GameObject effectsObject = new("DodgeHazards");
        Transform newRoot = effectsObject.transform;
        if (parent != null)
        {
            newRoot.SetParent(parent, false);
        }

        return newRoot;
    }

    private bool EnsureRuntimeLineMaterial()
    {
        if (runtimeLineMaterial != null)
        {
            return true;
        }

        Shader lineShader = Shader.Find("Sprites/Default");
        if (lineShader == null)
        {
            Debug.LogWarning("DodgeMechanic could not find the Sprites/Default shader.", this);
            return false;
        }

        runtimeLineMaterial = new Material(lineShader)
        {
            name = "RuntimeDodgeHazardMaterial",
            hideFlags = HideFlags.DontSave
        };

        return true;
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

    public bool HasActiveHazardOnTile(Transform tileTransform)
    {
        PruneInactiveHazards();

        for (int i = 0; i < activeHazards.Count; i++)
        {
            DodgeHazard hazard = activeHazards[i];
            if (hazard != null && hazard.TargetTile == tileTransform)
            {
                return true;
            }
        }

        return false;
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
