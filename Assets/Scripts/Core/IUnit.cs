using System.Collections.Generic;

namespace Core
{
    public interface IUnit : IOnHexTileObject
    {
        int MeleeAttackPower { get; }

        /// <summary>
        /// Key is property of opponent unit
        /// </summary>
        IReadOnlyDictionary<string, int> MeleeAttackPowerBonus { get; }

        IReadOnlyCollection<string> Properties { get; }

        void OnMeleeAttacked(IUnit unit, int damage);
    }
}