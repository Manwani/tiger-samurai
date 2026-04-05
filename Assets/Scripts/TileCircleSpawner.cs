using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TileCircleSpawner : MonoBehaviour
{
    [SerializeField] private Transform gameplayTilesRoot;
    [SerializeField] private Transform effectsRoot;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private ParryPointTracker parryPointTracker;
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private float shrinkDuration = 1.25f;
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField, Min(0f)] private float circlePaddingPixels = 500f;
    [SerializeField, Range(0.1f, 1.5f)] private float circleSizeMultiplier = 1f;
    [SerializeField, Range(0.1f, 1f)] private float ellipseHeightScale = 0.426f;
    [SerializeField, Range(8, 64)] private int circleSegments = 64;
    [SerializeField] private int sortingOrder = 0;
    [SerializeField] private Color circleColor = new(1f, 0.95f, 0.35f, 0.9f);

    private readonly List<SpriteRenderer> tileRenderers = new();
    private Material runtimeLineMaterial;
    private SpriteRenderer startingPlayerTile;

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

        if (!TryCollectTiles())
        {
            enabled = false;
            return;
        }

        if (!TryFindPlayer())
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

        StartCoroutine(SpawnLoop());
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

        while (enabled)
        {
            SpawnCircleAtStartingPlayerTile();
            yield return new WaitForSeconds(Mathf.Max(0.05f, spawnInterval));
        }
    }

    private void SpawnCircleAtStartingPlayerTile()
    {
        // Debug mode: keep spawning on the starting player tile instead of a random tile.
        // SpriteRenderer tile = tileRenderers[Random.Range(0, tileRenderers.Count)];
        SpriteRenderer tile = GetStartingPlayerTile();
        if (tile == null)
        {
            return;
        }

        Bounds tileBounds = tile.bounds;
        float radius = (Mathf.Min(tileBounds.size.x, tileBounds.size.y) * 0.5f * circleSizeMultiplier) +
                       GetPaddingInWorldUnits(tile, circlePaddingPixels);

        GameObject circleObject = new("TileCircle");
        Transform circleTransform = circleObject.transform;
        circleTransform.SetParent(effectsRoot, true);
        circleTransform.position = tileBounds.center;

        LineRenderer lineRenderer = circleObject.AddComponent<LineRenderer>();
        ConfigureLineRenderer(lineRenderer);
        DrawUnitEllipse(lineRenderer);

        ShrinkingCircle shrinkingCircle = circleObject.AddComponent<ShrinkingCircle>();
        shrinkingCircle.Initialize(lineRenderer, radius, shrinkDuration, circleColor, tile.transform, parryPointTracker);
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

    private bool TryFindPlayer()
    {
        if (playerTransform != null)
        {
            return true;
        }

        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        if (playerTransform == null)
        {
            Debug.LogError("TileCircleSpawner needs a Player reference for debug spawning.", this);
            return false;
        }

        return true;
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

    private void ConfigureLineRenderer(LineRenderer lineRenderer)
    {
        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = Mathf.Max(8, circleSegments);
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.numCapVertices = 4;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.sharedMaterial = runtimeLineMaterial;
        lineRenderer.sortingOrder = sortingOrder;
        lineRenderer.startColor = circleColor;
        lineRenderer.endColor = circleColor;
    }

    private void DrawUnitEllipse(LineRenderer lineRenderer)
    {
        int segmentCount = lineRenderer.positionCount;

        // The shrinking script scales this unit ellipse up to the tile size.
        for (int i = 0; i < segmentCount; i++)
        {
            float angle = (i / (float)segmentCount) * Mathf.PI * 2f;
            Vector3 point = new(Mathf.Cos(angle), Mathf.Sin(angle) * ellipseHeightScale, 0f);

            lineRenderer.SetPosition(i, point);
        }
    }
}
