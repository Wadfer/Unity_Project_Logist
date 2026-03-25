using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Метод для кнопки "Играть" (загружает первый уровень)
    public void PlayGame()
    {
        // Замени "Level1" на точное имя твоей первой сцены
        SceneManager.LoadScene("Level1");
    }

    // Метод для загрузки конкретного уровня (привяжем к кнопкам 1, 2, 3...)
    public void LoadLevelByName(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    public void QuitGame()
    {
        Debug.Log("Выход из игры");
        Application.Quit();
    }
}