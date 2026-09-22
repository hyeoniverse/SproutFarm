using UnityEngine;

// 플레이어가 닿으면 체력을 조금 채워주는 열매
public class Berry : MonoBehaviour
{
    public BerrySpawner spawner;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            spawner.TryEat(this);
        }
    }
}
