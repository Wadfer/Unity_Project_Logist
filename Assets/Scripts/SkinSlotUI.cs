using UnityEngine;
using UnityEngine.UI;

public class SkinSlotUI : MonoBehaviour
{
    [Header("КАКОЙ ЭТО СКИН? (Впиши ID из базы 1-9)")]
    public int skinIdFromDB; 

    // Ссылку на ShopManager нужно перетащить в инспекторе для каждой из 9 кнопок!
    public ShopManager shopManager; 

    private void Start()
    {
        // Автоматически вешаем команду на клик
        GetComponent<Button>().onClick.AddListener(() => shopManager.SelectSkin(skinIdFromDB));
    }
}