using SnIProductions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Controller of battle creation and flow
public class BattleController : MonoBehaviour, IUIAnchorProvider
{
    private BattleContext battleContext;
    private BattleStateStatus currentState;
    private BattleResultStatus currentResult;

    private PlayerUnitFactory playerUnitFactory;

    private BattleUIController battleUIController;

    private EnemySystem enemySystem;

    private CardCycleSystem cardCycleSystem;
    private CardPlaySystem cardPlaySystem;

    private TurnSystem turnSystem;

    private List<IBattleComponent> initOrder;

    private System.Action onPlayerDeath;

    [Header("Global UI")]
    [SerializeField] private Transform deckPileAnchor;
    [SerializeField] private Transform settingsButtonAnchor;

    private void Start() => GlobalUIManager.Instance.RegisterAnchorProvider(this);
    private void OnDestroy() => GlobalUIManager.Instance.UnregisterAnchorProvider(this);

    void Awake()
    {
        //Get the BattleUIController component from the scene
        battleUIController = GetComponent<BattleUIController>();
    }
    /// <summary>
    /// Sets up a brand-new battle: creates all systems, initializes them with fresh data,
    /// links battle context, subscribes to battle-end events, then builds the UI.
    /// </summary>
    public void StartBattle()
    {
        if (!CreateSystemComponents()) return;

        //Initialize each system
        foreach (var system in initOrder)
        {
            system.InitializeNew(GameSession.Instance.battleData);
        }

        //Pass the BattleContext to each system that implements IBattleLinkable, so that BattleContext is passed to once at the start of the battle and not every time a system needs to access it
        foreach (var system in initOrder.OfType<IBattleLinkable>())
        {
            system.Link(battleContext);
        }

        SubscribeToEvents();

        //Last step, initialize the UI with the references to the systems and the data that was just created
        InitializeUI();
    }

    /// <summary>
    /// Restores a battle after a game load. Systems are initialized from previously
    /// saved data (already applied via IDataPersistence) rather than created fresh.
    /// </summary>
    public void StartLoadedBattle()
    {
        if (!CreateSystemComponents()) return;

        foreach (var system in initOrder)
        {
            system.InitializeFromData(GameSession.Instance.battleData);
        }

        foreach (var system in initOrder.OfType<IBattleLinkable>())
        {
            system.Link(battleContext);
        }

        SubscribeToEvents();

        InitializeUI();
    }

    /// <summary>
    /// Ends the player's turn and hands control to the enemy turn routine.
    /// </summary>
    public void EndPlayerTurn()
    {
        currentState.SetState(BattleState.EnemyTurn);
        StartCoroutine(RunEnemyTurn());
    }

    /// <summary>
    /// Leaves the battle scene and returns the player to the map.
    /// </summary>
    private IEnumerator RunEnemyTurn()
    {
        yield return turnSystem.EnemyTurnHandler.TakeEnemyTurnRoutine();
        currentState.SetState(BattleState.PlayerTurn);
    }

    /// <summary>
    /// Ends the battle with the given result. Ignored if the result has already been set,
    /// so this is safe to call multiple times (e.g. from both a death event and win condition).
    /// </summary>
    /// <param name="result">Whether the battle ended in victory or loss.</param>
    public void EndBattle(BattleResult result)
    {
        if (currentResult.ResultStatus == BattleResult.Default)
        {
            currentState.SetState(BattleState.Ended);
            currentResult.SetResult(result);
            UnsubscribeFromEvents();

            if (result == BattleResult.Victory)
            {
                var rewardScreen = GlobalUIManager.Instance.OpenRewardScreen();
                rewardScreen.OnClosed += CloseBattleDomain;
            }
            else if (result == BattleResult.Loss)
            {
                GlobalUIManager.Instance.OpenDeathScreen();
            }
        }
        else
        {
            Debug.LogWarning("Battle result has already been set. Ignoring subsequent calls to EndBattle.");
        }
    }

    public bool CreateSystemComponents()
    {
        battleContext = BattleContext.Instance;
        if (battleContext == null)
        {
            Debug.LogError("BattleController: BattleContext instance is null... ");
            return false;
        }
        //Create the BattleResultStatus and BattleStateStatus states
        currentResult = new BattleResultStatus();
        currentState = new BattleStateStatus();


        //Create the systems that will be used in the battle
        playerUnitFactory = new PlayerUnitFactory();
        enemySystem = new EnemySystem();
        cardCycleSystem = new CardCycleSystem();
        cardPlaySystem = new CardPlaySystem();
        turnSystem = new TurnSystem(enemySystem.tracker, currentState);

        //Last step, build the initialization order (ensures that all components are created before they are initialized)
        BuildInitOrder();

        return true;
    }

    private void InitializeUI()
    {
        battleUIController.SetReferences(cardCycleSystem.hand, enemySystem.tracker, playerUnitFactory.playerUnit);
        battleUIController.InitializeFromData(GameSession.Instance.battleData);
    }

    void BuildInitOrder()
    {
        initOrder = new List<IBattleComponent> {
        playerUnitFactory, enemySystem, cardCycleSystem, cardPlaySystem, turnSystem
    };
    }

    private void SubscribeToEvents()
    {

        enemySystem.tracker.OnAllEnemiesDefeated += HandleAllEnemiesDefeated;

        onPlayerDeath = () => EndBattle(BattleResult.Loss);
        battleContext.playerUnit.HealthState.OnDeath += onPlayerDeath;
        battleContext.BattleData.isBattleActive = true;
        battleContext.battleResultStatus = currentResult;
    }

    private void UnsubscribeFromEvents()
    {
        enemySystem.tracker.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;
        battleContext.playerUnit.HealthState.OnDeath -= onPlayerDeath;
    }

    private void HandleAllEnemiesDefeated()
    {
        EndBattle(BattleResult.Victory);
    }
    
    private void CloseBattleDomain() 
    {
        GameSession.Instance.ResetBattle();

        HandleReturnToMap();
    }
    public void HandleReturnToMap()
    {
        GameSession.Instance.ReturningFromBattle = true;
        GameStateRouter.Instance.LoadSceneBasedOnGameState(GameState.Map);
    }

    /// <summary>
    /// Resolves a named UI anchor point (e.g. "DeckPile") to its Transform in this battle scene.
    /// </summary>
    /// <param name="key">Identifier for the anchor being requested.</param>
    /// <param name="anchor">The resolved Transform, or null if the key is not recognized.</param>
    /// <returns>True if a matching anchor was found.</returns>
    public bool TryGetAnchor(string key, out Transform anchor)
    {
        switch (key)
        {
            case "DeckPile": anchor = deckPileAnchor; return true;
            case "Settings": anchor = settingsButtonAnchor; return true;
            default: anchor = null; return false;
        }
    }
}
