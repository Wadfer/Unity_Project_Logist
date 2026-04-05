using UnityEngine;
using UnityEngine.UI; 
using TMPro; 
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public enum LevelDifficulty
{
    Easy,   // Легкий (x1.5)
    Medium, // Средний (x1.7)
    Hard    // Сложный (x2.0)
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Настройки Уровня")]
    public LevelDifficulty difficulty = LevelDifficulty.Easy;

    [Header("Ресурсы")]
    public int currentFuel = 20;
    public int maxFuel = 20;

    [Header("Прогресс")]
    public int totalOrders = 0;
    public int completedOrders = 0;
    public bool isGameActive = true;

    [Header("UI (Текст и Панели)")]
    public GameObject gameplayHUD;   // Контейнер с топливом и заказами во время игры
    public TMP_Text fuelText;        // Текст топлива
    public TMP_Text ordersText;      // Текст заказов
    
    public GameObject gameOverPanel; // Темная панель в конце игры
    public TMP_Text gameOverText;    // Текст статистики (остаток топлива)
    public TMP_Text scoreText;       // ОГРОМНЫЙ текст Очков
    public GameObject nextLevelButton; // Кнопка "Следующий уровень"

    [Header("UI (Картинки Результата)")]
    public Image resultImageBanner;  // Объект картинки на Canvas
    public Sprite winSprite;         // Файл картинки Победы
    public Sprite loseSprite;        // Файл картинки Поражения

    [Header("Эффекты")]
    public GameObject floatingTextPrefab;
    public Transform playerTransform;

    [Header("Лидерборд (в конце игры)")]
    public TMP_Text leaderboardText; // Текст, куда впишем список игроков

    private float lastFuelChangeTime = 0f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        CalculateTotalOrders();
        UpdateUI();
        
        // ВАЖНО: При старте включаем игровой HUD и прячем финальную панель
        if (gameplayHUD != null) gameplayHUD.SetActive(true);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // При старте обновляем стрелки
        RefreshNavigationArrows(new List<CargoType>());
    }

    void CalculateTotalOrders()
    {
        totalOrders = 0;
        MapPoint[] allPoints = FindObjectsByType<MapPoint>(FindObjectsSortMode.None);

        foreach (var point in allPoints)
        {
            if (point.type == PointType.Shop)
            {
                totalOrders += point.cargoList.Count;
            }
        }
    }

    public void ModifyFuel(int amount)
    {
        if (!isGameActive) return;

        if (Time.time - lastFuelChangeTime < 0.1f) return;
        lastFuelChangeTime = Time.time;

        if (floatingTextPrefab != null && playerTransform != null)
        {
            GameObject go = Instantiate(floatingTextPrefab, playerTransform.position + Vector3.up * 2, Quaternion.identity);
            go.GetComponent<FloatingText>().Setup(amount);
        }

        currentFuel += amount;
        if (currentFuel > maxFuel) currentFuel = maxFuel;

        UpdateUI();

        if (currentFuel <= 0)
        {
            EndGame(false); 
        }
    }

    public void DeliverCargo()
    {
        completedOrders++;
        UpdateUI();

        // [АЧИВКА ID 2]: Первая доставка
        if (completedOrders == 1 && AchievementManager.Instance != null) 
        {
            AchievementManager.Instance.UnlockAchievement(2);
        }

        if (completedOrders >= totalOrders)
        {
            EndGame(true); 
        }
    }

    void UpdateUI()
    {
        if (fuelText != null) 
            fuelText.text = $": {currentFuel} / {maxFuel}";
        
        if (ordersText != null)
            ordersText.text = $": {completedOrders} / {totalOrders}";
    }

    void EndGame(bool win)
    {
        isGameActive = false;
        
        if (gameplayHUD != null) gameplayHUD.SetActive(false);
        if (CargoSelectionUI.Instance != null) CargoSelectionUI.Instance.CloseInstantly();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            // Включаем Лидерборд всегда
            if (leaderboardText != null) leaderboardText.gameObject.SetActive(true);
            
            int currentLevelIndex = SceneManager.GetActiveScene().buildIndex;

            if (win)
            {
                // ==========================================
                // ПОБЕДА
                // ==========================================
                // Возвращаем текст счета при победе
                if (scoreText != null) scoreText.gameObject.SetActive(true);

                if (AchievementManager.Instance != null)
                {
                    AchievementManager.Instance.UnlockAchievement(4);
                    if (currentFuel == 1) AchievementManager.Instance.UnlockAchievement(5);
                    if (currentFuel >= 2) AchievementManager.Instance.UnlockAchievement(10);
        
                    int streak = PlayerPrefs.GetInt("WinStreak", 0) + 1;
                    PlayerPrefs.SetInt("WinStreak", streak);
                    if (streak >= 3) AchievementManager.Instance.UnlockAchievement(7);
                }
                
                // ПОДСЧЕТ ОЧКОВ
                float multiplier = 1.0f;
                switch (difficulty)
                {
                    case LevelDifficulty.Easy: multiplier = 1.5f; break;
                    case LevelDifficulty.Medium: multiplier = 1.7f; break;
                    case LevelDifficulty.Hard: multiplier = 2.0f; break;
                }
                int score = Mathf.RoundToInt(currentFuel * multiplier);

                // ОБНОВЛЕНИЕ ТЕКСТА
                if (gameOverText != null)
                {
                    gameOverText.text = $"Остаток топлива: {currentFuel}\n" +
                                        $"Сложность: {difficulty} (x{multiplier})";
                    gameOverText.color = Color.white;
                }

                if (scoreText != null)
                {
                    scoreText.text = $"СЧЕТ: {score}";
                }

                if (nextLevelButton != null) nextLevelButton.SetActive(true); 

                // СОХРАНЕНИЕ В БАЗУ
                int nextLevelIndex = currentLevelIndex + 1;
                int currentUnlocked = PlayerPrefs.GetInt("UnlockedLevel", 1);

                if (PlayerPrefs.HasKey("PlayerID"))
                {
                    int playerId = PlayerPrefs.GetInt("PlayerID");

                    // НОВАЯ ЛОГИКА: Сначала сохраняем, потом грузим таблицу (чтобы наш результат там был)
                    StartCoroutine(SaveScoreAndLoadLeaderboard(playerId, currentLevelIndex, score));

                    if (nextLevelIndex > currentUnlocked)
                    {
                        PlayerPrefs.SetInt("UnlockedLevel", nextLevelIndex);
                        PlayerPrefs.Save();
                        StartCoroutine(SaveProgressToServer(playerId, nextLevelIndex));
                    }
                }
                else
                {
                    // Гость: просто грузим таблицу
                    StartCoroutine(LoadLeaderboard(currentLevelIndex));

                    if (nextLevelIndex > currentUnlocked)
                    {
                        PlayerPrefs.SetInt("UnlockedLevel", nextLevelIndex);
                        PlayerPrefs.Save();
                    }
                }
            }
            else
            {
                // ==========================================
                // ПРОИГРЫШ
                // ==========================================
                PlayerPrefs.SetInt("WinStreak", 0);

                // Прячем текст счета (он равен 0, хвастаться нечем)
                if (scoreText != null) scoreText.gameObject.SetActive(false);

                if (resultImageBanner != null) resultImageBanner.sprite = loseSprite;

                if (gameOverText != null)
                {
                    gameOverText.text = "Машина заглохла...\nНе хватило топлива.";
                }

                if (nextLevelButton != null) nextLevelButton.SetActive(false);
                
                // При проигрыше просто качаем таблицу (нам нечего сохранять)
                StartCoroutine(LoadLeaderboard(currentLevelIndex));
            }
        }
    }

    // === НОВАЯ КОРУТИНА: ЖДЕТ СОХРАНЕНИЯ, И ТОЛЬКО ПОТОМ ГРУЗИТ ТОП ===
    private System.Collections.IEnumerator SaveScoreAndLoadLeaderboard(int userId, int levelId, int score)
    {
        // yield return заставляет Unity дождаться окончания сохранения
        yield return StartCoroutine(SaveScoreToServer(userId, levelId, score));
        
        // После того как сервер ответил "Сохранено!", качаем таблицу - наш рекорд уже там!
        yield return StartCoroutine(LoadLeaderboard(levelId));
    }

    // --- МЕНЮ И УРОВНИ ---
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("Menu"); 
    }

    public void LoadNextLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("Это был последний уровень!");
            LoadMainMenu();
        }
    }

    public void RefreshNavigationArrows(List<CargoType> playerCargoList)
    {
        MapPoint[] allPoints = FindObjectsByType<MapPoint>(FindObjectsSortMode.None);
        foreach (var point in allPoints)
        {
            point.UpdateArrows(playerCargoList);
        }
    }

    // --- ИНТЕГРАЦИЯ С БАЗОЙ ДАННЫХ ---
    public void SetupFromDatabase(int dbFuel, int dbDifficultyId)
    {
        maxFuel = dbFuel;
        currentFuel = dbFuel;

        switch (dbDifficultyId)
        {
            case 1: difficulty = LevelDifficulty.Easy; break;
            case 2: difficulty = LevelDifficulty.Medium; break;
            case 3: difficulty = LevelDifficulty.Hard; break;
            default: difficulty = LevelDifficulty.Easy; break; 
        }

        UpdateUI();
        Debug.Log($"<color=orange>[GameManager] Данные применены! Топливо: {maxFuel}, Сложность: {difficulty}</color>");
    }

    private System.Collections.IEnumerator SaveProgressToServer(int userId, int nextLevel)
    {
        string url = "http://138.124.230.211/save_level.php"; // ПРОВЕРЬ СВОЙ АДРЕС

        WWWForm form = new WWWForm();
        form.AddField("user_id", userId.ToString());
        form.AddField("new_level", nextLevel.ToString());

        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log("Прогресс успешно сохранен на сервере!");
            }
            else
            {
                Debug.LogError("Ошибка сохранения на сервер: " + request.error);
            }
        }
    }

    // --- НОВАЯ КОРУТИНА ДЛЯ СОХРАНЕНИЯ ОЧКОВ ---
    private System.Collections.IEnumerator SaveScoreToServer(int userId, int levelId, int score)
    {
        string url = "http://138.124.230.211/save_score.php";  // ПРОВЕРЬ СВОЙ АДРЕС

        WWWForm form = new WWWForm();
        form.AddField("user_id", userId.ToString());
        form.AddField("level_id", levelId.ToString());
        form.AddField("score", score.ToString());

        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log("<color=cyan>Ответ сервера (Очки): " + request.downloadHandler.text + "</color>");
            }
            else
            {
                Debug.LogError("Ошибка сохранения очков: " + request.error);
            }
        }
    }
    private System.Collections.IEnumerator LoadLeaderboard(int levelId)
    {
        if (leaderboardText != null) leaderboardText.text = "Загрузка топ игроков...";

        string url = "http://138.124.230.211/get_leaderboard.php";
        WWWForm form = new WWWForm();
        form.AddField("level_id", levelId.ToString());

        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                LeaderboardResponse res = JsonUtility.FromJson<LeaderboardResponse>(request.downloadHandler.text);
            
                if (res != null && res.status == "success")
                {
                    string board = "ТОП ИГРОКОВ:\n\n";
                    for (int i = 0; i < res.leaders.Length; i++)
                    {
                        board += $"{i + 1}. {res.leaders[i].username} - {res.leaders[i].score_points} очк.\n";
                    }

                    if (res.leaders.Length == 0) board += "Пока нет рекордов. Ты первый!";
                
                    if (leaderboardText != null) leaderboardText.text = board;
                }
            }
            else
            {
                if (leaderboardText != null) leaderboardText.text = "Ошибка загрузки лидеров";
            }
        }
    }

    // Вспомогательные классы для Лидерборда (положи в самом низу файла)
    [System.Serializable]
    public class LeaderboardResponse
    {
        public string status;
        public LeaderInfo[] leaders;
    }

    [System.Serializable]
    public class LeaderInfo
    {
        public string username;
        public int score_points;
    }
}