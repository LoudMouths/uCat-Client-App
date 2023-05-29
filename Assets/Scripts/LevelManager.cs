using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class LevelManager : MonoBehaviour
{
    public ScoreManager _scoreManager;

    public string currentLevel;

    public void Awake()
    {
        Scene scene = SceneManager.GetActiveScene();
        currentLevel = scene.name;
    }

    public void RepeatLevel() {
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }
    public void LevelComplete()
    {
        // Switch scenes based on score
        Scene scene = SceneManager.GetActiveScene();
        switch (scene.name)
        {

            case "Level1":
                // TODO - in theory this should never happen as the user has to repeat
                // answers until they get them right. But leaving as a backup.
                if (_scoreManager.Level1CurrentScore == _scoreManager.Level1MaxScore)
                {
                    // Change to level 2 if 100% score
                    SceneManager.LoadScene("Level2");
                }

                else
                {
                    // Repeat Level 1
                    SceneManager.LoadScene("Level1");
                }
                break;
            case "Level2":
                    // Change to level 3, no matter the score
                    SceneManager.LoadScene("Level3");
                break;

        }

    }
}
