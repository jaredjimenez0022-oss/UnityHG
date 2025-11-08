using Fusion;
using UnityEngine;

public class Arrow : NetworkBehaviour
{
    [SerializeField] private float velocity;
    private int arrowDamage;
    private string targetTodamage = "Enemy";
    private GameObject targetObject;
    /*Apenas empieza y para que no vea muchos objeto en escena lo borramos despues de 5 segundos, ya que se desplazara
     *casi infinitamente o hasta que choque con un enemigo
     */
    private void Start()
    {
        Invoke("DeleteArrow", 5f);        
    }

    private void Update()
    {
        MoveArrow();
    }

    public void DeleteArrow()
    {
        Destroy(targetObject);
        Destroy(gameObject);
    }
    /*Inicializamos la flecha asignandole un nuevo daño y un objetivo*/
    public void InitArrow(int damage, string target, GameObject targetObject)
    {
        arrowDamage = damage;
        targetTodamage = target;
        this.targetObject = targetObject;
    }
    /*Desplaza la flecha segun su direccion dada al ser creado*/
    public void MoveArrow()
    {
        Vector3 direction = targetObject.transform.position - transform.position;
        direction.Normalize();
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 0.01f);
        transform.position = Vector3.MoveTowards(transform.position, targetObject.transform.position, velocity * Time.deltaTime);
    }
    /*Detectamos al enemigo mediante un tag="Enemy", asu vez para evitar errores corroboramos que tenga el script Health
     *para quitarle vida
     */
    public void OnTriggerEnter(Collider other)
    {
        //if (other.CompareTag(targetTodamage))
        //{
        //    Health healthTarget = other.GetComponent<Health>();
        //    if (healthTarget != null)
        //    {
        //        healthTarget.DecrementHealth(arrowDamage);
        //        Debug.Log("Damage: " + arrowDamage + " with arrow");
        //        Destroy(gameObject);
        //    }
        //}
        if(other.gameObject != gameObject)
        {
            PlayerHealth playerHealth = other.gameObject.GetComponent<PlayerHealth>();
            if(playerHealth != null)
            {
                playerHealth.TakeDamage(arrowDamage);
            }
        }
    }
}