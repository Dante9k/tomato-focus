using System;

namespace Tomato
{
    public static class WheelMotion
    {
        // Exact damped-spring integration keeps the detent feel consistent across frame rates.
        public static void Advance(ref double position, ref double velocity, double target, double seconds)
        {
            const double frequency = 23, damping = .78;
            double decay = frequency * damping;
            double angular = frequency * Math.Sqrt(1 - damping * damping);
            double offset = position - target;
            double envelope = Math.Exp(-decay * seconds);
            double cosine = Math.Cos(angular * seconds), sine = Math.Sin(angular * seconds);
            position = target + envelope * (offset * cosine + (velocity + decay * offset) / angular * sine);
            velocity = envelope * (velocity * cosine - (decay * velocity + frequency * frequency * offset) / angular * sine);
        }
    }
}
