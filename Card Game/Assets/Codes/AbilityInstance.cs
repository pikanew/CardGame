using UnityEngine;
using System;
using System.Collections.Generic;

public class AbilityInstance
{
    public AbilityData Data { get; }
    public Card Source { get; }
    public Player Owner { get; }
    public Board Board { get; }

    private Action unregister;

    private readonly Dictionary<Card, int> continuousChanges = new();

    public AbilityInstance(
        AbilityData data,
        Card source,
        Player owner,
        Board board)
    {
        Data = data;
        Source = source;
        Owner = owner;
        Board = board;
    }

    public void Register()
    {
        if (unregister != null)
            return;

        unregister = Data.Trigger.Register(this, Board);
    }

    public void Unregister()
    {
        unregister?.Invoke();
        unregister = null;
    }

    public void Activate(Card triggeringCard = null)
    {
        AbilityContext context = new AbilityContext(
            Source,
            Owner,
            Board,
            triggeringCard
        );

        Data.Effect.Execute(context);
    }

    public void ApplyContinuousEffectTo(Card target)
    {
        if (Data.Effect is not IContinuousPowerEffect effect)
            return;

        if (Source == null ||
            Source.tile == null ||
            Source.tile.Card != Source ||
            target == null ||
            target.tile == null ||
            target.tile.Card != target)
            return;

        // Never apply the same ability twice to the same card.
        if (continuousChanges.ContainsKey(target))
            return;

        AbilityContext context = new AbilityContext(
            Source,
            Owner,
            Board,
            target
        );

        int amount = effect.GetPowerChange(context, target);

        if (amount == 0)
            return;

        int previousPower = target.Power;

        Board.ChangeCardPower(target, amount);

        // The target might have been destroyed by the effect.
        if (target == null ||
            target.tile == null ||
            target.tile.Card != target)
            return;

        int actualChange = target.Power - previousPower;

        if (actualChange != 0)
            continuousChanges[target] = actualChange;
    }

    public void ApplyContinuousEffects()
    {
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                Card target = Board.GetTile(row, col).Card;

                if (target != null)
                    ApplyContinuousEffectTo(target);
            }
        }
    }

    public void ForgetContinuousTarget(Card target)
    {
        continuousChanges.Remove(target);
    }

    public void RemoveContinuousEffects()
    {
        // Copy and clear first, since reversing an effect
        // can trigger additional board events.
        var changes = new Dictionary<Card, int>(continuousChanges);

        continuousChanges.Clear();

        foreach (var entry in changes)
        {
            Card target = entry.Key;

            if (target == null ||
                target.tile == null ||
                target.tile.Card != target)
                continue;

            Board.ChangeCardPower(target, -entry.Value);
        }
    }
}