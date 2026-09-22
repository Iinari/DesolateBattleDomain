using SnIProductions;
using System.Collections.Generic;
using UnityEngine;

//Creates a systems for managing the card cycle in battle, including the hand, draw pile, and discard pile.
public class CardCycleSystem : IBattleComponent
{
    public Hand hand;
    public DrawPile drawPile;
    public DiscardPile discardPile;

    /// <summary>
    /// Sets up a fresh card cycle for a new battle: builds a new draw pile from the
    /// player's deck and starts with an empty discard pile.
    /// </summary>

    public void InitializeNew(BattleData data)
    {
        CreateComponents(data);
        var dataFetcher = new DeckDataFetcher();
        drawPile.MakeNewDrawPile(dataFetcher.GetCardsInDeck());
        discardPile.Initialize();
        SetComponentsInBattleContext();
    }

    /// <summary>
    /// Restores the card cycle after a game load. Pile contents are already recovered
    /// from saved data via each pile's constructor, so no new pile is built here.
    /// </summary>
    public void InitializeFromData(BattleData data)
    {
        CreateComponents(data);
        discardPile.Initialize();
        SetComponentsInBattleContext();
    }

    private void CreateComponents(BattleData data)
    {
        //Ordered by dependency: DiscardPile is independent, Hand needs DiscardPile,
        //and DrawPile needs both Hand and DiscardPile to route cards during cycling.
        discardPile = new DiscardPile(data);
        hand = new Hand(data, discardPile);
        drawPile = new DrawPile(data, hand, discardPile);
    }

    //These are set to BattleContext mainly for CardPileUIBuilder and CardPlayManager
    //If BattleContext depency is removed from those classes later in the refactoring, this can be removed as well
    private void SetComponentsInBattleContext()
    {
        BattleContext.Instance.hand = hand;
        BattleContext.Instance.drawPile = drawPile;
        BattleContext.Instance.discardPile = discardPile;
    }
}
