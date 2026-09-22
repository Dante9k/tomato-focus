using System;
using System.Collections.Generic;

namespace Tomato
{
    // Coordinates are screen DIPs; time is monotonic seconds supplied by the caller.
    public sealed class ShakeDetector
    {
        readonly Queue<double> turns = new Queue<double>();
        double startX, startY, extreme;
        int axis, direction;
        bool triggered;
        public void Reset(double x, double y)
        {
            startX = x;
            startY = y;
            axis = direction = 0;
            triggered = false;
            turns.Clear();
        }

        public bool Move(double x, double y, double seconds)
        {
            if (triggered)
                return false;
            if (axis == 0)
            {
                double dx = x - startX, dy = y - startY;
                if (Math.Max(Math.Abs(dx), Math.Abs(dy)) < 20)
                    return false;
                axis = Math.Abs(dx) >= Math.Abs(dy) ? 1 : 2;
                direction = Math.Sign(axis == 1 ? dx : dy);
                extreme = axis == 1 ? x : y;
                return false;
            }

            double value = axis == 1 ? x : y;
            if ((value - extreme) * direction >= 0)
                extreme = value;
            else if (Math.Abs(value - extreme) >= 20)
            {
                direction = -direction;
                extreme = value;
                while (turns.Count > 0 && seconds - turns.Peek() > 1)
                    turns.Dequeue();
                turns.Enqueue(seconds);
                if (turns.Count >= 3)
                {
                    triggered = true;
                    return true;
                }
            }

            return false;
        }
    }
}
