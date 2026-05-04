using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class HealthPickup : MonoBehaviour
{
    [SerializeField] private Transform targetTile;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private LivesController livesController;
    [SerializeField, Min(1)] private int livesRestored = 1;
    [SerializeField, Min(0f)] private float lifetimeSeconds;

    private Coroutine lifetimeCoroutine;
    private bool hasBeenCollected;
    private bool isListeningForLanding;

    public Transform TargetTile => targetTile;

    public void Initialize(
        Transform pickupTile,
        PlayerController controller,
        LivesController targetLivesController,
        float targetLifetimeSeconds = 0f)
    {
        targetTile = pickupTile;
        playerController = controller;
        livesController = targetLivesController;
        lifetimeSeconds = Mathf.Max(0f, targetLifetimeSeconds);

        SubscribeToPlayerLanding();
        TryCollectIfPlayerIsAlreadyHere();
        StartLifetimeIfNeeded();
    }

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (livesController == null)
        {
            livesController = FindFirstObjectByType<LivesController>();
        }

        SubscribeToPlayerLanding();
        TryCollectIfPlayerIsAlreadyHere();
        StartLifetimeIfNeeded();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerLanding();
        StopLifetime();
    }

    private void HandlePlayerLanded(Transform landedTile)
    {
        if (landedTile == targetTile)
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (hasBeenCollected)
        {
            return;
        }

        hasBeenCollected = true;
        livesController?.RestoreLife(livesRestored);
        Destroy(gameObject);
    }

    private void StartLifetimeIfNeeded()
    {
        if (lifetimeSeconds <= 0f || lifetimeCoroutine != null)
        {
            return;
        }

        lifetimeCoroutine = StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetimeSeconds);
        Destroy(gameObject);
    }

    private void StopLifetime()
    {
        if (lifetimeCoroutine == null)
        {
            return;
        }

        StopCoroutine(lifetimeCoroutine);
        lifetimeCoroutine = null;
    }

    private void TryCollectIfPlayerIsAlreadyHere()
    {
        if (playerController != null && playerController.CurrentTileTransform == targetTile)
        {
            Collect();
        }
    }

    private void SubscribeToPlayerLanding()
    {
        if (isListeningForLanding || playerController == null)
        {
            return;
        }

        playerController.LandedOnTile += HandlePlayerLanded;
        isListeningForLanding = true;
    }

    private void UnsubscribeFromPlayerLanding()
    {
        if (!isListeningForLanding || playerController == null)
        {
            return;
        }

        playerController.LandedOnTile -= HandlePlayerLanded;
        isListeningForLanding = false;
    }
}
