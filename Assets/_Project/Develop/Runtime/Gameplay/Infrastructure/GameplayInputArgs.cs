using Assets._Project.Develop.Runtime.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;

namespace Assets._Project.Develop.Runtime.Gameplay.Infrastructure
{
    public class GameplayInputArgs : IInputSceneArgs
    {
        public int LevelNumber { get; }
        public SequenceMode Mode { get; }

        public GameplayInputArgs(int levelNumber, SequenceMode mode = SequenceMode.Digits)
        {
            LevelNumber = levelNumber;
            Mode = mode;
        }
    }
}
