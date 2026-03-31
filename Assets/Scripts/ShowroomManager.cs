using UnityEngine;
using UnityEngine.EventSystems; 

public class ShowroomManager : MonoBehaviour, IDragHandler 
{
    [System.Serializable]
    public class PreviewModel
    {
        public int dbID;             // ID от 1 до 9
        public GameObject model;     // Сама 3D модель
    }

    [Header("Настройки")]
    public Transform showroom3DObject; // Перетащи сюда свой 3D объект Showroom!
    public float rotationSpeed = 0.5f; 
    public PreviewModel[] allModels;   

    public void OnDrag(PointerEventData eventData)
    {
        // Крутим объект мышкой!
        if (showroom3DObject != null)
            showroom3DObject.Rotate(0, -eventData.delta.x * rotationSpeed, 0, Space.World);
    }

    public void ShowSkin(int skinId)
    {
        // Сбрасываем поворот
        if (showroom3DObject != null) showroom3DObject.rotation = Quaternion.identity;

        // Включаем нужную машину
        foreach (var preview in allModels)
        {
            if (preview.model != null)
                preview.model.SetActive(preview.dbID == skinId);
        }
    }
}