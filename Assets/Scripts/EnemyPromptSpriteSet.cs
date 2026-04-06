using UnityEngine;

[System.Serializable]
public struct EnemyPromptSpriteSet
{
    [SerializeField] private Sprite up;
    [SerializeField] private Sprite down;
    [SerializeField] private Sprite left;
    [SerializeField] private Sprite right;

    public Sprite Up => up;
    public Sprite Down => down;
    public Sprite Left => left;
    public Sprite Right => right;
}
