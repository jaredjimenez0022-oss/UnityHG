using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    public int damage;
    public float range;

    public abstract void Attack();
}
