using System.Collections;
using System.Collections.Generic;
using ShiftedSignal.Garden.GridSystem;
using ShiftedSignal.Garden.Managers;
using ShiftedSignal.Garden.Misc;
using ShiftedSignal.Garden.SaveAndLoad;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace ShiftedSignal.Garden.SceneManagement
{
    public enum TransitionType
    {
        Fade,
        Wipe,
        Door,
        Dream
    }

    public class LevelLoader : Singleton<LevelLoader>
    {
        [System.Serializable]
        public class TransitionControllerEntry
        {
            public TransitionType type;
            public GameObject controller;
        }

        [Header("Transitions")]
        [SerializeField] private List<TransitionControllerEntry> transitionControllers = new();

        [Header("World Runtime State")]
        [SerializeField] private string WorldSceneName = "World";

        private Dictionary<TransitionType, GameObject> transitionDictionary = new();
        private Animator transition;

        protected override void Awake()
        {
            base.Awake();

            transitionDictionary.Clear();

            foreach (TransitionControllerEntry entry in transitionControllers)
            {
                if (entry.controller == null)
                    continue;

                if (!transitionDictionary.ContainsKey(entry.type))
                    transitionDictionary.Add(entry.type, entry.controller);
            }

            DisableAllTransitions();
        }

        public void LoadScene(
            string sceneName,
            string targetEntranceName,
            TransitionType transitionType,
            string loadingMessage)
        {
            StartCoroutine(FadeOut(sceneName, targetEntranceName, transitionType, loadingMessage));
        }

        public void StartScene(TransitionType transitionType)
        {
            StartCoroutine(FadeIn(transitionType));
        }

        private void DisableAllTransitions()
        {
            foreach (TransitionControllerEntry entry in transitionControllers)
            {
                if (entry.controller != null)
                    entry.controller.SetActive(false);
            }

            transition = null;
        }

        private bool SetActiveTransition(TransitionType transitionType)
        {
            DisableAllTransitions();

            if (!transitionDictionary.TryGetValue(transitionType, out GameObject selectedController))
            {
                Debug.LogWarning($"No transition controller found for {transitionType}");
                return false;
            }

            selectedController.SetActive(true);
            transition = selectedController.GetComponent<Animator>();

            if (transition == null)
            {
                Debug.LogWarning($"Transition controller {selectedController.name} has no Animator component.");
                return false;
            }

            return true;
        }

        private IEnumerator FadeOut(
                string sceneName,
                string targetEntranceName,
                TransitionType transitionType,
                string loadingMessage)
        {
            var totalWatch = LoadProfiler.Start("Total Transition");

            string currentSceneName =
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            bool leavingWorldScene = currentSceneName == WorldSceneName;
            bool enteringWorldScene = sceneName == WorldSceneName;

            LoadingScreenAnimationTriggers.CurrentLoadingMessage =
                string.IsNullOrWhiteSpace(loadingMessage)
                    ? "Loading..."
                    : loadingMessage;

            bool hasTransition = SetActiveTransition(transitionType);

            if (hasTransition)
                transition.SetTrigger("Start");

            // Wait for fade-out animation to finish.
            // Animation event at the end of fade-out should call ShowLoadingScreen().
            yield return Helpers.GetWait(1f);

            if (leavingWorldScene)
            {
                var captureWatch = LoadProfiler.Start("Capture World Runtime");

                CaptureGridBeforeSceneChange();

                if (SaveManager.Instance != null)
                    SaveManager.Instance.CaptureWorldRuntimeData();

                LoadProfiler.End("Capture World Runtime", captureWatch);
            }

            var sceneLoadWatch =
                LoadProfiler.Start($"Async Scene Load + Activation ({sceneName})");

            AsyncOperation operation =
                UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

            if (operation == null)
            {
                Debug.LogError($"[LevelLoader] Failed to start async scene load: {sceneName}");
                yield break;
            }

            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                if (LoadingScreen.Instance != null)
                    LoadingScreen.Instance.SetProgress(operation.progress / 0.9f);

                yield return null;
            }

            ShiftedSignal.Garden.SceneManagement.SceneManager.Instance
                .SetTransitionName(targetEntranceName);

            operation.allowSceneActivation = true;

            while (!operation.isDone)
                yield return null;

            LoadProfiler.End(
                $"Async Scene Load + Activation ({sceneName})",
                sceneLoadWatch);

            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();

            if (enteringWorldScene)
            {
                var restoreWatch = LoadProfiler.Start("Restore World Runtime");

                if (SaveManager.Instance != null)
                    SaveManager.Instance.LoadWorldRuntimeData();

                LoadProfiler.End("Restore World Runtime", restoreWatch);

                if (GridManager.Instance != null)
                {
                    var gridWatch = LoadProfiler.Start("Grid Restore");

                    GridManager.Instance.RequestGridRestore();

                    LoadProfiler.End("Grid Restore", gridWatch);
                }
            }

            if (LoadingScreen.Instance != null)
                LoadingScreen.Instance.SetProgress(1f);

            // Animation event at the start of fade-in should call HideLoadingScreen().
            yield return FadeIn(transitionType);

            LoadProfiler.End("Total Transition", totalWatch);
        }

        private IEnumerator FadeIn(TransitionType transitionType)
        {
            bool hasTransition = SetActiveTransition(transitionType);

            if (hasTransition)
                transition.SetTrigger("End");

            yield return Helpers.GetWait(1f);
        }

        private void CaptureGridBeforeSceneChange()
        {
            if (GridInfo.Instance == null)
                return;

            if (GridManager.Instance == null)
                return;

            if (GridManager.Instance.BlockRows == null || GridManager.Instance.BlockRows.Count == 0)
                return;

            GridInfo.Instance.UpdateInfoFromGrid();

            Debug.Log("[LevelLoader] Captured grid before scene change.");
        }
    }
}