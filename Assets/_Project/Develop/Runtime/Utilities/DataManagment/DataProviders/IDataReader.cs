namespace Assets._Project.Develop.Runtime.Utilities.DataManagment.DataProviders
{
    public interface IDataReader<TData> where TData : class, ISaveData
    {
        public void ReadFrom(TData data);
    }
}
