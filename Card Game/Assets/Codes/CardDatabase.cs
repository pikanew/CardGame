using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CardDatabase", menuName = "Cards/Card Database")]
public class CardDatabase : ScriptableObject
{
    [SerializeField] private List<CardData> cards = new();

    public IReadOnlyList<CardData> Cards => cards;
}
