using Assets._Project.Develop.Runtime.Gameplay.Sequence;

namespace Assets._Project.Develop.Runtime.Gameplay
{
    public enum GameplayNavigationDestination
    {
        MainMenu,
        Retry
    }

    public sealed class GameplayNavigationRequest
    {
        public GameplayNavigationDestination Destination { get; }
        public int LevelNumber { get; }
        public SequenceMode Mode { get; }

        public GameplayNavigationRequest(
            GameplayNavigationDestination destination,
            int levelNumber,
            SequenceMode mode)
        {
            Destination = destination;
            LevelNumber = levelNumber;
            Mode = mode;
        }
    }
}
