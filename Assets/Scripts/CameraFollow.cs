using UnityEngine;
using UnityEngine.InputSystem; // Обязательно для работы мыши

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Машинка

    [Header("Настройки Зума (Колесико)")]
    public float currentZoom = 15f; // Текущая дистанция
    public float minZoom = 5f;      // Макс. приближение
    public float maxZoom = 40f;     // Макс. отдаление
    public float zoomSensitivity = 0.02f; // Чувствительность колесика

    [Header("Настройки Вращения (ПКМ)")]
    public float rotateSpeedX = 0.5f; // Скорость вращения влево-вправо
    public float rotateSpeedY = 0.5f; // Скорость вращения вверх-вниз
    public float minPitch = 10f;      // Не опускаться ниже земли (градусы)
    public float maxPitch = 80f;      // Не перелетать через голову (градусы)

    // Текущие углы поворота камеры
    private float currentYaw = 0f;   // Горизонтальный угол
    private float currentPitch = 45f; // Вертикальный угол

    void Start()
    {
        // Если при старте камера стоит как-то криво, вычисляем начальные углы
        if (target != null)
        {
            Vector3 angles = transform.eulerAngles;
            currentYaw = angles.y;
            currentPitch = angles.x;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (Mouse.current == null) return;

        // --- 1. ВРАЩЕНИЕ (Только если зажата Правая Кнопка) ---
        if (Mouse.current.rightButton.isPressed)
        {
            // Получаем движение мыши (Delta)
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            // Изменяем углы
            currentYaw += mouseDelta.x * rotateSpeedX;
            currentPitch -= mouseDelta.y * rotateSpeedY;

            // Ограничиваем вертикальный угол (чтобы камера не ушла под землю)
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        // --- 2. ЗУМ (Колесико) ---
        float scrollInput = Mouse.current.scroll.y.ReadValue();
        if (scrollInput != 0)
        {
            currentZoom -= scrollInput * zoomSensitivity;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        }

        // --- 3. РАСЧЕТ ПОЗИЦИИ ---
        // Превращаем углы в Кватернион (поворот)
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        // Позиция = ПозицияЦели - (Направление * Дистанция)
        // Добавляем Vector3.up, чтобы смотреть на крышу машины, а не на колеса
        Vector3 targetCenter = target.position + Vector3.up * 1.5f;
        Vector3 position = targetCenter - (rotation * Vector3.forward * currentZoom);

        // Применяем
        transform.position = position;
        transform.LookAt(targetCenter);
    }
}