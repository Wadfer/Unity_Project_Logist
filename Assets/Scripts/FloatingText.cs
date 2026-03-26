using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public float destroyTime = 1.5f; 
    public Vector3 moveSpeed = new Vector3(0, 2f, 0); 
    
    private Camera mainCam; // Запоминаем камеру, чтобы не искать её каждый кадр

    void Start()
    {
        mainCam = Camera.main; // Находим камеру один раз при старте
        
        // Уничтожаем объект через заданное время
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // 1. Текст летит вверх
        transform.position += moveSpeed * Time.deltaTime;

        // 2. Идеальный поворот к камере (Билборд)
        if (mainCam != null)
        {
            // Направление от текста к камере
            Vector3 directionToCamera = mainCam.transform.position - transform.position;
            
            // Запрещаем тексту наклоняться вверх/вниз (стоит строго вертикально)
            directionToCamera.y = 0; 
            
            if (directionToCamera != Vector3.zero)
            {
                // Поворачиваем текст "лицом" к камере
                // (Минус перед direction нужен, потому что 3D-текст в Unity читается с обратной стороны)
                transform.rotation = Quaternion.LookRotation(-directionToCamera);
            }
        }
    }

    public void Setup(int amount)
    {
        TMP_Text tm = GetComponentInChildren<TMP_Text>();
        
        if (tm == null) return;

        if (amount == 0) 
        {
            tm.text = "";
            return;
        }

        tm.text = amount > 0 ? "+" + amount.ToString() : amount.ToString();
        tm.color = amount > 0 ? Color.green : Color.red; 
    }
}