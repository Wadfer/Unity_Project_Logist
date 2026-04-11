using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance; // Синглтон для удобного доступа

    [Header("UI Элементы")]
    public TMP_Text balanceText; // Текст монеток в главном меню
    public GameObject coinIcon;  // Иконка монетки (чтобы прятать её для гостей)

    private float currentBalance = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Делаем объект бессмертным
        }
        else
        {
            // === ВОТ ОН, СЕКРЕТНЫЙ ТРЮК! ===
            // Клон перед смертью передает бессмертному менеджеру ссылки на НОВЫЙ интерфейс
            if (this.balanceText != null) Instance.balanceText = this.balanceText;
            if (this.coinIcon != null) Instance.coinIcon = this.coinIcon;
            
            // Заставляем бессмертного менеджера сразу обновить эти новые тексты
            Instance.UpdateBalanceUI(); 

            Destroy(gameObject); // Убиваем клона
            return;
        }
    }

    private void Start()
    {
        UpdateBalanceUI();
    }

    // Включаем или выключаем показ денег (для Гостей прячем)
    public void UpdateBalanceUI()
    {
        if (PlayerPrefs.HasKey("PlayerName")) // Если игрок вошел в аккаунт
        {
            currentBalance = PlayerPrefs.GetFloat("PlayerBalance", 0);
            if (balanceText != null) balanceText.text = currentBalance.ToString();
            if (coinIcon != null) coinIcon.SetActive(true);
            if (balanceText != null) balanceText.gameObject.SetActive(true);
        }
        else // Если это гость
        {
            if (coinIcon != null) coinIcon.SetActive(false);
            if (balanceText != null) balanceText.gameObject.SetActive(false);
        }
    }

    // Метод для начисления монет (вызовем его, когда дадим ачивку)
    public void AddCoins(float amount)
    {
        if (!PlayerPrefs.HasKey("PlayerID")) return; // Гостям не даем монеты

        currentBalance += amount;
        PlayerPrefs.SetFloat("PlayerBalance", currentBalance);
        PlayerPrefs.Save();

        UpdateBalanceUI();

        // Отправляем новые данные в БД
        // int playerId = PlayerPrefs.GetInt("PlayerID");
        // StartCoroutine(UpdateBalanceInDB(playerId, currentBalance));
    }

    // Метод для покупки скина (вызовем из магазина)
    public bool TrySpendCoins(float amount)
    {
        if (currentBalance >= amount)
        {
            currentBalance -= amount;
            PlayerPrefs.SetFloat("PlayerBalance", currentBalance);
            PlayerPrefs.Save();
            
            UpdateBalanceUI();

            // int playerId = PlayerPrefs.GetInt("PlayerID");
            // StartCoroutine(UpdateBalanceInDB(playerId, currentBalance));
            // return true; // Покупка успешна
        }
        return false; // Не хватает денег
    }

    private IEnumerator UpdateBalanceInDB(int userId, float newBalance)
    {
        string url = "http://138.124.230.211/update_balance.php"; 

        WWWForm form = new WWWForm();
        form.AddField("user_id", userId.ToString());
        form.AddField("new_balance", newBalance.ToString());

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Не удалось сохранить баланс в БД: " + request.error);
            }
        }
    }
}