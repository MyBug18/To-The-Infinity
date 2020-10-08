namespace Core
{
    public interface IOnHexTileObject : IModifierHolder, ISpecialActionHolder
    {
        string Owner { get; }

        string IdentifierName { get; }

        string CustomName { get; }

        HexTile CurrentTile { get; }

        void StartNewTurn(int month);
    }
}
