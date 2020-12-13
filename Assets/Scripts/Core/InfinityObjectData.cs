using Newtonsoft.Json;

namespace Core
{
    public sealed class InfinityObjectData
    {
        public InfinityObjectData(int id, string typeName, object data)
        {
            Id = id;
            TypeName = typeName;
            Data = JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public int Id { get; }

        public string TypeName { get; }

        public string Data { get; }
    }
}
