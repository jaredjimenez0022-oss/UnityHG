using UnityEngine;
using TMPro;
using Fusion;

public class AlivePlayersCounter : NetworkBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI alivePlayersText;

    [Networked] private int networkAliveCount { get; set; }


    public override void Spawned()
    {
        if (alivePlayersText == null)
            alivePlayersText = GameObject.Find("AlivePlayersText")?.GetComponent<TextMeshProUGUI>();
            
        UpdateText();
    }

    public override void FixedUpdateNetwork()
    {
        // Sincronizar con el GameStateManager real
        if (GameStateManager.Instance != null && HasStateAuthority)
        {
            networkAliveCount = GameStateManager.Instance.AlivePlayersCount;
        }
        
        UpdateText();
    }
    private void UpdateText()
    {
        if (alivePlayersText != null)
        {
            alivePlayersText.text = $"Alive: {networkAliveCount}";
        }
    }


}