using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Meta.WitAi;
using MText;

public class WordReciteManager : MonoBehaviour
{
    private bool isDeciding = false;
    public bool resuming = false;

    // public bool isCountdownPaused = false;
    
    // Current word tracking
    int currentWordOrSentenceIndex;

    // Word lists
    string[] currentWordOrSentenceList;
    string[] changPaperWordList = new string[] { "hello","thirsty", "they", "hope", "up", "goodbye", "music", "tired", "nurse", "computer" };

    // string[] changPaperWordList = new string[] { "hello" };

    string[] changPaperSentenceList = new string[] { 
         "How do you like my music", "My glasses are comfortable", "What do you do", "I do not feel comfortable", "Bring my glasses here",
         "You are not right", "That is very clean", "My family is here"
    };

    // Track if the lists have been completed
    bool changComplete;
    
    bool uiComplete;

    // Text colours

    public Material correctColour;
    public Material incorrectColour;
    public Material defaultColour;
    public Material listeningColour;

    string[] uiControlsWordList = new string[] { "one", "two", "three", "proceed", "next", "repeat", "back", "pause", "menu", "help" };
    //  string[] uiControlsWordList = new string[] { "one" };

    
    string[] uiControlsSentenceList = new string[] { "go to main menu", "I would like to repeat sentences" };

    // External Managers

    public FreeSpeechManager _freeSpeechManager;

    WitListeningStateManager _witListeningStateManager;

    public ScoreManager _scoreManager;
    public LevelManager _levelManager;

    public Modular3DText reciteText3D;
    public Modular3DText partialText3D;

    [SerializeField] private Wit wit;

    void Start()
    {
        partialText3D = GameObject.FindWithTag("PartialText3D").GetComponent<Modular3DText>();
        _witListeningStateManager = GameObject.FindWithTag("WitListeningStateManager").GetComponent<WitListeningStateManager>();
        uiComplete = false;
        changComplete = false;

        if (_levelManager.currentLevel == "Level1")
        {
            _scoreManager.SetMaxScoreBasedOnWordListCount(changPaperWordList.Length + uiControlsWordList.Length);
            currentWordOrSentenceList = changPaperWordList; 

        }
        else if (_levelManager.currentLevel == "Level2")
        {
            _scoreManager.SetMaxScoreBasedOnWordListCount(changPaperSentenceList.Length + uiControlsSentenceList.Length);
            currentWordOrSentenceList = changPaperSentenceList;
        }

        reciteText3D.Material = defaultColour;

        // Start with the first chang word
        currentWordOrSentenceIndex = 0;
        StartCoroutine(StartCurrentWordCountdown());
    }

    public IEnumerator StartCurrentWordCountdown()
    {
        partialText3D.UpdateText("");

        reciteText3D.Material = defaultColour;
        string word = currentWordOrSentenceList[currentWordOrSentenceIndex];
        
        for (int i = 0; i < 3; i++)
        {
            Debug.Log("COUNTING DOWN - " + i);
            switch (i)
            {
                case 0:
                    reciteText3D.UpdateText("..." + word + "...");
                    break;
                case 1:
                    reciteText3D.UpdateText(".." + word + "..");
                    break;
                case 2:
                    reciteText3D.UpdateText("." + word + ".");
                    break;
            }
           
            yield return new WaitForSeconds(1);
        }
        reciteText3D.UpdateText(word);
        reciteText3D.Material = listeningColour;
    }

    public void OnMicrophoneTimeOut()
    {
        // Do not add to score
        StartCoroutine(ChangeTimeOutText());
    }

    IEnumerator ChangeTimeOutText()
    {
        reciteText3D.UpdateText("Timed out! Moving on...");
        yield return new WaitForSeconds(2);
        GoToNextWord();
    }

    public void OnMicrophoneInactivity()
    {
        // Do not add to score
        StartCoroutine(ChangeTimeOutText());
    }

    void GoToNextWord()
    {
        // If the next word does not exceed the limit
        if (currentWordOrSentenceIndex < currentWordOrSentenceList.Length-1)
        {
            currentWordOrSentenceIndex++;
        }
        
        // TODO re-enable
        _witListeningStateManager.ChangeState("ListeningForEverything");
        StartCoroutine(StartCurrentWordCountdown());

    }
    public void RepeatSameWord()
    {
        Debug.Log("REPEATING SAME WORD");
        // If it was halfway through a countdown, stop it
        // string word = currentWordOrSentenceList[currentWordOrSentenceIndex];
        // reciteText3D.UpdateText("..." + word + "...");

        // StopCoroutine(StartCurrentWordCountdown());
        _witListeningStateManager.ChangeState("ListeningForEverything");
    
        StartCoroutine(StartCurrentWordCountdown());
    }
    public void StartWordCheck(string transcription)
    {
        StartCoroutine(CheckRecitedWord(transcription));
    }
   
    public IEnumerator CheckRecitedWord(string text)
    {
        // Mic should be disabled / only listening for recite words here.
        // If the user just resumed, repeat the word countdown from the start   
        if (resuming) {
            // resuming = false;
            RepeatSameWord();
            yield break;
        }
        bool wordAnsweredCorrectly;

         if (isDeciding) {
            if (text.ToLower() == "next")
            {
                _levelManager.LevelComplete();
            }
            else if (text.ToLower() == "repeat")
            {
                _levelManager.RepeatLevel();
            }
        }

        // Does their answer match the current word?
        wordAnsweredCorrectly = text.ToLower() == currentWordOrSentenceList[currentWordOrSentenceIndex].ToLower();


        // Change text to reflect correct / incorrect 
        reciteText3D.UpdateText(wordAnsweredCorrectly ? "Correct! " : "Incorrect.");
        reciteText3D.Material = wordAnsweredCorrectly ? correctColour : incorrectColour;

        yield return new WaitForSeconds(2);

        if (wordAnsweredCorrectly)
        {
            Debug.Log("Word answered correctly");
            WordAnsweredCorrectly();
        }
        else
        {
            StartCoroutine(WordAnsweredIncorrectly());
        }


    }
    void AddScoreToScoreManager()
    {
         if (_levelManager.currentLevel == "Level1")
        {
            _scoreManager.Level1CurrentScore = _scoreManager.Level1CurrentScore + 1;

        }
        else if (_levelManager.currentLevel == "Level2")
        {
            _scoreManager.Level2CurrentScore = _scoreManager.Level2CurrentScore + 1;
        }
    }

    IEnumerator WordAnsweredIncorrectly()
    {

        reciteText3D.UpdateText("Try again!");
        yield return new WaitForSeconds(1);
        RepeatSameWord();
    }

    void MoveOnIfMoreWordsInList ()
    {
        Debug.Log("checking if more words in list" + currentWordOrSentenceIndex + " " + currentWordOrSentenceList.Length);
        if (currentWordOrSentenceIndex < currentWordOrSentenceList.Length - 1)
        {
                                Debug.Log("going to next word because index valid");

            GoToNextWord();
        }

        else
        {
            if (currentWordOrSentenceList == changPaperWordList) { changComplete = true; }
            if (currentWordOrSentenceList == uiControlsWordList) { uiComplete = true; }
            StartCoroutine(CheckWordListStatus());
        }
    }
    void WordAnsweredCorrectly()
    {
        AddScoreToScoreManager();
                    Debug.Log("moving on");

        MoveOnIfMoreWordsInList();
    }


    IEnumerator CheckWordListStatus()
    {
        // Either proceed to next word list, or end the game.

        if (changComplete && !uiComplete)
        { 
            currentWordOrSentenceList = uiControlsWordList;
            currentWordOrSentenceIndex = 0;
            reciteText3D.UpdateText("Great! Moving onto UI word list.");

            yield return new WaitForSeconds(2);
            StartCoroutine(StartCurrentWordCountdown());
            
        }
        else if (changComplete && uiComplete)
        {
            currentWordOrSentenceIndex = 0;
            reciteText3D.UpdateText("Finished!");

            GameOver();
        }

        else
        {
            Debug.Log("Something went wrong in conditional for list changing.");
        }
    }

    void GameOver()
    {
        _scoreManager.DisplayScoreInPartialTextSection();
        reciteText3D.UpdateText("Say 'next' to proceed.\nOr 'repeat' to repeat sentences.");
        isDeciding = true;
    }
}
