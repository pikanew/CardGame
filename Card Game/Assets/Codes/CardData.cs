using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    [SerializeField] private int price;
    [SerializeField] private int power;
    [SerializeField] private string cardName;
    [SerializeField] private CardPattern addedPawns;
    [SerializeField] private CardPattern effectTiles;
    [SerializeField] private Sprite artwork;
    [SerializeField] private Sprite player2Artwork;

    [SerializeField] private bool legendary;

    public bool Legendary => legendary;


    [SerializeField] private string cardId;
    [SerializeField, TextArea(2, 5)]
    private string description;

    public string Description => description;

    public string CardId => cardId;

    [SerializeField] private List<AbilityData> abilities = new();

    public IReadOnlyList<AbilityData> Abilities => abilities;

    public Sprite Artwork => artwork;
    public int Price => price;
    public int Power => power;
    public string Name => cardName;
    public CardPattern AddedPawns => addedPawns;
    public CardPattern EffectTiles => effectTiles;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    
    }

    // Update is called once per frame
    void Update()
    {

    }

    public Sprite GetArtwork(int playerNumber)
    {
        if (playerNumber == 2 && player2Artwork != null)
            return player2Artwork;

        return Artwork;
    }

    public CardData(CardData other)
    {
        this.price = other.price;
        this.power = other.power;
        this.name = other.name;
        this.addedPawns = other.addedPawns;
        this.effectTiles = other.effectTiles;
    }
}
