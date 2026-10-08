using System;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Board : MonoBehaviour
{



    private Tile[,] board = new Tile[3, 5];
    private Row[] rows = new Row[3];
    [SerializeField] private GameManager gameManager;


    //events
    public event Action<Card, Tile, Player> CardPlaced;
    public event Action<Card, Tile, Player> CardDestroyed;

    private const int middleIndex = 2;
    private const int boardHeight = 3;
    private const int boardWidth = 5;
    public Player Player1;
    public Player Player2;

    [SerializeField] private Transform boardPanel;
    [SerializeField] Tile tilePrefab;

    public GameManager GameManager => gameManager;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Initialize();
    }

    public Tile GetTile(int row, int column)
    {
        return board[row, column];
    }

    public bool CanPlayCard(Card card, Tile tile, Player player)
    {
        if (tile.Card != null)
            return false;

        if (tile.Owner != player)
            return false;

        if (tile.Pawns < card.Price)
            return false;

        return true;
    }



    public void Initialize()
    {

        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = new Row();
        }
        for (int i = 0; i < boardHeight; i++)
        {
            for (int j = 0; j < boardWidth; j++)
            {
                Tile tile = Instantiate(tilePrefab, transform);

                TileView tileView = boardPanel
                .GetChild(i * boardWidth + j)
                .GetComponent<TileView>();

                tile.Initialize(i, j, this, tileView);

                board[i, j] = tile;

                //add starting pawns
                if (j == 0)
                {
                    board[i, j].AddPawn(Player1);
                }
                else if (j == boardWidth - 1)
                {
                    board[i, j].AddPawn(Player2);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayCard(Card card, Tile tile, Player player)
    {
        // Perform the actual game logic
        tile.PlaceCard(card, player);
        card.CardPlayed(tile);

        AddPower(tile.Row, player, card.Power);
        AddPawns(card, tile, player);
        ApplyTileEffects(card, tile, player);

        // Announce that the card has now been played
        CardPlaced?.Invoke(card, tile, player);
    }

    public void AddPower(int row, Player player, int power)
    {
        if (player.Num == 1)
            rows[row].AddP1Sum(power);
        else
            rows[row].AddP2Sum(power);
    }


    public void DestroyCard(Card card)
    {
        Tile tile = card.tile;
        Player player = tile.Owner;

        AddPower(tile.Row, player, -card.Power);

        tile.RemoveCard();

        CardDestroyed?.Invoke(card, tile, player);

        card.DestroyCard();
    }

    public void AddPawns(Card card, Tile tile, Player player)
     { 
        int i = 0,boardRow, boardColumn;
        foreach (PatternRow row in card.data.AddedPawns.rows)
        {
            boardRow = i - middleIndex  + tile.Row;
            for (int j = 0; j < boardWidth; j++)
            {
                boardColumn = j - middleIndex + tile.Column;

                if (row.values[j]==1 && 
                    boardRow >= 0 && boardRow < boardHeight &&
                    boardColumn >= 0 && boardColumn < boardWidth)
                {
                    board[boardRow, boardColumn].AddPawn(player);
                }
            }
            i++;
        }
    }

    public void ApplyTileEffects(Card card, Tile tile, Player player)
    {

    }

    public MovePreview GetMovePreview(Card card, Tile tile, Player player)
    {
        MovePreview preview = new MovePreview();

        int i = 0;

        foreach (PatternRow row in card.data.AddedPawns.rows)
        {
            int boardRow = i - middleIndex + tile.Row;

            for (int j = 0; j < boardWidth; j++)
            {
                int boardColumn = j - middleIndex + tile.Column;

                if (row.values[j] == 1 &&
                    boardRow >= 0 && boardRow < boardHeight &&
                    boardColumn >= 0 && boardColumn < boardWidth)
                {
                    preview.PawnTiles.Add(board[boardRow, boardColumn]);
                }
            }

            i++;
        }

        return preview;
    }

}
