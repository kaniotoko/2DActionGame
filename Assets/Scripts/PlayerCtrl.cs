using UnityEngine;

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
        if(Input.GetButtonDown("Jump"))
        {
            rb.AddForceY(jumpPower, ForceMode2D.Impulse);
        }
    }
}
