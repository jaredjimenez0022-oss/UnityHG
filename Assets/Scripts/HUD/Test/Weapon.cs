using UnityEngine;
using System;

//Clase de prueba, quitarla después o adaptarla con la de Fredrick
public class Weapon : MonoBehaviour
{
    public event Action<int, int> OnAmmoChanged;

    [Header("Configuración de Munición")]
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int currentAmmo;

    void Start()
    {
        currentAmmo = maxAmmo;
        NotifyAmmoChanged();
    }

    public bool Shoot()
    {
        if (currentAmmo > 0)
        {
            currentAmmo--;
            NotifyAmmoChanged();
            Debug.Log("Disparo. Munición: " + currentAmmo);
            return true;
        }

        Debug.Log("Sin munición!");
        return false;
    }

    public void Reload()
    {
        currentAmmo = maxAmmo;
        NotifyAmmoChanged();
        Debug.Log("Recargado. Munición: " + currentAmmo);
    }

    private void NotifyAmmoChanged()
    {
        OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
    }

    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
}