using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using System;
using ShiftedSignal.Garden.Managers;

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

        private void OnDisable()
        {
            RemoveEventHandlers();
        }

        private void RemoveEventHandlers()
        {
            TimeManger.Instance.OnTimeChanged -= UpdateTimeUI;
            TimeManger.Instance.OnDayStarted -= UpdateDayUI;
            TimeManger.Instance.OnDayPeriodChanged -= UpdateDayPeriodUI;
        }

        private void AddEventHandlers()
        {
            TimeManger.Instance.OnTimeChanged += UpdateTimeUI;
            TimeManger.Instance.OnDayStarted += UpdateDayUI;
            TimeManger.Instance.OnDayPeriodChanged += UpdateDayPeriodUI;
        }

        private void UpdateDayUI()
        {
            // TODO Implement UpdateDayUI
        }

        private void UpdateTimeUI()
        {
            TimeText.text = TimeManger.Instance.FormattedTime;
        }

        private void UpdateDayPeriodUI(DayPeriod period)
        {
            switch(period)
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
    }
}