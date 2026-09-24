using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    [SerializeField] private int price;
    [SerializeField] private int power;
    [SerializeField] private string cardName;
    [SerializeField] private CardPattern addedPawns;
    [SerializeField] private CardPattern effectTiles;

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

    public CardData(CardData other)
    {
        this.price = other.price;
        this.power = other.power;
        this.name = other.name;
        this.addedPawns = other.addedPawns;
        this.effectTiles = other.effectTiles;
    }
}
