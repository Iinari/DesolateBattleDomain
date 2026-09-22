using SnIProductions;
using System;
using System.Collections.Generic;
using System.Linq;

public class Hand
{
    /// <summary>
    /// The cards currently in hand, resolved from stored card IDs against the CardDatabase.
    /// </summary>
    public IReadOnlyList<CardData> Cards =>
        battleData.cardsInHand.Select(id => CardDatabase.GetCard(id)).ToList();
    private readonly BattleData battleData;

    /// <summary>Raised when a card is added to the hand.</summary>
    public event Action<CardData> OnCardAdded;
    /// <summary>Raised when a card is removed from the hand.</summary>
    public event Action<CardData> OnCardRemoved;
    /// <summary>Raised when a card is removed from the hand.</summary>
    public event Action OnHandCleared;

    private readonly DiscardPile discardPile;

    public Hand(BattleData battleData, DiscardPile discardPile)
    {
        this.battleData = battleData;
        this.discardPile = discardPile;
    }

    /// <summary>
    /// Attempts to add a card to the hand. Fails without effect if the hand is already at max size.
    /// </summary>
    /// <returns>True if the card was added.</returns>
    public bool AddCard(CardData cardData)
    {
        if (battleData.cardsInHand.Count >= battleData.maxHandSize) return false;
        battleData.cardsInHand.Add(cardData.ID);
        OnCardAdded?.Invoke(cardData);
        return true;
    }

    /// <summary>Removes a card from the hand.</summary>
    public void RemoveCard(CardData cardData)
    {
        battleData.cardsInHand.Remove(cardData.ID);
        OnCardRemoved?.Invoke(cardData);
    }

    /// <summary>Moves every card currently in hand to the discard pile.</summary>
    public void DiscardHand()
    {
        foreach (CardData card in Cards)
        {
            discardPile.AddToDiscard(card);
            RemoveCard(card);
        }
        OnHandCleared?.Invoke();
    }
}
