using System.Collections;
using UnityEngine;
using UnityEngine.Networking; 

public class DatabaseManager : MonoBehaviour
{
    private string url = "http://example.local/get_data.php"; // Твой адрес!

    [Header("Настройки сцены")]
    public string currentLevelNumber = "1";

    void Start()
    {
        StartCoroutine(GetLevelsData());
    }

    IEnumerator GetLevelsData()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Ошибка подключения: " + request.error);
            }
            else
            {
                string jsonResponse = request.downloadHandler.text.Trim();
                
                // Фикс массива (оборачиваем в объект, если нужно)
                if (jsonResponse.StartsWith("[")) 
                {
                    jsonResponse = "{\"levels\":" + jsonResponse + "}";
                }

                LevelList allData = JsonUtility.FromJson<LevelList>(jsonResponse);

                if (allData != null && allData.levels != null)
                {
                    bool levelFound = false;
                    foreach (LevelData lvl in allData.levels)
                    {
                        if (lvl.level_number == currentLevelNumber)
                        {
                            levelFound = true;
                            
                            // 1. ПРЕВРАЩАЕМ ТЕКСТ ИЗ БД В ЧИСЛА
                            int parsedFuel = int.Parse(lvl.fuel_amount);
                            int parsedDifficultyId = int.Parse(lvl.id_coefficient);

                            // 2. ПЕРЕДАЕМ ЭТИ ЧИСЛА В GAMEMANAGER
                            if (GameManager.Instance != null)
                            {
                                GameManager.Instance.SetupFromDatabase(parsedFuel, parsedDifficultyId);
                            }
                            else
                            {
                                Debug.LogError("DatabaseManager скачал данные, но не нашел GameManager на сцене!");
                            }
                            break; 
                        }
                    }

                    if (!levelFound) Debug.LogWarning("Уровень " + currentLevelNumber + " не найден в БД!");
                }
            }
        }
    }
}

[System.Serializable]
public class LevelList
{
    public LevelData[] levels; 
}

[System.Serializable]
public class LevelData
{
    public string id_level;       
    public string level_number;
    public string id_coefficient;
    public string fuel_amount;
}