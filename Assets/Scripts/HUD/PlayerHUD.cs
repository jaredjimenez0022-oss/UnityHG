using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("Referencias UI")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private Slider shieldBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI shieldText;
    [SerializeField] private Image healthFill; 

    [Header("Animación")]
    [SerializeField] private float smoothSpeed = 5f; 

    private PlayerHealth playerHealth;
    private PlayerShield playerShield;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerShield = GetComponent<PlayerShield>();

        healthBar.maxValue = 100; 
        shieldBar.maxValue = 50;  

        healthBar.value = playerHealth.GetCurrentHealth();
        shieldBar.value = playerShield.GetCurrentShield();
    }

    void Update()
    {
        // Animacion para que la barra de vida baje suavemente
        float targetHealth = playerHealth.GetCurrentHealth();
        healthBar.value = Mathf.Lerp(healthBar.value, targetHealth, Time.deltaTime * smoothSpeed);
        //healthBar.value = Mathf.MoveTowards(healthBar.value, targetHealth, smoothSpeed * Time.deltaTime * 100f);

        // Lo mismo para la del escudo
        float targetShield = playerShield.GetCurrentShield();
        shieldBar.value = Mathf.Lerp(shieldBar.value, targetShield, Time.deltaTime * smoothSpeed);
        //shieldBar.value = Mathf.MoveTowards(shieldBar.value, targetShield, smoothSpeed * Time.deltaTime * 50f);


        healthText.text = $"{playerHealth.GetCurrentHealth()} / {healthBar.maxValue} HP";
        shieldText.text = $"{playerShield.GetCurrentShield()} / {shieldBar.maxValue} Shield";


        float healthPercent = healthBar.value / healthBar.maxValue;
        //float healthPercent = playerHealth.GetCurrentHealth() / healthBar.maxValue;
        if (healthPercent > 0.6f)
            healthFill.color = Color.green;
        else if (healthPercent > 0.3f)
            healthFill.color = Color.yellow;
        else
            healthFill.color = Color.red;
    }
}
