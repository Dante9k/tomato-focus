using System;

namespace Tomato
{
    public sealed class Flight
    {
        public double X, Y, Vx, Vy, Angle, Spin, Size, Age;
        public int Bounces;
        public bool Impacted { get; private set; }
        public double ImpactSpeed { get; private set; }
        public bool Resting { get; private set; }
        public double RestAge { get; private set; }

        public bool Step(double dt, double width, double height)
        {
            Impacted = false;
            Age += dt;
            if (Resting)
            {
                RestAge += dt;
                return RestAge < .32 && Age < 6;
            }

            X += Vx * dt;
            Y += Vy * dt + 0.5 * 920 * dt * dt;
            Vy += 920 * dt;
            Angle += Spin * dt;
            double radius = Size * .39;
            if (Y + radius >= height - 8 && Vy > 0)
            {
                Y = height - 8 - radius;
                Impacted = true;
                ImpactSpeed = Vy;
                if (Bounces == 0)
                {
                    Vy = -Vy * .48;
                    Vx *= .77;
                    Spin *= .72;
                    Bounces++;
                }
                else
                {
                    Resting = true;
                    Vx = Vy = Spin = 0;
                }
            }

            return Age < 6 && X > -Size * 2 && X < width + Size * 2 && Y < height + Size * 2;
        }
    }
}
