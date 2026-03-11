using UnityEngine;
using TMPro; // ! ВАЖНО: Подключаем библиотеку для нового текста

public class FloatingText : MonoBehaviour
{
    public float destroyTime = 1.5f; 
    public Vector3 moveSpeed = new Vector3(0, 2f, 0); 

    void Start()
    {
        // Поворачиваем текст к камере
        transform.LookAt(Camera.main.transform);
        transform.Rotate(0, 180, 0); 

        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        transform.position += moveSpeed * Time.deltaTime;
    }

    public void Setup(int amount)
    {
        // ! ВАЖНО: Используем TMP_Text вместо TextMesh
        TMP_Text tm = GetComponent<TMP_Text>();
        
        if (tm == null) return;

        tm.text = amount > 0 ? "+" + amount.ToString() : amount.ToString();
        
        // В TMP цвета меняются так же
        tm.color = amount > 0 ? Color.green : Color.red; 
    }
}