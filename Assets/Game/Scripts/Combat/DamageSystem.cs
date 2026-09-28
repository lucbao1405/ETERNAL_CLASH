using UnityEngine;
using EternalClash.Character;

namespace EternalClash.Combat
{
    public static class DamageSystem
    {
        public static float CalculateDamage(CharacterStats attacker, CharacterStats defender)
        {
            float damage = attacker.attack - defender.defense;

            if (damage < 1)
                damage = 1;

            if (Random.value <= attacker.criticalRate)
            {
                damage *= 2f;
            }

            return damage;
        }
    }
}
