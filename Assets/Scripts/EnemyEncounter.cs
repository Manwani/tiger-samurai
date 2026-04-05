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
    [SerializeField] private Color popupColor = Color.white;
    [SerializeField] private int popupFontSize = 48;
    [SerializeField] private float popupCharacterSize = 0.08f;
    [SerializeField] private int popupSortingOrder = 10;
    [SerializeField] private bool logSequenceEvents = true;

    private PlayerController playerController;
    private Transform targetTile;
    private TextMesh popupText;
    private Transform popupTransform;
    private int sequenceIndex;
    private bool encounterActive;
    private bool isDefeated;

    public void Initialize(PlayerController controller, Transform tile)
    {
        playerController = controller;
        targetTile = tile;

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

        UpdatePopupPosition();

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

        if (popupTransform != null)
        {
            Destroy(popupTransform.gameObject);
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
        UpdatePopupText();
        UpdatePopupPosition();
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

            UpdatePopupText();
            LogSequenceEvent($"Correct input: {input}");
            return;
        }

        sequenceIndex = input == RequiredSequence[0] ? 1 : 0;
        UpdatePopupText();
        LogSequenceEvent($"Wrong input: {input}. Sequence reset.");
    }

    private void DefeatEnemy()
    {
        isDefeated = true;
        encounterActive = false;
        playerController.SetControlsLocked(false);
        LogSequenceEvent("Enemy defeated.");
        Destroy(gameObject);
    }

    private void EnsurePopup()
    {
        if (popupText != null)
        {
            popupText.gameObject.SetActive(true);
            return;
        }

        GameObject popupObject = new("EnemySequencePopup");
        popupTransform = popupObject.transform;
        popupTransform.SetParent(transform.parent, true);

        popupText = popupObject.AddComponent<TextMesh>();
        popupText.anchor = TextAnchor.MiddleCenter;
        popupText.alignment = TextAlignment.Center;
        popupText.color = popupColor;
        popupText.fontSize = popupFontSize;
        popupText.characterSize = popupCharacterSize;

        Font font = GetPopupFont();
        if (font != null)
        {
            popupText.font = font;

            MeshRenderer meshRenderer = popupObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = font.material;
                meshRenderer.sortingOrder = popupSortingOrder;
            }
        }
    }

    private void UpdatePopupPosition()
    {
        if (popupTransform == null)
        {
            return;
        }

        popupTransform.position = transform.position + popupOffset;
    }

    private void UpdatePopupText()
    {
        if (popupText == null)
        {
            return;
        }

        popupText.text = BuildSequenceDisplay();
    }

    private string BuildSequenceDisplay()
    {
        string display = string.Empty;

        for (int i = 0; i < RequiredSequence.Length; i++)
        {
            if (i > 0)
            {
                display += " ";
            }

            string token = GetDisplayToken(RequiredSequence[i]);
            if (i < sequenceIndex)
            {
                display += "(" + token + ")";
            }
            else if (i == sequenceIndex)
            {
                display += "[" + token + "]";
            }
            else
            {
                display += token;
            }
        }

        return display;
    }

    private static string GetDisplayToken(SequenceInput input)
    {
        switch (input)
        {
            case SequenceInput.Up:
                return "^";
            case SequenceInput.Down:
                return "v";
            case SequenceInput.Left:
                return "<";
            case SequenceInput.Right:
                return ">";
            default:
                return "?";
        }
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

    private static Font GetPopupFont()
    {

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
