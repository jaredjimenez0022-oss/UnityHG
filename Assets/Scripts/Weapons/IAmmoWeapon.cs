using UnityEngine;

public interface IAmmoWeapon
{
    int GetCurrentAmmo();
    int GetMaxAmmo();
    System.Action<int, int> OnAmmoChanged { get; set; }
}