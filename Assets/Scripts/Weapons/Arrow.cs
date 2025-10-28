using Fusion;
using UnityEngine;

public class Arrow : NetworkBehaviour
{
    [SerializeField] private float velocity;
    [Networked] private int ArrowDamage { get; set; }
    private GameObject targetObject;

    private int initialDamage;

    private void Start()
    {
        ArrowDamage = initialDamage;
        Invoke(nameof(DeleteArrow), 5f);        
    }

    public override void FixedUpdateNetwork()
    {
        if (targetObject != null)
        {
            MoveArrow();
        }
    }

    public void DeleteArrow()
    {
        if (targetObject != null)
            Destroy(targetObject);
        
        if (Object != null && Object.IsValid)
            Runner.Despawn(Object);
    }
    /*Inicializamos la flecha asignandole un nuevo daño y un objetivo*/
    public void InitArrow(int damage, GameObject targetObject)
    {
        initialDamage = damage;
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
        if (other.gameObject == gameObject) return;

        PlayerCombat playerCombat = other.gameObject.GetComponent<PlayerCombat>();
        if (playerCombat != null && playerCombat.IsAlive())
        {
            playerCombat.ApplyDamage(ArrowDamage, Fusion.PlayerRef.None);
            Debug.Log($"Fecha aplicó {ArrowDamage} de daño a {other.gameObject.name}");
            DeleteArrow();
        }
    }
}