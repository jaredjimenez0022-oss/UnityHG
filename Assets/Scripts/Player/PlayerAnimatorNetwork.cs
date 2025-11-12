using UnityEngine;
using Fusion;

public class PlayerAnimatorNetwork : NetworkBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform objectAnim;
    
    // Variables sincronizadas por red
    [Networked] private NetworkBool isRun { get; set; }
    [Networked] private NetworkBool isJump { get; set; }
    [Networked] private NetworkBool isCrounch { get; set; }
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
        // Actualizar el Animator con los valores sincronizados
        if (animator != null)
        {
            animator.SetBool(hashIsRun, isRun);
            animator.SetBool(hashIsJump, isJump);
            animator.SetBool(hashIsCrounch, isCrounch);
            
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
    }

    public void SetIsRun(bool value)
    {
        isRun = value;
    }

    public void SetIsJump(bool value)
    {
        isJump = value;
    }
    
    public void SetIsCrounch(bool value)
    {
        isCrounch = value;
    }
    
    public void SetAttackBow()
    {
        attackBowTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
    }
    
    public void SetAttackSword()
    {
        attackSwordTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
    }
    
    public void SetAttackSpeaer()
    {
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
