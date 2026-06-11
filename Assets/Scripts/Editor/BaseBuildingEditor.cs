using UnityEditor;
using UnityEngine;

namespace ShiftedSignal.Garden.Buildable.Editor
{
    [CustomEditor(typeof(BaseBuildable), true)]
    public class BaseBuildableEditor : UnityEditor.Editor
    {
        private SerializedProperty buildableData;
        private SerializedProperty isActive;
        private SerializedProperty ghostMaterial;

        private SerializedProperty durability;
        private SerializedProperty maxHP;

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
            buildableData = serializedObject.FindProperty("buildableData");
            isActive = serializedObject.FindProperty("IsActive");
            ghostMaterial = serializedObject.FindProperty("GhostMaterial");

            durability = serializedObject.FindProperty("Durability");
            maxHP = serializedObject.FindProperty("MaxHP");

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

            DrawGhostPreview();

            EditorGUILayout.Space(10);

            DrawStats();

            EditorGUILayout.Space(10);

            DrawEffects();

            EditorGUILayout.Space(10);

            DrawChildClassFields();
            
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawChildClassFields()
        {
            EditorGUILayout.LabelField(
                target.GetType().Name + " Fields",
                EditorStyles.boldLabel);

            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "buildableData",
                "IsActive",
                "GhostMaterial",
                "Durability",
                "MaxHP",
                "HasTimedEffects",
                "HasConstantEffects",
                "TimedEffects",
                "ConstantEffects",
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
        }

        private void DrawBuildInfo()
        {
            EditorGUILayout.LabelField("Build Info", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(buildableData);
            EditorGUILayout.PropertyField(isActive);
        }

        private void DrawGhostPreview()
        {
            EditorGUILayout.LabelField("Ghost Preview", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(
                ghostMaterial,
                new GUIContent("Ghost Material"));
        }

        private void DrawStats()
        {
            EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(durability);
            EditorGUILayout.PropertyField(maxHP, new GUIContent("Max HP"));
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