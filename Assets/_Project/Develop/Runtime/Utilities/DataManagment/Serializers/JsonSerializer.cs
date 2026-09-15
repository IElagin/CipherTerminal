using Newtonsoft.Json;

namespace Assets._Project.Develop.Runtime.Utilities.DataManagment.Serializers
{
    public sealed class JsonSerializer : IDataSerializer
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            MissingMemberHandling = MissingMemberHandling.Error,
            TypeNameHandling = TypeNameHandling.None
        };

        public string Serialize<TData>(TData data)
            => JsonConvert.SerializeObject(data, Settings);

        public TData Deserialize<TData>(string serializedData)
            => JsonConvert.DeserializeObject<TData>(serializedData, Settings);
    }
}
