using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Utilities.Audio
{
    [CreateAssetMenu(menuName = "Cipher Terminal/Audio Catalog")]
    public sealed class AudioCatalog : ScriptableObject
    {
        [SerializeField] private AudioClip _ambientClip;
        [SerializeField] private AudioClip _keyClip1;
        [SerializeField] private AudioClip _keyClip2;
        [SerializeField] private AudioClip _keyClip3;
        [SerializeField] private AudioClip _errorClip;
        [SerializeField] private AudioClip _successClip;
        [SerializeField, Range(0f, 1f)] private float _ambientVolume = .22f;
        [SerializeField, Range(0f, 1f)] private float _keyVolume = .65f;
        [SerializeField, Range(0f, 1f)] private float _errorVolume = .55f;
        [SerializeField, Range(0f, 1f)] private float _successVolume = .55f;

        public AudioClip AmbientClip => _ambientClip;
        public AudioClip ErrorClip => _errorClip;
        public AudioClip SuccessClip => _successClip;
        public float AmbientVolume => _ambientVolume;
        public float KeyVolume => _keyVolume;
        public float ErrorVolume => _errorVolume;
        public float SuccessVolume => _successVolume;
        public int KeyClipCount => 3;

        public AudioClip GetKeyClip(int index)
        {
            return index switch
            {
                0 => _keyClip1,
                1 => _keyClip2,
                2 => _keyClip3,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }

        public void Validate()
        {
            Require(_ambientClip, nameof(_ambientClip));
            Require(_keyClip1, nameof(_keyClip1));
            Require(_keyClip2, nameof(_keyClip2));
            Require(_keyClip3, nameof(_keyClip3));
            Require(_errorClip, nameof(_errorClip));
            Require(_successClip, nameof(_successClip));
        }

        private static void Require(AudioClip clip, string field)
        {
            if (clip == null)
                throw new InvalidOperationException("AudioCatalog is missing required clip " + field);
        }
    }
}
