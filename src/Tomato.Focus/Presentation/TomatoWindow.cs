using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Automation;

namespace Tomato
{
    public sealed class TomatoWindow : Window
    {
        public const double DesktopScale = .72;
        readonly AppController controller;
        readonly Canvas canvas;
        readonly TimeWheel hours, minutes, seconds;
        readonly StackPanel picker;
        readonly TextBlock hint, status;
        readonly Border glass;
        readonly FrameworkElement branding;
        readonly Button menu;
        Point screenDown;
        bool pressed, moved;
        public bool AlarmMode { get; private set; }

        public TomatoWindow(AppController controller, BitmapSource art)
        {
            this.controller = controller;
            Title = "朱果 · 番茄钟";
            Width = 440 * DesktopScale;
            Height = 456 * DesktopScale;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = false;
            canvas = new Canvas
            {
                Width = 440,
                Height = 456,
                Background = Brushes.Transparent
            };
            Content = new Viewbox
            {
                Width = this.Width,
                Height = this.Height,
                Child = canvas,
                Stretch = Stretch.Uniform
            };
            var fruit = new Image
            {
                Source = art,
                Width = 440,
                Height = 440,
                IsHitTestVisible = false
            };
            canvas.Children.Add(fruit);
            branding = Label("T O M A T O   /   F O C U S", 10, "#FCDDB8", true);
            Place(branding, 0, 183, 440);
            glass = new Border
            {
                Width = 276,
                Height = 146,
                CornerRadius = new CornerRadius(25),
                Background = Art.Brush("#641F070D"),
                BorderBrush = Art.Brush("#32FFD4AC"),
                BorderThickness = new Thickness(1)
            };
            Place(glass, 82, 204);
            var selection = new Border
            {
                Width = 256,
                Height = 43,
                CornerRadius = new CornerRadius(10),
                Background = Art.Brush("#12FFF2CE"),
                BorderBrush = Art.Brush("#17FFE6C3"),
                BorderThickness = new Thickness(0, 1, 0, 1)
            };
            var inside = new Canvas();
            glass.Child = inside;
            inside.Children.Add(selection);
            Canvas.SetLeft(selection, 9);
            Canvas.SetTop(selection, 45);
            picker = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            hours = new TimeWheel(24, "小时");
            minutes = new TimeWheel(60, "分钟");
            seconds = new TimeWheel(60, "秒");
            picker.Children.Add(hours);
            picker.Children.Add(minutes);
            picker.Children.Add(seconds);
            Place(picker, 103, 204);
            foreach (var wheel in new[]
            {
                hours,
                minutes,
                seconds
            }

            )
                wheel.ValueChanged += delegate
                {
                    if (controller != null)
                        controller.DurationChanged();
                };
            var units = new Grid
            {
                Width = 234
            };
            for (int i = 0; i < 3; i++)
                units.ColumnDefinitions.Add(new ColumnDefinition());
            string[] captions =
            {
                "小时",
                "分钟",
                "秒"
            };
            for (int i = 0; i < 3; i++)
            {
                var unit = Label(captions[i], 11.5, "#E6F6D9BF", false);
                Grid.SetColumn(unit, i);
                units.Children.Add(unit);
            }

            Place(units, 103, 334);
            units.Name = "Units";
            units.IsHitTestVisible = false;
            status = Label("让时间，慢慢成熟。", 13.5, "#FFF0D2", false);
            Place(status, 0, 354, 440);
            hint = Label("双击绿蒂开始  ·  拖动果身移动", 13, "#FFF3DC", false);
            var hintBack = new Border
            {
                Width = 330,
                Background = Art.Brush("#DA292C28"),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(15, 8, 15, 8),
                Child = hint
            };
            Place(hintBack, 55, 413);
            menu = new Button
            {
                Content = "···",
                Width = 32,
                Height = 24,
                FontSize = 18,
                Foreground = Art.Brush("#FFF2D4"),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                ToolTip = "预设、试听与退出"
            };
            AutomationProperties.SetName(menu, "菜单");
            menu.Click += delegate
            {
                OpenMenu();
            };
            Place(menu, 329, 176);
            MouseLeftButtonDown += OnDown;
            MouseMove += OnMove;
            MouseLeftButtonUp += OnUp;
            KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Escape)
                {
                    controller.StopAlarm();
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter && !AlarmMode)
                {
                    controller.Start();
                    e.Handled = true;
                }
            };
            Closing += delegate (object sender, System.ComponentModel.CancelEventArgs e)
            {
                if (!controller.Exiting)
                {
                    e.Cancel = true;
                    Hide();
                }
            };
            ContextMenu = BuildMenu();
        }

        static TextBlock Label(string text, double size, string color, bool bold)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = size,
                FontFamily = new FontFamily("Microsoft YaHei UI"),
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = Art.Brush(color),
                TextAlignment = TextAlignment.Center,
                IsHitTestVisible = false
            };
        }

        void Place(UIElement element, double x, double y, double width = 0)
        {
            canvas.Children.Add(element);
            Canvas.SetLeft(element, x);
            Canvas.SetTop(element, y);
            if (width > 0)
                ((FrameworkElement)element).Width = width;
        }

        public int Duration
        {
            get
            {
                return hours.Value * 3600 + minutes.Value * 60 + seconds.Value;
            }

            set
            {
                hours.Value = value / 3600;
                minutes.Value = (value / 60) % 60;
                seconds.Value = value % 60;
            }
        }

        public void Settle()
        {
            hours.Settle();
            minutes.Settle();
            seconds.Settle();
        }

        public void SetAlarm(bool alarm)
        {
            AlarmMode = alarm;
            picker.Visibility = alarm ? Visibility.Hidden : Visibility.Visible;
            glass.Visibility = alarm ? Visibility.Hidden : Visibility.Visible;
            foreach (UIElement child in canvas.Children)
            {
                var fe = child as FrameworkElement;
                if (fe != null && fe.Name == "Units")
                    fe.Visibility = alarm ? Visibility.Hidden : Visibility.Visible;
            }

            status.Text = alarm ? "好好休息，再次出发。" : "让时间，慢慢成熟。";
            hint.Text = alarm ? "拖动番茄或双击，结束提醒" : "双击绿蒂开始  ·  拖动果身移动";
            if (alarm)
            {
                var title = Label("时间到了", 31, "#FFF4DE", true);
                title.Name = "AlarmTitle";
                Place(title, 0, 229, 440);
                var subtitle = Label("W E L L   D O N E", 10, "#FFDBBC", false);
                subtitle.Name = "AlarmSubtitle";
                Place(subtitle, 0, 281, 440);
            }
            else
            {
                for (int i = canvas.Children.Count - 1; i >= 0; i--)
                {
                    var fe = canvas.Children[i] as FrameworkElement;
                    if (fe != null && (fe.Name == "AlarmTitle" || fe.Name == "AlarmSubtitle"))
                        canvas.Children.RemoveAt(i);
                }
            }
        }

        public void Error(string text)
        {
            status.Text = text;
        }

        public Point ThrowOrigin
        {
            get
            {
                return canvas.TranslatePoint(new Point(222, 120), this);
            }
        }

        void OnDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;
            var p = e.GetPosition(canvas);
            // Only the visible fruit is draggable; transparent desktop corners pass through.
            if (p.Y < 45 || p.Y > 395 || p.X < 40 || p.X > 409)
                return;
            if (e.ClickCount == 2)
            {
                pressed = false;
                ReleaseMouseCapture();
                if (AlarmMode)
                    controller.StopAlarm();
                else
                    controller.Start();
                e.Handled = true;
                return;
            }

            pressed = true;
            moved = false;
            screenDown = canvas.PointToScreen(p);
            CaptureMouse();
            e.Handled = true;
        }

        void OnMove(object sender, MouseEventArgs e)
        {
            if (!pressed || e.LeftButton != MouseButtonState.Pressed)
                return;
            var screen = PointToScreen(e.GetPosition(this));
            if (!moved && (screen - screenDown).Length < 6)
                return;
            if (!moved && AlarmMode)
            {
                controller.StopAlarmForDrag();
            }

            moved = true;
            var source = PresentationSource.FromVisual(this);
            var transform = source.CompositionTarget.TransformFromDevice;
            var delta = transform.Transform(screen - screenDown);
            Left += delta.X;
            Top += delta.Y;
            screenDown = screen;
        }

        void OnUp(object sender, MouseButtonEventArgs e)
        {
            pressed = false;
            ReleaseMouseCapture();
            if (moved)
            {
                controller.ClampWindow();
                controller.Save();
            }

            e.Handled = true;
        }

        public void ShowCountdown(string remaining)
        {
            status.Text = "专注中 · " + remaining;
            hint.Text = "计时仍在继续 · 从托盘取消";
            picker.IsEnabled = false;
        }

        public void ShowEditor()
        {
            picker.IsEnabled = true;
            SetAlarm(false);
        }

        public ContextMenu BuildMenu()
        {
            var context = new ContextMenu
            {
                Background = Art.Brush("#F9F5ED"),
                Foreground = Art.Brush("#263528"),
                Padding = new Thickness(7),
                FontFamily = new FontFamily("Microsoft YaHei UI")
            };
            Add(context, "25 分钟 · 专注", delegate
            {
                controller.Preset(1500);
            });
            Add(context, "5 分钟 · 短休息", delegate
            {
                controller.Preset(300);
            });
            Add(context, "15 分钟 · 长休息", delegate
            {
                controller.Preset(900);
            });
            context.Items.Add(new Separator());
            Add(context, "预览投掷效果 · 8 秒", delegate
            {
                controller.Preview();
            });
            Add(context, controller.Sound ? "✓ 提示音" : "提示音", delegate
            {
                controller.ToggleSound();
            });
            Add(context, "取消当前计时 / 停止提醒", delegate
            {
                controller.Cancel();
            });
            context.Items.Add(new Separator());
            Add(context, "退出朱果", delegate
            {
                controller.Quit();
            });
            return context;
        }

        void Add(ContextMenu menu, string name, Action action)
        {
            var item = new MenuItem
            {
                Header = name,
                Padding = new Thickness(12, 7, 12, 7)
            };
            item.Click += delegate
            {
                action();
            };
            menu.Items.Add(item);
        }

        void OpenMenu()
        {
            ContextMenu = BuildMenu();
            ContextMenu.PlacementTarget = menu;
            ContextMenu.IsOpen = true;
        }
    }
}
