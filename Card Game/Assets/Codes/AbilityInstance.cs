using UnityEngine;
using System;
public class AbilityInstance
{
    public AbilityData Data { get; }
    public Card Source { get; }
    public Player Owner { get; }
    public Board Board { get; }

    private Action unregister;

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
            triggeringCard);

        Data.Effect.Execute(context);
    }
}
