namespace Assets._Project.Develop.Runtime.Utilities.LoadingScreen
{
    public interface ILoadingScreen
    {
        bool IsShown { get; }
        void Show();
        void Hide();
    }
}
