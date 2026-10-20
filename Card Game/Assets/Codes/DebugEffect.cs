using UnityEngine;

[CreateAssetMenu(
    fileName = "DebugEffect",
    menuName = "Card Game/Effects/Debug")]
public class DebugEffect : CardEffect
{
    public override void Execute(AbilityContext context)
    {
        Debug.Log($"{context.Source.CardName}'s ability activated!");
    }
}
