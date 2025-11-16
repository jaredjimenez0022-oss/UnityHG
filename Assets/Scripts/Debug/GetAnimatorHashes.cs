using UnityEngine;

/// <summary>
/// Script temporal para obtener los hash values de los parámetros del Animator.
/// Adjunta este script al GameObject Player Test y ejecuta el juego.
/// Los valores aparecerán en la consola.
/// </summary>
public class GetAnimatorHashes : MonoBehaviour
{
    [SerializeField] private Animator animator;

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            Debug.LogError("No se encontró Animator!");
            return;
        }

        Debug.Log("========== ANIMATOR PARAMETER HASHES ==========");
        
        // State Hashes (Bools, Floats, Ints)
        Debug.Log("--- STATE HASHES (para Network Mecanim Animator) ---");
        Debug.Log($"isRun = {Animator.StringToHash("isRun")}");
        Debug.Log($"isJump = {Animator.StringToHash("isJump")}");
        Debug.Log($"isCrounch = {Animator.StringToHash("isCrounch")}");
        
        // Trigger Hashes
        Debug.Log("--- TRIGGER HASHES (para Network Mecanim Animator) ---");
        Debug.Log($"Attack_Bow = {Animator.StringToHash("Attack_Bow")}");
        Debug.Log($"Attack_Sword = {Animator.StringToHash("Attack_Sword")}");
        Debug.Log($"Attack_Spear = {Animator.StringToHash("Attack_Spear")}");
        
        Debug.Log("===============================================");
        Debug.Log("Copia estos números en el Network Mecanim Animator del prefab Player.");
        
        // Después de mostrar los hashes, destruir este componente
        Destroy(this);
    }
}
