namespace Assets._Project.Develop.Runtime.Utilities.DataManagment.KeysStorage
{
    public interface IDataKeysStorage
    {
        public string GetKeyFor<TData>() where TData : class, ISaveData;
    }
}
