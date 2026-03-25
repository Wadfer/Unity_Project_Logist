using UnityEngine;

public class FloatingArrow : MonoBehaviour
{
    [Header("Настройки высоты")]
    public float minHeight = 2.0f; 
    public float maxHeight = 3.0f; 
    public float hoverSpeed = 2f;  

    [Header("Поправка поворота (для 3D модели)")]
    // Задаем наклон по умолчанию: 90 градусов по оси X, чтобы острие смотрело вниз
    public Vector3 rotationOffset = new Vector3(90, 0, 0); 

    private Vector3 basePosition;
    private Camera mainCam;

    void Start()
    {
        basePosition = transform.position;
        mainCam = Camera.main;
    }

    void Update()
    {
        // 1. ПОЛЕТ ОТ MIN ДО MAX
        float t = (Mathf.Sin(Time.time * hoverSpeed) + 1f) / 2f;
        float currentY = Mathf.Lerp(minHeight, maxHeight, t);
        transform.position = new Vector3(basePosition.x, basePosition.y + currentY, basePosition.z);
        
        // 2. ПОВОРОТ К КАМЕРЕ + НАКЛОН ВНИЗ
        if (mainCam != null)
        {
            Vector3 directionToCamera = mainCam.transform.position - transform.position;
            directionToCamera.y = 0; // Не даем заваливаться
            
            if (directionToCamera != Vector3.zero)
            {
                // Сначала вычисляем поворот к камере
                Quaternion lookAtCamera = Quaternion.LookRotation(-directionToCamera);
                
                // Затем "докручиваем" модельку на 90 градусов вниз (используя наш offset)
                transform.rotation = lookAtCamera * Quaternion.Euler(rotationOffset);
            }
        }
    }
    
    public void SetColor(Color color)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.material.color = color;
        }
    }
}