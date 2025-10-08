using UnityEngine;
using TMPro;

public class WeaponAmmoHUD : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private GameObject ammoPanel;    
    [SerializeField] private TextMeshProUGUI ammoText; 

    [SerializeField] private Weapon currentWeapon;

    void Start()
    {
        // Ocultar al inicio hasta que se equipe un arma
        if (ammoPanel != null)
            ammoPanel.SetActive(false);

        ConnectToWeapon(currentWeapon);
    }

    public void ConnectToWeapon(Weapon weapon)
    {
        // Desconectar del arma anterior
        if (currentWeapon != null)
        {
            currentWeapon.OnAmmoChanged -= UpdateAmmoDisplay;
        }

        // Conectar al nuevo arma
        currentWeapon = weapon;
        
        if (ammoPanel != null)
            ammoPanel.SetActive(true);
        
        if (weapon != null && ammoText != null)
        {
            weapon.OnAmmoChanged += UpdateAmmoDisplay;
            // Actualizar display con valores iniciales
            UpdateAmmoDisplay(weapon.GetCurrentAmmo(), weapon.GetMaxAmmo());
        }
    }

    public void DisconnectWeapon()
    {
        if (currentWeapon != null)
        {
            currentWeapon.OnAmmoChanged -= UpdateAmmoDisplay;
            currentWeapon = null;
        }
        
        if (ammoPanel != null)
            ammoPanel.SetActive(false);
    }

    private void UpdateAmmoDisplay(int current, int max)
    {
        if (ammoText != null)
            ammoText.text = $"Ammo: {current} / {max}";
            
        //Cambiar a rojo la letra si queda poca municion
        if (current <= max * 0.2f) 
            ammoText.color = Color.red;
        else
            ammoText.color = Color.white;
    }
}