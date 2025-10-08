using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem; // Para el nuevo sistema de Input

public class WeaponSystem : MonoBehaviour
{
    [Header("Armas en el inventario (prefabs o hijos en la escena)")]
    public List<GameObject> weapons; // Espada, lanza, arco
    private int currentWeapon = 0;

    void Start()
    {
        if (weapons == null || weapons.Count == 0)
        {
            Debug.LogError("⚠️ No hay armas asignadas en WeaponSystem.");
            return;
        }

        EquipWeapon(currentWeapon);
    }

    void Update()
    {
        if (Mouse.current == null) return; // Seguridad

        float scroll = Mouse.current.scroll.ReadValue().y;

        if (scroll > 0f)
        {
            currentWeapon = (currentWeapon + 1) % weapons.Count;
            EquipWeapon(currentWeapon);
        }
        else if (scroll < 0f)
        {
            currentWeapon = (currentWeapon - 1 + weapons.Count) % weapons.Count;
            EquipWeapon(currentWeapon);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Attack();
        }
    }

    void EquipWeapon(int index)
    {
        for (int i = 0; i < weapons.Count; i++)
            weapons[i].SetActive(i == index);

        Debug.Log("🔀 Arma equipada: " + weapons[currentWeapon].name);
    }

    void Attack()
    {
        IWeapon weapon = weapons[currentWeapon].GetComponent<IWeapon>();
        if (weapon != null)
        {
            weapon.Use();
        }
        else
        {
            Debug.LogWarning("⚠️ El arma equipada no tiene script que implemente IWeapon.");
        }
    }
}
