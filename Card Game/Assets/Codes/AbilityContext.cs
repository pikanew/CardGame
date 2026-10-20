using UnityEngine;

public class AbilityContext
{
    public Card Source { get; }
    public Player Owner { get; }
    public Board Board { get; }
    public Card TriggeringCard { get; }

    public AbilityContext(
        Card source,
        Player owner,
        Board board,
        Card triggeringCard = null)
    {
        Source = source;
        Owner = owner;
        Board = board;
        TriggeringCard = triggeringCard;
    }
}
