using System;
using UnityEngine;

public class Board : MonoBehaviour
{



    private Tile[,] board = new Tile[3, 5];

    private Row[] rows = new Row[3];
    [SerializeField] private RowScoreView[] p1RowScores = new RowScoreView[3];
    [SerializeField] private RowScoreView[] p2RowScores = new RowScoreView[3];

    [SerializeField] private GameManager gameManager;


    //events
    public event Action<Card, Tile, Player> CardPlaced;
    public event Action<Card, Tile, Player> CardDestroyed;
    public event Action<Card> CardEnhanced;
    public event Action<Card> CardEnfeebled;

    private const int middleIndex = 2;
    private const int boardHeight = 3;
    private const int boardWidth = 5;
    public Player Player1;
    public Player Player2;

    [SerializeField] private Transform boardPanel;
    [SerializeField] Tile tilePrefab;

    public GameManager GameManager => gameManager;
    public Row[] Rows => rows;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Initialize();
    }

    public Vector2Int GetPatternPosition(
    Tile sourceTile,
    int patternRow,
    int patternColumn,
    Player player)
    {
        int rowOffset = patternRow - 2;
        int columnOffset = patternColumn - 2;

        if (player == Player2)
        {
            columnOffset = -columnOffset;
        }

        return new Vector2Int(
            sourceTile.Row + rowOffset,
            sourceTile.Column + columnOffset
        );
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
                    board[i, j].AddPawns(Player1, 1);
                }
                else if (j == boardWidth - 1)
                {
                    board[i, j].AddPawns(Player2, 1);
                }
            }
        }
        for (int i = 0; i < boardHeight; i++)
        {
            p1RowScores[i].SetScore(0);
            p2RowScores[i].SetScore(0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayCard(Card card, Tile tile, Player player)
    {
        tile.PlaceCard(card, player);
        card.CardPlayed(tile);

        AddPower(tile.Row, player, card.Power);

        // Register abilities before triggering placement effects
        card.RegisterAbilities(this, player);

        // Apply on-play effects, including card destruction
        CardPlaced?.Invoke(card, tile, player);

        // Add pawns after effects have been resolved
        AddPawns(card, tile, player);
    }

    public void AddPower(int row, Player player, int power)
    {
        if (player.Num == 1)
        {
            rows[row].AddP1Sum(power);
            p1RowScores[row].SetScore(rows[row].P1Sum);
        }
        else
        {
            rows[row].AddP2Sum(power);
            p2RowScores[row].SetScore(rows[row].P2Sum);
        }
    }


    public void DestroyCard(Card card)
    {
        if (card == null || card.tile == null)
            return;

        Tile tile = card.tile;
        Player player = tile.Owner;

        AddPower(tile.Row, player, -card.Power);

        tile.RemoveCard();

        CardDestroyed?.Invoke(card, tile, player);

        card.UnregisterAbilities();

        card.DestroyCard();
    }

    public void AddPawns(Card card, Tile tile, Player player)
    {
        CardPattern pattern = card.data.AddedPawns;

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                int amount = pattern.rows[i].values[j];

                if (amount == 0)
                    continue;

                Vector2Int position = GetPatternPosition(tile, i, j, player);

                if (position.x < 0 || position.x >= boardHeight ||
                    position.y < 0 || position.y >= boardWidth)
                    continue;

                board[position.x, position.y].AddPawns(player, amount);
            }
        }
    }

    public void ApplyTileEffects(Card card, Tile tile, Player player)
    {

    }

    public MovePreview GetMovePreview(Card card, Tile tile, Player player)
    {
        MovePreview preview = new MovePreview();

        CardPattern pattern = card.data.AddedPawns;

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                if (pattern.rows[i].values[j] == 0)
                    continue;

                Vector2Int position = GetPatternPosition(tile, i, j, player);

                if (position.x < 0 || position.x >= boardHeight ||
                    position.y < 0 || position.y >= boardWidth)
                    continue;

                preview.PawnTiles.Add(board[position.x, position.y]);
            }
        }

        return preview;
    }
    public void ChangeCardPower(Card card, int amount)
    {
        if (card == null || card.tile == null || amount == 0)
            return;

        int oldPower = card.Power;
        int newPower = Mathf.Max(0, oldPower + amount);
        int actualChange = newPower - oldPower;

        if (actualChange == 0)
            return;

        card.ChangePower(actualChange);

        AddPower(card.tile.Row, card.tile.Owner, actualChange);

        if (actualChange > 0)
            CardEnhanced?.Invoke(card);
        else
            CardEnfeebled?.Invoke(card);

        // An ability triggered by the power change may have modified
        // the card again or destroyed it.
        if (card != null && card.tile != null && card.Power <= 0)
        {
            DestroyCard(card);
        }
    }
    public (int p1Score, int p2Score) GetFinalScores()
    {
        int p1Score = 0;
        int p2Score = 0;

        foreach (Row row in rows)
        {
            if (row.P1Sum > row.P2Sum)
            {
                p1Score += row.P1Sum;
            }
            else if (row.P2Sum > row.P1Sum)
            {
                p2Score += row.P2Sum;
            }
        }

        return (p1Score, p2Score);
    }

}
