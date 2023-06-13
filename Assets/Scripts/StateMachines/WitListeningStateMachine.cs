using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "State Machine/Wit Listening State Machine")]
public class WitListeningStateMachine : ScriptableObject
{
    public enum State
    {
        NotListening,
        ListeningForMenuCommandsOnly,
        ListeningForEverything,
        ListeningForRecitedWordsOnly,
        ListeningForNavigationCommandsOnly,
        ListeningForConfirmation
    }

    public enum Transition
    {
        TransitionNotListeningToMenuCommandsOnly,
        TransitionMenuCommandsOnlyToListeningForEverything,
        TransitionListeningForEverythingToListeningForRecitedWordsOnly,
        TransitionListeningForRecitedWordsOnlyToNotListening,
        TransitionListeningForRecitedWordsOnlyToListeningForNavigationCommandsOnly,
        TransitionListeningForNavigationCommandsOnlyToListeningForConfirmation,
    }

    public State currentState;
    public List<string> menuCommandPhrases; // List of phrases for MenuCommandsOnly state
}
