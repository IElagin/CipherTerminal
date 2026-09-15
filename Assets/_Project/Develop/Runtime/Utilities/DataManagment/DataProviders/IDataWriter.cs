namespace Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders
{
    public interface IDataWriter<TData> where TData : class, ISaveData
    {
        void WriteTo(TData data);
    }
}
