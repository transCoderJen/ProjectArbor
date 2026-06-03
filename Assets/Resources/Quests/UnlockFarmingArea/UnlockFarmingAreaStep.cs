using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using ShiftedSignal.Garden.QuestSystem;

public class UnlockFarmingAreaStep : QuestStep
{
    private int farmingBlocksUnlocked = 0;
    private int farmingBlocksToComplete = 100;

    private void Start()
    {
        UpdateState();
    }

    private void OnEnable()
    {
        Bus<UnlockFarmingAreaEvent>.OnEvent += HandleUnlockFarmingArea;
    }

    private void OnDisable()
    {
        Bus<UnlockFarmingAreaEvent>.OnEvent -= HandleUnlockFarmingArea;
    }

    private void HandleUnlockFarmingArea(UnlockFarmingAreaEvent evt)
    {
        farmingBlocksUnlocked++;

        UpdateState();

        if (farmingBlocksUnlocked >= farmingBlocksToComplete)
        {
            FinishQuestStep();
        }
    }

    private void UpdateState()
    {
        string state = farmingBlocksUnlocked.ToString();

        string status =
            "Unlocked " +
            farmingBlocksUnlocked +
            " / " +
            farmingBlocksToComplete +
            " farming blocks.";

        ChangeState(state, status);
    }

    protected override void SetQuestStepState(string state)
    {
        farmingBlocksUnlocked = System.Int32.Parse(state);
        UpdateState();
    }
}