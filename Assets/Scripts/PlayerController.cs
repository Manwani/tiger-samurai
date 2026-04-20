using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform gameplayTilesRoot;
    [SerializeField] private Vector2Int gridSize = new(3, 3);
    [SerializeField] private Vector2Int startCell = new(1, 1);
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private float jumpHeight = 0.35f;

    private Vector2Int currentCell;
    private bool isMoving;
    private float baseZ;
    private Transform[,] tileGrid;
    private bool hasBufferedMove;
    private Vector2Int bufferedMove;
    private int controlLockCount;

    public event Action<Transform> LandedOnTile;

    public Transform CurrentTileTransform
    {
        get
        {
            if (tileGrid == null)
            {
                return null;
            }

            return tileGrid[currentCell.x, currentCell.y];
        }
    }

    public bool AreControlsLocked => controlLockCount > 0;

    private void Start()
    {
        baseZ = transform.position.z;

        if (gameplayTilesRoot == null)
        {
            var tilesRoot = GameObject.Find("GameplayTiles");
            if (tilesRoot != null)
            {
                gameplayTilesRoot = tilesRoot.transform;
            }
        }

        if (gameplayTilesRoot == null)
        {
            Debug.LogError("PlayerController needs a GameplayTiles root reference.", this);
            enabled = false;
            return;
        }

        if (!TryBuildTileGrid())
        {
            enabled = false;
            return;
        }

        currentCell = ClampToGrid(startCell);
        transform.position = CellToWorld(currentCell);
    }

    private void Update()
    {
        if (AreControlsLocked)
        {
            return;
        }

        BufferMoveInputs();

        if (isMoving)
        {
            return;
        }

        TryStartBufferedMove();
    }

    private void BufferMoveInputs()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
        {
            SetBufferedMove(Vector2Int.up);
        }

        if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
        {
            SetBufferedMove(Vector2Int.down);
        }

        if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
        {
            SetBufferedMove(Vector2Int.left);
        }

        if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
        {
            SetBufferedMove(Vector2Int.right);
        }
    }

    private IEnumerator JumpToCell(Vector2Int targetCell)
    {
        isMoving = true;

        Vector3 startPosition = transform.position;
        Vector3 endPosition = CellToWorld(targetCell);
        float duration = Mathf.Max(0.0001f, moveDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 position = Vector3.Lerp(startPosition, endPosition, t);
            position.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            transform.position = position;

            yield return null;
        }

        currentCell = targetCell;
        transform.position = endPosition;
        isMoving = false;
        LandedOnTile?.Invoke(CurrentTileTransform);
        TryStartBufferedMove();
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        Transform tile = tileGrid[cell.x, cell.y];
        Vector3 tilePosition = tile.position;
        return new Vector3(tilePosition.x, tilePosition.y, baseZ);
    }

    private Vector2Int ClampToGrid(Vector2Int cell)
    {
        return new Vector2Int(
            Mathf.Clamp(cell.x, 0, gridSize.x - 1),
            Mathf.Clamp(cell.y, 0, gridSize.y - 1));
    }

    private bool IsInsideGrid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < gridSize.x &&
               cell.y >= 0 && cell.y < gridSize.y;
    }

    private void TryStartBufferedMove()
    {
        if (!hasBufferedMove)
        {
            return;
        }

        Vector2Int nextCell = currentCell + bufferedMove;
        hasBufferedMove = false;

        if (!IsInsideGrid(nextCell))
        {
            return;
        }

        StartCoroutine(JumpToCell(nextCell));
    }

    private void SetBufferedMove(Vector2Int move)
    {
        bufferedMove = move;
        hasBufferedMove = true;
    }

    public void SetControlsLocked(bool locked)
    {
        if (locked)
        {
            controlLockCount++;
            hasBufferedMove = false;
            bufferedMove = Vector2Int.zero;
            return;
        }

        controlLockCount = Mathf.Max(0, controlLockCount - 1);
    }

    private bool TryBuildTileGrid()
    {
        int expectedTileCount = gridSize.x * gridSize.y;
        var tiles = new List<Transform>(expectedTileCount);

        foreach (Transform child in gameplayTilesRoot)
        {
            tiles.Add(child);
        }

        if (tiles.Count != expectedTileCount)
        {
            Debug.LogError(
                $"PlayerController expected {expectedTileCount} tiles under {gameplayTilesRoot.name}, but found {tiles.Count}.",
                this);
            return false;
        }

        tiles.Sort(CompareTiles);
        tileGrid = new Transform[gridSize.x, gridSize.y];

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                tileGrid[x, y] = tiles[(y * gridSize.x) + x];
            }
        }

        return true;
    }

    private static int CompareTiles(Transform a, Transform b)
    {
        float yDifference = a.position.y - b.position.y;
        if (Mathf.Abs(yDifference) > 0.01f)
        {
            return yDifference.CompareTo(0f);
        }

        return a.position.x.CompareTo(b.position.x);
    }
}
