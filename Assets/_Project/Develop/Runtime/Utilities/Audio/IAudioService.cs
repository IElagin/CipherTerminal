namespace Assets._Project.Develop.Runtime.Utilities.Audio
{
    public interface IAudioService
    {
        public void Initialize(AudioCatalog catalog);

        public void Play(AudioCue cue);
    }
}
