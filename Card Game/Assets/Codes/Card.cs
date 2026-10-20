using System.Collections.Generic;
using UnityEngine;

public class Card : MonoBehaviour
{
    [SerializeField] public CardData data { get; private set; }

    private readonly List<AbilityInstance> abilities = new();


    private int currentPower;
    public Tile tile;

    public int Power => currentPower;
    public int Price => data.Price;
    public string CardName => data.name;

    public event System.Action<int> PowerChanged;

    public void Initialize(CardData cardData)
    {
        data = cardData;
        currentPower = data.Power;
    }

    public void ChangePower(int amount)
    {
        currentPower += amount;

        PowerChanged?.Invoke(currentPower);
    }

    public void DestroyCard()
    {
        tile = null;
        Destroy(gameObject);
    }

    public virtual void CardPlayed(Tile playingTile)
    {
        tile = playingTile;
    }

    public void RegisterAbilities(Board board, Player owner)
    {
        foreach (AbilityData abilityData in data.Abilities)
        {
            AbilityInstance ability = new AbilityInstance(
                abilityData,
                this,
                owner,
                board
            );

            abilities.Add(ability);
            ability.Register();
        }
    }

    public void UnregisterAbilities()
    {
        foreach (AbilityInstance ability in abilities)
        {
            ability.Unregister();
        }

        abilities.Clear();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }


}
