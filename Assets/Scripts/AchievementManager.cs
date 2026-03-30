using UnityEngine;
using TMPro;
using UnityEngine.UI; // Нужен для картинок
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// === НОВЫЙ КЛАСС ДЛЯ БАЗЫ ДАННЫХ АЧИВОК ===
[System.Serializable]
public class AchievementData
{
    public int id;               // ID ачивки (как в БД)
    public string title;         // Название ("Первая кровь", "Водила")
    [TextArea]
    public string description;   // Описание ("Пройдите 1 уровень")
    public float coinReward;     // Сколько монет даем (50)
    public Sprite icon;          // Картинка ачивки
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;

    [Header("База всех достижений")]
    public AchievementData[] achievementsDatabase; // Заполним в Инспекторе!

    [Header("UI Всплывающего окна (Steam-style)")]
    public GameObject achievementPanel;      
    public RectTransform panelRect;          // Для анимации движения
    public CanvasGroup panelCanvasGroup;     
    public TMP_Text titleText;               
    public TMP_Text descText;                
    public TMP_Text rewardText;              
    public Image iconImage;                  // <--- НОВОЕ: Картинка ачивки

    [Header("Настройки анимации")]
    public float displayTime = 4f;           // Сколько висит на экране
    public float slideSpeed = 5f;            // Скорость выезда
    private Vector2 hiddenPos = new Vector2(20, -150); // Прячем внизу (за экраном)
    private Vector2 visiblePos = new Vector2(20, 20);  // Показываем в левом нижнем углу

    // Очередь ачивок (чтобы они не накладывались, если получить 2 сразу)
    private System.Collections.Generic.Queue<AchievementData> achievementQueue = new System.Collections.Generic.Queue<AchievementData>();
    private bool isShowing = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 

            // МАГИЯ: Заставляем скрипт вызывать функцию OnSceneLoaded при каждой смене уровня!
            SceneManager.sceneLoaded += OnSceneLoaded; 
        }
        else
        {
            Destroy(gameObject); 
            return;
        }

        if (achievementPanel != null)
        {
            panelRect.anchoredPosition = hiddenPos;
            panelCanvasGroup.alpha = 0f;
            achievementPanel.SetActive(false);
        }
    }

        // Этот метод срабатывает АВТОМАТИЧЕСКИ каждый раз, когда открывается новый уровень или меню
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Сбрасываем флаг "я сейчас показываю ачивку"
        isShowing = false; 
        
        // 2. Мгновенно прячем панель за экран
        if (achievementPanel != null)
        {
            panelRect.anchoredPosition = hiddenPos;
            panelCanvasGroup.alpha = 0f;
            achievementPanel.SetActive(false);
        }

        // 3. Если в очереди еще остались ачивки - запускаем их показ заново!
        if (achievementQueue.Count > 0)
        {
            StartCoroutine(ShowNextAchievement());
        }
    }

    // ==============================================================
    // ГЛАВНЫЙ МЕТОД: Теперь передаем ТОЛЬКО ID ачивки!
    // Пример вызова: AchievementManager.Instance.UnlockAchievement(1);
    // ==============================================================
    public void UnlockAchievement(int achId)
    {
        // 1. Ищем ачивку в нашей базе по ID
        AchievementData ach = null;
        foreach (var data in achievementsDatabase)
        {
            if (data.id == achId) ach = data;
        }

        if (ach == null) 
        {
            Debug.LogError("Ачивка с ID " + achId + " не найдена в базе!");
            return;
        }

        // 2. Проверяем локальную память (чтобы не дать дважды)
        if (PlayerPrefs.GetInt("ACH_" + achId, 0) == 1) return;

        // 3. Записываем в память
        PlayerPrefs.SetInt("ACH_" + achId, 1);
        PlayerPrefs.Save();

        // 4. Добавляем в очередь на показ
        achievementQueue.Enqueue(ach);
        
        // 5. Выдаем монеты
        if (CurrencyManager.Instance != null && ach.coinReward > 0)
        {
            CurrencyManager.Instance.AddCoins(ach.coinReward);
        }

        // 6. Отправляем в БД
        if (PlayerPrefs.HasKey("PlayerID"))
        {
            int playerId = PlayerPrefs.GetInt("PlayerID");
            StartCoroutine(SendAchievementToDB(playerId, achId));
        }

        // 7. Запускаем показ, если окно сейчас свободно
        if (!isShowing)
        {
            StartCoroutine(ShowNextAchievement());
        }
    }

    // --- Корутина: Показ ачивок по очереди (Steam Style) ---
    private IEnumerator ShowNextAchievement()
    {
        isShowing = true;

        while (achievementQueue.Count > 0)
        {
            AchievementData currentAch = achievementQueue.Dequeue();

            // Подставляем данные в UI
            if (titleText != null) titleText.text = currentAch.title;
            if (descText != null) descText.text = currentAch.description;
            if (rewardText != null) rewardText.text = "+" + currentAch.coinReward;
            if (iconImage != null && currentAch.icon != null) iconImage.sprite = currentAch.icon;

            achievementPanel.SetActive(true);

            // АНИМАЦИЯ: Выезд снизу + плавное появление
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * slideSpeed;
                panelRect.anchoredPosition = Vector2.Lerp(hiddenPos, visiblePos, t);
                panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }

            // Ждем
            yield return new WaitForSeconds(displayTime);

            // АНИМАЦИЯ: Уезд вниз + плавное исчезновение
            t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * slideSpeed;
                panelRect.anchoredPosition = Vector2.Lerp(visiblePos, hiddenPos, t);
                panelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            achievementPanel.SetActive(false);
            yield return new WaitForSeconds(0.5f); // Пауза перед следующей ачивкой
        }

        isShowing = false;
    }

    // --- Корутина: Отправка данных на сервер (осталась без изменений) ---
    private IEnumerator SendAchievementToDB(int userId, int achId)
    {
        // ВНИМАНИЕ: Проверь, правильный ли здесь IP-адрес!
        string url = "http://example.local/save_achievement.php"; 

        WWWForm form = new WWWForm();
        form.AddField("user_id", userId.ToString());
        form.AddField("ach_id", achId.ToString()); 

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Ошибка сети (Сервер не найден): " + request.error);
            }
            else
            {
                // Читаем текст, который нам вывел PHP-скрипт (echo json_encode...)
                string serverResponse = request.downloadHandler.text;
                Debug.Log("<color=magenta>Ответ сервера (Ачивки): " + serverResponse + "</color>");
            }
        }
    }
}