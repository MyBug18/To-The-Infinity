namespace Core
{
    public interface IUnit : IOnHexTileObject
    {
        void OnMeleeAttacked(IUnit unit, int damage);
    }
}