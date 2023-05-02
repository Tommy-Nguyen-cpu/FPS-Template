using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MenuScript : MonoBehaviour
{
    private string TitleOfGame = "Title Placeholder 2";
    public TMPro.TextMeshProUGUI TitleUI;

    // Used by the "Pause menu" to disable and resume gameplay.
    public Canvas menuScreen;
    
    public void Start()
    {
        TitleUI.text = TitleOfGame;
    }

    // Loads the game (for "Start" button).
    public void PlayGame()
    {
        SceneManager.LoadScene(1);
    }

    // Resumes time and disables canvas.
    public void UnPause()
    {
        // Removes the current game object from the "selected" event. Ensures we can hover over and color changes again.
        EventSystem.current.SetSelectedGameObject(null);

        // Continues time and hides pause menu.
        Time.timeScale = 1f;
        menuScreen.gameObject.SetActive(false);
        PlayerController.isPaused = false;
    }

    public void Options()
    {
        // TODO: Once we implement an options menu.

        // Removes the current game object from the "selected" event. Ensures we can hover over and color changes again.
        EventSystem.current.SetSelectedGameObject(null);
    }

    // For "Quit" button. Closes game window.
    public void Quit()
    {
        Application.Quit();
    }
}
