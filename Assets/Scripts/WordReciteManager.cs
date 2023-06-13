using System.Collections;
using UnityEngine;
using Meta.WitAi;
using MText;

public class WordReciteManager : MonoBehaviour
{
    public bool isDeciding = false;
    public bool resuming = false;

    // Current word tracking
    int currentWordOrSentenceIndex;

    // Word lists
    string[] currentWordOrSentenceList;
    // This word list combines some from Chang paper and some from Willet
    string[] changAndWilletPaperWordList = new string[] { "hello", "computer", "choice", "day", "kite", "though", "veto", "were", "tired", "nurse" };
    

    // string[] changAndWilletPaperWordList = new string[] { "hello" };

    string[] changPaperSentenceList = new string[] { 
         "How do you like my music", "My glasses are comfortable", "What do you do", "I do not feel comfortable", "Bring my glasses here",
         "You are not right", "That is very clean", "My family is here"
    };

    // string[] openQuestionsList = new string[] { "What is your name", "What is your favourite colour",
    //  "What is your favourite food", "What is your favourite animal", "What is your favourite movie" };

     string[] openQuestionsList = new string[] { "What is your name" };

    // Track if the lists have been completed
    bool changComplete;
    
    bool uiComplete;

    bool openQuestionsComplete;

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

    public Modular3DText subtitleText3D;

    [SerializeField] private Wit wit;

    void Start()
    {
        // Assigning gameobjects
        wit = GameObject.FindWithTag("Wit").GetComponent<Wit>();
        subtitleText3D = GameObject.FindWithTag("SubtitleText3D").GetComponent<Modular3DText>();
        partialText3D = GameObject.FindWithTag("PartialText3D").GetComponent<Modular3DText>();
        _witListeningStateManager = GameObject.FindWithTag("WitListeningStateManager").GetComponent<WitListeningStateManager>();
       
        // Game state variables
        uiComplete = false;
        changComplete = false;

        if (_levelManager.currentLevel == "Level1")
        {
            _scoreManager.SetMaxScoreBasedOnWordListCount(changAndWilletPaperWordList.Length + uiControlsWordList.Length);
            currentWordOrSentenceList = changAndWilletPaperWordList; 

        }
        else if (_levelManager.currentLevel == "Level2")
        {
            _scoreManager.SetMaxScoreBasedOnWordListCount(changPaperSentenceList.Length + uiControlsSentenceList.Length);
            currentWordOrSentenceList = changPaperSentenceList;
        }

        else if (_levelManager.currentLevel == "Level3")
        {
            currentWordOrSentenceList = openQuestionsList;
        }

        // Do not need to set max score for lv 3 as there is no right or wrong

        reciteText3D.Material = defaultColour;

        // Start with the first chang word
        currentWordOrSentenceIndex = 0;
        StartCoroutine(StartCurrentWordCountdown());
    }

    public IEnumerator StartCurrentWordCountdown()
    {
        subtitleText3D.UpdateText("");

        if (_witListeningStateManager.currentListeningState == "ListeningForNavigationCommandsOnly")
        {
            Debug.Log("Breaking out of countdown because in navigation state");
            yield break;
        }

        partialText3D.UpdateText("");
        reciteText3D.Material = defaultColour;
        string word = currentWordOrSentenceList[currentWordOrSentenceIndex];
    
        for (float i = 0; i < 3; i++)
        {

        if (_witListeningStateManager.currentListeningState == "ListeningForNavigationCommandsOnly") {
            Debug.Log("Breaking out of countdown because in navigation state");
            yield break;
        }
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
                    // Discard anything said during countdown and start fresh
                    _witListeningStateManager.ChangeState("NotListening");
                    break;
            }
           
            yield return new WaitForSeconds(1);
        }

        // Countdown finished, start listening for the word
        if (_witListeningStateManager.currentListeningState == "ListeningForNavigationCommandsOnly") {
             Debug.Log("EXITING OUT BECAUSE WE ARE IN MENU STATE " + _witListeningStateManager.currentListeningState);
            yield break;
        } else {
            subtitleText3D.UpdateText("");
            Debug.Log("CONTINUING, STATE IS " + _witListeningStateManager.currentListeningState);
            _witListeningStateManager.ChangeState("ListeningForEverything");
            reciteText3D.UpdateText(word);
            reciteText3D.Material = listeningColour;
        }

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
    void Update() {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GoToNextWord();
        }
    }

    public void GoToNextWord()
    {
        _witListeningStateManager.ChangeState("ListeningForMenuCommandsOnly");
        // If the next word does not exceed the limit
        if (currentWordOrSentenceIndex < currentWordOrSentenceList.Length-1)
        {
            currentWordOrSentenceIndex++;
        }
        
        StartCoroutine(StartCurrentWordCountdown());

    }
    public void RepeatSameWord()
    {
        _witListeningStateManager.ChangeState("ListeningForMenuCommandsOnly");
        StartCoroutine(StartCurrentWordCountdown());
    }
    public void StartWordCheck(string transcription)
    {
        StartCoroutine(CheckRecitedWord(transcription));
    }
   
    public IEnumerator CheckRecitedWord(string text)
    {
        if (_witListeningStateManager.currentListeningState == "ListeningForNavigationCommandsOnly"
        || _witListeningStateManager.currentListeningState == "ListeningForMenuCommandsOnly")
        {
            Debug.Log("Breaking out of recited word because menu active or in command mode" + _witListeningStateManager.currentListeningState);
            yield break;
        }
        bool wordAnsweredCorrectly;

         if (isDeciding) {
            Debug.Log("TRUE isDeciding");
            if (text.ToLower() == "next")
            {
                _levelManager.LevelComplete();
                yield break;
            }
            else if (text.ToLower() == "repeat")
            {
                _levelManager.RepeatLevel();
                yield break;

            }

            else {
                Debug.Log("Not next or repeat");
                partialText3D.UpdateText("I didn't understand that, please try again.");
                GameOver();
                yield break;

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

    public void MoveOnIfMoreWordsInList ()
    {
        Debug.Log("checking if more words in list" + currentWordOrSentenceIndex + " " + currentWordOrSentenceList.Length);
        if (currentWordOrSentenceIndex < currentWordOrSentenceList.Length - 1)
        {
            Debug.Log("going to next word because index valid");
            GoToNextWord();
        }

        else
        {
            _witListeningStateManager.ChangeState("ListeningForEverything");
            Debug.Log("Proceeding?" + currentWordOrSentenceList);
            if (currentWordOrSentenceList == changAndWilletPaperWordList) { changComplete = true; }
            if (currentWordOrSentenceList == uiControlsWordList) { uiComplete = true; }
            if (currentWordOrSentenceList == openQuestionsList) { openQuestionsComplete = true; }
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
        Debug.Log("Checking word list status");
        // Either proceed to next word list, or end the game.
        if (changComplete && !uiComplete)
        { 
            currentWordOrSentenceList = uiControlsWordList;
            currentWordOrSentenceIndex = 0;
            reciteText3D.UpdateText("Great! Moving onto UI word list.");

            yield return new WaitForSeconds(2);
            StartCoroutine(StartCurrentWordCountdown());
            
        }
        // If level 1/2 have both lists done, or level 3 has open questions done, end the game.
        else if (changComplete && uiComplete || openQuestionsComplete)
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
        StartCoroutine(_witListeningStateManager.TurnWitOffAndOn());
        isDeciding = true;
    }
}
