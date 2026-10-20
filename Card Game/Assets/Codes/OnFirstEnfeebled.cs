using UnityEngine;
using System;

[CreateAssetMenu(
    fileName = "OnFirstEnfeebledTrigger",
    menuName = "Card Game/Triggers/On First Enfeebled")]
public class OnFirstEnfeebledTrigger : AbilityTrigger
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

        board.CardEnfeebled += Handler;

        return () => board.CardEnfeebled -= Handler;
    }
}
