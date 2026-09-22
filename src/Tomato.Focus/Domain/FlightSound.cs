using System;

namespace Tomato
{
    public enum FlightSoundKind
    {
        Launch,
        Impact
    }

    public sealed class FlightSoundEventArgs : EventArgs
    {
        public FlightSoundKind Kind { get; private set; }
        public double Pan { get; private set; }
        public double Size { get; private set; }
        public double Speed { get; private set; }
        public bool Settling { get; private set; }

        public FlightSoundEventArgs(FlightSoundKind kind, double x, double width, double size, double speed, bool settling)
        {
            Kind = kind;
            Pan = Math.Max(-.8, Math.Min(.8, (x / Math.Max(1, width) * 2 - 1) * .8));
            Size = size;
            Speed = speed;
            Settling = settling;
        }
    }
}
