using UnityEngine;
using UnityEngine.InputSystem; // 1. これを追加

public class PlayerCrtl : MonoBehaviour
{
    Rigidbody2D rb;
    public float jumpPower;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 2. 新しいInput Systemの書き方に変更
        if(Keyboard.current.spaceKey.wasPressedThisFrame) 
        {
            rb.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
        }
    }
}