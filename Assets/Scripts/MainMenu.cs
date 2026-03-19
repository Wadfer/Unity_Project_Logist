using UnityEngine;
using UnityEngine.SceneManagement; // Обязательно добавьте эту строку

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        // Название сцены должно в точности совпадать с названием вашего файла сцены
        SceneManager.LoadScene("GameLevel");
    }
}