using System;
using UnityEngine;
using Assets._Project.Develop.Runtime.Gameplay.Sequence;

namespace Assets._Project.Develop.Runtime.Gameplay.Configs
{
    [CreateAssetMenu(menuName = "Cipher Terminal/Sequence Config")]
    public sealed class SequenceConfig : ScriptableObject
    {
        [SerializeField, Range(SequenceGenerator.MinimumLength, SequenceGenerator.MaximumLength)]
        private int _length = 6;
        [SerializeField] private ModeSymbols[] _modes =
        {
            new ModeSymbols(SequenceMode.Digits, "0123456789"),
            new ModeSymbols(SequenceMode.Letters, "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
        };

        public int Length => _length;

        public string GetSymbols(SequenceMode mode)
        {
            if (_length < SequenceGenerator.MinimumLength || _length > SequenceGenerator.MaximumLength)
                throw new InvalidOperationException("Sequence length must be " +
                    SequenceGenerator.MinimumLength + "–" + SequenceGenerator.MaximumLength);

            foreach (ModeSymbols entry in _modes)
            {
                if (entry.Mode == mode && string.IsNullOrWhiteSpace(entry.Symbols) == false)
                    return entry.Symbols;
            }

            throw new InvalidOperationException("Missing symbol set for " + mode);
        }
    }
}
