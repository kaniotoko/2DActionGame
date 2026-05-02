using UnityEngine;

public class CameraCtrl : MonoBehaviour
{
    public Transform player; //Transform型関数では位置情報を扱う
    Vector3 startPos; //最初の座標

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.position = new Vector3(player.position.x, player.position.y, startPos.z);
    }
}
