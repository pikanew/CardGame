using System;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class Board : MonoBehaviour
{

    //test
    [SerializeField] private Card cardPrefab;
    [SerializeField] private CardData testCardData;


    Tile[,] board = new Tile[3, 5];
    private Row[] rows = new Row[3];

    //events
    public event Action<Card, Tile, Player> CardPlaced;
    public event Action<Card, Tile, Player> CardDestroyed;

    private const int middleIndex = 2;
    private const int boardHeight = 3;
    private const int boardWidth = 5;
    public Player Player1;
    public Player Player2;

    [SerializeField] Tile tilePrefab;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Initialize();
        Test();
    }

    public void Test()
    {
        Card card = Instantiate(cardPrefab);

        card.Initialize(testCardData);

        PlayCard(card, board[1, 0], Player1);
        for(int i = 0; i < boardHeight; i++)
        {
            Debug.Log($"P1: {rows[i].Player1Sum}, P2: {rows[i].Player2Sum}");
        }
    }

    public void Initialize()
    {
        Player1.Initialize(1);
        Player2.Initialize(2);

        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = new Row();
        }
        for (int i = 0; i < boardHeight; i++)
        {
            for (int j = 0; j < boardWidth; j++)
            {
                Tile tile = Instantiate(tilePrefab, transform);

                tile.Initialize(i, j);

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

}
