using UnityEngine;

[CreateAssetMenu(fileName = "NewAbility", menuName = "Card Game/Ability")]
public class AbilityData : ScriptableObject
{
    [SerializeField] private AbilityTrigger trigger;
    [SerializeField] private CardEffect effect;

    public AbilityTrigger Trigger => trigger;
    public CardEffect Effect => effect;
}
