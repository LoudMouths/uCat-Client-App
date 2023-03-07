using System.Collections;
using UnityEngine;
using Meta.WitAi;
using Meta.WitAi.Data;
using Meta.WitAi.Json;

public class WordReciteManager : MonoBehaviour
{
    // For tracking if the user is repeating a word currently
    private bool isLastAttemptAtWord;

    // Current word tracking
    int currentWordIndex = 0;

    // Word lists
    string[] currentWordList;
    string[] changPaperWordList = new string[] { "hello","thirsty", "they", "hope", "up", "goodbye", "music", "tired", "nurse", "computer" };
    bool changComplete;
    string[] uiControlsWordList = new string[] { "one", "two", "three", "proceed", "next", "repeat", "back", "pause", "menu", "help" };
    string emergencyStopWord = "emergency stop";

    public ScoreManager _scoreManager;
    public LevelManager _levelManager;

    bool uiComplete;

    // UI elements
    public TMPro.TextMeshPro reciteText;


    [SerializeField] private Wit wit;

    void Start()
    {
        uiComplete = false;
        changComplete = false;
        isLastAttemptAtWord = false;

        // Start with the first chang word
        currentWordList = changPaperWordList;

        UpdateReciteTextToCurrentWord();

        // Activate microphone
    }

    void UpdateReciteTextToCurrentWord()
    {
        reciteText.text = "Word to recite: " + currentWordList[currentWordIndex];
    }

    public void OnMicrophoneTimeOut()
    {
        // Do not add to score
        StartCoroutine(ChangeTimeOutText());
    }

    IEnumerator ChangeTimeOutText()
    {
        reciteText.text = "Timed out! Moving on...";
        yield return new WaitForSeconds(2);
        GoToNextWord();
    }

    public void OnMicrophoneInactivity()
    {
        Debug.Log("Inactivity");
        // Do not add to score
        StartCoroutine(ChangeTimeOutText());
    }

    void GoToNextWord()
    {
        // If the next word does not exceed the limit
        if (currentWordIndex < currentWordList.Length-1)
        {
            currentWordIndex++;
            Debug.Log("Increased index to " + currentWordIndex);
        }

        UpdateReciteTextToCurrentWord();

    }
    void RepeatSameWord()
    {
        UpdateReciteTextToCurrentWord();
    }
    public void StartWordCheck(string transcription)
    {
        Debug.Log("recieved " + transcription);
        StartCoroutine(CheckRecitedWord(transcription));
    }
   
    public IEnumerator CheckRecitedWord(string text)
    {
        Debug.Log("Coroutine " + text);
        bool wordAnsweredCorrectly;

        if (text.ToLower() == emergencyStopWord)
        {

            Debug.Log("Emergency stop");
            wit.Deactivate();
            reciteText.text = "Emergency Stop Called";
            yield break;
            
        }

        // Does their answer match the current word?
        wordAnsweredCorrectly = text.ToLower() == currentWordList[currentWordIndex].ToLower();
        Debug.Log(wordAnsweredCorrectly);
       // Debug.Log("Word answered correctly? " + wordAnsweredCorrectly);
        Debug.Log("current: " + currentWordList[currentWordIndex] + " answer: " + text.ToLower() + " " + wordAnsweredCorrectly);
        // Change text to reflect correct / incorrect 

        reciteText.text = wordAnsweredCorrectly ? "Correct! :D " : "Incorrect :(";
        yield return new WaitForSeconds(2);

        if (wordAnsweredCorrectly)
        {
            WordAnsweredCorrectly();
        }
        else
        {
            StartCoroutine(WordAnsweredIncorrectly());
        }

    }
    void AddScoreToScoreManager()
    {
        _scoreManager.Level1CurrentScore = _scoreManager.Level1CurrentScore + 1;
    }

    IEnumerator WordAnsweredIncorrectly()
    {

        reciteText.text = isLastAttemptAtWord ? "Moving on..." : "Try again...";
        yield return new WaitForSeconds(1);

        // If they still have 1 chance to answer
        if (!isLastAttemptAtWord)
        {
            isLastAttemptAtWord = true;
            RepeatSameWord();
        }
        else

        // Move onto next one
        {
            isLastAttemptAtWord = false;
            MoveOnIfMoreWordsInList();
        }
    }

    void MoveOnIfMoreWordsInList ()
    {
        if (currentWordIndex < currentWordList.Length - 1)
        {
            Debug.Log("There are more words in this list");
            GoToNextWord();
        }

        else
        {
            Debug.Log("NO MORE words in this list");
            if (currentWordList == changPaperWordList) { changComplete = true; }
            if (currentWordList == uiControlsWordList) { uiComplete = true; }
            StartCoroutine(CheckWordListStatus());
        }
    }
    void WordAnsweredCorrectly()
    {
        AddScoreToScoreManager();

        // Reset last word check, as moving on to next word
        isLastAttemptAtWord = false;

        MoveOnIfMoreWordsInList();
    }


    IEnumerator CheckWordListStatus()
    {
        Debug.Log("Checking word list ");

        // Either proceed to next word list, or end the game.

        if (changComplete && !uiComplete)
        { 
            currentWordList = uiControlsWordList;
            currentWordIndex = 0;
            reciteText.text = "Great! Moving onto UI word list.";
            yield return new WaitForSeconds(2);
            UpdateReciteTextToCurrentWord();
            
        }
        else if (changComplete && uiComplete)
        {
            currentWordIndex = 0;
            reciteText.text = "Finished!";
            GameOver();
        }

        else
        {
            Debug.Log("Something went wrong in conditional for list changing.");
        }
    }

    void GameOver()
    {
        reciteText.text = "Word list finished";
        _levelManager.LevelComplete();
    }
}
