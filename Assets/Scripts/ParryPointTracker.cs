using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class ParryPointTracker : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField, Range(0f, 1f)] private float parryWindowMinNormalizedSize = 0.35f;
    [SerializeField, Range(0f, 1f)] private float parryWindowMaxNormalizedSize = 0.55f;
    [SerializeField] private bool logParryAttempts = true;
    [SerializeField] private int parryPoints;

    private readonly List<ShrinkingCircle> activeCircles = new();

    public int ParryPoints => parryPoints;

    private void Start()
    {
        if (playerController == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                playerController = player.GetComponent<PlayerController>();
            }
        }

        if (playerController == null)
        {
            Debug.LogError("ParryPointTracker needs a PlayerController reference.", this);
            enabled = false;
            return;
        }

        NormalizeParryWindow();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
        {
            return;
        }

        TryParryCurrentTile();
    }

    public void RegisterCircle(ShrinkingCircle circle)
    {
        if (circle == null || activeCircles.Contains(circle))
        {
            return;
        }

        activeCircles.Add(circle);
    }

    public void UnregisterCircle(ShrinkingCircle circle)
    {
        if (circle == null)
        {
            return;
        }

        activeCircles.Remove(circle);
    }

    private void OnValidate()
    {
        NormalizeParryWindow();
    }

    private void TryParryCurrentTile()
    {
        Transform playerTile = playerController.CurrentTileTransform;
        if (playerTile == null)
        {
            LogParryEvent("Parry miss. The player is not on a valid tile.");
            return;
        }

        ShrinkingCircle bestCircle = FindBestCircleOnTile(playerTile);
        if (bestCircle == null)
        {
            LogParryEvent("Parry miss. There is no active circle on the player's tile.");
            return;
        }

        if (!IsInsideParryWindow(bestCircle.NormalizedSize))
        {
            LogParryEvent($"Parry miss. Circle size {bestCircle.NormalizedSize:F2} is outside the parry window.");
            return;
        }

        parryPoints++;
        bestCircle.Consume();
        LogParryEvent($"Parry! Points: {parryPoints}");
    }

    private ShrinkingCircle FindBestCircleOnTile(Transform tileTransform)
    {
        ShrinkingCircle bestCircle = null;
        float bestDistanceToWindowCenter = float.MaxValue;
        float targetWindowCenter = (parryWindowMinNormalizedSize + parryWindowMaxNormalizedSize) * 0.5f;

        for (int i = activeCircles.Count - 1; i >= 0; i--)
        {
            ShrinkingCircle circle = activeCircles[i];
            if (circle == null)
            {
                activeCircles.RemoveAt(i);
                continue;
            }

            if (circle.TargetTile != tileTransform)
            {
                continue;
            }

            float distanceToWindowCenter = Mathf.Abs(circle.NormalizedSize - targetWindowCenter);
            if (distanceToWindowCenter < bestDistanceToWindowCenter)
            {
                bestDistanceToWindowCenter = distanceToWindowCenter;
                bestCircle = circle;
            }
        }

        return bestCircle;
    }

    private bool IsInsideParryWindow(float normalizedSize)
    {
        return normalizedSize >= parryWindowMinNormalizedSize &&
               normalizedSize <= parryWindowMaxNormalizedSize;
    }

    private void NormalizeParryWindow()
    {
        if (parryWindowMaxNormalizedSize < parryWindowMinNormalizedSize)
        {
            parryWindowMaxNormalizedSize = parryWindowMinNormalizedSize;
        }
    }

    private void LogParryEvent(string message)
    {
        if (!logParryAttempts)
        {
            return;
        }

        Debug.Log(message, this);
    }
}
