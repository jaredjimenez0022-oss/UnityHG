using UnityEngine;
using Fusion;

public class PlayerAnimatorNetwork : NetworkBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform objectAnim;
    
    // NetworkMecanimAnimator maneja la sincronización automática del Animator
    private NetworkMecanimAnimator networkAnimator;
    
    // Variables sincronizadas por red para triggers de ataque (los bools ya están en el Animator)
    [Networked] private TickTimer attackBowTimer { get; set; }
    [Networked] private TickTimer attackSwordTimer { get; set; }
    [Networked] private TickTimer attackSpearTimer { get; set; }
    
    //Hashs animations
    private int hashIsRun;
    private int hashIsJump;
    private int hashIsCrounch;
    private int hashAttackBow;
    private int hashAttackSword;
    private int hashAttackSpear;
    
    private void Awake()
    {
        // Obtener o añadir NetworkMecanimAnimator
        networkAnimator = GetComponent<NetworkMecanimAnimator>();
        if (networkAnimator == null)
        {
            Debug.LogWarning("[PlayerAnimatorNetwork] NetworkMecanimAnimator no encontrado. Las animaciones no se sincronizarán correctamente.");
        }
    }
    
    void Start()
    {
        hashIsRun = Animator.StringToHash("isRun");
        hashIsJump = Animator.StringToHash("isJump");
        hashIsCrounch = Animator.StringToHash("isCrounch");
        hashAttackBow = Animator.StringToHash("Attack_Bow");
        hashAttackSword = Animator.StringToHash("Attack_Sword");
        hashAttackSpear = Animator.StringToHash("Attack_Spear");
    }

    public override void FixedUpdateNetwork()
    {
        if (animator == null)
            return;
            
        // Los bools del Animator ahora se sincronizan automáticamente via NetworkMecanimAnimator
        // Solo necesitamos manejar los triggers de ataque
        
        // Ejecutar triggers de ataque si están activos
        if (attackBowTimer.IsRunning)
        {
            animator.SetTrigger(hashAttackBow);
            attackBowTimer = TickTimer.None;
        }
        
        if (attackSwordTimer.IsRunning)
        {
            animator.SetTrigger(hashAttackSword);
            attackSwordTimer = TickTimer.None;
        }
        
        if (attackSpearTimer.IsRunning)
        {
            animator.SetTrigger(hashAttackSpear);
            attackSpearTimer = TickTimer.None;
        }
    }

    public void SetIsRun(bool value)
    {
        // Solo modificar si tenemos autoridad de estado
        if (Object == null || !Object.HasStateAuthority)
            return;
        
        // Ahora usamos directamente el Animator, NetworkMecanimAnimator lo sincroniza
        if (animator != null)
        {
            animator.SetBool(hashIsRun, value);
        }
    }

    public void SetIsJump(bool value)
    {
        // Solo modificar si tenemos autoridad de estado
        if (Object == null || !Object.HasStateAuthority)
            return;
        
        if (animator != null)
        {
            animator.SetBool(hashIsJump, value);
        }
    }
    
    public void SetIsCrounch(bool value)
    {
        // Solo modificar si tenemos autoridad de estado
        if (Object == null || !Object.HasStateAuthority)
            return;
        
        if (animator != null)
        {
            animator.SetBool(hashIsCrounch, value);
        }
    }
    
    public void SetAttackBow()
    {
        // Solo modificar si tenemos autoridad de estado
        if (Object == null || !Object.HasStateAuthority)
            return;
            
        attackBowTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
    }
    
    public void SetAttackSword()
    {
        // Solo modificar si tenemos autoridad de estado
        if (Object == null || !Object.HasStateAuthority)
            return;
            
        attackSwordTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
    }
    
    public void SetAttackSpeaer()
    {
        // Solo modificar si tenemos autoridad de estado
        if (Object == null || !Object.HasStateAuthority)
            return;
            
        attackSpearTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
    }

    public void ResetAnim()
    {
        if (objectAnim)
        {
            objectAnim.localPosition = Vector3.zero;
            objectAnim.rotation = Quaternion.Euler(0, 0, 0);
            Debug.Log("RESET ANIMATION");
        }
    }
}
