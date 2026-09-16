using System;
using Assets._Project.Develop.Runtime.Infrastructure;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;

namespace Assets._Project.Develop.Runtime.Gameplay.Infrastructure
{
    public class GameplayInputArgs : IInputSceneArgs
    {
        public GameplayInputArgs(int levelNumber, SequenceMode mode = SequenceMode.Digits)
        {
            LevelNumber = levelNumber;
            Mode = mode;
        }

        public int LevelNumber { get; }
        public SequenceMode Mode { get; }

        public void Validate(string parameterName)
        {
            if (LevelNumber <= 0)
                throw new ArgumentException("Gameplay level number must be positive", parameterName);

            if (Enum.IsDefined(typeof(SequenceMode), Mode) == false)
                throw new ArgumentException("Gameplay mode is invalid", parameterName);
        }
    }
}
