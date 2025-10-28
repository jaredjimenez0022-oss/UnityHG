using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System;
using Scripts;

public class PlayerHUD : MonoBehaviour
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

    public static PlayerHUD Instance { get; private set; }
    private GameObject hudInstance;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    void Start()
    {
        InstantiateHUD();
        InitializeHUDReferences();
        FindLocalPlayer();
        //playersCounter = FindFirstObjectByType<AlivePlayersCounter>();
    }

    void Update()
    {
        if (playerHealth == null || playerShield == null)
        {
            FindLocalPlayer();
            return;
        }

        UpdateHealthDisplay();
        UpdateShieldDisplay();
        UpdateHealthColor();
        UpdateWeaponReferences();
    }

    private void InstantiateHUD()
    {
        if (hudPrefab != null)
        {
            hudInstance = Instantiate(hudPrefab);
        }
        else
        {
            Debug.LogError("HUD Canvas Prefab no asignado en el Inspector");
        }
    }

    private void InitializeHUDReferences()
    {
        if (hudInstance == null)
        {
            Debug.LogError("HUD instance es null, no se pueden inicializar referencias");
            return;
        }

        healthBar = GameObject.Find("HealthBar")?.GetComponent<Slider>();
        shieldBar = GameObject.Find("ShieldBar")?.GetComponent<Slider>();
        healthText = GameObject.Find("HealthText")?.GetComponent<TextMeshProUGUI>();
        shieldText = GameObject.Find("ShieldText")?.GetComponent<TextMeshProUGUI>();
        healthFill = GameObject.Find("HealthFill")?.GetComponent<Image>();
        shieldFill = GameObject.Find("ShieldFill")?.GetComponent<Image>();
        ammoPanel = GameObject.Find("WeaponAmmoPanel");
        ammoText = GameObject.Find("AmmoText")?.GetComponent<TextMeshProUGUI>();
        if (ammoText == null)
        {
            if (ammoPanel != null)
                ammoText = ammoPanel.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (healthBar == null) Debug.LogError("HealthBar no encontrado en el HUD");
        if (shieldBar == null) Debug.LogError("ShieldBar no encontrado en el HUD");
        if (healthText == null) Debug.LogError("HealthText no encontrado en el HUD");
        if (shieldText == null) Debug.LogError("ShieldText no encontrado en el HUD");
        if (ammoPanel == null) Debug.LogWarning("AmmoPanel no encontrado en el HUD");
        if (ammoText == null) Debug.LogWarning("AmmoText no encontrado en el HUD");

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


    private void UpdateWeaponReferences()
    {
        if (weaponManager == null && playerHealth != null)
        {
            FindWeaponManager();
        }

        if (weaponManager != null && currentWeapon == null)
        {
            FindPlayerWeapon();
        }
        else if (weaponManager != null && currentWeapon != weaponManager.GetCurrentWeapon())
        {
            ConnectToWeapon(weaponManager.GetCurrentWeapon());
        }
    }


    private void FindWeaponManager()
    {
        if (playerHealth == null) return;

        weaponManager = playerHealth.GetComponent<WeaponManagerNetwork>();
        if (weaponManager == null)
        {
            weaponManager = playerHealth.GetComponentInChildren<WeaponManagerNetwork>();
        }
        
        if (weaponManager != null)
        {
            Debug.Log("PlayerHUD encontrado WeaponManagerNetwork");
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

    private void FindLocalPlayer()
    {

        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");

        foreach (GameObject playerObj in playerObjects)
        {
            PlayerHealth health = playerObj.GetComponent<PlayerHealth>();


            if (health != null && health.HasStateAuthority)
            {
                if (playerHealth != null)
                {
                    playerHealth.OnHealthChanged -= OnHealthChanged;
                }
                if (playerShield != null)
                {
                    playerShield.OnShieldChanged -= OnShieldChanged;
                }

                playerHealth = health;
                playerShield = playerObj.GetComponent<PlayerShield>();

                if (playerHealth == null || playerShield == null)
                {
                    Debug.LogWarning("Jugador encontrado pero falta PlayerHealth o PlayerShield");
                    continue;
                }

                playerHealth.OnHealthChanged += OnHealthChanged;
                playerShield.OnShieldChanged += OnShieldChanged;

                currentHealthDisplay = playerHealth.GetCurrentHealth();
                currentShieldDisplay = playerShield.GetCurrentShield();

                if (healthBar != null) healthBar.value = currentHealthDisplay;
                if (shieldBar != null) shieldBar.value = currentShieldDisplay;

                UpdateTexts();

                FindWeaponManager();

                Debug.Log("HUD Global conectado al jugador local");
                break;
            }
        }
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
