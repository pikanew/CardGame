using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDeck", menuName = "Card Game/Deck")]
public class DeckData : ScriptableObject
{
    [SerializeField] private string deckName;
    [SerializeField] private List<CardData> cards = new();

    public string DeckName => deckName;
    public IReadOnlyList<CardData> Cards => cards;
}
