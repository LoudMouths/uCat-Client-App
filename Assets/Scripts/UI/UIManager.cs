using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using EListeningState = WitListeningStateManager.ListeningState;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
     private static UIManager instance;

     public Animator animator;
     public GameObject textElements;

     public WitListeningStateManager _witListeningStateManager;
    public WordReciteManager _wordReciteManager;

     List<string> acceptableWakeWords = new List<string>()
    {
        // TODO move to UIHandler class
        "menu",
        "activate menu",
        "hey cat",
        "hey kat",
        "hey cap",
        "hey you cap",
        "hey you can't",
        "hey you cat",
        "hey you kat",
        "hey, you cat",
        "hey, you kat"
    };

    private void Awake()
    {
        textElements = GameObject.FindWithTag("TextElements");
        if (instance == null)
        {
            instance = this;
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public GameObject menu;

    public void CheckIfMenuActivationCommandsWereSpoken(string text) {
        // TODO move the below to external UIHandler class

        // Listen for any of the wake phrases
        if (!menu.activeInHierarchy && acceptableWakeWords.Any(text.Contains))
        {
            textElements.SetActive(false);
            menu.SetActive(true);
            _witListeningStateManager.TransitionToState(EListeningState.ListeningForTaskMenuCommandsOnly);
        }
    }

    public void CheckIfMenuNavigationCommandsWereSpoken(string text) {

            Debug.Log("Menu is active and listening for navigation commands only: " + text);
            switch (text)
            {
                case "repeat level":
                    // TODO move to levelmanager
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    break;
                case "next level":
                int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
                    if (nextBuildIndex < SceneManager.sceneCountInBuildSettings)
                    {
                        SceneManager.LoadScene(nextBuildIndex);
                    }
                    else
                    {
                        // Handle failure case when there is no next scene
                        Debug.Log("No next scene available.");
                    }
                    break;
                case "quit":
                    Application.Quit();
                    break;
                case "restart":
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    break;
                case "resume": 
                    animator.Play("CloseClip");
                    StartCoroutine(WaitForAnimationToEnd());
                    string scene = SceneManager.GetActiveScene().name;
                    if (scene == "Level3") {
                        _witListeningStateManager.TransitionToState(EListeningState.ListeningForEverything);
                    } else {
                        _witListeningStateManager.TransitionToState(EListeningState.ListeningForMenuActivationCommandsOnly);
                        _wordReciteManager.RepeatSameWord();
                    }
                    textElements.SetActive(true);
                    break;
                case ("level one"):
                    SceneManager.LoadScene("Level1"); 
                    break;
                case "level two":
                    SceneManager.LoadScene("Level2"); 
                    break;
                case "level three":
                    SceneManager.LoadScene("Level3");
                    break;
                default:
                    break;
        }
        
        
    }

     private System.Collections.IEnumerator WaitForAnimationToEnd()
    {
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1)
        {
            yield return null;
        }

        menu.SetActive(false);
    }

    void Start()
    {
        menu.SetActive(false);
    }
}
