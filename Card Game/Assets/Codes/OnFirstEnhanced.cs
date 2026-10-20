using UnityEngine;
using System;

[CreateAssetMenu(
    fileName = "OnFirstEnhancedTrigger",
    menuName = "Card Game/Triggers/On First Enhanced")]
public class OnFirstEnhancedTrigger : AbilityTrigger
{
    public override Action Register(AbilityInstance ability, Board board)
    {
        bool activated = false;

        void Handler(Card card)
        {
            if (activated || card != ability.Source)
                return;

            activated = true;
            ability.Activate(card);
        }

        board.CardEnhanced += Handler;

        return () => board.CardEnhanced -= Handler;
    }
}