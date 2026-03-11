using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        // Загружаем сцену с игрой (убедись, что имя совпадает!)
        SceneManager.LoadScene("GameLevel");
    }

    public void QuitGame()
    {
        Debug.Log("Выход из игры");
        Application.Quit();
    }
}