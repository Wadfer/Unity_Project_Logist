using System.Collections;
using UnityEngine;
using UnityEngine.Networking; // Обязательно добавляем эту библиотеку для веб-запросов

public class DatabaseManager : MonoBehaviour
{
    // ВПИШИ СЮДА ТОТ ЖЕ АДРЕС, КОТОРЫЙ ТЫ ВВОДИЛ В БРАУЗЕРЕ
    // Например: "http://example.local/get_data.php"
    private string url = "http://example.local/get_data.php";

    void Start()
    {
        // Запускаем процесс скачивания данных сразу при старте игры
        StartCoroutine(GetLevelsData());
    }

    IEnumerator GetLevelsData()
    {
        Debug.Log("Отправляем запрос к базе данных...");

        // Создаем GET запрос по нашему URL
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Ждем, пока сервер (PHP) ответит
            yield return request.SendWebRequest();

            // Проверяем, не было ли ошибок (сервер выключен, нет интернета и т.д.)
            if (request.result == UnityWebRequest.Result.ConnectionError || 
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Ошибка подключения: " + request.error);
            }
            else
            {
                // Если всё отлично, выводим полученный текст (наш JSON) в консоль
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("УРА! Данные получены из базы: \n" + jsonResponse);
                
                // --- Здесь мы потом будем превращать текст в игровые переменные ---
            }
        }
    }
}