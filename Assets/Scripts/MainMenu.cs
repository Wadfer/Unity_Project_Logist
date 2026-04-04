using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Нужен для работы с текстом

public class MainMenu : MonoBehaviour
{
    [Header("Окно: Ошибка доступа (Для гостей)")]
    public GameObject guestErrorPanel; 

    [Header("Куда перекинуть игрока?")]
    public GameObject settingsPanel;   

    [Header("Глобальный Лидерборд (На главном экране)")]
    public TMP_Text globalLeaderboardText; // Текст, куда впишем Топ-10

    private void Start()
    {
        // Обновляем монетки
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.UpdateBalanceUI();

        // АВТОМАТИЧЕСКИ загружаем таблицу лидеров при входе в меню!
        StartCoroutine(FetchGlobalLeaderboard());
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

    // МЕТОД ДЛЯ КНОПКИ В ОКНЕ ОШИБКИ
    public void GoToSettings()
    {
        if (guestErrorPanel != null) guestErrorPanel.SetActive(false);
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

    // === КОРУТИНА ЗАГРУЗКИ ТОПА ===
    private System.Collections.IEnumerator FetchGlobalLeaderboard()
    {
        if (globalLeaderboardText != null) globalLeaderboardText.text = "Загрузка Топ-10...";

        string url = "http://138.124.230.211/get_global_leaderboard.php"; 

        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                GlobalLeaderboardResponse res = JsonUtility.FromJson<GlobalLeaderboardResponse>(request.downloadHandler.text);
                
                if (res != null && res.status == "success")
                {
                    string board = "Таблица рейтинга\n\n";
                    for (int i = 0; i < res.leaders.Length; i++)
                    {
                        board += $"{i + 1}. {res.leaders[i].username} — {res.leaders[i].total_score} очк.\n";
                    }
                    
                    if (res.leaders.Length == 0) board += "Пока нет игроков. Стань первым!";
                    
                    if (globalLeaderboardText != null) globalLeaderboardText.text = board;
                }
            }
            else
            {
                if (globalLeaderboardText != null) globalLeaderboardText.text = "Ошибка сети";
            }
        }
    }
}

// --- КЛАССЫ ДЛЯ РАСШИФРОВКИ JSON ---
[System.Serializable]
public class GlobalLeaderboardResponse
{
    public string status;
    public GlobalLeaderInfo[] leaders;
}

[System.Serializable]
public class GlobalLeaderInfo
{
    public string username;
    public int total_score;
}