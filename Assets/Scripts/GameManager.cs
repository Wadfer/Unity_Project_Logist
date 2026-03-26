using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// Создаем перечисление для сложности
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
    public LevelDifficulty difficulty = LevelDifficulty.Easy; // Выбирается в Инспекторе

    [Header("Ресурсы")]
    public int currentFuel = 20;
    public int maxFuel = 20;

    [Header("Прогресс")]
    public int totalOrders = 0;
    public int completedOrders = 0;
    public bool isGameActive = true;

    [Header("UI")]
    public Text fuelText;
    public Text ordersText;
    public GameObject gameOverPanel;
    public Text gameOverText; // Текст Победы/Поражения (сюда допишем очки)

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
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Обновляем стрелки (считаем, что в начале груза нет)
        RefreshNavigationArrows(new List<CargoType>());
    }

    void CalculateTotalOrders()
    {
        totalOrders = 0;
        // Используем новый метод поиска
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

        // Защита от двойного списания
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
            EndGame(false); // Проигрыш
        }
    }

    public void DeliverCargo()
    {
        completedOrders++;
        UpdateUI();

        if (completedOrders >= totalOrders)
        {
            EndGame(true); // Победа
        }
    }

    public void PickUpCargo() { }

    void UpdateUI()
    {
        if (fuelText != null) 
            fuelText.text = $"Топливо: {currentFuel} / {maxFuel}";
        
        if (ordersText != null)
            ordersText.text = $"Заказы: {completedOrders} / {totalOrders}";
    }

    // --- ЛОГИКА ОКОНЧАНИЯ ИГРЫ И ПОДСЧЕТА ОЧКОВ ---
    void EndGame(bool win)
    {
        isGameActive = false;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);

            if (gameOverText != null)
            {
                if (win)
                {
                    // 1. Определяем коэффициент
                    float multiplier = 1.0f;
                    switch (difficulty)
                    {
                        case LevelDifficulty.Easy: multiplier = 1.5f; break;
                        case LevelDifficulty.Medium: multiplier = 1.7f; break;
                        case LevelDifficulty.Hard: multiplier = 2.0f; break;
                    }

                    // 2. Считаем очки (Топливо * Коэффициент)
                    // Используем Mathf.RoundToInt, чтобы получить целое число
                    int score = Mathf.RoundToInt(currentFuel * multiplier);

                    // 3. Выводим красивый текст
                    gameOverText.text = $"ПОБЕДА!\n\n" +
                                        $"Остаток топлива: {currentFuel}\n" +
                                        $"Сложность: {difficulty} (x{multiplier})\n" +
                                        $"----------------\n" +
                                        $"ИТОГОВЫЙ СЧЕТ: {score}";
                    
                    // (Опционально) Красим текст в зеленый
                    gameOverText.color = Color.green;
                }
                else
                {
                    gameOverText.text = "ТОПЛИВО КОНЧИЛОСЬ!\n\nСЧЕТ: 0";
                    gameOverText.color = Color.red;
                }
            }
        }
    }

    // Навигация (Стрелки)
    public void RefreshNavigationArrows(List<CargoType> playerCargoList)
    {
        MapPoint[] allPoints = FindObjectsByType<MapPoint>(FindObjectsSortMode.None);

        foreach (var point in allPoints)
        {
            // Передаем каждой точке информацию о том, что сейчас лежит в кузове
            point.UpdateArrows(playerCargoList);
        }
    }
}