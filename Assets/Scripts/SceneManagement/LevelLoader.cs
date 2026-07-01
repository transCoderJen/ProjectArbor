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

        public void LoadScene(string sceneName, string targetEntranceName, TransitionType transitionType)
        {
            StartCoroutine(FadeOut(sceneName, targetEntranceName, transitionType));
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

        // private IEnumerator FadeOut(string sceneName, string targetEntranceName, TransitionType transitionType)
        // {
        //     string currentSceneName =
        //         UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        //     bool leavingWorldScene = currentSceneName == WorldSceneName;
        //     bool enteringWorldScene = sceneName == WorldSceneName;

        //     bool hasTransition = SetActiveTransition(transitionType);

        //     if (hasTransition)
        //         transition.SetTrigger("Start");

        //     yield return Helpers.GetWait(1f);

        //     if (leavingWorldScene)
        //     {
        //         CaptureGridBeforeSceneChange();

        //         if (SaveManager.Instance != null)
        //             SaveManager.Instance.CaptureWorldRuntimeData();

        //         Debug.Log("[LevelLoader] Captured world runtime data before leaving world scene.");
        //     }

        //     UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);

        //     ShiftedSignal.Garden.SceneManagement.SceneManager.Instance
        //         .SetTransitionName(targetEntranceName);

        //     yield return null;
        //     yield return null;

        //     if (enteringWorldScene)
        //     {
        //         if (SaveManager.Instance != null)
        //             SaveManager.Instance.LoadWorldRuntimeData();

        //         Debug.Log("[LevelLoader] Loaded world runtime data after entering world scene.");

        //         if (GridManager.Instance != null)
        //         {
        //             Debug.Log("[LevelLoader] Requesting grid restore after world scene load.");
        //             GridManager.Instance.RequestGridRestore();
        //         }
        //     }
        // }

        private IEnumerator FadeOut(string sceneName, string targetEntranceName, TransitionType transitionType)
{
    Stopwatch totalStopwatch = Stopwatch.StartNew();

    string currentSceneName =
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

    bool leavingWorldScene = currentSceneName == WorldSceneName;
    bool enteringWorldScene = sceneName == WorldSceneName;

    bool hasTransition = SetActiveTransition(transitionType);

    if (hasTransition)
        transition.SetTrigger("Start");

    yield return Helpers.GetWait(1f);

    if (leavingWorldScene)
    {
        Stopwatch captureStopwatch = Stopwatch.StartNew();

        CaptureGridBeforeSceneChange();

        if (SaveManager.Instance != null)
            SaveManager.Instance.CaptureWorldRuntimeData();

        captureStopwatch.Stop();

        Debug.Log($"[LevelLoader Timing] Capture world data: {captureStopwatch.ElapsedMilliseconds} ms");
    }

    Stopwatch sceneLoadStopwatch = Stopwatch.StartNew();

    UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);

    sceneLoadStopwatch.Stop();

    Debug.Log($"[LevelLoader Timing] SceneManager.LoadScene({sceneName}): {sceneLoadStopwatch.ElapsedMilliseconds} ms");

    ShiftedSignal.Garden.SceneManagement.SceneManager.Instance
        .SetTransitionName(targetEntranceName);

    yield return null;
    yield return null;

    if (enteringWorldScene)
    {
        Stopwatch restoreStopwatch = Stopwatch.StartNew();

        if (SaveManager.Instance != null)
            SaveManager.Instance.LoadWorldRuntimeData();

        restoreStopwatch.Stop();

        Debug.Log($"[LevelLoader Timing] Load world runtime data: {restoreStopwatch.ElapsedMilliseconds} ms");

        if (GridManager.Instance != null)
        {
            Stopwatch gridRestoreStopwatch = Stopwatch.StartNew();

            GridManager.Instance.RequestGridRestore();

            gridRestoreStopwatch.Stop();

            Debug.Log($"[LevelLoader Timing] RequestGridRestore: {gridRestoreStopwatch.ElapsedMilliseconds} ms");
        }
    }

    totalStopwatch.Stop();

    Debug.Log($"[LevelLoader Timing] Total transition to {sceneName}: {totalStopwatch.ElapsedMilliseconds} ms");
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