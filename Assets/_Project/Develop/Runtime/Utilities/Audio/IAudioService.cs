namespace Assets._Project.Develop.Runtime.Utilities.Audio
{
    public interface IAudioService
    {
        void Initialize(AudioCatalog catalog);
        void Play(AudioCue cue);
    }
}
