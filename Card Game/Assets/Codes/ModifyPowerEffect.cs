using UnityEngine;

[CreateAssetMenu(
    fileName = "ModifyPowerEffect",
    menuName = "Card Game/Effects/Modify Power")]
public class ModifyPowerEffect : CardEffect, IContinuousPowerEffect
{
    [SerializeField] private TargetType.targetType targetType;
    [SerializeField] private bool enhance = true;

    public TargetType.targetType Target => targetType;

    public bool Enhance => enhance;


    public int GetPowerChange(AbilityContext context, Card target)
    {
        if (context.Source == null ||
            context.Source.tile == null ||
            context.Source.tile.Card != context.Source ||
            target == null ||
            target.tile == null ||
            target.tile.Card != target)
            return 0;

        bool isAlly = target.tile.Owner == context.Owner;

        if (targetType == TargetType.targetType.Allies && !isAlly)
            return 0;

        if (targetType == TargetType.targetType.Enemies && isAlly)
            return 0;

        CardPattern pattern = context.Source.data.EffectTiles;

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                Vector2Int position = context.Board.GetPatternPosition(
                    context.Source.tile,
                    i,
                    j,
                    context.Owner
                );

                if (position.x == target.tile.Row &&
                    position.y == target.tile.Column)
                {
                    int multiplier = enhance ? 1 : -1;

                    return Mathf.Abs(pattern.rows[i].values[j])
                        * multiplier;
                }
            }
        }

        return 0;
    }

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
                int multiplier = enhance ? 1 : -1;

                int amount = pattern.rows[i].values[j]*multiplier;

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