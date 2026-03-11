using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Сюда перетащить игрока (машинку)
    public float smoothSpeed = 0.125f; // Плавность движения
    public Vector3 offset; // Сдвиг камеры относительно игрока

    void Start()
    {
        // Если забыл настроить offset в редакторе, он вычислится автоматически на старте
        if (target != null)
            offset = transform.position - target.position;
    }

    void LateUpdate() // LateUpdate лучше для камер, чтобы не дергалась
    {
        if (target == null) return;

        // Желаемая позиция камеры
        Vector3 desiredPosition = target.position + offset;
        
        // Плавное перемещение (Lerp) от текущей позиции к желаемой
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        transform.position = smoothedPosition;
        
        // Камера всегда смотрит на игрока (опционально)
        // transform.LookAt(target); 
    }
}