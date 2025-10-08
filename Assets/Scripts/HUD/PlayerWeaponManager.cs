using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWeaponManager : MonoBehaviour
{
    [SerializeField] private WeaponAmmoHUD ammoHUD;
    [SerializeField] private Weapon currentWeapon;

    void Start() => ConnectWeaponToHUD();

    void Update()
    {
        if (currentWeapon == null) return;
        
        if (Mouse.current.leftButton.wasPressedThisFrame) 
            currentWeapon.Shoot();
            
        if (Keyboard.current.rKey.wasPressedThisFrame) 
            currentWeapon.Reload();
    }

    void ConnectWeaponToHUD()
    {
        if (ammoHUD != null && currentWeapon != null)
            ammoHUD.ConnectToWeapon(currentWeapon);
    }

    public void EquipWeapon(Weapon newWeapon)
    {
        currentWeapon = newWeapon;
        ConnectWeaponToHUD();
    }
}