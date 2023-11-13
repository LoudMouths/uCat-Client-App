using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MText;

public class DebuggerClass : MonoBehaviour
{
    public Animator boardAnimator;
    public FreeSpeechManager _freeSpeechManager;

    public WitListeningStateManager _witListeningStateManager;

    private WordReciteManager _wordReciteManager;

    private LevelTransition _levelTransition;
    private DialogueManager _dialogueManager;

    public Modular3DText debugText;
    // Start is called before the first frame update
    void Start()
    {
        _levelTransition = FindObjectOfType<LevelTransition>();
        _wordReciteManager = GetComponent<WordReciteManager>();
        _freeSpeechManager = GetComponent<FreeSpeechManager>();
        _witListeningStateManager = GetComponent<WitListeningStateManager>();
        _dialogueManager = GetComponent<DialogueManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            ActivateMenuViaVoice();
        }

        if (Input.GetKeyDown(KeyCode.Space)) {
            _freeSpeechManager.HandleFullTranscription("milk");
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) {
             _freeSpeechManager.HandleFullTranscription("Yes");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2)) {
            _levelTransition.BeginSpecificLevelTransition("Level2");
        }
        if (Input.GetKeyDown(KeyCode.Alpha3)) {
            _levelTransition.BeginSpecificLevelTransition("Level3");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4)) {
            _wordReciteManager.StopAllCoroutines();
            _wordReciteManager.LevelTaskIsComplete();
        }

        if (Input.GetKeyDown(KeyCode.F5)) {
            _dialogueManager.SkipToNextLine();
        }
    }

    void ActivateMenuViaVoice() {
        _freeSpeechManager.HandleFullTranscription("menu");
    }
}
