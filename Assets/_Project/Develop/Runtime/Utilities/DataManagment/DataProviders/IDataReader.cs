namespace Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders
{
    public interface IDataReader<TData> where TData : class, ISaveData
    {
        void ReadFrom(TData data);
    }
}
