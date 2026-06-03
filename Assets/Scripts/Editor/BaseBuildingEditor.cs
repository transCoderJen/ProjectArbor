using UnityEditor;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable.Editor
{
    [CustomEditor(typeof(BaseBuildable), true)]
    public class BaseBuildableEditor : UnityEditor.Editor
    {
        private SerializedProperty mainRenderer;
        private SerializedProperty primaryMaterial;
        private SerializedProperty gridLayer;
        private SerializedProperty buildableData;
        private SerializedProperty isActive;

        private SerializedProperty hasBuildingEffect;
        private SerializedProperty effects;

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
            gridLayer = serializedObject.FindProperty("GridLayer");
            buildableData = serializedObject.FindProperty("buildableData");
            isActive = serializedObject.FindProperty("IsActive");

            hasBuildingEffect = serializedObject.FindProperty("HasBuildingEffect");
            effects = serializedObject.FindProperty("Effects");

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

            EditorGUILayout.Space();

            DrawBuildingEffectOptions();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawBuildInfo()
        {
            EditorGUILayout.LabelField("Build Info", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(mainRenderer);
            EditorGUILayout.PropertyField(primaryMaterial);
            EditorGUILayout.PropertyField(gridLayer);
            EditorGUILayout.PropertyField(buildableData);
            EditorGUILayout.PropertyField(isActive);
        }

        private void DrawBuildingEffectOptions()
        {
            EditorGUILayout.LabelField("Building Effect", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(hasBuildingEffect);

            if (!hasBuildingEffect.boolValue)
            {
                return;
            }

            EditorGUILayout.Space();

            DrawEffects();
            DrawDayEvents();
            DrawDayPeriodEvents();
            DrawHourEvents();
            DrawOtherTimeEvents();
        }

        private void DrawEffects()
        {
            EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(effects, new GUIContent("Buildable Effects"), true);

            EditorGUILayout.Space();
        }

        private void DrawDayEvents()
        {
            EditorGUILayout.LabelField("Day Events", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(runOnDayChanged);

            if (runOnDayChanged.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(runEveryXDays, new GUIContent("Run Every X Days"));
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
                EditorGUILayout.PropertyField(dayPeriodsToRun, new GUIContent("Day Periods To Run"), true);
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
                EditorGUILayout.PropertyField(hoursToRun, new GUIContent("Hours To Run"), true);
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