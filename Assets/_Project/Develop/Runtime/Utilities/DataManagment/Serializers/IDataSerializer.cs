namespace Assets._Project.Develop.Runtime.Utilities.DataManagment.Serializers
{
    public interface IDataSerializer
    {
        public string Serialize<TData>(TData data);

        public TData Deserialize<TData>(string serializedData);
    }
}
