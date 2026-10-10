namespace Assets._Project.Develop.Runtime.Utilities.Timer
{
    public class TimerServiceFactory
    {
        public TimerService Create(float cooldown) => new TimerService(cooldown);
    }
}
