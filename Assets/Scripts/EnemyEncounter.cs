using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class EnemyEncounter : MonoBehaviour
{
    private enum SequenceInput
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    private static readonly SequenceInput[] RequiredSequence =
    {
        SequenceInput.Up,
        SequenceInput.Up,
        SequenceInput.Down,
        SequenceInput.Up
    };

    [SerializeField] private Vector3 popupOffset = new(0f, 1.1f, 0f);
    [SerializeField] private int popupSortingOrder = 10;
    [SerializeField] private float defeatDelay = 0.18f;
    [SerializeField] private bool logSequenceEvents = true;

    private PlayerController playerController;
    private Transform targetTile;
    private EnemyPromptSpriteSet promptSprites;
    private EnemySequencePopup popup;
    private int sequenceIndex;
    private bool encounterActive;
    private bool isDefeated;
    private Sprite[] cachedSequenceSprites;

    public void Initialize(PlayerController controller, Transform tile, EnemyPromptSpriteSet sprites)
    {
        playerController = controller;
        targetTile = tile;
        promptSprites = sprites;

        if (playerController == null || targetTile == null)
        {
            Debug.LogError("EnemyEncounter needs both a player and target tile reference.", this);
            enabled = false;
            return;
        }

        playerController.LandedOnTile += HandlePlayerLanded;
    }

    private void Update()
    {
        if (!encounterActive || isDefeated)
        {
            return;
        }

        SequenceInput input = ReadSequenceInput();
        if (input == SequenceInput.None)
        {
            return;
        }

        HandleSequenceInput(input);
    }

    private void OnDestroy()
    {
        if (playerController != null)
        {
            playerController.LandedOnTile -= HandlePlayerLanded;

            if (encounterActive)
            {
                playerController.SetControlsLocked(false);
            }
        }

        if (popup != null)
        {
            Destroy(popup.gameObject);
        }
    }

    private void HandlePlayerLanded(Transform landedTile)
    {
        if (encounterActive || isDefeated || landedTile != targetTile)
        {
            return;
        }

        BeginEncounter();
    }

    private void BeginEncounter()
    {
        encounterActive = true;
        sequenceIndex = 0;
        playerController.SetControlsLocked(true);

        EnsurePopup();
        UpdatePopupDisplay();
        LogSequenceEvent("Enemy encounter started.");
    }

    private void HandleSequenceInput(SequenceInput input)
    {
        if (input == RequiredSequence[sequenceIndex])
        {
            sequenceIndex++;

            if (sequenceIndex >= RequiredSequence.Length)
            {
                DefeatEnemy();
                return;
            }

            UpdatePopupDisplay();
            LogSequenceEvent($"Correct input: {input}");
            return;
        }

        sequenceIndex = input == RequiredSequence[0] ? 1 : 0;
        if (popup != null)
        {
            popup.FlashFailure();
        }

        UpdatePopupDisplay();
        LogSequenceEvent($"Wrong input: {input}. Sequence reset.");
    }

    private void DefeatEnemy()
    {
        isDefeated = true;
        encounterActive = false;
        playerController.SetControlsLocked(false);
        UpdatePopupDisplay();

        if (popup != null)
        {
            popup.FlashSuccess();
        }

        LogSequenceEvent("Enemy defeated.");
        StartCoroutine(DestroyAfterVictory());
    }

    private void EnsurePopup()
    {
        if (popup != null)
        {
            return;
        }

        GameObject popupObject = new("EnemySequencePopup");
        popupObject.transform.SetParent(transform.parent, true);

        popup = popupObject.AddComponent<EnemySequencePopup>();
        popup.Initialize(transform, popupOffset, popupSortingOrder);
    }

    private void UpdatePopupDisplay()
    {
        if (popup == null)
        {
            return;
        }

        popup.SetSequence(GetSequenceSprites(), sequenceIndex);
    }

    private static SequenceInput ReadSequenceInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return SequenceInput.None;
        }

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
        {
            return SequenceInput.Up;
        }

        if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
        {
            return SequenceInput.Down;
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
        {
            return SequenceInput.Left;
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
        {
            return SequenceInput.Right;
        }

        return SequenceInput.None;
    }

    private void LogSequenceEvent(string message)
    {
        if (!logSequenceEvents)
        {
            return;
        }

        Debug.Log(message, this);
    }

    private Sprite[] GetSequenceSprites()
    {
        if (cachedSequenceSprites != null && cachedSequenceSprites.Length == RequiredSequence.Length)
        {
            return cachedSequenceSprites;
        }

        cachedSequenceSprites = new Sprite[RequiredSequence.Length];
        for (int i = 0; i < RequiredSequence.Length; i++)
        {
            cachedSequenceSprites[i] = GetPromptSprite(RequiredSequence[i]);
        }

        return cachedSequenceSprites;
    }

    private Sprite GetPromptSprite(SequenceInput input)
    {
        switch (input)
        {
            case SequenceInput.Up:
                return promptSprites.Up;
            case SequenceInput.Down:
                return promptSprites.Down;
            case SequenceInput.Left:
                return promptSprites.Left;
            case SequenceInput.Right:
                return promptSprites.Right;
            default:
                return null;
        }
    }

    private System.Collections.IEnumerator DestroyAfterVictory()
    {
        float delay = Mathf.Max(0f, defeatDelay);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        Destroy(gameObject);
    }
}
