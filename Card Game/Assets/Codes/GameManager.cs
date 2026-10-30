using System.Collections.Generic;
using UnityEngine;
using static TileView;
using static UnityEngine.Rendering.DebugUI.Table;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameManager manager;
    [SerializeField] private Board board;

    [SerializeField] private Player player1;
    [SerializeField] private Player player2;

    [SerializeField] private Card cardPrefab;
    [SerializeField] private CardView cardViewPrefab;

    [SerializeField] private CardDatabase cardDatabase;

    [SerializeField] private Transform p1HandPanel;
    [SerializeField] private Transform p2HandPanel;

    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text p1ScoreText;
    [SerializeField] private TMP_Text p2ScoreText;

    [SerializeField] private MatchSetup matchSetup;

    private GamePhase.gamePhase gamePhase = GamePhase.gamePhase.Mulligan;

    private readonly HashSet<Card> mulliganSelection = new();
    private Player mulliganPlayer;
    [SerializeField] private GameObject confirmMulliganButton;


    private Dictionary<Card, CardView> cardViews = new();

    private MovePreview currentPreview;

    private const int boardHeight = 3;
    private const int boardWidth = 5;

    private Player currentPlayer;
    private Card selectedCard;

    private int consecutivePasses = 0;
    private const int MaxHandSize = 10;


    void Start()
    {
        player1.Initialize(1);
        player2.Initialize(2);

        InitializePlayerDeck(player1, matchSetup.GetPlayer1Deck());
        InitializePlayerDeck(player2, matchSetup.GetPlayer2Deck());

        InitializeHand(player1, p1HandPanel);
        InitializeHand(player2, p2HandPanel);

        currentPlayer = player1;

        gamePhase = GamePhase.gamePhase.Mulligan;

        StartMulligan(player1);

        board.CardDestroyed += OnCardDestroyed;
    }

    public void InitializePlayerDeck(Player player, IReadOnlyList<CardData> cards)
    {
        foreach (CardData cardData in cards)
        {
            Card card = Instantiate(cardPrefab);
            card.Initialize(cardData);

            player.Deck.AddCard(card);
        }

        player.Deck.Shuffle();
    }

    public void OnCardClicked(Card card, Player player)
    {
        if (gamePhase == GamePhase.gamePhase.Mulligan)
        {
            ToggleMulligan(card, player);
            return;
        }

        SelectCard(card);
    }


    private void ToggleMulligan(Card card, Player player)
    {
        if (player != mulliganPlayer)
            return;

        bool selected;

        if (mulliganSelection.Contains(card))
        {
            mulliganSelection.Remove(card);
            selected = false;
        }
        else
        {
            mulliganSelection.Add(card);
            selected = true;
        }

        cardViews[card].SetMulliganSelected(selected);

        Debug.Log($"Mulligan: {card.CardName}, selected = {selected}");
    }

    private void StartMulligan(Player player)
    {
        mulliganPlayer = player;
        mulliganSelection.Clear();


        confirmMulliganButton.SetActive(true);

    }

    public void ConfirmMulligan()
    {
        if (gamePhase != GamePhase.gamePhase.Mulligan)
            return;

        Player player = mulliganPlayer;

        Transform handPanel = player == player1
            ? p1HandPanel
            : p2HandPanel;

        List<Card> replacedCards = new(mulliganSelection);

        foreach (Card card in replacedCards)
        {
            player.Hand.RemoveCard(card);

            CardView view = cardViews[card];
            cardViews.Remove(card);

            Destroy(view.gameObject);
        }

        foreach (Card card in replacedCards)
        {
            DrawCard(player, handPanel);
        }

        foreach (Card card in replacedCards)
        {
            player.Deck.AddCard(card);
        }

        player.Deck.Shuffle();

        mulliganSelection.Clear();

        if (player == player1)
        {
            StartMulligan(player2);
        }
        else
        {
            gamePhase = GamePhase.gamePhase.Playing;
            currentPlayer = player1;

            Debug.Log("Mulligan complete. Game started!");
        }
    }

    public void PassTurn()
    {
        if (gamePhase != GamePhase.gamePhase.Playing)
            return;

        consecutivePasses++;

        Debug.Log($"Player {currentPlayer.Num} passed.");

        ClearPreview();

        selectedCard = null;

        // Remove all placement highlights
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 5; column++)
            {
                board.GetTile(row, column).UI.SetGlow(TileView.GlowType.None);
            }
        }

        if (consecutivePasses >= 2)
        {
            EndGame();
            return;
        }

        SwitchTurn();
    }

    private void PreviewPowerEffects(Card card, Tile targetTile, Player player)
    {
        CardPattern pattern = card.data.EffectTiles;

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                int amount = pattern.rows[i].values[j];

                if (amount == 0)
                    continue;

                Vector2Int position = board.GetPatternPosition(
                    targetTile,
                    i,
                    j,
                    player
                );

                if (position.x < 0 || position.x >= 3 ||
                    position.y < 0 || position.y >= 5)
                    continue;

                Tile affectedTile = board.GetTile(position.x, position.y);

                if (amount > 0)
                    affectedTile.UI.SetGlow(GlowType.Enhanced);
                else
                    affectedTile.UI.SetGlow(GlowType.Enfeebled);
            }
        }
    }

    public void SelectCard(Card card)
    {
        if (!currentPlayer.Hand.Cards.Contains(card))
            return;

        selectedCard = card;
        Tile tile;
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 5; column++)
            {
                tile = board.GetTile(row, column);

                bool canPlay = board.CanPlayCard(
                    card,
                    tile,
                    currentPlayer
                );

                tile.UI.SetGlow(
    canPlay ? TileView.GlowType.Playable : TileView.GlowType.None
);
            }
        }
    }

    private void DrawCard(Player player, Transform handPanel)
    {
        Debug.Log(player.Hand.Cards.Count);
        if (player.Hand.Cards.Count >= MaxHandSize)
            return;

        Card card = player.Deck.DrawCard();

        if (card == null)
            return;

        player.Hand.AddCard(card);

        CardView view = Instantiate(cardViewPrefab, handPanel);
        view.Initialize(card, this, player);

        cardViews.Add(card, view);
    }

    private void InitializeHand(Player player, Transform handPanel)
    {
        for (int i = 0; i < 5; i++)
        {
            DrawCard(player, handPanel);
        }
    }

    public void ClearPreview()
    {
        if (currentPreview == null)
            return;

        foreach (Tile affectedTile in currentPreview.PawnTiles)
        {
            affectedTile.UI.ClearPreview();
        }
        RefreshTileGlows();
        currentPreview = null;
    }

    public void PreviewTile(Tile tile)
    {
        // Remove any previous preview
        ClearPreview();

        if (selectedCard == null)
            return;

        if (!board.CanPlayCard(selectedCard, tile, currentPlayer))
            return;

        PreviewPowerEffects(selectedCard, tile, currentPlayer);

        // Calculate and remember the current preview
        currentPreview = board.GetMovePreview(selectedCard, tile, currentPlayer);

        foreach (Tile affectedTile in currentPreview.PawnTiles)
        {
            if (affectedTile.Card != null)
                continue;

            int predictedPawns = affectedTile.Pawns;
            Player predictedOwner = currentPlayer;

            if (affectedTile.Owner == null || affectedTile.Owner == currentPlayer)
            {
                predictedPawns++;
            }

            predictedPawns = Mathf.Min(predictedPawns, 3);

            affectedTile.UI.PreviewPawns(predictedPawns, predictedOwner);
        }

    }

    public void PlaySelectedCard(Tile tile)
    {
        if (gamePhase != GamePhase.gamePhase.Playing)
            return;

        if (selectedCard == null)
            return;

        if (!board.CanPlayCard(selectedCard, tile, currentPlayer))
            return;

        // Remove the temporary pawn preview
        ClearPreview();

        // Play the card
        board.PlayCard(selectedCard, tile, currentPlayer);

        consecutivePasses = 0;

        // Remove it from the player's hand
        currentPlayer.Hand.RemoveCard(selectedCard);

        // Move the card's visual representation onto the tile
        CardView view = cardViews[selectedCard];

        view.MoveToTile(tile.UI);

        tile.UI.ShowCard();


        // Clear selection
        selectedCard = null;

        // Remove all valid-placement highlights
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 5; column++)
            {
                board.GetTile(row, column).UI.SetGlow(false);
            }
        }

        SwitchTurn();
    }

    private void OnCardDestroyed(Card card, Tile tile, Player player)
    {
        Debug.Log($"Destroying visual for {card.CardName}");

        if (cardViews.TryGetValue(card, out CardView view))
        {
            cardViews.Remove(card);
            Destroy(view.gameObject);
        }
        else
        {
            Debug.LogWarning($"No CardView found for {card.CardName}");
        }

        tile.UI.ShowEmptyTile();
    }

    private void RefreshTileGlows()
    {
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                Tile tile = board.GetTile(row, col);

                if (selectedCard != null &&
                    board.CanPlayCard(selectedCard, tile, currentPlayer))
                {
                    tile.UI.SetGlow(GlowType.Playable);
                }
                else
                {
                    tile.UI.SetGlow(GlowType.None);
                }
            }
        }
    }

    private void InitializeDeck(Player player, DeckData deckData)
    {
        foreach (CardData cardData in deckData.Cards)
        {
            Card card = Instantiate(cardPrefab);
            card.Initialize(cardData);

            player.Deck.AddCard(card);
        }

        player.Deck.Shuffle();
    }

    private void InitializeDeck(Player player, IReadOnlyList<CardData> cards)
    {
        foreach (CardData cardData in cards)
        {
            Card card = Instantiate(cardPrefab);
            card.Initialize(cardData);

            player.Deck.AddCard(card);
        }

        player.Deck.Shuffle();
    }

    private void OnDestroy()
    {
        if (board != null)
            board.CardDestroyed -= OnCardDestroyed;
    }

    private void SwitchTurn()
    {

        Transform handPanel = currentPlayer == player1
    ? p1HandPanel
    : p2HandPanel;
        DrawCard(currentPlayer, handPanel);
        currentPlayer = currentPlayer == player1 ? player2 : player1;


        Debug.Log($"Player {currentPlayer.Num}'s turn");
    }


    private void EndGame()
    {
        gamePhase = GamePhase.gamePhase.Finished;

        var (p1Score, p2Score) = board.GetFinalScores();

        if (p1Score > p2Score)
            resultText.text = "Player 1 Wins!";
        else if (p2Score > p1Score)
            resultText.text = "Player 2 Wins!";
        else
            resultText.text = "Draw!";

        p1ScoreText.text = $"Player 1: {p1Score}";
        p2ScoreText.text = $"Player 2: {p2Score}";

        resultsPanel.SetActive(true);
    }
}
