using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private Board board;

    [SerializeField] private Player player1;
    [SerializeField] private Player player2;

    [SerializeField] private Card cardPrefab;
    [SerializeField] private CardData soldierData;
    [SerializeField] private CardView cardViewPrefab;

    [SerializeField] private Transform p1HandPanel;
    [SerializeField] private Transform p2HandPanel;

    private Dictionary<Card, CardView> cardViews = new();   

    private MovePreview currentPreview;

    private const int boardHeight = 3;
    private const int boardWidth = 5;

    private Player currentPlayer;
    private Card selectedCard;

    void Start()
    {
        player1.Initialize(1);
        player2.Initialize(2);

        InitializeHand(player1, p1HandPanel);
        InitializeHand(player2, p2HandPanel);

        currentPlayer = player1;
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

                tile.UI.SetGlow(canPlay);
            }
        }
    }

    private void InitializeHand(Player player, Transform handPanel)
    {
        for (int i = 0; i < 4; i++)
        {
            Card card = Instantiate(cardPrefab);
            card.Initialize(soldierData);

            player.Hand.AddCard(card);

            CardView view = Instantiate(cardViewPrefab, handPanel);
            view.Initialize(card, this);

            cardViews.Add(card, view);
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
        if (selectedCard == null)
            return;

        if (!board.CanPlayCard(selectedCard, tile, currentPlayer))
            return;

        // Remove the temporary pawn preview
        ClearPreview();

        // Play the card
        board.PlayCard(selectedCard, tile, currentPlayer);

        // Remove it from the player's hand
        currentPlayer.Hand.RemoveCard(selectedCard);

        cardViews[selectedCard].RemoveFromHand();
        cardViews.Remove(selectedCard);

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

        Debug.Log($"Player {currentPlayer.Num} placed a card at ({tile.Row}, {tile.Column})");
    }
}
