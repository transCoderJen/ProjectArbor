using System.Collections;
using System.Collections.Generic;
using ShiftedSignal.Garden.Misc;
using UnityEngine;

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

        private Dictionary<TransitionType, GameObject> transitionDictionary = new();
        private Animator transition;

        protected override void Awake()
        {
            base.Awake();

            transitionDictionary.Clear();

            foreach (var entry in transitionControllers)
            {
                if (entry.controller == null)
                    continue;

                if (!transitionDictionary.ContainsKey(entry.type))
                {
                    transitionDictionary.Add(entry.type, entry.controller);
                }
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
            foreach (var entry in transitionControllers)
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

        private IEnumerator FadeOut(string sceneName, string targetEntranceName, TransitionType transitionType)
        {
            bool hasTransition = SetActiveTransition(transitionType);

            if (hasTransition)
                transition.SetTrigger("Start");

            yield return Helpers.GetWait(1f);

            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            SceneManager.Instance.SetTransitionName(targetEntranceName);
        }

        private IEnumerator FadeIn(TransitionType transitionType)
        {
            bool hasTransition = SetActiveTransition(transitionType);

            if (hasTransition)
                transition.SetTrigger("End");

            yield return Helpers.GetWait(1f);
        }
    }
}