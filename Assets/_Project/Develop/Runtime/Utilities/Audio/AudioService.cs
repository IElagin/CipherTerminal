using System;
using UnityEngine;

namespace Assets._Project.Develop.Runtime.Utilities.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        private const float FullVolume = 1f;

        [SerializeField] private AudioSource _ambientSource;
        [SerializeField] private AudioSource _effectsSource;

        private AudioCatalog _catalog;
        private bool _initialized;

        public void Initialize(AudioCatalog catalog)
        {
            if (_initialized)
                return;

            if (_ambientSource == null)
                throw new InvalidOperationException("AudioService is missing " + nameof(_ambientSource));

            if (_effectsSource == null)
                throw new InvalidOperationException("AudioService is missing " + nameof(_effectsSource));

            catalog.Validate();
            _catalog = catalog;
            _initialized = true;

            _ambientSource.spatialBlend = 0f;
            _ambientSource.loop = true;
            _ambientSource.clip = _catalog.AmbientClip;
            _ambientSource.volume = _catalog.AmbientVolume;

            _effectsSource.spatialBlend = 0f;
            _effectsSource.loop = false;
            _effectsSource.volume = FullVolume;

            _ambientSource.Play();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void Play(AudioCue cue)
        {
            if (_initialized == false)
                throw new InvalidOperationException("AudioService must be initialized before playback");

            switch (cue)
            {
                case AudioCue.Key:
                    int index = UnityEngine.Random.Range(0, _catalog.KeyClipCount);
                    _effectsSource.PlayOneShot(_catalog.GetKeyClip(index), _catalog.KeyVolume);
                    break;

                case AudioCue.Error:
                    _effectsSource.PlayOneShot(_catalog.ErrorClip, _catalog.ErrorVolume);
                    break;

                case AudioCue.Success:
                    _effectsSource.PlayOneShot(_catalog.SuccessClip, _catalog.SuccessVolume);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(cue), cue, null);
            }
        }
    }
}
