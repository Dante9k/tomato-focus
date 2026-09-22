namespace Tomato
{
    public sealed class Preferences
    {
        public int Seconds = 1500;
        public long DeadlineTicks;
        public bool Active;
        public bool Sound = true;
        public double Left = double.NaN;
        public double Top = double.NaN;
    }
}
