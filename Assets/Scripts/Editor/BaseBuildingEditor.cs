using UnityEditor;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable.Editor
{
    [CustomEditor(typeof(BaseBuildable), true)]
    public class BaseBuildableEditor : UnityEditor.Editor
    {
        private SerializedProperty mainRenderer;
        private SerializedProperty primaryMaterial;
        private SerializedProperty buildableData;
        private SerializedProperty isActive;

        private SerializedProperty hasTimedEffects;
        private SerializedProperty hasConstantEffects;

        private SerializedProperty timedEffects;
        private SerializedProperty constantEffects;

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
            mainRenderer = serializedObject.FindProperty("<MainRenderer>k__BackingField");
            primaryMaterial = serializedObject.FindProperty("PrimaryMaterial");
            buildableData = serializedObject.FindProperty("buildableData");
            isActive = serializedObject.FindProperty("IsActive");

            hasTimedEffects = serializedObject.FindProperty("HasTimedEffects");
            hasConstantEffects = serializedObject.FindProperty("HasConstantEffects");

            timedEffects = serializedObject.FindProperty("TimedEffects");
            constantEffects = serializedObject.FindProperty("ConstantEffects");

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

            DrawBuildInfo();

            EditorGUILayout.Space(10);

            DrawEffects();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBuildInfo()
        {
            EditorGUILayout.LabelField("Build Info", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(mainRenderer);
            EditorGUILayout.PropertyField(primaryMaterial);
            EditorGUILayout.PropertyField(buildableData);
            EditorGUILayout.PropertyField(isActive);
        }

        private void DrawEffects()
        {
            EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);

            DrawConstantEffects();

            EditorGUILayout.Space();

            DrawTimedEffects();
        }

        private void DrawConstantEffects()
        {
            EditorGUILayout.PropertyField(hasConstantEffects);

            if (!hasConstantEffects.boolValue)
                return;

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(
                constantEffects,
                new GUIContent("Constant Effects"),
                true);

            EditorGUI.indentLevel--;
        }

        private void DrawTimedEffects()
        {
            EditorGUILayout.PropertyField(hasTimedEffects);

            if (!hasTimedEffects.boolValue)
                return;

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(
                timedEffects,
                new GUIContent("Timed Effects"),
                true);

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

                EditorGUILayout.PropertyField(
                    runEveryXDays,
                    new GUIContent("Run Every X Days"));

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

                EditorGUILayout.PropertyField(
                    dayPeriodsToRun,
                    new GUIContent("Day Periods To Run"),
                    true);

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

                EditorGUILayout.PropertyField(
                    hoursToRun,
                    new GUIContent("Hours To Run"),
                    true);

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