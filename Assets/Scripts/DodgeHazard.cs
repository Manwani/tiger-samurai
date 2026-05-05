using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class DodgeHazard : MonoBehaviour
{
    public enum LineDirection
    {
        Horizontal,
        Vertical
    }

    [SerializeField] private Transform targetTile;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private LivesController livesController;
    [SerializeField] private LineDirection lineDirection;
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

    private LineRenderer firstLine;
    private LineRenderer secondLine;
    private Bounds tileBounds;
    private Material lineMaterial;
    private bool hasDealtDamage;
    private bool hasPlayerEscaped;

    public Transform TargetTile => targetTile;

    public void Initialize(
        Transform tile,
        PlayerController controller,
        LivesController targetLivesController,
        Material material,
        LineDirection direction,
        float targetWarningDuration,
        float targetCollapseDuration,
        float targetLingerDuration,
        float targetLineWidth,
        float targetEdgeInsetPercent,
        float targetZOffset,
        Color targetWarningColor,
        Color targetCollapseColor,
        int targetSortingOrder,
        int targetDamage)
    {
        targetTile = tile;
        playerController = controller;
        livesController = targetLivesController;
        lineMaterial = material;
        lineDirection = direction;
        warningDuration = Mathf.Max(0f, targetWarningDuration);
        collapseDuration = Mathf.Max(0.01f, targetCollapseDuration);
        lingerDuration = Mathf.Max(0f, targetLingerDuration);
        lineWidth = Mathf.Max(0.001f, targetLineWidth);
        edgeInsetPercent = Mathf.Clamp(targetEdgeInsetPercent, -0.5f, 0.45f);
        zOffset = targetZOffset;
        warningColor = targetWarningColor;
        collapseColor = targetCollapseColor;
        sortingOrder = targetSortingOrder;
        damage = Mathf.Max(1, targetDamage);

        if (targetTile == null || playerController == null)
        {
            Debug.LogWarning("DodgeHazard is missing a target tile or player reference.", this);
            Destroy(gameObject);
            return;
        }

        BuildLines();
        playerController.StartedMoveFromTile += HandlePlayerStartedMove;
        StartCoroutine(HazardRoutine());
    }

    private void OnDestroy()
    {
        if (playerController != null)
        {
            playerController.StartedMoveFromTile -= HandlePlayerStartedMove;
        }
    }

    private IEnumerator HazardRoutine()
    {
        UpdateLinePositions(0f);
        ApplyLineColor(warningColor);

        if (warningDuration > 0f)
        {
            yield return new WaitForSeconds(warningDuration);
        }

        float elapsed = 0f;
        while (elapsed < collapseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / collapseDuration);
            UpdateLinePositions(t);
            ApplyLineColor(Color.Lerp(warningColor, collapseColor, t));
            yield return null;
        }

        UpdateLinePositions(1f);
        ApplyLineColor(collapseColor);
        TryDamageIfPlayerStayed();

        if (lingerDuration > 0f)
        {
            yield return new WaitForSeconds(lingerDuration);
        }

        Destroy(gameObject);
    }

    private void HandlePlayerStartedMove(Transform fromTile, Transform toTile, Vector2Int moveDirection)
    {
        if (hasDealtDamage || hasPlayerEscaped || fromTile != targetTile)
        {
            return;
        }

        if (IsUnsafeMoveDirection(moveDirection))
        {
            playerController.CancelPendingMove();
            playerController.PlayHazardKnockback(moveDirection);
            DealDamage($"dodge hazard unsafe {lineDirection} move from {GetTileName(targetTile)}");
            Destroy(gameObject);
            return;
        }

        hasPlayerEscaped = true;
    }

    private bool IsUnsafeMoveDirection(Vector2Int moveDirection)
    {
        return lineDirection == LineDirection.Vertical
            ? moveDirection.x != 0
            : moveDirection.y != 0;
    }

    private void TryDamageIfPlayerStayed()
    {
        if (hasDealtDamage || hasPlayerEscaped || playerController == null)
        {
            return;
        }

        if (playerController.CurrentTileTransform == targetTile)
        {
            DealDamage($"dodge hazard collapse on {GetTileName(targetTile)}");
        }
    }

    private void DealDamage(string damageReason)
    {
        if (hasDealtDamage)
        {
            return;
        }

        hasDealtDamage = true;
        if (livesController == null)
        {
            livesController = FindFirstObjectByType<LivesController>();
        }

        for (int i = 0; i < damage; i++)
        {
            livesController?.LoseLife(damageReason);
        }
    }

    private static string GetTileName(Transform tile)
    {
        return tile != null ? tile.name : "unknown tile";
    }

    private void BuildLines()
    {
        SpriteRenderer tileRenderer = targetTile.GetComponent<SpriteRenderer>();
        tileBounds = tileRenderer != null
            ? tileRenderer.bounds
            : new Bounds(targetTile.position, Vector3.one);

        firstLine = CreateLineRenderer("DodgeLineA");
        secondLine = CreateLineRenderer("DodgeLineB");
    }

    private LineRenderer CreateLineRenderer(string lineName)
    {
        GameObject lineObject = new(lineName);
        lineObject.transform.SetParent(transform, false);

        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.numCapVertices = 4;
        lineRenderer.sharedMaterial = lineMaterial;
        lineRenderer.sortingOrder = sortingOrder;
        return lineRenderer;
    }

    private void UpdateLinePositions(float collapseProgress)
    {
        if (firstLine == null || secondLine == null)
        {
            return;
        }

        float t = Mathf.Clamp01(collapseProgress);
        float z = tileBounds.center.z + zOffset;
        float xInset = tileBounds.size.x * edgeInsetPercent;
        float yInset = tileBounds.size.y * edgeInsetPercent;
        float minX = tileBounds.min.x + xInset;
        float maxX = tileBounds.max.x - xInset;
        float minY = tileBounds.min.y + yInset;
        float maxY = tileBounds.max.y - yInset;

        if (lineDirection == LineDirection.Horizontal)
        {
            float topY = Mathf.Lerp(maxY, tileBounds.center.y, t);
            float bottomY = Mathf.Lerp(minY, tileBounds.center.y, t);
            SetLine(firstLine, new Vector3(minX, topY, z), new Vector3(maxX, topY, z));
            SetLine(secondLine, new Vector3(minX, bottomY, z), new Vector3(maxX, bottomY, z));
            return;
        }

        float leftX = Mathf.Lerp(minX, tileBounds.center.x, t);
        float rightX = Mathf.Lerp(maxX, tileBounds.center.x, t);
        SetLine(firstLine, new Vector3(leftX, minY, z), new Vector3(leftX, maxY, z));
        SetLine(secondLine, new Vector3(rightX, minY, z), new Vector3(rightX, maxY, z));
    }

    private static void SetLine(LineRenderer lineRenderer, Vector3 start, Vector3 end)
    {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    private void ApplyLineColor(Color color)
    {
        ApplyLineColor(firstLine, color);
        ApplyLineColor(secondLine, color);
    }

    private static void ApplyLineColor(LineRenderer lineRenderer, Color color)
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}
