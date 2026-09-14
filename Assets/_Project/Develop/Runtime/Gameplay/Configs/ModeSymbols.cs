using System;
using UnityEngine;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;

namespace Assets._Project.Develop.Runtime.Gameplay.Configs
{
    [Serializable]
    public sealed class ModeSymbols
    {
        [SerializeField] private SequenceMode _mode;
        [SerializeField] private string _symbols;

        public SequenceMode Mode => _mode;
        public string Symbols => _symbols;

        public ModeSymbols(SequenceMode mode, string symbols)
        {
            _mode = mode;
            _symbols = symbols;
        }
    }
}
