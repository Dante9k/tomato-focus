using System;

namespace Tomato
{
    public sealed class Flight
    {
        public double X, Y, Vx, Vy, Angle, Spin, Size, Age;
        public int Bounces;
        public bool Step(double dt, double width, double height)
        {
            Age += dt;
            X += Vx * dt;
            Y += Vy * dt + 0.5 * 920 * dt * dt;
            Vy += 920 * dt;
            Angle += Spin * dt;
            double radius = Size * .39;
            if (Y + radius >= height - 8 && Vy > 0 && Bounces == 0)
            {
                Y = height - 8 - radius;
                Vy = -Vy * .48;
                Vx *= .77;
                Spin *= .72;
                Bounces++;
            }

            return Age < 6 && X > -Size * 2 && X < width + Size * 2 && Y < height + Size * 2;
        }
    }
}
