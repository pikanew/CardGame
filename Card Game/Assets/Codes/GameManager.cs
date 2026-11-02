using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static TileView;
using static UnityEngine.Rendering.DebugUI.Table;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private OnlineMatchClient onlineMatchClient;
    private bool onlineMulliganRequestInProgress;
    private int lastProcessedActionSequence = 0;
    private bool rankedResultSubmissionStarted;

    private bool onlineActionInProgress;


    [SerializeField] private GameManager manager;
    [SerializeField] private Board board;

    [SerializeField] private Player player1;
    [SerializeField] private Player player2;

    [SerializeField] private GameObject opponentCardBackPrefab;

    private readonly List<GameObject> opponentCardBacks = new();

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

    [SerializeField] private GameObject playAgainButton;
    [SerializeField] private GameObject findGameButton;
    [SerializeField] private GameObject mainMenuButton;

    [SerializeField] private TMP_Text turnTimerText;

    private const float TurnDuration = 90f;
    private float timeRemaining = TurnDuration;
    private bool isRankedOnlineMatch;



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

    private bool isOnlineMatch;

    private Player localPlayer;

    private readonly Dictionary<Card, int> onlineCardIds = new();

    private bool onlineMulliganSubmitted;


    private IEnumerator Start()
    {
        player1.Initialize(1);
        player2.Initialize(2);

        isOnlineMatch = RankedMatchSession.HasMatch;

        if (isOnlineMatch)
        {
            board.CardDestroyed += OnCardDestroyed;

            yield return StartCoroutine(InitializeOnlineMatch());

            yield break;
        }

        bool decksLoaded = false;

        yield return StartCoroutine(
            matchSetup.LoadMatchDecks((p1Cards, p2Cards) =>
            {
                InitializePlayerDeck(player1, p1Cards);
                InitializePlayerDeck(player2, p2Cards);

                decksLoaded = true;
            })
        );

        if (!decksLoaded)
        {
            Debug.LogError("Cannot start match: decks failed to load.");
            yield break;
        }

        InitializeHand(player1, p1HandPanel);
        InitializeHand(player2, p2HandPanel);

        currentPlayer = player1;

        gamePhase = GamePhase.gamePhase.Mulligan;

        StartMulligan(player1);

        board.CardDestroyed += OnCardDestroyed;
    }

    private void Update()
    {
        if (isOnlineMatch)
            return;

        if (gamePhase != GamePhase.gamePhase.Playing &&
            gamePhase != GamePhase.gamePhase.Mulligan)
            return;

        timeRemaining -= Time.deltaTime;

        UpdateTimerDisplay(timeRemaining);

        if (timeRemaining > 0f)
            return;

        timeRemaining = 0f;

        if (gamePhase == GamePhase.gamePhase.Mulligan)
        {
            Debug.Log("Mulligan timer expired.");

            // Deselect every card.
            foreach (Card card in mulliganSelection)
            {
                if (cardViews.TryGetValue(card, out CardView view))
                    view.SetMulliganSelected(false);
            }

            // Keep the original hand.
            mulliganSelection.Clear();

            // Continue to the next mulligan or start playing.
            ConfirmMulligan();
        }
        else
        {
            Debug.Log($"Player {currentPlayer.Num} ran out of time.");

            PassTurn();
        }
    }

    private void UpdateOpponentHand(int cardCount)
    {
        if (!isOnlineMatch || localPlayer == null)
            return;

        Transform opponentHandPanel = localPlayer == player1
            ? p2HandPanel
            : p1HandPanel;

        cardCount = Mathf.Max(0, cardCount);

        while (opponentCardBacks.Count > cardCount)
        {
            int lastIndex = opponentCardBacks.Count - 1;

            GameObject cardBack = opponentCardBacks[lastIndex];

            opponentCardBacks.RemoveAt(lastIndex);

            Destroy(cardBack);
        }

        while (opponentCardBacks.Count < cardCount)
        {
            GameObject cardBack = Instantiate(
                opponentCardBackPrefab,
                opponentHandPanel
            );

            opponentCardBacks.Add(cardBack);
        }
    }

    private IEnumerator SendOnlineCardPlacement(Card card, Tile tile)
    {
        if (!onlineCardIds.TryGetValue(card, out int instanceId))
        {
            Debug.LogError("Card has no online instance ID.");
            yield break;
        }

        onlineActionInProgress = true;

        OnlineMatchView response = null;

        yield return StartCoroutine(
            onlineMatchClient.PlayCard(
                instanceId,
                tile.Row,
                tile.Column,
                match => response = match
            )
        );

        if (response != null)
        {
            ApplyOnlineMatchState(response);
        }

        onlineActionInProgress = false;
    }

    private void ApplyOnlineMatchState(OnlineMatchView match)
    {
        if (match.phase == "mulligan")
        {
            UpdateTimerDisplay(match.mulliganSecondsRemaining);
        }
        else if (match.phase == "playing")
        {
            UpdateTimerDisplay(match.turnSecondsRemaining);
        }
        else
        {
            turnTimerText.text = "";
        }

        if (match.actions != null)
        {

            foreach (OnlineMatchAction action in match.actions)
            {
                if (action.sequence <= lastProcessedActionSequence)
                    continue;

                if (action.sequence != lastProcessedActionSequence + 1)
                {
                    Debug.LogError("Missing online action.");
                    return;
                }

                if (!ApplyOnlineAction(action))
                    return;

                lastProcessedActionSequence = action.sequence;
            }
        }

        if (match.hand != null)
            UpdateOnlineHandIfChanged(match.hand);

        UpdateOpponentHand(match.opponentHandCount);

        currentPlayer = match.currentPlayer == 1
            ? player1
            : player2;

        if (match.phase == "playing" &&
            gamePhase == GamePhase.gamePhase.Mulligan)
        {
            StartOnlinePlayingPhase(match);
        }

        if (match.phase == "finished" &&
            gamePhase != GamePhase.gamePhase.Finished)
        {
            EndGame();
        }
    }

    private void UpdateOnlineHandIfChanged(OnlineCardInfo[] serverHand)
    {
        if (localPlayer == null)
            return;

        if (onlineCardIds.Count == serverHand.Length)
        {
            bool identical = true;

            foreach (OnlineCardInfo card in serverHand)
            {
                if (!onlineCardIds.ContainsValue(card.instanceId))
                {
                    identical = false;
                    break;
                }
            }

            if (identical)
                return;
        }

        SynchronizeOnlineHand(serverHand);
    }

    private bool ApplyOnlineAction(OnlineMatchAction action)
    {
        Player player = action.playerNumber == 1
            ? player1
            : player2;

        if (action.type == "pass")
        {
            ClearPreview();
            selectedCard = null;

            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    board.GetTile(row, col).UI.SetGlow(false);
                }
            }

            Debug.Log($"Player {action.playerNumber} passed.");

            return true;
        }

        if (action.type != "play")
        {
            Debug.LogError($"Unknown online action: {action.type}");
            return false;
        }

        Tile tile = board.GetTile(action.row, action.column);

        Card card = null;
        CardView view = null;

        // If this is our card, reuse the Card and CardView
        // that already exist in our hand.
        if (player == localPlayer)
        {
            foreach (var entry in onlineCardIds)
            {
                if (entry.Value == action.instanceId)
                {
                    card = entry.Key;
                    break;
                }
            }

            if (card != null)
            {
                cardViews.TryGetValue(card, out view);

                if (localPlayer.Hand.Cards.Contains(card))
                    localPlayer.Hand.RemoveCard(card);

                onlineCardIds.Remove(card);
            }
        }

        // Opponent's cards, or previously played cards when
        // restoring a match, must be created from their public IDs.
        if (card == null)
        {
            CardData data = null;

            foreach (CardData availableCard in cardDatabase.Cards)
            {
                if (availableCard.CardId == action.cardId)
                {
                    data = availableCard;
                    break;
                }
            }

            if (data == null)
            {
                Debug.LogError($"Unknown card: {action.cardId}");
                return false;
            }

            card = Instantiate(cardPrefab);
            card.Initialize(data);

            view = Instantiate(cardViewPrefab, tile.UI.transform);
            view.Initialize(card, this, player);

            cardViews.Add(card, view);
        }

        if (!board.CanPlayCard(card, tile, player))
        {
            Debug.LogError(
                $"Board desynchronization! Player {action.playerNumber}, " +
                $"card {action.cardId}, tile ({action.row}, {action.column})."
            );

            return false;
        }

        // Move the visual first, because some abilities can destroy
        // a card immediately during Board.PlayCard().
        view.MoveToTile(tile.UI);

        ClearPreview();
        selectedCard = null;

        board.PlayCard(card, tile, player);

        if (tile.Card != null)
            tile.UI.ShowCard();

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 5; col++)
            {
                board.GetTile(row, col).UI.SetGlow(false);
            }
        }

        Debug.Log(
            $"Online action {action.sequence}: " +
            $"Player {action.playerNumber} played {action.cardId}."
        );

        return true;
    }

    private IEnumerator InitializeOnlineMatch()
    {
        OnlineMatchView match = null;

        yield return StartCoroutine(
            onlineMatchClient.GetMatch(
                response => match = response
            )
        );

        if (match == null)
        {
            Debug.LogError("Could not initialize online match.");
            yield break;
        }

        isRankedOnlineMatch = match.isRanked;

        localPlayer = match.playerNumber == 1
            ? player1
            : player2;

        currentPlayer = match.currentPlayer == 1
            ? player1
            : player2;

        InitializeOnlineHand(match.hand);
        UpdateTimerDisplay(
            match.phase == "mulligan"
                ? match.mulliganSecondsRemaining
                : match.turnSecondsRemaining
        );
        UpdateOpponentHand(match.opponentHandCount);
        ApplyOnlineMatchState(match);

        if (match.phase == "mulligan")
        {
            gamePhase = GamePhase.gamePhase.Mulligan;

            if (!match.mulliganCompleted)
            {
                StartMulligan(localPlayer);
            }
            else
            {
                onlineMulliganSubmitted = true;
                confirmMulliganButton.SetActive(false);
            }
        }
        else if (match.phase == "playing")
        {
            StartOnlinePlayingPhase(match);
        }

        Debug.Log(
            $"Online match initialized. " +
            $"Room: {match.roomCode}, " +
            $"Local player: {localPlayer.Num}"
        );

        StartCoroutine(PollOnlineMatch());
    }

    private void InitializeOnlineHand(
    OnlineCardInfo[] cards)
    {
        Transform handPanel = localPlayer == player1
            ? p1HandPanel
            : p2HandPanel;

        foreach (OnlineCardInfo cardInfo in cards)
        {
            CardData data = null;

            foreach (CardData availableCard in cardDatabase.Cards)
            {
                if (availableCard.CardId == cardInfo.cardId)
                {
                    data = availableCard;
                    break;
                }
            }

            if (data == null)
            {
                Debug.LogError(
                    $"Unknown card ID: {cardInfo.cardId}"
                );

                continue;
            }

            Card card = Instantiate(cardPrefab);
            card.Initialize(data);

            localPlayer.Hand.AddCard(card);

            CardView view = Instantiate(
                cardViewPrefab,
                handPanel
            );

            view.Initialize(card, this, localPlayer);

            cardViews.Add(card, view);

            onlineCardIds.Add(
                card,
                cardInfo.instanceId
            );
        }
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
        if (isOnlineMatch && onlineActionInProgress)
            return;

        if (isOnlineMatch)
        {
            if (localPlayer == null)
                return;

            if (player != localPlayer)
                return;

            if (onlineMulliganSubmitted &&
                gamePhase == GamePhase.gamePhase.Mulligan)
                return;
        }

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

        if (!isOnlineMatch)
        {
            timeRemaining = TurnDuration;
            UpdateTimerDisplay(timeRemaining);
        }
    }

    public void ConfirmMulligan()
    {
        if (gamePhase != GamePhase.gamePhase.Mulligan)
            return;

        if (isOnlineMatch)
        {
            if (onlineMulliganSubmitted || onlineMulliganRequestInProgress)
                return;

            StartCoroutine(ConfirmOnlineMulligan());
            return;
        }

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

            timeRemaining = TurnDuration;
            UpdateTimerDisplay(timeRemaining);

            Debug.Log("Mulligan complete. Game started!");
        }
    }

    private IEnumerator ConfirmOnlineMulligan()
    {
        onlineMulliganRequestInProgress = true;

        List<int> selectedIds = new();

        foreach (Card card in mulliganSelection)
        {
            if (!onlineCardIds.TryGetValue(card, out int instanceId))
            {
                Debug.LogError(
                    $"Missing online instance ID for {card.CardName}"
                );

                onlineMulliganRequestInProgress = false;
                yield break;
            }

            selectedIds.Add(instanceId);
        }

        OnlineMatchView response = null;

        yield return StartCoroutine(
            onlineMatchClient.ConfirmMulligan(
                selectedIds.ToArray(),
                match => response = match
            )
        );

        onlineMulliganRequestInProgress = false;

        if (response == null)
        {
            Debug.LogError("Online mulligan failed.");
            yield break;
        }

        onlineMulliganSubmitted = true;

        confirmMulliganButton.SetActive(false);

        mulliganSelection.Clear();

        SynchronizeOnlineHand(response.hand);

        Debug.Log("Mulligan submitted. Waiting for opponent.");
    }

    private void SynchronizeOnlineHand(OnlineCardInfo[] cards)
    {
        if (cards == null)
        {
            Debug.LogError("Server returned an invalid hand.");
            return;
        }

        List<Card> oldCards = new(localPlayer.Hand.Cards);

        foreach (Card card in oldCards)
        {
            localPlayer.Hand.RemoveCard(card);

            if (cardViews.TryGetValue(card, out CardView view))
            {
                cardViews.Remove(card);
                Destroy(view.gameObject);
            }

            onlineCardIds.Remove(card);

            Destroy(card.gameObject);
        }

        InitializeOnlineHand(cards);
    }

    private IEnumerator PollOnlineMatch()
    {
        while (isOnlineMatch &&
               gamePhase != GamePhase.gamePhase.Finished)
        {
            yield return new WaitForSeconds(1f);

            if (onlineActionInProgress)
                continue;

            OnlineMatchView match = null;

            yield return StartCoroutine(
                onlineMatchClient.GetMatch(
                    response => match = response
                )
            );

            if (match == null)
                continue;

            ApplyOnlineMatchState(match);
        }
    }

    private void StartOnlinePlayingPhase(OnlineMatchView match)
    {
        gamePhase = GamePhase.gamePhase.Playing;

        currentPlayer = match.currentPlayer == 1
            ? player1
            : player2;

        confirmMulliganButton.SetActive(false);

        mulliganSelection.Clear();

        Debug.Log(
            $"Online match started! " +
            $"Player {currentPlayer.Num}'s turn."
        );
    }

    public void PassTurn()
    {
        if (gamePhase != GamePhase.gamePhase.Playing)
            return;

        if (isOnlineMatch)
        {
            if (gamePhase != GamePhase.gamePhase.Playing ||
                currentPlayer != localPlayer ||
                onlineActionInProgress)
                return;

            StartCoroutine(SendOnlinePass());
            return;
        }

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

    private IEnumerator SendOnlinePass()
    {
        onlineActionInProgress = true;

        OnlineMatchView response = null;

        yield return StartCoroutine(
            onlineMatchClient.PassTurn(
                match => response = match
            )
        );

        if (response != null)
            ApplyOnlineMatchState(response);

        onlineActionInProgress = false;
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
        if (isOnlineMatch && onlineActionInProgress)
            return;

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

        if (isOnlineMatch)
        {
            if (gamePhase != GamePhase.gamePhase.Playing ||
                currentPlayer != localPlayer ||
                selectedCard == null ||
                onlineActionInProgress)
                return;

            if (!board.CanPlayCard(selectedCard, tile, localPlayer))
                return;

            StartCoroutine(SendOnlineCardPlacement(selectedCard, tile));
            return;
        }

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

    private IEnumerator SubmitOnlineRankedResult(
    int p1Score,
    int p2Score)
    {
        RankedResultResponse response = null;

        yield return StartCoroutine(
            onlineMatchClient.SubmitRankedResult(
                p1Score,
                p2Score,
                result => response = result
            )
        );

        if (response == null)
        {
            Debug.LogError(
                "Could not submit ranked match result."
            );

            yield break;
        }

        switch (response.status)
        {
            case "waiting":
                Debug.Log(
                    "Ranked result submitted. Waiting for opponent."
                );
                break;

            case "recorded":
                int ratingChange = localPlayer == player1
                    ? response.player1RatingChange
                    : response.player2RatingChange;

                Debug.Log(
                    $"Ranked result recorded! " +
                    $"Rating change: {ratingChange:+#;-#;0}"
                );
                break;

            case "processing":
                Debug.Log("Ranked result is being processed.");
                break;

            case "mismatch":
                Debug.LogError(
                    "Players reported different final scores."
                );
                break;

            default:
                Debug.LogWarning(
                    $"Unexpected ranked result status: {response.status}"
                );
                break;
        }
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

        timeRemaining = TurnDuration;
        UpdateTimerDisplay(timeRemaining);
    }


    private void EndGame()
    {
        gamePhase = GamePhase.gamePhase.Finished;

        var (p1Score, p2Score) = board.GetFinalScores();

        if (isOnlineMatch &&
        isRankedOnlineMatch &&
        !rankedResultSubmissionStarted)
        {
            rankedResultSubmissionStarted = true;

            StartCoroutine(
                SubmitOnlineRankedResult(p1Score, p2Score)
            );
        }

        if (p1Score > p2Score)
            resultText.text = "Player 1 Wins!";
        else if (p2Score > p1Score)
            resultText.text = "Player 2 Wins!";
        else
            resultText.text = "Draw!";

        p1ScoreText.text = $"Player 1: {p1Score}";
        p2ScoreText.text = $"Player 2: {p2Score}";

        bool showPlayAgain = !isOnlineMatch || !isRankedOnlineMatch;
        bool showFindGame = isOnlineMatch && isRankedOnlineMatch;

        playAgainButton.SetActive(showPlayAgain);
        findGameButton.SetActive(showFindGame);
        mainMenuButton.SetActive(true);

        resultsPanel.SetActive(true);

    }

    private void UpdateTimerDisplay(float seconds)
    {
        seconds = Mathf.Max(0, seconds);

        int totalSeconds = Mathf.CeilToInt(seconds);

        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;

        turnTimerText.text = $"{minutes:00}:{remainingSeconds:00}";

        turnTimerText.color = seconds <= 15f
            ? Color.red
            : Color.white;
    }

    public void ReturnToMainMenu()
    {
        RankedMatchSession.Clear();

        SceneManager.LoadScene("MainMenu");
    }

    public void ReturnToRanked()
    {
        RankedMatchSession.Clear();

        SceneManager.LoadScene("RankedMatchMaking");
    }
}
