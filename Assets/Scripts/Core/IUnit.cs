using System.Collections.Generic;

namespace Core
{
    public interface IUnit : IOnHexTileObject
    {
        int BaseAttackPower { get; }

        IReadOnlyCollection<string> Properties { get; }

        void OnDamaged(DamageInfo damageInfo);

        void Move(HexTileCoord coord);
    }
}
