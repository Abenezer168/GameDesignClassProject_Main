using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonManagerScript: MonoBehaviour
{

    //stores scene name so we can load previous scene. This is used when pressing on the back button on the options and shop screen.
    private static string lastSceneName;

    //This function loads whichever scene the user puts as the parameter
    public void LoadGameScene(string SceneName)
    {
        lastSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(SceneName);
        Debug.Log("Scene is being loaded: " + SceneName);
    }

    public void LoadPreviousScene()
    {
        //We check if the previous scene string is empty or null. if it is not null or empty we then load that scene, otherwise we load the mainmenu screen scene.
        if (!string.IsNullOrEmpty(lastSceneName))
        {
            SceneManager.LoadScene(lastSceneName);
        }
        else
        {
            Debug.Log("No previous scene recorded, loading Main Menu instead.");
            SceneManager.LoadScene("MainMenuScreenScene"); 
        }
    }
    public void QuitGame()
    {
        //Quits application
        Debug.Log("Player is quitting.");
        Application.Quit();

        //Simulates closing game but in unity editor
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
