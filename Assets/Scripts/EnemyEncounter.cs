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

    private static readonly SequenceInput[] RandomInputPool =
    {
        SequenceInput.Up,
        SequenceInput.Down,
        SequenceInput.Left,
        SequenceInput.Right
    };

    [SerializeField] private Vector3 popupOffset = new(0f, 1.1f, 0f);
    [SerializeField] private int popupSortingOrder = 10;
    [SerializeField, Min(1)] private int requiredSequenceLength = 6;
    [SerializeField] private float defeatDelay = 0.18f;
    [SerializeField] private bool logSequenceEvents = true;

    private PlayerController playerController;
    private Transform targetTile;
    private EnemyPromptSpriteSet promptSprites;
    private EnemySequencePopup popup;
    private int sequenceIndex;
    private bool encounterActive;
    private bool isDefeated;
    private SequenceInput[] requiredSequence = System.Array.Empty<SequenceInput>();
    private Sprite[] cachedSequenceSprites;

    public Transform TargetTile => targetTile;

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

        GenerateRandomSequence();
        playerController.LandedOnTile += HandlePlayerLanded;
    }

    public bool IsOnTile(Transform tile)
    {
        return targetTile == tile;
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

    private void OnValidate()
    {
        requiredSequenceLength = Mathf.Max(1, requiredSequenceLength);
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
        if (requiredSequence == null || requiredSequence.Length == 0)
        {
            GenerateRandomSequence();
        }

        encounterActive = true;
        sequenceIndex = 0;
        playerController.SetControlsLocked(true);

        EnsurePopup();
        UpdatePopupDisplay();
        LogSequenceEvent("Enemy encounter started.");
    }

    private void HandleSequenceInput(SequenceInput input)
    {
        if (requiredSequence == null || requiredSequence.Length == 0)
        {
            GenerateRandomSequence();
        }

        if (input == requiredSequence[sequenceIndex])
        {
            sequenceIndex++;

            if (sequenceIndex >= requiredSequence.Length)
            {
                DefeatEnemy();
                return;
            }

            UpdatePopupDisplay();
            LogSequenceEvent($"Correct input: {input}");
            return;
        }

        sequenceIndex = input == requiredSequence[0] ? 1 : 0;
        if (popup != null)
        {
            popup.FlashFailure();
        }

        UpdatePopupDisplay();
        LogSequenceEvent($"Wrong input: {input}. Sequence reset.");
    }

    private void GenerateRandomSequence()
    {
        int sequenceLength = Mathf.Max(1, requiredSequenceLength);
        requiredSequence = new SequenceInput[sequenceLength];

        for (int i = 0; i < requiredSequence.Length; i++)
        {
            int randomIndex = Random.Range(0, RandomInputPool.Length);
            requiredSequence[i] = RandomInputPool[randomIndex];
        }

        cachedSequenceSprites = null;
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

        if (keyboard.upArrowKey.wasPressedThisFrame)
        {
            return SequenceInput.Up;
        }

        if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            return SequenceInput.Down;
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            return SequenceInput.Left;
        }

        if (keyboard.rightArrowKey.wasPressedThisFrame)
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
        if (requiredSequence == null || requiredSequence.Length == 0)
        {
            GenerateRandomSequence();
        }

        if (cachedSequenceSprites != null && cachedSequenceSprites.Length == requiredSequence.Length)
        {
            return cachedSequenceSprites;
        }

        cachedSequenceSprites = new Sprite[requiredSequence.Length];
        for (int i = 0; i < requiredSequence.Length; i++)
        {
            cachedSequenceSprites[i] = GetPromptSprite(requiredSequence[i]);
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
