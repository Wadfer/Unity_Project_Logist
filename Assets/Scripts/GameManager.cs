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

    // --- ЛОГИКА ОКОНЧАНИЯ ИГРЫ ---
    void EndGame(bool win)
    {
        isGameActive = false;
        
        // ВАЖНО: Выключаем игровой HUD (топливо и заказы пропадают с экрана)
        if (gameplayHUD != null) gameplayHUD.SetActive(false);
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (win)
            {
                if (resultImageBanner != null) resultImageBanner.sprite = winSprite;

                float multiplier = 1.0f;
                switch (difficulty)
                {
                    case LevelDifficulty.Easy: multiplier = 1.5f; break;
                    case LevelDifficulty.Medium: multiplier = 1.7f; break;
                    case LevelDifficulty.Hard: multiplier = 2.0f; break;
                }
                int score = Mathf.RoundToInt(currentFuel * multiplier);

                if (gameOverText != null)
                {
                    gameOverText.text = $"Остаток топлива: {currentFuel}\n" +
                                        $"Сложность: {difficulty} (x{multiplier})";
                    gameOverText.color = Color.white;
                }

                if (scoreText != null)
                {
                    scoreText.text = $"СЧЕТ: {score}";
                    scoreText.color = Color.yellow;
                }

                if (nextLevelButton != null) nextLevelButton.SetActive(true); 
            }
            else
            {
                if (resultImageBanner != null) resultImageBanner.sprite = loseSprite;

                if (gameOverText != null)
                {
                    gameOverText.text = "Машина заглохла...\nНе хватило топлива.";
                    gameOverText.color = Color.red;
                }

                if (scoreText != null)
                {
                    scoreText.text = "СЧЕТ: 0";
                    scoreText.color = Color.red;
                }

                if (nextLevelButton != null) nextLevelButton.SetActive(false);
            }
        }
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
}