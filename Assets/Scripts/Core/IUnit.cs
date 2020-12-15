using System.Collections.Generic;

namespace Core
{
    public interface IUnit : IOnHexTileObject, IResourceStorageHolder
    {
        string AttackDamageType { get; }

        int BaseAttackPower { get; }

        int AttackPower { get; }

        int BaseMaxHp { get; }

        int MaxHp { get; }

        int RemainHp { get; }

        int BaseMaxMovePoint { get; }

        int MaxMovePoint { get; }

        int RemainMovePoint { get; }

        IReadOnlyCollection<string> Properties { get; }

        void OnDamaged(DamageInfo damageInfo);

        void Move(HexTileCoord coord);
    }
}
