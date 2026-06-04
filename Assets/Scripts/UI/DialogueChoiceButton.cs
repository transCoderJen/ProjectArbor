using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;
using System;

public class DialogueChoiceButton : MonoBehaviour, ISelectHandler
{
    [Header("Components")]
    [SerializeField] private Button Button;
    [SerializeField] private TextMeshProUGUI ChoiceText;

    private int ChoiceIndex = -1;

    private void OnEnable()
    {
        Button.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        Button.onClick.RemoveListener(HandleClick);
    }

    public void SetChoiceText(string ChoiceTextString)
    {
        ChoiceText.text = ChoiceTextString;
    }

    public void SetChoiceIndex(int ChoiceIndex)
    {
        this.ChoiceIndex = ChoiceIndex;
    }

    public void SelectButton()
    {
        Button.Select();
    }

    public void OnSelect(BaseEventData eventData)
    {
        Bus<UpdateDialogueChoiceIndexEvent>.Raise(
            new UpdateDialogueChoiceIndexEvent(ChoiceIndex)
        );
    }

    private void HandleClick()
    {
        Bus<DialogueSubmitEvent>.Raise(new DialogueSubmitEvent());
    }
}
