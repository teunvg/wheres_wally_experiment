using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Unity behaviour class to switch scenes. To be improved.
/// </summary>
public class SceneController : MonoBehaviour {
    public void LoadScene(string scene) {
        SceneManager.LoadScene(scene);
    }

    public void Exit() {
        Application.Quit();
    }
}
