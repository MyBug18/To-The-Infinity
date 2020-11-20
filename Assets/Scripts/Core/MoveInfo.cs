namespace Core
{
    public readonly struct MoveInfo
    {
        public HexTileCoord FromCoord { get; }

        public int CostUntilHere { get; }

        public MoveInfo(HexTileCoord fromCoord, int costUntilHere)
        {
            FromCoord = fromCoord;
            CostUntilHere = costUntilHere;
        }
    }
}
