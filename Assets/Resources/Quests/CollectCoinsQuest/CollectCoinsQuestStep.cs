using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.QuestSystem;

public class CollectCoinsQuestStep : QuestStep
{
    private int coinsCollected = 0;
    private int coinsToComplete = 5;

    void OnEnable()
    {
        Bus<CurrencyUpdatedEvent>.OnEvent += HandleUpdateCurrency;
    }

    void OnDisable()
    {
        Bus<CurrencyUpdatedEvent>.OnEvent -= HandleUpdateCurrency;
    }

    private void HandleUpdateCurrency(CurrencyUpdatedEvent evt)
    {
        if (evt.Coins <= 0)
            return;
        
        coinsCollected += evt.Coins;
        UpdateState();

        if (coinsCollected >= coinsToComplete)
        {
            FinishQuestStep();
        }
    }

    private void UpdateState()
    {
        string state = coinsCollected.ToString();
        ChangeState(state);
    }
    
    protected override void SetQuestStepState(string state)
    {
        this.coinsCollected = System.Int32.Parse(state);
        UpdateState();
    }
}
