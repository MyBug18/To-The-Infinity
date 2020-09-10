namespace Core
{
    public class TileSpecialResourceType
    {
        public string Name { get; }

        public int MoveCost { get; }

        public TileSpecialResourceType(string name, int moveCost)
        {
            Name = name;
            MoveCost = moveCost;
        }
    }
}
