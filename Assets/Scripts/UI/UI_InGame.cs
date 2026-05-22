using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using System;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.EventBus;
using ShiftedSignal.Garden.Events;

namespace ShiftedSignal.Garden.UserInterface
{
    public class UI_InGame : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI TimeText;

        void Start()
        {
            AddEventHandlers();
            UpdateTimeUI();
        }

        private void OnEnable()
        {
            AddEventHandlers();
        }

        private void OnDisable()
        {
            RemoveEventHandlers();
        }

        private void AddEventHandlers()
        {
            Bus<TimeChangedEvent>.OnEvent += UpdateTimeUI;
            Bus<DayStartedEvent>.OnEvent += UpdateDayUI;
            Bus<DayPeriodChangedEvent>.OnEvent += UpdateDayPeriodUI;
        }

        private void RemoveEventHandlers()
        {
            Bus<TimeChangedEvent>.OnEvent -= UpdateTimeUI;
            Bus<DayStartedEvent>.OnEvent -= UpdateDayUI;
            Bus<DayPeriodChangedEvent>.OnEvent -= UpdateDayPeriodUI;
        }

        private void UpdateDayPeriodUI(DayPeriodChangedEvent evt)
        {
            switch(evt.DayPeriod)
            {
                case DayPeriod.Dawn:
                    Debug.Log("It is currently Dawn");
                    break;
                case DayPeriod.Morning:
                    Debug.Log("It is currently Morning");
                    break;
                case DayPeriod.Afternoon:
                    Debug.Log("It is currently Afternoon");
                    break;
                case DayPeriod.Evening:
                    Debug.Log("It is currently Evening");
                    break;
                case DayPeriod.Night:
                    Debug.Log("It is currently Night");
                    break;
                
            }
        }

        private void UpdateDayUI(DayStartedEvent args)
        {
            // TODO Implement UpdateDayUI
        }

        private void UpdateTimeUI(TimeChangedEvent evt)
        {
            TimeText.text = TimeManger.Instance.FormattedTime;
        }

        private void UpdateTimeUI()
        {
            TimeText.text = TimeManger.Instance.FormattedTime;
        }
    }
}