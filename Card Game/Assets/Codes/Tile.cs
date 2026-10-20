using UnityEngine;

public class Tile : MonoBehaviour
{

    [SerializeField] private int row;
    [SerializeField] private int column;
    [SerializeField] private int pawns;
    [SerializeField] private Card card;
    [SerializeField] private Player owner;
    [SerializeField] private Board boardPanel;
    [SerializeField] private TileView ui;

    public TileView UI => ui;
    public int Row => row;
    public int Column => column;
    public int Pawns => pawns;
    public Card Card => card;
    public Player Owner => owner;

    public Board Board => boardPanel;

    public void Initialize(int row, int column, Board board, TileView tileView)
    {
        this.row = row;
        this.column = column;
        this.boardPanel = board;
        this.ui = tileView;

        ui.Initialize(this, board.GameManager);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddPawns(Player player, int amount)
    {
        if (card != null) { return; }//dont add a pawn to a tile that has a card on it

        if(Owner == null)//if the tile is unowned, set the owner to the player and add a pawn
        {
            owner = player;
            pawns = Mathf.Min(3, pawns + amount);
        }
        else if(player == Owner)//if the player already owns this tile, add a pawn
        {
            pawns = Mathf.Min(3, pawns + amount);
        }
        else//if the other player owns this tile, keep the same pawn count, but switch ownership
        {
            owner =player; 
        }
        ui.FixSprite();
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
