using UnityEngine;
using Meta.WitAi;
using UnityEngine.SceneManagement;
using EListeningState = WitListeningStateManager.ListeningState;
using EConfirmationResponseType = ConfirmationHandler.ConfirmationResponseType;
using EMenuActivationResponseType = UICommandHandler.MenuActivationResponseType;
using EMenuNavigationResponseType = UICommandHandler.MenuNavigationResponseType;
using EProceedResponseType = ConfirmationHandler.ProceedResponseType;
using System.Collections;

namespace MText
{
    public class FreeSpeechManager : MonoBehaviour
    {
        public UIManager _uiManager;
        public WordReciteManager _wordReciteManager;
        public Modular3DText partialText3D;
        public Modular3DText subtitleText3D;

        // Used to cache the text when we are in a confirmation state
        private string originallyUtteredText;

        public WitListeningStateManager _witListeningStateManager;

        public string cachedText = "";

        public bool micIsActive;

        public float hasBeenListeningForSeconds;

        Scene scene;

        void Start()
        {   
            subtitleText3D = GameObject.FindWithTag("SubtitleText3D").GetComponent<Modular3DText>();
            scene = SceneManager.GetActiveScene();
        }

        public void HandlePartialTranscription(string text)
        {
            // Always update subtitles when attempting speech
            subtitleText3D.UpdateText(text);
            if (_witListeningStateManager.RecitingWordsIsAllowed()) {
                partialText3D.UpdateText(text);
            }
        }

        public void HandleMenuActivationResponse(EMenuActivationResponseType menuActivationResponse) {
            switch (menuActivationResponse) {
                case EMenuActivationResponseType.POSITIVE_ACTIVATE_MENU_RESPONSE:
                    _uiManager.ActivateMenu();
                    break;
                case EMenuActivationResponseType.UNKNOWN_ACTIVATION_RESPONSE:
                    Debug.Log("Activating menu was allowed but phrase was invalid: " + menuActivationResponse);
                    break;
                default:
                    break;
            }
        }

        public void HandleFullTranscription(string text)
        {
            if (_witListeningStateManager.MenuActivationCommandsAreAllowed()) {
                // Listen for menu activation
                EMenuActivationResponseType menuActivationResponse = UICommandHandler.CheckIfMenuActivationCommandsWereSpoken(text);
                HandleMenuActivationResponse(menuActivationResponse);
            }

            if (_witListeningStateManager.MenuNavigationCommandsAreAllowed()) {
                // Listen for commands within the menu
                EMenuNavigationResponseType menuNavigationResponse = UICommandHandler.CheckIfMenuNavigationCommandsWereSpoken(text);
                _uiManager.ActivateMenuNavigationCommandsBasedOnResponse(menuNavigationResponse);
            }

            if (_witListeningStateManager.currentListeningState == EListeningState.ListeningForConfirmation) {
                //Listen for 'yes' or 'no?' (confirmation)
                EConfirmationResponseType confirmationResponse = ConfirmationHandler.CheckIfConfirmationWasSpoken(text);
                StartCoroutine(ProceedBasedOnConfirmation(confirmationResponse, originallyUtteredText));
            }

            if (_witListeningStateManager.currentListeningState == EListeningState.ListeningForNextOrRepeat) {
                // Listen for 'next' or 'repeat' (word recite)
                Debug.Log("Checking if next or repeat was spoken: " + text);
                EProceedResponseType proceedResponse = ConfirmationHandler.CheckIfProceedPhraseWasSpoken(text);
                StartCoroutine(_wordReciteManager.HandleProceedResponse(proceedResponse));
            }
            if (_witListeningStateManager.RecitingWordsIsAllowed()) {
                // Activate Tasks (recite words, etc) if in any valid reciting states
                ActivateTasksBasedOnTranscription(text);
            }

            else {
                Debug.LogError("Did not activate word task with phrase " + text + " . You are probably in the menu." + _witListeningStateManager.currentListeningState);
                 if (_witListeningStateManager.MenuNavigationCommandsAreAllowed()) {
                    _witListeningStateManager.ReactivateToTryMenuNavigationCommandsAgain();
                 }
            }
        }

        private IEnumerator ProceedBasedOnConfirmation(EConfirmationResponseType confirmationResponse, string originallyUtteredText) {

            string confirmationText = ConfirmationHandler.confirmationResponses[confirmationResponse];
            partialText3D.UpdateText(confirmationText);
            yield return new WaitForSeconds(ConfirmationHandler.confirmationWaitTimeInSeconds);

            switch (confirmationResponse) {
                case EConfirmationResponseType.POSITIVE_CONFIRMATION_RESPONSE:
                    _wordReciteManager.MoveOnIfMoreWordsInList();
                    break;
                case EConfirmationResponseType.NEGATIVE_CONFIRMATION_RESPONSE:
                    _wordReciteManager.RepeatSameWord();
                    break;
                case EConfirmationResponseType.UNKNOWN_CONFIRMATION_RESPONSE:
                    ConfirmWhatUserSaid(originallyUtteredText);
                    break;
                default:
                    Debug.LogError("ERROR: Confirmation response type not recognised");
                    break;
            }
        }

        public void OnStartListening() {
            // Clear the text
            subtitleText3D.UpdateText("STARTED LISTENING");
            micIsActive = true;
            if (_witListeningStateManager.TimeoutCountingIsAllowed()) {
                hasBeenListeningForSeconds = 0;
            }
        }

        public void OnStoppedListening() {
            // Clear the text
            subtitleText3D.UpdateText("STOPPED LISTENING: " + hasBeenListeningForSeconds + " seconds");
            micIsActive = false;
        }

        public void OnTimeOut() {
            // Clear the text
            subtitleText3D.UpdateText("TIMED OUT " + hasBeenListeningForSeconds + " seconds");
            micIsActive = false;
        }

        public void OnInactivity() {
            // Do not time out if we are in the menu, etc. Only when reciting.
            if (_witListeningStateManager.TimeoutCountingIsAllowed()) {
                _witListeningStateManager.TransitionToState(EListeningState.NotListening);
                micIsActive = false;
                subtitleText3D.UpdateText("INACTIVITY" + hasBeenListeningForSeconds + " seconds");
                hasBeenListeningForSeconds = 0;
                _wordReciteManager.OnMicrophoneTimeOut();
            }
        }

        void Update() {
            // Only count seconds if the mic is active and we are reciting words (not in menu etc)
            if (micIsActive && _witListeningStateManager.TimeoutCountingIsAllowed()) {
                hasBeenListeningForSeconds += Time.deltaTime;
            }

            if (hasBeenListeningForSeconds > 5) {
                OnInactivity();
            }
        }

        public void ActivateTasksBasedOnTranscription(string text)
        {        
            if (SceneManager.GetActiveScene().name != "Level3") 
            {
                _wordReciteManager.StartWordCheck(text);
          
            } else {
                 // Run level 3 task
                 // Update the spoken text
                CalculateCachedText(text);
                // Only confirm yes/no if the 'next/proceed' prompt is not active
                if (_wordReciteManager.isDecidingToProceedOrNot) {
                    StartCoroutine(_wordReciteManager.CheckRecitedWord(text));
                } else {
                    ConfirmWhatUserSaid(text.ToLower());
                }
            }

        }

        public void ConfirmWhatUserSaid(string text) {
            originallyUtteredText = text;
            Debug.Log("Setting state to confirtmation mode ");
            _witListeningStateManager.TransitionToState(EListeningState.ListeningForConfirmation);

            // Ask them to confirm
            partialText3D.UpdateText("Did you say " + text + "?");
        }

     void CalculateCachedText(string newText) {
        // Prevent the text log becoming too long
        int maxLengthBasedOnScene = 0;
        Scene scene = SceneManager.GetActiveScene();
        switch (scene.name) {
            case "Level1":
                maxLengthBasedOnScene = 40;
                break;
            case "Level2":
                maxLengthBasedOnScene = 70;
                break;
            case "Level3":
                maxLengthBasedOnScene = 120;
                break;
            default:
                maxLengthBasedOnScene = 30;
                break;}

        if (cachedText.Length > maxLengthBasedOnScene) {
            cachedText = newText;
        } else {
            cachedText = cachedText + '\n' + ' ' + newText;
        }
    }
    }
}
