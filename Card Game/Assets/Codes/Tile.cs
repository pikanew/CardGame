using UnityEngine;

public class Tile : MonoBehaviour
{
    [SerializeField] private int row;
    [SerializeField] private int column;
    [SerializeField] private int pawns;
    [SerializeField] private Card card;
    [SerializeField] private Player owner;

    public int Row => row;
    public int Column => column;
    public int Pawns => pawns;
    public Card Card => card;
    public Player Owner => owner;

    public void Initialize(int row, int column)
    {
        this.row = row;
        this.column = column;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddPawn(Player player)
    {
        if(Owner == null)//if the tile is unowned, set the owner to the player and add a pawn
        {
            this.owner = player;
            this.pawns++;
        }
        else if(player == Owner)//if the player already owns this tile, add a pawn
        {
            this.pawns++;
        }
        else//if the other player owns this tile, keep the same pawn count, but switch ownership
        {
            this.owner=player; 
        }
    }

    public void PlaceCard(Card card, Player player)
    {
        this.card = card;
        this.owner = player;
    }

    public void RemoveCard()
    {
        this.card=null;
    }
}
