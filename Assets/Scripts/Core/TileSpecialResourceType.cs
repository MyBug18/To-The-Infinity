namespace Core
{
    public class TileSpecialResourceType
    {
        public readonly string Name;

        public readonly int MoveCost;

        public TileSpecialResourceType(string name, int moveCost)
        {
            Name = name;
            MoveCost = moveCost;
        }
    }
}