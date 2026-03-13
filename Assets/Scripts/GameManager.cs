using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement; 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Ресурсы")]
    public int currentFuel = 20;
    public int maxFuel = 20;

    [Header("Прогресс Игры")]
    public int totalOrders = 0;      // Сколько всего нужно доставить
    public int completedOrders = 0;  // Сколько уже доставлено
    public bool isGameActive = true;

    [Header("UI Элементы")]
    public Text fuelText;  
    public Text ordersText; // НОВОЕ: Текст "Заказы: 0/5"
    public GameObject gameOverPanel; 
    public Text gameOverText; 

    [Header("Эффекты")]
    public GameObject floatingTextPrefab; 
    public Transform playerTransform; 

    // Защита от двойного списания
    private float lastFuelChangeTime = 0f;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // 1. Считаем, сколько всего заказов на карте
        CalculateTotalOrders();

        UpdateUI();
        if(gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    void CalculateTotalOrders()
    {
        totalOrders = 0;
        // Находим все точки на карте
        MapPoint[] allPoints = FindObjectsByType<MapPoint>(FindObjectsSortMode.None);

        foreach (var point in allPoints)
        {
            // Если это магазин - добавляем количество его заказов в общую сумму
            if (point.type == PointType.Shop)
            {
                totalOrders += point.cargoList.Count;
            }
        }
        Debug.Log($"Всего заказов на уровне: {totalOrders}");
    }

    public void ModifyFuel(int amount)
    {
        if (!isGameActive) return;

        // Защита от двойного списания
        if (Time.time - lastFuelChangeTime < 0.1f) return;
        lastFuelChangeTime = Time.time; 

        // Эффект текста
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
            EndGame(false); // Проигрыш (бензин)
        }
    }

    // Этот метод вызывает Игрок, когда доставил груз
    public void DeliverCargo()
    {
        completedOrders++;
        UpdateUI();

        // Проверяем победу
        if (completedOrders >= totalOrders)
        {
            EndGame(true); // Победа (все заказы выполнены)
        }
    }

    // Метод для обновления UI (просто заглушка, чтобы PlayerController не ругался)
    public void PickUpCargo() 
    {
        // Можно добавить звук или эффект
    }

    void UpdateUI()
    {
        if (fuelText != null) 
            fuelText.text = $"Топливо: {currentFuel} / {maxFuel}";
        
        // Обновляем текст заказов
        if (ordersText != null)
            ordersText.text = $"Заказы: {completedOrders} / {totalOrders}";
    }

    void EndGame(bool win)
    {
        isGameActive = false;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverText != null)
                gameOverText.text = win ? "ВСЕ ЗАКАЗЫ ДОСТАВЛЕНЫ!\nПОБЕДА!" : "ТОПЛИВО КОНЧИЛОСЬ!\nПРОИГРЫШ";
        }
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}