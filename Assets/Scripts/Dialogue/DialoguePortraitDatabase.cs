using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShiftedSignal.Garden.Dialogue
{
    /// <summary>
    /// Stores all dialogue speaker data used by the dialogue UI.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DialogueSpeakerDatabase",
        menuName = "Shifted Signal/Garden/Dialogue/Speaker Database")]
    public class DialogueSpeakerDatabase : ScriptableObject
    {
        [SerializeField] private List<DialogueSpeakerData> Speakers = new();

        /// <summary>
        /// Attempts to find speaker data by speaker ID.
        /// </summary>
        public bool TryGetSpeaker(string speakerId, out DialogueSpeakerData speakerData)
        {
            speakerData = null;

            if (string.IsNullOrWhiteSpace(speakerId))
                return false;

            foreach (DialogueSpeakerData speaker in Speakers)
            {
                if (speaker == null)
                    continue;

                if (speaker.SpeakerID == speakerId)
                {
                    speakerData = speaker;
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Data used to visually represent a dialogue speaker.
    /// </summary>
    [Serializable]
    public class DialogueSpeakerData
    {
        public string SpeakerID;
        public string DisplayName;
        public Sprite Portrait;
        public Color NameColor = Color.white;
    }
}