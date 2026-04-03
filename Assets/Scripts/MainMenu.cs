using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Окно: Ошибка доступа (Для гостей)")]
    public GameObject guestErrorPanel; // Твоя картинка/окно с ошибкой

    [Header("Куда перекинуть игрока?")]
    public GameObject settingsPanel;   // Сюда перетащим панель настроек/авторизации

    private void Start()
    {
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.UpdateBalanceUI();
    }

    public void PlayGame()
    {
        if (!PlayerPrefs.HasKey("PlayerID"))
        {
            if (guestErrorPanel != null) guestErrorPanel.SetActive(true);
            return; 
        }
        SceneManager.LoadScene("Level1");
    }

    public void LoadLevelByName(string levelName)
    {
        if (!PlayerPrefs.HasKey("PlayerID"))
        {
            if (guestErrorPanel != null) guestErrorPanel.SetActive(true);
            return;
        }
        SceneManager.LoadScene(levelName);
    }

    // === НОВЫЙ МЕТОД ДЛЯ КНОПКИ В ОКНЕ ОШИБКИ ===
    public void GoToSettings()
    {
        // 1. Прячем окно с ошибкой
        if (guestErrorPanel != null) guestErrorPanel.SetActive(false);
        
        // 2. Открываем меню настроек (или регистрации)
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Выход из игры");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}