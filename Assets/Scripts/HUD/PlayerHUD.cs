using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System;
using Scripts;

public class PlayerHUD : NetworkBehaviour
{
    [Header("Prefab del Canvas HUD")]
    [SerializeField] private GameObject hudPrefab;

    [Header("Referencias UI Salud y Escudo")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider shieldBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI shieldText;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image shieldFill;

    [Header("Referencias UI - Munición")]
    [SerializeField] private GameObject ammoPanel;
    [SerializeField] private TextMeshProUGUI ammoText;

    [Header("Animación")]
    [SerializeField] private float smoothSpeed = 5f;

    private PlayerHealth playerHealth;
    private PlayerShield playerShield;

    private WeaponManagerNetwork weaponManager;
    private Weapon currentWeapon;
    //private AlivePlayersCounter playersCounter;

    private float currentHealthDisplay;
    private float currentShieldDisplay;
    private GameObject hudInstance;


    public override void Spawned()
    {
        if (!HasInputAuthority)
        {
            Destroy(this);
            return;
        }
        InitializeHUD();
        FindPlayerComponents();
    }



    private void InitializeHUD()
    {
        if (hudPrefab != null)
        {
            hudInstance = Instantiate(hudPrefab);

            healthBar = FindComponent<Slider>("HealthBar");
            shieldBar = FindComponent<Slider>("ShieldBar");
            healthText = FindComponent<TextMeshProUGUI>("HealthText");
            shieldText = FindComponent<TextMeshProUGUI>("ShieldText");
            healthFill = FindComponent<Image>("HealthFill");
            shieldFill = FindComponent<Image>("ShieldFill");
            ammoPanel = FindGameObject("WeaponAmmoPanel");
            ammoText = FindComponent<TextMeshProUGUI>("AmmoText");

            currentHealthDisplay = 0f;
            currentShieldDisplay = 0f;

            if (healthBar != null)
            {
                healthBar.maxValue = 100;
                healthBar.value = 0;
            }
            if (shieldBar != null)
            {
                shieldBar.maxValue = 50;
                shieldBar.value = 0;
            }

            if (ammoPanel != null)
            {
                ammoPanel.SetActive(false);
            }

            //playersCounter = FindFirstObjectByType<AlivePlayersCounter>();
        }

    }

    private void FindPlayerComponents()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerShield = GetComponent<PlayerShield>();
        weaponManager = GetComponent<WeaponManagerNetwork>();

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += OnHealthChanged;
            currentHealthDisplay = playerHealth.GetCurrentHealth();
            if (healthBar != null) healthBar.value = currentHealthDisplay;
        }

        if (playerShield != null)
        {
            playerShield.OnShieldChanged += OnShieldChanged;
            currentShieldDisplay = playerShield.GetCurrentShield();
            if (shieldBar != null) shieldBar.value = currentShieldDisplay;
        }

        UpdateTexts();
    }

    void Update()
    {
        if (!HasInputAuthority) return;

        UpdateHealthDisplay();
        UpdateShieldDisplay();
        UpdateHealthColor();
        UpdateWeaponReferences();
    }


    private T FindComponent<T>(string name) where T : Component
    {
        if (hudInstance == null) return null;
        return FindComponentInChildren<T>(hudInstance, name);
    }

    private GameObject FindGameObject(string name)
    {
        if (hudInstance == null) return null;
        return FindGameObjectInChildren(hudInstance, name);
    }

    private T FindComponentInChildren<T>(GameObject parent, string name) where T : Component
    {
        if (parent == null) return null;

        foreach (Transform child in parent.transform)
        {
            if (child.name == name)
                return child.GetComponent<T>();

            T found = FindComponentInChildren<T>(child.gameObject, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private GameObject FindGameObjectInChildren(GameObject parent, string name)
    {
        if (parent == null) return null;

        foreach (Transform child in parent.transform)
        {
            if (child.name == name)
                return child.gameObject;

            GameObject found = FindGameObjectInChildren(child.gameObject, name);
            if (found != null)
                return found;
        }
        return null;
    }


    private void UpdateWeaponReferences()
    {
        if (weaponManager == null) return;


        if (currentWeapon == null)
        {
            FindPlayerWeapon();
        }
        else if (currentWeapon != weaponManager.GetCurrentWeapon())
        {
            ConnectToWeapon(weaponManager.GetCurrentWeapon());
        }
    }


    private void FindPlayerWeapon()
    {
        if (weaponManager != null)
        {
            Weapon weapon = weaponManager.GetCurrentWeapon();
            if (weapon != null && weapon != currentWeapon)
            {
                ConnectToWeapon(weapon);
            }
        }
    }

    public void ConnectToWeapon(Weapon weapon)
    {
        DisconnectWeapon();
       
        currentWeapon = weapon;
        
        if (weapon != null)
        {
            IAmmoWeapon ammoWeapon = GetAmmoWeaponComponent(weapon);
            
            if (ammoWeapon != null)
            {
                ammoWeapon.OnAmmoChanged += UpdateAmmoDisplay;
                
                if (ammoPanel != null)
                    ammoPanel.SetActive(true);
                
                UpdateAmmoDisplay(ammoWeapon.GetCurrentAmmo(), ammoWeapon.GetMaxAmmo());
                
                Debug.Log($"PlayerHUD conectado al arma con munición: {weapon.nameWeapon}");
            }
            else
            {
                if (ammoPanel != null)
                    ammoPanel.SetActive(false);
                
                Debug.Log($"PlayerHUD: {weapon.nameWeapon} no usa munición");
            }
        }
        else
        {
            if (ammoPanel != null)
                ammoPanel.SetActive(false);
        }
    }

    public void DisconnectWeapon()
    {
        if (currentWeapon != null)
        {
            IAmmoWeapon ammoWeapon = GetAmmoWeaponComponent(currentWeapon);
            if (ammoWeapon != null)
            {
                ammoWeapon.OnAmmoChanged -= UpdateAmmoDisplay;
            }
            currentWeapon = null;
        }

        if (ammoPanel != null)
            ammoPanel.SetActive(false);
    }

    private IAmmoWeapon GetAmmoWeaponComponent(Weapon weapon)
    {
        MonoBehaviour[] components = weapon.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component is IAmmoWeapon ammoWeapon)
            {
                return ammoWeapon;
            }
        }
        return null;
    }

    private void UpdateAmmoDisplay(int current, int max)
    {
        if (ammoText == null)
        {
            Debug.LogWarning("ammoText es null en UpdateAmmoDisplay");
            return;
        }

        ammoText.text = $"Ammo: {current} / {max}";

        if (current <= max * 0.2f)
            ammoText.color = Color.red;
        else
            ammoText.color = Color.white;
    }

    
    private void OnHealthChanged(int current, int max)
    {
        currentHealthDisplay = current;
        UpdateTexts();
    }

    private void OnShieldChanged(int current, int max)
    {
        currentShieldDisplay = current;
        UpdateTexts();
    }

    private void UpdateHealthDisplay()
    {
        if (playerHealth == null || healthBar == null) return;

        float targetHealth = playerHealth.GetCurrentHealth();
        currentHealthDisplay = Mathf.Lerp(currentHealthDisplay, targetHealth, Time.deltaTime * smoothSpeed);
        healthBar.value = currentHealthDisplay;
    }

    private void UpdateShieldDisplay()
    {
        if (playerShield == null || shieldBar == null) return;

        float targetShield = playerShield.GetCurrentShield();
        currentShieldDisplay = Mathf.Lerp(currentShieldDisplay, targetShield, Time.deltaTime * smoothSpeed);
        shieldBar.value = currentShieldDisplay;
    }

    private void UpdateTexts()
    {
        if (healthText != null && playerHealth != null)
            healthText.text = $"{playerHealth.GetCurrentHealth()} / {healthBar.maxValue} HP";
        
        if (shieldText != null && playerShield != null)
            shieldText.text = $"{playerShield.GetCurrentShield()} / {shieldBar.maxValue} Shield";
    }

    private void UpdateHealthColor()
    {
        if (healthFill != null && healthBar != null)
        {
            float healthPercent = healthBar.value / healthBar.maxValue;

            if (healthPercent > 0.6f)
                healthFill.color = Color.green;
            else if (healthPercent > 0.3f)
                healthFill.color = Color.yellow;
            else
                healthFill.color = Color.red;
        }
        
        if (shieldFill != null)
        {
            shieldFill.color = Color.blue;
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= OnHealthChanged;
        if (playerShield != null)
            playerShield.OnShieldChanged -= OnShieldChanged;

        DisconnectWeapon();
    }
}
