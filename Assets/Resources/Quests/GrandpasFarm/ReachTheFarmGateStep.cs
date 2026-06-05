using ShiftedSignal.Garden.EntitySpace.PlayerSpace;
using ShiftedSignal.Garden.QuestSystem;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ReachTheFarmGateStep : QuestStep
{
    private bool reachedGate = false;

    private void Start()
    {
        UpdateState();
    }

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (reachedGate)
            return;

        if (!other.TryGetComponent<Player>(out _))
            return;

        reachedGate = true;
        UpdateState();
        FinishQuestStep();
    }

    private void UpdateState()
    {
        string state = reachedGate.ToString();
        string status = reachedGate
            ? "Reached the farm gate."
            : "Walk to the front gate.";

        ChangeState(state, status);
    }

    protected override void SetQuestStepState(string state)
    {
        reachedGate = bool.Parse(state);
        UpdateState();
    }
}