using Newtonsoft.Json;

namespace Core
{
    public sealed class InfinityObjectData
    {
        public InfinityObjectData(string guid, string typeName, object data)
        {
            Guid = guid;
            TypeName = typeName;
            Data = JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public string Guid { get; }

        public string TypeName { get; }

        public string Data { get; }
    }
}
