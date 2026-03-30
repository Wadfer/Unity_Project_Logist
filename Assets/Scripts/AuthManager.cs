using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class AuthManager : MonoBehaviour
{
    [Header("Ссылки на сервер")]
    private string loginURL = "http://example.local/login.php";
    private string registerURL = "http://example.local/register.php";

    [Header("Окно: ВХОД")]
    public GameObject authMenuPanel; // Сама панель AutihMenu (чтобы закрыть её при успехе)
    public TMP_InputField loginLogInput;
    public TMP_InputField loginPassInput;
    public TMP_Text loginStatusText;

    [Header("Окно: РЕГИСТРАЦИЯ")]
    public GameObject regMenuPanel; // Сама панель RegistrationMenu (чтобы закрыть её)
    public TMP_InputField regLogInput;
    public TMP_InputField regPassInput;
    public TMP_Text regStatusText;

    [Header("Главное меню (Профиль)")]
    public GameObject guestPanel;      // Панель с кнопками Войти/Зарегаться
    public GameObject settingMenuPanel;
    public GameObject profilePanel;    // Панель с ником и кнопкой Выход
    public TMP_Text profileNameText;   // Текст, где будет написан Никнейм

    private void Start()
    {
        // При старте игры проверяем, вошел ли игрок ранее
        CheckLoginState();
    }

    // ==========================================
    // ПРОВЕРКА СОСТОЯНИЯ (ВОШЕЛ ИЛИ НЕТ?)
    // ==========================================
    public void CheckLoginState()
    {
        // Если в памяти есть сохраненное Имя игрока
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            string playerName = PlayerPrefs.GetString("PlayerName");
            
            // Включаем профиль, выключаем кнопки входа
            guestPanel.SetActive(false);
            profilePanel.SetActive(true);
            profileNameText.text = "Игрок: " + playerName;
        }
        else
        {
            // Игрок не авторизован (Гость)
            guestPanel.SetActive(true);
            profilePanel.SetActive(false);
        }
    }

    // ==========================================
    // КНОПКА: ВЫЙТИ ИЗ АККАУНТА
    // ==========================================
    public void OnLogoutButtonClicked()
    {
        PlayerPrefs.DeleteKey("PlayerID");
        PlayerPrefs.DeleteKey("PlayerName");
        PlayerPrefs.DeleteKey("PlayerScore");
        PlayerPrefs.DeleteKey("PlayerBalance");
        PlayerPrefs.DeleteKey("UnlockedLevel");

        // --- ДОБАВЛЯЕМ ВОТ ЭТУ СТРОЧКУ ---
        PlayerPrefs.DeleteKey("WinStreak");
        // ---------------------------------

        for (int i = 1; i <= 10; i++)
        {
            PlayerPrefs.DeleteKey("ACH_" + i);
        }
        // ==========================================

        PlayerPrefs.Save();

        CheckLoginState();
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.UpdateBalanceUI();
    }

    // ==========================================
    // КНОПКА: ВОЙТИ
    // ==========================================
    public void OnLoginButtonClicked()
    {
        string login = loginLogInput.text;
        string password = loginPassInput.text;

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            SetStatus(loginStatusText, "Заполните все поля!", Color.red);
            return;
        }

        SetStatus(loginStatusText, "Подключение...", Color.yellow);
        StartCoroutine(SendAuthRequest(loginURL, login, password, isLogin: true));
    }

    // ==========================================
    // КНОПКА: ЗАРЕГИСТРИРОВАТЬСЯ
    // ==========================================
    public void OnRegisterButtonClicked()
    {
        string login = regLogInput.text;
        string password = regPassInput.text;

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            SetStatus(regStatusText, "Заполните все поля!", Color.red);
            return;
        }

        SetStatus(regStatusText, "Создание аккаунта...", Color.yellow);
        StartCoroutine(SendAuthRequest(registerURL, login, password, isLogin: false));
    }

    // ==========================================
    // ОБЩАЯ КАРТИНА ДЛЯ ОТПРАВКИ ЗАПРОСОВ
    // ==========================================
    private IEnumerator SendAuthRequest(string url, string login, string password, bool isLogin)
    {
        WWWForm form = new WWWForm();
        form.AddField("login", login);
        form.AddField("password", password);

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            TMP_Text currentStatusText = isLogin ? loginStatusText : regStatusText;

            if (request.result != UnityWebRequest.Result.Success)
            {
                SetStatus(currentStatusText, "Ошибка сервера!", Color.red);
            }
            else
            {
                string jsonResponse = request.downloadHandler.text;
                AuthResponse response = JsonUtility.FromJson<AuthResponse>(jsonResponse);

                if (response != null && response.status == "success")
                {
                    SetStatus(currentStatusText, response.message, Color.green);

                    if (isLogin)
                    {
                        // УСПЕШНЫЙ ВХОД
                        PlayerPrefs.SetInt("PlayerID", response.user_id);
                        PlayerPrefs.SetString("PlayerName", response.username);
                        PlayerPrefs.SetInt("PlayerScore", response.score);
                        PlayerPrefs.SetInt("UnlockedLevel", response.unlocked_level);
                        PlayerPrefs.SetFloat("PlayerBalance", response.balance); 
                        PlayerPrefs.Save(); 

                        if (authMenuPanel != null) authMenuPanel.SetActive(false);
                        if (regMenuPanel != null) regMenuPanel.SetActive(false);
                        if (settingMenuPanel != null) settingMenuPanel.SetActive(true);

                        CheckLoginState();

                        // --- ДОБАВИТЬ ЭТУ СТРОЧКУ СЮДА ---
                        if (CurrencyManager.Instance != null) CurrencyManager.Instance.UpdateBalanceUI();
                        // ---------------------------------

                        loginLogInput.text = ""; loginPassInput.text = "";
                        regLogInput.text = ""; regPassInput.text = "";
                    }
                    else
                    {
                        // УСПЕШНАЯ РЕГИСТРАЦИЯ -> Автоматически входим
                        loginLogInput.text = login;
                        loginPassInput.text = password;
                        OnLoginButtonClicked(); 
                    }
                }
                else
                {
                    SetStatus(currentStatusText, response?.message ?? "Ошибка", Color.red);
                }
            }
        }
    }

    private void SetStatus(TMP_Text textElement, string message, Color color)
    {
        if (textElement != null)
        {
            textElement.text = message;
            textElement.color = color;
        }
    }
}

[System.Serializable]
public class AuthResponse
{
    public string status;
    public string message;
    public int user_id;
    public string username;
    public int unlocked_level;
    public int score;
    public int balance;
}