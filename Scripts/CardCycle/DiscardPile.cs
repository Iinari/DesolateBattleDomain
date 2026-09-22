using SnIProductions;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DiscardPile : ICardPile
{
    public event Action OnCardsChanged;

    private BattleData battleData;

    /// <summary>Number of cards currently in the discard pile.</summary>
    public int Count => battleData.cardsInDiscard.Count;

    public DiscardPile(BattleData data)
    {
        battleData = data;
    }

    /// <summary>
    /// Fires an initial change notification for UI relying on discard state after a game load.
    /// </summary>
    public void Initialize()
    {
        if (battleData != null)
        {
            OnCardsChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning("DiscardPile: BattleData is null after game load.");
        }
    }

    /// <summary>Adds a card to the discard pile.</summary>
    public void AddToDiscard(CardData card)
    {
        if (card != null)
        {
            battleData.cardsInDiscard.Add(card.ID);
            OnCardsChanged?.Invoke();
        }
        else Debug.Log("AddToDiscard failed due to null card value");
    }

    /// <summary>Removes and returns all cards from the discard pile, clearing it.</summary>
    public List<int> PullAllFromDiscard() 
    {
        var cardsToReturn = new List<int>(battleData.cardsInDiscard);
        battleData.cardsInDiscard.Clear();
        if (cardsToReturn.Count > 0) OnCardsChanged?.Invoke();
        return cardsToReturn;
    }

    /// <summary>Removes and returns the top card of the discard pile, or null if empty.</summary>
    public CardData PullFromDiscard()
    {
        var cardToReturn = CardDatabase.GetCard(battleData.cardsInDiscard[battleData.cardsInDiscard.Count - 1]);
        battleData.cardsInDiscard.RemoveAt(battleData.cardsInDiscard.Count - 1);
        OnCardsChanged?.Invoke();
        return cardToReturn;
    }

    /// <summary>Returns the card IDs currently in the discard pile, without modifying it.</summary>
    public List<int> GetCards()
    {
        return battleData.cardsInDiscard;
    }
}
