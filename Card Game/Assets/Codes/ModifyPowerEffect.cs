using UnityEngine;

[CreateAssetMenu(
    fileName = "ModifyPowerEffect",
    menuName = "Card Game/Effects/Modify Power")]
public class ModifyPowerEffect : CardEffect
{
    [SerializeField] private TargetType.targetType targetType;

    public override void Execute(AbilityContext context)
    {
        Card source = context.Source;
        Tile sourceTile = source.tile;

        if (sourceTile == null)
            return;

        CardPattern pattern = source.data.EffectTiles;

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                int amount = pattern.rows[i].values[j];

                if (amount == 0)
                    continue;

                Vector2Int position = context.Board.GetPatternPosition(
                    sourceTile,
                    i,
                    j,
                    context.Owner
                );

                int row = position.x;
                int column = position.y;

                if (row < 0 || row >= 3 ||
                    column < 0 || column >= 5)
                    continue;

                Tile targetTile = context.Board.GetTile(row, column);

                if (targetTile.Card == null)
                    continue;

                bool isAlly = targetTile.Owner == context.Owner;

                if (targetType == TargetType.targetType.Allies && !isAlly)
                    continue;

                if (targetType == TargetType.targetType.Enemies && isAlly)
                    continue;

                context.Board.ChangeCardPower(targetTile.Card, amount);
            }
        }
    }
}