using UnityEngine;

public class FloatingWarning : MonoBehaviour
{
    [Header("Настройки высоты (относительно земли)")]
    public float minHeight = 0.5f; // Нижняя точка (например, полметра над землей)
    public float maxHeight = 1.0f; // Верхняя точка (например, метр над землей)
    public float hoverSpeed = 2f;  // Скорость полета вверх-вниз
    
    private Vector3 basePosition;
    private Camera mainCam;

    void Start()
    {
        // Запоминаем точку на земле (центр серого круга)
        basePosition = transform.position; 
        mainCam = Camera.main;
    }

    void Update()
    {
        // 1. ПОЛЕТ ОТ MIN ДО MAX
        // Mathf.Sin выдает значения от -1 до 1. 
        // Мы превращаем это в ползунок "t" от 0 до 1.
        float t = (Mathf.Sin(Time.time * hoverSpeed) + 1f) / 2f;
        
        // Плавно вычисляем текущую высоту между min и max
        float currentY = Mathf.Lerp(minHeight, maxHeight, t);
        
        // Применяем позицию (базовая X и Z остаются, Y меняется)
        transform.position = new Vector3(basePosition.x, basePosition.y + currentY, basePosition.z);
        
        // 2. ПОВОРОТ К КАМЕРЕ (БИЛБОРД)
        if (mainCam != null)
        {
            Vector3 directionToCamera = mainCam.transform.position - transform.position;
            directionToCamera.y = 0; // Запрещаем наклоняться
            
            if (directionToCamera != Vector3.zero) // Защита от ошибки
            {
                transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }
}