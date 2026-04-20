using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private Transform gameplayTilesRoot;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private Transform enemiesRoot;
    [SerializeField] private EnemyPromptSpriteSet promptSprites;
    [SerializeField] private bool spawnEnemyOnStart = false;
    [SerializeField] private bool avoidPlayerTile = true;
    [SerializeField, Range(0.1f, 1f)] private float enemySizeMultiplier = 0.35f;
    [SerializeField] private Color enemyColor = new(0.85f, 0.2f, 0.2f, 1f);
    [SerializeField] private float enemyZ = -0.5f;
    [SerializeField] private int enemySortingOrder = 0;

    private readonly List<SpriteRenderer> tileRenderers = new();
    private static Sprite runtimeSquareSprite;

    private void Start()
    {
        if (!TryResolveReferences())
        {
            enabled = false;
            return;
        }

        if (!spawnEnemyOnStart)
        {
            return;
        }

        SpawnEnemy();
    }

    public void SpawnEnemy()
    {
        SpriteRenderer tile = GetRandomTile();
        if (tile == null)
        {
            Debug.LogWarning("EnemySpawner could not find a tile to spawn on.", this);
            return;
        }

        if (enemiesRoot == null)
        {
            enemiesRoot = CreateEnemiesRoot();
        }

        GameObject enemyObject = new("Enemy");
        Transform enemyTransform = enemyObject.transform;
        enemyTransform.SetParent(enemiesRoot, true);

        Bounds tileBounds = tile.bounds;
        enemyTransform.position = new Vector3(tileBounds.center.x, tileBounds.center.y, enemyZ);

        SpriteRenderer spriteRenderer = enemyObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetRuntimeSquareSprite();
        spriteRenderer.color = enemyColor;
        spriteRenderer.sortingOrder = enemySortingOrder;

        float size = Mathf.Min(tileBounds.size.x, tileBounds.size.y) * enemySizeMultiplier;
        enemyTransform.localScale = new Vector3(size, size, 1f);

        EnemyEncounter encounter = enemyObject.AddComponent<EnemyEncounter>();
        encounter.Initialize(playerController, tile.transform, promptSprites);
    }

    private bool TryResolveReferences()
    {
        if (gameplayTilesRoot == null)
        {
            GameObject tilesRoot = GameObject.Find("GameplayTiles");
            if (tilesRoot != null)
            {
                gameplayTilesRoot = tilesRoot.transform;
            }
        }

        if (gameplayTilesRoot == null)
        {
            Debug.LogError("EnemySpawner needs a GameplayTiles root reference.", this);
            return false;
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        CollectTiles();
        return tileRenderers.Count > 0;
    }

    private void CollectTiles()
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
            Debug.LogError("EnemySpawner could not find any tile SpriteRenderers.", this);
        }
    }

    private SpriteRenderer GetRandomTile()
    {
        Transform excludedTile = avoidPlayerTile && playerController != null
            ? playerController.CurrentTileTransform
            : null;

        var candidateTiles = new List<SpriteRenderer>(tileRenderers.Count);
        foreach (SpriteRenderer tileRenderer in tileRenderers)
        {
            if (tileRenderer.transform == excludedTile)
            {
                continue;
            }

            candidateTiles.Add(tileRenderer);
        }

        if (candidateTiles.Count == 0)
        {
            return tileRenderers.Count > 0 ? tileRenderers[0] : null;
        }

        int randomIndex = Random.Range(0, candidateTiles.Count);
        return candidateTiles[randomIndex];
    }

    private Transform CreateEnemiesRoot()
    {
        Transform parent = gameplayTilesRoot.parent;
        if (parent != null)
        {
            Transform existingRoot = parent.Find("Enemies");
            if (existingRoot != null)
            {
                return existingRoot;
            }
        }

        GameObject enemiesObject = new("Enemies");
        Transform newRoot = enemiesObject.transform;

        if (parent != null)
        {
            newRoot.SetParent(parent, false);
        }

        return newRoot;
    }

    private static Sprite GetRuntimeSquareSprite()
    {
        if (runtimeSquareSprite != null)
        {
            return runtimeSquareSprite;
        }

        // A 1x1 white sprite keeps the enemy art-free and easy to tint.
        runtimeSquareSprite = Sprite.Create(
            Texture2D.whiteTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);

        runtimeSquareSprite.name = "RuntimeEnemySquare";
        return runtimeSquareSprite;
    }
}
