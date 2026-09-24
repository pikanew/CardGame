using UnityEngine;

public class Card : MonoBehaviour
{
    [SerializeField] public CardData data { get; private set; }


    private int currentPower;
    public Tile tile;

    public int Power => currentPower;
    public int Price => data.Price;
    public string CardName => data.name;

    public void Initialize(CardData cardData)
    {
        data = cardData;
        currentPower = data.Power;
    }

    public void ChangePower(int amount)
    {
        currentPower += amount;
    }

    public void DestroyCard()
    {
        tile = null;
        Destroy(this);
    }

    public virtual void CardPlayed(Tile playingTile)
    {
        tile = playingTile;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        data = new CardData(data);
    }

    // Update is called once per frame
    void Update()
    {

    }


}
