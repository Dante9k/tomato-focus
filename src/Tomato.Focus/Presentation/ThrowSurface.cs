using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Tomato
{
    public sealed class ThrowSurface : FrameworkElement
    {
        readonly DrawingVisual visual = new DrawingVisual();
        readonly List<Flight> flights = new List<Flight>(40);
        readonly Dictionary<Flight, Sprite> sprites = new Dictionary<Flight, Sprite>();
        readonly Random random = new Random();
        readonly BitmapSource texture;
        readonly Stopwatch watch = new Stopwatch();
        double previous, spawn;
        public Point Origin;
        public Func<Point> OriginProvider { get; set; }
        public int Frames { get; private set; }
        public int PeakParticles { get; private set; }

        public event EventHandler<FlightSoundEventArgs> SoundCue;
        public int LaunchCues { get; private set; }
        public int ImpactCues { get; private set; }

        public int RenderTier
        {
            get
            {
                return RenderCapability.Tier >> 16;
            }
        }

        public double MaxFrameMs { get; private set; }
        public double UpdateMs { get; private set; }

        public double MeanFrameMs
        {
            get
            {
                return Frames == 0 ? 0 : watch.Elapsed.TotalMilliseconds / Frames;
            }
        }

        public ThrowSurface(BitmapSource texture)
        {
            this.texture = texture;
            AddVisualChild(visual);
            IsHitTestVisible = false;
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.LowQuality);
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return 1;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0)
                throw new ArgumentOutOfRangeException("index");
            return visual;
        }

        public void Begin()
        {
            watch.Restart();
            previous = 0;
            spawn = .25;
            CompositionTarget.Rendering += Frame;
        }

        public void End()
        {
            CompositionTarget.Rendering -= Frame;
            watch.Stop();
            OriginProvider = null;
            flights.Clear();
            sprites.Clear();
            visual.Children.Clear();
        }

        void Frame(object sender, EventArgs e)
        {
            double now = watch.Elapsed.TotalSeconds;
            double dt = now - previous;
            if (dt < .001)
                return;
            previous = now;
            // Cap catch-up after suspend and advance with bounded physics substeps.
            if (now > 1)
                MaxFrameMs = Math.Max(MaxFrameMs, dt * 1000);
            long work = Stopwatch.GetTimestamp();
            Advance(Math.Min(.08, dt), ActualWidth, ActualHeight);
            Draw();
            Frames++;
            UpdateMs += (Stopwatch.GetTimestamp() - work) * 1000.0 / Stopwatch.Frequency;
        }

        public void Advance(double dt, double width, double height)
        {
            spawn += dt;
            if (spawn >= .21 && flights.Count < 36)
            {
                spawn = 0;
                Launch(width, height);
            }

            int steps = Math.Max(1, (int)Math.Ceiling(dt / .016));
            for (int s = 0; s < steps; s++)
                for (int i = flights.Count - 1; i >= 0; i--)
                {
                    var flight = flights[i];
                    bool alive = flight.Step(dt / steps, width, height);
                    if (flight.Impacted && alive && flight.X >= 0 && flight.X <= width)
                        EmitSound(new FlightSoundEventArgs(FlightSoundKind.Impact, flight.X, width, flight.Size, flight.ImpactSpeed, flight.Resting));
                    if (!alive)
                    {
                        Sprite sprite;
                        if (sprites.TryGetValue(flights[i], out sprite))
                        {
                            visual.Children.Remove(sprite.Visual);
                            sprites.Remove(flights[i]);
                        }

                        flights.RemoveAt(i);
                    }
                }

            PeakParticles = Math.Max(PeakParticles, flights.Count);
        }

        void Launch(double width, double height)
        {
            if (OriginProvider != null)
                Origin = OriginProvider();
            // Solve a ballistic arc from the actual stem towards a random floor target.
            double target = 40 + random.NextDouble() * Math.Max(20, width - 80);
            double duration = 1.05 + random.NextDouble() * .85;
            double size = 50 + random.NextDouble() * 50;
            var f = new Flight
            {
                X = Origin.X,
                Y = Origin.Y,
                Size = size,
                Angle = random.NextDouble() * 80 - 40,
                Spin = -360 + random.NextDouble() * 720
            };
            f.Vx = (target - f.X) / duration;
            f.Vy = (height - size * .4 - f.Y - .5 * 920 * duration * duration) / duration;
            flights.Add(f);
            EmitSound(new FlightSoundEventArgs(FlightSoundKind.Launch, f.X, width, f.Size, Math.Sqrt(f.Vx * f.Vx + f.Vy * f.Vy), false));
        }

        void EmitSound(FlightSoundEventArgs cue)
        {
            if (cue.Kind == FlightSoundKind.Launch)
                LaunchCues++;
            else
                ImpactCues++;
            if (SoundCue != null)
                SoundCue(this, cue);
        }

        public void Draw()
        {
            foreach (var f in flights)
            {
                Sprite sprite;
                if (!sprites.TryGetValue(f, out sprite))
                {
                    sprite = new Sprite();
                    using (var dc = sprite.Visual.RenderOpen())
                        dc.DrawImage(texture, new Rect(-f.Size / 2, -f.Size / 2, f.Size, f.Size));
                    sprite.Visual.Transform = sprite.Transform;
                    sprites.Add(f, sprite);
                    visual.Children.Add(sprite.Visual);
                }

                // Retain drawing commands and only update the compositor transform.
                var matrix = Matrix.Identity;
                matrix.Rotate(f.Angle);
                matrix.Translate(f.X, f.Y);
                sprite.Transform.Matrix = matrix;
                sprite.Visual.Opacity = Math.Min(f.Resting ? Math.Max(0, 1 - f.RestAge / .32) : 1, Math.Max(0, (6 - f.Age) / .45));
            }
        }

        sealed class Sprite
        {
            public readonly DrawingVisual Visual = new DrawingVisual();
            public readonly MatrixTransform Transform = new MatrixTransform();
        }
    }
}
