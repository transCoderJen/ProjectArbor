using UnityEditor;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable.Editor
{
    [CustomEditor(typeof(BaseBuildable), true)]
    public class BaseBuildableEditor : UnityEditor.Editor
    {
        private SerializedProperty hasTimedEffects;
        private SerializedProperty timedEffects;

        private SerializedProperty runOnDayChanged;
        private SerializedProperty runEveryXDays;

        private SerializedProperty runOnDayPeriodChanged;
        private SerializedProperty dayPeriodsToRun;

        private SerializedProperty runOnHourChanged;
        private SerializedProperty hoursToRun;

        private SerializedProperty runOnDayStarted;
        private SerializedProperty runOnTimeChanged;
        private SerializedProperty runOnNightStarted;

        private void OnEnable()
        {
            hasTimedEffects = serializedObject.FindProperty("HasTimedEffects");
            timedEffects = serializedObject.FindProperty("TimedEffects");

            runOnDayChanged = serializedObject.FindProperty("RunOnDayChanged");
            runEveryXDays = serializedObject.FindProperty("RunEveryXDays");

            runOnDayPeriodChanged = serializedObject.FindProperty("RunOnDayPeriodChanged");
            dayPeriodsToRun = serializedObject.FindProperty("DayPeriodsToRun");

            runOnHourChanged = serializedObject.FindProperty("RunOnHourChanged");
            hoursToRun = serializedObject.FindProperty("HoursToRun");

            runOnDayStarted = serializedObject.FindProperty("RunOnDayStarted");
            runOnTimeChanged = serializedObject.FindProperty("RunOnTimeChanged");
            runOnNightStarted = serializedObject.FindProperty("RunOnNightStarted");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "HasTimedEffects",
                "TimedEffects",
                "RunOnDayChanged",
                "RunEveryXDays",
                "RunOnDayPeriodChanged",
                "DayPeriodsToRun",
                "RunOnHourChanged",
                "HoursToRun",
                "RunOnDayStarted",
                "RunOnTimeChanged",
                "RunOnNightStarted"
            );

            EditorGUILayout.Space(10);

            DrawTimedEffectsSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTimedEffectsSection()
        {
            EditorGUILayout.LabelField("Timed Effects", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(hasTimedEffects);

            if (!hasTimedEffects.boolValue)
                return;

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(timedEffects, true);

            EditorGUILayout.Space();

            DrawDayEvents();
            DrawDayPeriodEvents();
            DrawHourEvents();
            DrawOtherTimeEvents();

            EditorGUI.indentLevel--;
        }

        private void DrawDayEvents()
        {
            EditorGUILayout.LabelField("Day Events", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(runOnDayChanged);

            if (runOnDayChanged.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(runEveryXDays);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
        }

        private void DrawDayPeriodEvents()
        {
            EditorGUILayout.LabelField("Day Period Events", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(runOnDayPeriodChanged);

            if (runOnDayPeriodChanged.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(dayPeriodsToRun, true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
        }

        private void DrawHourEvents()
        {
            EditorGUILayout.LabelField("Hour Events", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(runOnHourChanged);

            if (runOnHourChanged.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(hoursToRun, true);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
        }

        private void DrawOtherTimeEvents()
        {
            EditorGUILayout.LabelField("Other Time Events", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(runOnDayStarted);
            EditorGUILayout.PropertyField(runOnTimeChanged);
            EditorGUILayout.PropertyField(runOnNightStarted);
        }
    }
}