using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "WhileInPlayTrigger",
    menuName = "Card Game/Triggers/While In Play")]
public class WhileInPlayTrigger : AbilityTrigger
{
    public override Action Register(AbilityInstance ability, Board board)
    {
        void OnCardPlaced(Card placedCard, Tile tile, Player player)
        {
            if (ability.Source == null ||
                ability.Source.tile == null ||
                ability.Source.tile.Card != ability.Source)
                return;

            if (placedCard == ability.Source)
            {
                // Source just entered play.
                // Apply its effect to all existing cards.
                ability.ApplyContinuousEffects();
            }
            else
            {
                // A new card entered play.
                // Affect only that card.
                ability.ApplyContinuousEffectTo(placedCard);
            }
        }

        void OnCardDestroyed(Card destroyedCard, Tile tile, Player player)
        {
            if (destroyedCard == ability.Source)
            {
                // Source left play: reverse its continuous bonuses.
                ability.RemoveContinuousEffects();
            }
            else
            {
                // Don't keep references to destroyed targets.
                ability.ForgetContinuousTarget(destroyedCard);
            }
        }

        board.CardPlaced += OnCardPlaced;
        board.CardDestroyed += OnCardDestroyed;

        return () =>
        {
            board.CardPlaced -= OnCardPlaced;
            board.CardDestroyed -= OnCardDestroyed;

            ability.RemoveContinuousEffects();
        };
    }
}