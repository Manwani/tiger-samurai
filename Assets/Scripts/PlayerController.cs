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
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private SpriteRenderer playerSpriteRenderer;
    [SerializeField] private string parryAnimationTrigger = "Parry";
    [SerializeField] private string dashAnimationTrigger = "Dash";
    [SerializeField, Min(0f)] private float hazardKnockbackDistance = 0.28f;
    [SerializeField, Min(0.01f)] private float hazardKnockbackOutDuration = 0.045f;
    [SerializeField, Min(0.01f)] private float hazardKnockbackReturnDuration = 0.1f;

    private Vector2Int currentCell;
    private bool isMoving;
    private float baseZ;
    private Transform[,] tileGrid;
    private bool hasBufferedMove;
    private Vector2Int bufferedMove;
    private int controlLockCount;
    private bool normalSpriteFlipX;
    private bool moveCancelRequested;
    private Coroutine hazardKnockbackCoroutine;

    public event Action<Transform, Transform, Vector2Int> StartedMoveFromTile;
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
        ResolvePlayerAnimator();
        ResolvePlayerSpriteRenderer();
        if (playerSpriteRenderer != null)
        {
            normalSpriteFlipX = playerSpriteRenderer.flipX;
        }
    }

    private void Update()
    {
        PlayParryAnimationOnInput();

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

    private void OnValidate()
    {
        hazardKnockbackDistance = Mathf.Max(0f, hazardKnockbackDistance);
        hazardKnockbackOutDuration = Mathf.Max(0.01f, hazardKnockbackOutDuration);
        hazardKnockbackReturnDuration = Mathf.Max(0.01f, hazardKnockbackReturnDuration);
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

    private void PlayParryAnimationOnInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
        {
            return;
        }

        PlayAnimationTrigger(parryAnimationTrigger, "parry");
    }

    private void PlayAnimationTrigger(string triggerName, string animationName)
    {
        ResolvePlayerAnimator();
        if (playerAnimator == null || string.IsNullOrEmpty(triggerName))
        {
            return;
        }

        int triggerHash = Animator.StringToHash(triggerName);
        if (!HasAnimatorTrigger(triggerHash))
        {
            Debug.LogWarning($"Player Animator does not have a '{triggerName}' trigger for the {animationName} animation.", this);
            return;
        }

        playerAnimator.ResetTrigger(triggerHash);
        playerAnimator.SetTrigger(triggerHash);
    }

    private bool HasAnimatorTrigger(int triggerHash)
    {
        if (playerAnimator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = playerAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.type == AnimatorControllerParameterType.Trigger &&
                parameter.nameHash == triggerHash)
            {
                return true;
            }
        }

        return false;
    }

    private void ResolvePlayerAnimator()
    {
        if (playerAnimator != null)
        {
            return;
        }

        playerAnimator = GetComponentInChildren<Animator>();
    }

    private void ResolvePlayerSpriteRenderer()
    {
        if (playerSpriteRenderer != null)
        {
            return;
        }

        playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private IEnumerator JumpToCell(Vector2Int targetCell, bool restoreDashFacing)
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
        if (restoreDashFacing)
        {
            RestoreNormalFacing();
        }

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

        Vector2Int moveDirection = bufferedMove;
        Vector2Int nextCell = currentCell + moveDirection;
        hasBufferedMove = false;

        if (!IsInsideGrid(nextCell))
        {
            return;
        }

        Transform startTile = CurrentTileTransform;
        Transform targetTile = tileGrid[nextCell.x, nextCell.y];
        moveCancelRequested = false;
        StartedMoveFromTile?.Invoke(startTile, targetTile, moveDirection);
        if (moveCancelRequested || Time.timeScale <= 0f)
        {
            moveCancelRequested = false;
            return;
        }

        bool isHorizontalDash = moveDirection.x != 0;
        if (isHorizontalDash)
        {
            SetDashFacing(moveDirection);
            PlayAnimationTrigger(dashAnimationTrigger, "dash");
        }

        StartCoroutine(JumpToCell(nextCell, isHorizontalDash));
    }

    private void SetDashFacing(Vector2Int move)
    {
        ResolvePlayerSpriteRenderer();
        if (playerSpriteRenderer == null)
        {
            return;
        }

        playerSpriteRenderer.flipX = move.x > 0;
    }

    private void RestoreNormalFacing()
    {
        ResolvePlayerSpriteRenderer();
        if (playerSpriteRenderer == null)
        {
            return;
        }

        playerSpriteRenderer.flipX = normalSpriteFlipX;
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

    public void CancelPendingMove()
    {
        moveCancelRequested = true;
        hasBufferedMove = false;
        bufferedMove = Vector2Int.zero;
    }

    public void PlayHazardKnockback(Vector2Int moveDirection)
    {
        if (moveDirection == Vector2Int.zero)
        {
            return;
        }

        if (hazardKnockbackCoroutine != null)
        {
            StopCoroutine(hazardKnockbackCoroutine);
            hazardKnockbackCoroutine = null;
        }

        hazardKnockbackCoroutine = StartCoroutine(HazardKnockbackRoutine(moveDirection));
    }

    private IEnumerator HazardKnockbackRoutine(Vector2Int moveDirection)
    {
        isMoving = true;
        hasBufferedMove = false;
        bufferedMove = Vector2Int.zero;

        Vector3 startPosition = CellToWorld(currentCell);
        Vector3 direction = new(moveDirection.x, moveDirection.y, 0f);
        Vector3 impactPosition = startPosition + direction.normalized * hazardKnockbackDistance;
        impactPosition.z = baseZ;

        yield return MoveBetweenPositions(startPosition, impactPosition, hazardKnockbackOutDuration);
        yield return MoveBetweenPositions(impactPosition, startPosition, hazardKnockbackReturnDuration);

        transform.position = startPosition;
        isMoving = false;
        hasBufferedMove = false;
        bufferedMove = Vector2Int.zero;
        hazardKnockbackCoroutine = null;
    }

    private IEnumerator MoveBetweenPositions(Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(from, to, easedT);
            yield return null;
        }

        transform.position = to;
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
