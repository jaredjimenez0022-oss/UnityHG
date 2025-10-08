using UnityEngine;

public class PlayerShield : MonoBehaviour
{
    [Header("Atributos de Escudo")]
    [SerializeField] private int maxShield = 50;
    [SerializeField] private int currentShield;

    void Start()
    {
        currentShield = maxShield;
    }


    public int AbsorbDamage(int damageAmount)
    {
        if (currentShield > 0)
        {
            int damageAbsorbed = Mathf.Min(damageAmount, currentShield);
            currentShield -= damageAbsorbed;

            Debug.Log(gameObject.name + " absorbió " + damageAbsorbed + " de daño. Escudo actual: " + currentShield);

            //La idea aquí es retornar el daño sobrante si el escudo no alcanzó
            //Se lo pasaríamos a PlayerHealth para que quite ese sobrante a la vida
            return damageAmount - damageAbsorbed;
        }

        //Si no hay escudo todo el daño pasa a la vida
        return damageAmount;
    }

    public void RestoreShield(int shieldAmount)
    {
        currentShield += shieldAmount;

        if (currentShield > maxShield)
            currentShield = maxShield;

        Debug.Log(gameObject.name + " recuperó " + shieldAmount + " de escudo. Escudo actual: " + currentShield);
    }

    public int GetCurrentShield()
    {
        return currentShield;
    }

}
