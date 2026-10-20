using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "OnPlayedTrigger", menuName = "Card Game/Triggers/On Played")]
public class OnPlayedTrigger : AbilityTrigger
{
    public override Action Register(AbilityInstance ability, Board board)
    {
        void Handler(Card card, Tile tile, Player player)
        {
            if (card == ability.Source)
            {
                ability.Activate(card);
            }
        }

        board.CardPlaced += Handler;

        return () => board.CardPlaced -= Handler;
    }
}
