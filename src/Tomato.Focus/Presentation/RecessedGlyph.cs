using System;
using System.Windows;
using System.Windows.Media;

namespace Tomato
{
    // Vector apertures: soft white light is clipped by the glyph, with an occluding upper skin edge.
    // Geometry changes only when the displayed text/size changes, never on animation frames.
    public sealed class RecessedGlyph : FrameworkElement
    {
        // Original rounded, equal-width numerals. Shared curves keep all ten digits in one family.
        static readonly Geometry[] Numerals = MakeNumerals();
        static readonly Brush Light = MakeLight();
        static readonly Brush InnerEdge = Art.Brush("#756B2D24");
        static readonly Brush LowerLip = Art.Brush("#32E6DDDC");
        static readonly Pen SkinEdge = MakePen();
        string text = "00";
        double fontSize = 40, cachedWidth = -1;
        Geometry aperture, occlusion, lowerLip;
        public string Text
        {
            get
            {
                return text;
            }

            set
            {
                if (text == value)
                    return;
                text = value ?? "";
                InvalidateGlyph();
            }
        }

        public double FontSize
        {
            get
            {
                return fontSize;
            }

            set
            {
                if (fontSize == value)
                    return;
                fontSize = value;
                InvalidateGlyph();
            }
        }

        public RecessedGlyph()
        {
            IsHitTestVisible = false;
            SnapsToDevicePixels = false;
        }

        protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        {
            return new GlyphPeer(this);
        }

        sealed class GlyphPeer : System.Windows.Automation.Peers.FrameworkElementAutomationPeer
        {
            public GlyphPeer(RecessedGlyph owner) : base(owner)
            {
            }

            protected override string GetNameCore()
            {
                return ((RecessedGlyph)Owner).Text;
            }

            protected override System.Windows.Automation.Peers.AutomationControlType GetAutomationControlTypeCore()
            {
                return System.Windows.Automation.Peers.AutomationControlType.Text;
            }
        }

        void InvalidateGlyph()
        {
            aperture = null;
            InvalidateMeasure();
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(double.IsNaN(Width) ? fontSize * 1.2 : Width, fontSize * 1.36);
        }

        protected override void OnRender(DrawingContext drawing)
        {
            if (aperture == null || cachedWidth != ActualWidth)
            {
                cachedWidth = ActualWidth;
                aperture = BuildAperture();
                aperture.Freeze();
                var shifted = aperture.Clone();
                shifted.Transform = new TranslateTransform(.35, .65);
                shifted.Freeze();
                occlusion = new CombinedGeometry(GeometryCombineMode.Exclude, aperture, shifted);
                occlusion.Freeze();
                var lowered = aperture.Clone();
                lowered.Transform = new TranslateTransform(0, .35);
                lowered.Freeze();
                lowerLip = new CombinedGeometry(GeometryCombineMode.Exclude, lowered, aperture);
                lowerLip.Freeze();
            }

            drawing.DrawGeometry(LowerLip, null, lowerLip);
            drawing.DrawGeometry(Light, SkinEdge, aperture);
            drawing.DrawGeometry(InnerEdge, null, occlusion);
        }

        Geometry BuildAperture()
        {
            double scale = fontSize / 40;
            var result = new GeometryGroup
            {
                FillRule = FillRule.Nonzero
            };
            if (text == ":")
            {
                result.Children.Add(new EllipseGeometry(new Point(ActualWidth / 2, fontSize * .22 + (2.1 + 9 * .86) * scale), 2.3 * scale, 2.3 * scale));
                result.Children.Add(new EllipseGeometry(new Point(ActualWidth / 2, fontSize * .22 + (2.1 + 22 * .86) * scale), 2.3 * scale, 2.3 * scale));
            }
            else
            {
                double start = (ActualWidth - text.Length * 24 * scale) / 2;
                for (int i = 0; i < text.Length; i++)
                {
                    int digit = text[i] - '0';
                    if (digit < 0 || digit > 9)
                        continue;
                    var shape = Numerals[digit].Clone();
                    shape.Transform = new MatrixTransform(scale, 0, 0, scale * .86, start + i * 24 * scale + 2 * scale, fontSize * .22 + 2.1 * scale);
                    result.Children.Add(shape);
                }
            }

            return result;
        }

        static Geometry[] MakeNumerals()
        {
            string[] paths =
            {
                "M10,2 C4,2 2,7 2,15 C2,23 4,28 10,28 C16,28 18,23 18,15 C18,7 16,2 10,2 Z",
                "M5,8 Q9,5 12,2 L12,28",
                "M2,7 C4,0 16,0 18,7 C20,15 7,17 3,25 Q1,28 5,28 L18,28",
                "M3,4 C9,0 18,2 18,8 C18,13 13,15 9,15 M9,15 C14,15 19,18 18,23 C17,29 7,30 2,25",
                "M13,2 L3,18 Q1,21 5,21 L19,21 M15,12 L15,28",
                "M18,2 L4,2 L3,14 C7,10 18,12 18,20 C18,28 8,31 2,25",
                "M16,3 C7,-1 2,10 2,19 C2,32 19,30 18,20 C17,12 4,11 2,19",
                "M2,2 L17,2 Q20,2 17,7 C12,15 8,22 7,28",
                "M10,2 C-1,2 0,14 10,15 C20,14 21,2 10,2 Z M10,15 C-2,16 0,28 10,28 C20,28 22,16 10,15 Z",
                "M18,12 C18,-1 1,0 2,10 C3,18 16,19 18,11 M18,11 C18,20 13,31 4,27"
            };
            var pen = new Pen(Brushes.White, 4.8)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            pen.Freeze();
            var shapes = new Geometry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                shapes[i] = Geometry.Parse(paths[i]).GetWidenedPathGeometry(pen);
                shapes[i].Freeze();
            }

            return shapes;
        }

        static Brush MakeLight()
        {
            var brush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
                GradientStops = new GradientStopCollection
                {
                    new GradientStop(Color.FromRgb(211, 209, 209), 0),
                    new GradientStop(Color.FromRgb(236, 236, 236), .24),
                    new GradientStop(Color.FromRgb(246, 246, 246), .5),
                    new GradientStop(Color.FromRgb(239, 239, 239), .77),
                    new GradientStop(Color.FromRgb(219, 217, 217), 1)
                }
            };
            brush.Freeze();
            return brush;
        }

        static Pen MakePen()
        {
            var pen = new Pen(Art.Brush("#186D3226"), .7)
            {
                LineJoin = PenLineJoin.Round
            };
            pen.Freeze();
            return pen;
        }
    }
}
