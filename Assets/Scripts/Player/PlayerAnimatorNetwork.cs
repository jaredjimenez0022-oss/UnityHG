using UnityEngine;

public class PlayerAnimatorNetwork : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Transform objectAnim;
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

    public void SetIsRun(bool isRun)
    {
        if (hashIsRun != 0)
        {
            animator.SetBool(hashIsRun, isRun);
        }
    }

    public void SetIsJump(bool isJump)
    {
        if (hashIsJump != 0)
        {
            animator.SetBool(hashIsJump, isJump);
        }
    }
    public void SetIsCrounch(bool isCrounch)
    {
        if (hashIsCrounch != 0)
        {
            animator.SetBool(hashIsCrounch, isCrounch);
        }
    }
    public void SetAttackBow()
    {
        if (hashAttackBow != 0)
        {
            animator.SetTrigger(hashAttackBow);
        }
    }
    public void SetAttackSword()
    {
        if (hashAttackSword != 0)
        {
            animator.SetTrigger(hashAttackSword);
        }
    }
    public void SetAttackSpeaer()
    {
        if (hashAttackSpear != 0)
        {
            animator.SetTrigger(hashAttackSpear);
        }
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
