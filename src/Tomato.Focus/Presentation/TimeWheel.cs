using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Media;

namespace Tomato
{
    public sealed class TimeWheel : FrameworkElement
    {
        public int Limit { get; private set; }
        public string Unit { get; private set; }

        public event EventHandler ValueChanged;
        double position, target, downY, downPosition, lastY, speed;
        long lastMove;
        bool dragging, animating;
        int lastValue;
        const double Row = 42;
        public TimeWheel(int limit, string unit)
        {
            Limit = limit;
            Unit = unit;
            Width = 78;
            Height = 134;
            Focusable = true;
            Cursor = Cursors.Hand;
            AutomationProperties.SetName(this, unit);
            ToolTip = "滚动或上下拖动调整" + unit + "；方向键微调，也可直接输入数字";
            Unloaded += delegate
            {
                EndAnimation();
            };
        }

        public int Value
        {
            get
            {
                return Wrap((int)Math.Round(target));
            }

            set
            {
                position = target = Wrap(value);
                Notify();
                InvalidateVisual();
            }
        }

        int Wrap(int n)
        {
            return ((n % Limit) + Limit) % Limit;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new WheelPeer(this);
        }

        protected override void OnRender(DrawingContext dc)
        {
            dc.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));
            int nearest = (int)Math.Round(position);
            for (int i = nearest - 3; i <= nearest + 3; i++)
            {
                double y = 46 + (i - position) * Row;
                double distance = Math.Abs((i - position) * Row);
                double alpha = Math.Max(0, 1 - distance / 83);
                dc.PushOpacity(alpha * alpha);
                double scale = 1 - Math.Min(.17, distance / 350);
                dc.PushTransform(new ScaleTransform(scale, scale, ActualWidth / 2, y + 22));
                Art.CenterText(dc, Wrap(i).ToString("00"), 34, Art.Brush("#FFF4DF"), ActualWidth / 2, y, "Segoe UI Variable Display", false);
                dc.Pop();
                dc.Pop();
            }

            if (IsKeyboardFocused)
                dc.DrawRoundedRectangle(null, new Pen(Art.Brush("#9AFFE8BC"), 1), new Rect(3, 46, ActualWidth - 6, 43), 8, 8);
            dc.Pop();
        }

        void Notify()
        {
            int v = Value;
            if (v == lastValue)
                return;
            lastValue = v;
            if (ValueChanged != null)
                ValueChanged(this, EventArgs.Empty);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            target += e.Delta > 0 ? -1 : 1;
            BeginAnimation();
            Notify();
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            Focus();
            dragging = true;
            downY = lastY = e.GetPosition(this).Y;
            downPosition = position;
            speed = 0;
            lastMove = Stopwatch.GetTimestamp();
            CaptureMouse();
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!dragging)
                return;
            double y = e.GetPosition(this).Y;
            double dt = (Stopwatch.GetTimestamp() - lastMove) / (double)Stopwatch.Frequency;
            if (dt > .001)
                speed = .55 * speed + .45 * (lastY - y) / Row / dt;
            position = downPosition + (downY - y) / Row;
            target = position;
            lastY = y;
            lastMove = Stopwatch.GetTimestamp();
            InvalidateVisual();
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!dragging)
                return;
            dragging = false;
            ReleaseMouseCapture();
            double y = e.GetPosition(this).Y;
            if (Math.Abs(y - downY) < 4)
                target = Math.Round(position) + (y < 46 ? -1 : y > 90 ? 1 : 0);
            else
            {
                if ((Stopwatch.GetTimestamp() - lastMove) / (double)Stopwatch.Frequency > .12)
                    speed = 0;
                target = Math.Round(position + Math.Max(-8, Math.Min(8, speed * .15)));
            }

            BeginAnimation();
            Notify();
            e.Handled = true;
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            if (dragging)
            {
                dragging = false;
                target = Math.Round(position);
                BeginAnimation();
                Notify();
            }

            base.OnLostMouseCapture(e);
        }

        string digits = "";
        DateTime digitTime;
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                target += e.Key == Key.Up ? -1 : 1;
                BeginAnimation();
                Notify();
                e.Handled = true;
            }
            else if (e.Key == Key.Home)
            {
                Value = 0;
                e.Handled = true;
            }
            else if ((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            {
                int d = e.Key >= Key.NumPad0 ? (int)e.Key - (int)Key.NumPad0 : (int)e.Key - (int)Key.D0;
                if ((DateTime.UtcNow - digitTime).TotalSeconds > 1.2 || digits.Length == 2)
                    digits = "";
                digits += d.ToString();
                digitTime = DateTime.UtcNow;
                int number = int.Parse(digits);
                Value = number < Limit ? number : d;
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        public void Settle()
        {
            target = Math.Round(target);
            position = target;
            EndAnimation();
            Notify();
            InvalidateVisual();
        }

        void BeginAnimation()
        {
            if (animating)
                return;
            animating = true;
            previous = Stopwatch.GetTimestamp();
            CompositionTarget.Rendering += Animate;
        }

        long previous;
        void Animate(object sender, EventArgs e)
        {
            if (dragging)
                return;
            long now = Stopwatch.GetTimestamp();
            double dt = Math.Min(.1, (now - previous) / (double)Stopwatch.Frequency);
            previous = now;
            position += (target - position) * (1 - Math.Exp(-20 * dt));
            if (Math.Abs(target - position) < .003)
            {
                position = target;
                EndAnimation();
            }

            InvalidateVisual();
        }

        void EndAnimation()
        {
            CompositionTarget.Rendering -= Animate;
            animating = false;
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            InvalidateVisual();
            base.OnGotKeyboardFocus(e);
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            InvalidateVisual();
            base.OnLostKeyboardFocus(e);
        }

        sealed class WheelPeer : FrameworkElementAutomationPeer, IRangeValueProvider
        {
            readonly TimeWheel wheel;
            public WheelPeer(TimeWheel owner) : base(owner)
            {
                wheel = owner;
            }

            protected override string GetClassNameCore()
            {
                return "TimeWheel";
            }

            protected override AutomationControlType GetAutomationControlTypeCore()
            {
                return AutomationControlType.Spinner;
            }

            public override object GetPattern(PatternInterface pattern)
            {
                return pattern == PatternInterface.RangeValue ? this : base.GetPattern(pattern);
            }

            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public double LargeChange
            {
                get
                {
                    return 5;
                }
            }

            public double SmallChange
            {
                get
                {
                    return 1;
                }
            }

            public double Maximum
            {
                get
                {
                    return wheel.Limit - 1;
                }
            }

            public double Minimum
            {
                get
                {
                    return 0;
                }
            }

            public double Value
            {
                get
                {
                    return wheel.Value;
                }
            }

            public void SetValue(double value)
            {
                if (value < 0 || value >= wheel.Limit)
                    throw new ArgumentOutOfRangeException("value");
                wheel.Value = (int)value;
            }
        }
    }
}
