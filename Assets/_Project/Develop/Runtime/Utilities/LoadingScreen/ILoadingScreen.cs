namespace Assets._Project.Develop.Runtime.Utilities.LoadingScreen
{
    public interface ILoadingScreen
    {
        public bool IsShown { get; }

        public void Show();

        public void Hide();
    }
}
