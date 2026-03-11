using UnityEngine;
using UnityEngine.UI; // Обязательно для работы с UI
using UnityEngine.SceneManagement; // Для перезагрузки сцены

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Ресурсы")]
    public int currentFuel = 10;
    public int maxFuel = 20;
    
    [Header("Логистика")]
    public bool hasCargo = false;
    public bool isGameActive = true;

    [Header("UI Элементы")]
    public Text fuelText;  // Сюда перетащить текст топлива
    public Text cargoText; // Сюда перетащить текст груза
    public GameObject gameOverPanel; // Сюда перетащить панель конца игры
    public Text gameOverText; // Текст на панели (победа или поражение)

    [Header("Эффекты")]
    public GameObject floatingTextPrefab; // Сюда перетащишь префаб текста
    public Transform playerTransform; // Сюда перетащишь машинку

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
        if(gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void ModifyFuel(int amount)
    {
        if (!isGameActive) return;

        if (floatingTextPrefab != null && playerTransform != null)
        {
            // Создаем текст над машиной (+2 метра по высоте)
            GameObject go = Instantiate(floatingTextPrefab, playerTransform.position + Vector3.up * 2, Quaternion.identity);
            
            // Настраиваем число
            go.GetComponent<FloatingText>().Setup(amount);
        }

        currentFuel += amount;
        if (currentFuel > maxFuel) currentFuel = maxFuel;

        Debug.Log($"Топливо: {currentFuel}");
        UpdateUI();

        currentFuel += amount;
        if (currentFuel <= 0)
        {
            EndGame(false); // Проиграл
        }
    }

    public void PickUpCargo()
    {
        if (!hasCargo)
        {
            hasCargo = true;
            UpdateUI();
        }
    }

    public void DeliverCargo()
    {
        if (hasCargo)
        {
            hasCargo = false;
            UpdateUI();
            EndGame(true); // Выиграл
        }
    }

    void UpdateUI()
    {
        if (fuelText != null) 
            fuelText.text = "Топливо: " + currentFuel + " / " + maxFuel;
        
        if (cargoText != null) 
            cargoText.text = hasCargo ? "Груз: ЕСТЬ" : "Груз: НЕТ";
    }

    void EndGame(bool win)
    {
        isGameActive = false;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverText != null)
                gameOverText.text = win ? "ГРУЗ ДОСТАВЛЕН!\nПОБЕДА!" : "ТОПЛИВО КОНЧИЛОСЬ!\nПРОИГРЫШ";
        }
    }

    // Этот метод нужно привязать к кнопке Restart
    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}