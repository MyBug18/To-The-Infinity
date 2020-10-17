namespace Core
{
    public readonly struct MoveInfo
    {
        public int CurrentCost { get; }

        public HexTileCoord FromCoord { get; }

        public MoveInfo(int currentCost, HexTileCoord fromCoord)
        {
            CurrentCost = currentCost;
            FromCoord = fromCoord;
        }
    }
}
