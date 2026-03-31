using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    private string getShopUrl = "http://138.124.230.211/get_shop.php"; 
    private string buySkinUrl = "http://138.124.230.211/buy_skin.php"; 
    private string equipSkinUrl = "http://138.124.230.211/equip_skin.php"; 

    [Header("Общие элементы UI (ОДНИ НА ВЕСЬ МАГАЗИН)")]
    public TMP_Text globalNameText;     // Перетащи сюда NameCar
    public TMP_Text globalPriceText;    // Перетащи сюда PriceCar
    public Button buyButton;            // Общая кнопка "Купить"
    public Button equipButton;          // Общая кнопка "Применить"
    public Button equippedButton;       // Общая кнопка "Применено"

    [Header("Окно магазина и 3D Витрина")]
    public GameObject shopPanel;       
    public ShowroomManager showroom;    // Сюда перетащим скрипт крутилки (сделаем в шаге 3)

    private ShopResponse currentShopData;
    private int currentSelectedSkinId = 1; // Какая машина сейчас на экране

    public void OpenShop() 
    {
        shopPanel.SetActive(true);
        if (PlayerPrefs.HasKey("PlayerID"))
        {
            StartCoroutine(LoadShopData(PlayerPrefs.GetInt("PlayerID")));
        }
    }

    private IEnumerator LoadShopData(int userId)
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", userId.ToString());

        using (UnityWebRequest request = UnityWebRequest.Post(getShopUrl, form))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                currentShopData = JsonUtility.FromJson<ShopResponse>(request.downloadHandler.text);
                
                // При открытии магазина сразу показываем НАДЕТУЮ машину
                SelectSkin(currentShopData.equipped_skin); 
            }
        }
    }

    // ЭТА ФУНКЦИЯ ВЫЗЫВАЕТСЯ ПРИ КЛИКЕ НА ЛЮБУЮ ИЗ 9 КНОПОК
    public void SelectSkin(int skinId)
    {
        if (currentShopData == null || currentShopData.all_skins == null) return;

        currentSelectedSkinId = skinId;

        // Ищем машину в базе
        SkinData skinInfo = null;
        foreach (SkinData s in currentShopData.all_skins)
        {
            if (s.id_skin == skinId) skinInfo = s;
        }
        if (skinInfo == null) return;

        // Проверяем статусы
        bool isOwned = false;
        if (currentShopData.owned_skins != null)
        {
            foreach (int id in currentShopData.owned_skins)
            {
                if (id == skinId) isOwned = true;
            }
        }
        bool isEquipped = (currentShopData.equipped_skin == skinId);

        // Обновляем тексты
        if (globalNameText != null) globalNameText.text = skinInfo.skin_name;

        // Обновляем 3D модель в витрине!
        if (showroom != null) showroom.ShowSkin(skinId);

        // Очищаем старые нажатия кнопок
        buyButton.onClick.RemoveAllListeners();
        equipButton.onClick.RemoveAllListeners();
        equippedButton.onClick.RemoveAllListeners();

        // Логика кнопок
        if (isEquipped)
        {
            if (globalPriceText != null) globalPriceText.text = "НАДЕТО";
            buyButton.gameObject.SetActive(false);
            equipButton.gameObject.SetActive(false);
            equippedButton.gameObject.SetActive(true);

            // Если нажать "Применено" - надеваем первую (базовую) машину
            equippedButton.onClick.AddListener(() => { if (skinId != 1) EquipSkin(1); });
        }
        else if (isOwned)
        {
            if (globalPriceText != null) globalPriceText.text = "В ГАРАЖЕ";
            buyButton.gameObject.SetActive(false);
            equipButton.gameObject.SetActive(true);
            equippedButton.gameObject.SetActive(false);

            equipButton.onClick.AddListener(() => EquipSkin(skinId));
        }
        else
        {
            if (globalPriceText != null) globalPriceText.text = skinInfo.skin_price.ToString() + " монет";
            buyButton.gameObject.SetActive(true);
            equipButton.gameObject.SetActive(false);
            equippedButton.gameObject.SetActive(false);

            buyButton.onClick.AddListener(() => BuySkin(skinId, skinInfo.skin_price));
        }
    }

    // Покупка и экипировка остаются такими же (только в конце вызываем SelectSkin)
    private void BuySkin(int skinId, float price)
    {
        if (PlayerPrefs.GetFloat("PlayerBalance", 0) >= price)
            StartCoroutine(SendBuyRequest(PlayerPrefs.GetInt("PlayerID"), skinId, price));
    }

    private IEnumerator SendBuyRequest(int userId, int skinId, float price)
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", userId.ToString());
        form.AddField("skin_id", skinId.ToString());

        using (UnityWebRequest request = UnityWebRequest.Post(buySkinUrl, form))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                BuyResponse response = JsonUtility.FromJson<BuyResponse>(request.downloadHandler.text);
                if (response != null && response.status == "success")
                {
                    PlayerPrefs.SetFloat("PlayerBalance", response.new_balance);
                    if (CurrencyManager.Instance != null) CurrencyManager.Instance.UpdateBalanceUI();

                    List<int> tempList = new List<int>();
                    if (currentShopData.owned_skins != null) tempList.AddRange(currentShopData.owned_skins);
                    tempList.Add(skinId);
                    currentShopData.owned_skins = tempList.ToArray();
                    
                    SelectSkin(currentSelectedSkinId); // Обновляем экран
                }
            }
        }
    }

    private void EquipSkin(int skinId)
    {
        StartCoroutine(SendEquipRequest(PlayerPrefs.GetInt("PlayerID"), skinId));
    }

    private IEnumerator SendEquipRequest(int userId, int skinId)
    {
        WWWForm form = new WWWForm();
        form.AddField("user_id", userId.ToString());
        form.AddField("skin_id", skinId.ToString());

        using (UnityWebRequest request = UnityWebRequest.Post(equipSkinUrl, form))
        {
            yield return request.SendWebRequest();
            if (request.downloadHandler.text.Contains("success"))
            {
                currentShopData.equipped_skin = skinId; 
                PlayerPrefs.SetInt("EquippedSkinID", skinId);
                PlayerPrefs.Save();
                
                SelectSkin(currentSelectedSkinId); // Обновляем экран
            }
        }
    }
}

// === КЛАССЫ JSON ОСТАЮТСЯ КАК БЫЛИ ===
[System.Serializable] public class ShopResponse { public string status; public SkinData[] all_skins; public int[] owned_skins; public int equipped_skin; }
[System.Serializable] public class SkinData { public int id_skin; public string skin_name; public float skin_price; public string skin_rarity; }
[System.Serializable] public class BuyResponse { public string status; public float new_balance; }