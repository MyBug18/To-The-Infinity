using System.Collections.Generic;

namespace Core
{
    public enum DamageType
    {
        Error, // Should be only used to indicate an error.
        Mass,
        Beam,
        Magic,
    }

    public interface IUnit : IOnHexTileObject
    {
        int MeleeAttackPower { get; }

        /// <summary>
        ///     Key is property of opponent unit
        /// </summary>
        IReadOnlyDictionary<string, int> MeleeAttackPowerBonus { get; }

        IReadOnlyCollection<string> Properties { get; }

        void OnDamaged(IInfinityObject inflicter, int damage, DamageType damageType, bool isMelee);

        void Move(HexTileCoord coord);
    }
}
