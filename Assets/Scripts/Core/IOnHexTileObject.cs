namespace Core
{
    public interface IOnHexTileObject : IModifierHolder, ISpecialActionHolder
    {
        string Owner { get; }

        string Name { get; }

        HexTile CurrentTile { get; }

        void StartNewTurn(int month);
    }
}
