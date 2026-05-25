using UnityEngine;

public class LiftCtrl : MonoBehaviour
{
    public float targetX = 20f;   // 停止するX座標
    public float speed = 3f;      // 移動速度

    Rigidbody2D rb;
    PlayerCrtl playerCtrl;
    bool arrived = false;
    bool initialRide = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!initialRide || arrived) return;

        if (rb.position.x >= targetX)
        {
            rb.MovePosition(new Vector2(targetX, rb.position.y));
            arrived = true;
            if (playerCtrl != null)
            {
                playerCtrl.platformVelX = 0f;
            }
            return;
        }

        Vector2 delta = new Vector2(speed * Time.fixedDeltaTime, 0);
        rb.MovePosition(rb.position + delta);
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player") &&
            other.transform.position.y > transform.position.y)
        {
            playerCtrl = other.gameObject.GetComponent<PlayerCrtl>();
            initialRide = true;
            if (playerCtrl != null && !arrived)
            {
                playerCtrl.platformVelX = speed;
            }
        }
    }

    void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (playerCtrl != null)
            {
                playerCtrl.platformVelX = 0f;
            }
            playerCtrl = null;
        }
    }
}
