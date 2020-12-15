using System.Collections.Generic;
using Newtonsoft.Json;

namespace Core
{
    public sealed class InfinityObjectData
    {
        public InfinityObjectData(string typeName, object data)
        {
            TypeName = typeName;
            Data = JsonConvert.SerializeObject(data, Formatting.Indented);
        }

        public string TypeName { get; }

        public string Data { get; }

        public Dictionary<string, object> Dict => JsonConvert.DeserializeObject<Dictionary<string, object>>(Data);
    }
}
