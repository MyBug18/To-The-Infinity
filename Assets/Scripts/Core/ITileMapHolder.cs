namespace Core
{
    public interface ITileMapHolder : IModifierHolder
    {
        TileMap TileMap { get; }
    }
}