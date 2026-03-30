using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private void Start()
    {
        // Когда мы возвращаемся в меню после уровня, заставляем UI показать свежие деньги
        if (CurrencyManager.Instance != null)
        {
            CurrencyManager.Instance.UpdateBalanceUI();
        }
    }
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
        
        // Эта команда закрывает сбилженную игру (на ПК или телефоне)
        Application.Quit();

        // А эта команда останавливает режим Play прямо внутри редактора Unity!
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}