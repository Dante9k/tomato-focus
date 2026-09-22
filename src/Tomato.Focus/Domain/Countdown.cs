using System;

namespace Tomato
{
    public enum TimerPhase
    {
        Editing,
        Running,
        Ringing
    }

    public sealed class Countdown
    {
        public TimerPhase Phase { get; private set; }
        public DateTime DeadlineUtc { get; private set; }
        public int DurationSeconds { get; private set; }

        public Countdown()
        {
            DurationSeconds = 1500;
        }

        public void Start(int seconds, DateTime now)
        {
            if (seconds < 1 || seconds > 86399)
                throw new ArgumentOutOfRangeException("seconds");
            DurationSeconds = seconds;
            DeadlineUtc = now.AddSeconds(seconds);
            Phase = TimerPhase.Running;
        }

        public bool Tick(DateTime now)
        {
            if (Phase != TimerPhase.Running || now < DeadlineUtc)
                return false;
            Phase = TimerPhase.Ringing;
            return true;
        }

        public int Remaining(DateTime now)
        {
            return Phase == TimerPhase.Running ? (int)Math.Max(0, Math.Ceiling((DeadlineUtc - now).TotalSeconds)) : 0;
        }

        public void Restore(int seconds, DateTime deadline, DateTime now)
        {
            DurationSeconds = Math.Max(1, Math.Min(86399, seconds));
            DeadlineUtc = deadline;
            Phase = now >= deadline ? TimerPhase.Ringing : TimerPhase.Running;
        }

        public void Stop()
        {
            Phase = TimerPhase.Editing;
        }
    }
}
