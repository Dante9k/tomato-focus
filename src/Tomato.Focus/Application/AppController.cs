using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Tomato
{
    public sealed class AppController
    {
        readonly Application app;
        readonly StateStore store;
        readonly Preferences preferences;
        readonly Countdown clock = new Countdown();
        readonly DispatcherTimer ticker = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        readonly BitmapSource art;
        readonly BitmapSource smallArt;
        Forms.NotifyIcon tray;
        ThrowWindow overlay;
        DateTime previewUntil;
        bool preview, initialized, hotkey;
        double editorLeft, editorTop;
        HwndSource source;
        SoundPlayer player;
        MemoryStream sound;
        public TomatoWindow Window { get; private set; }
        public bool Exiting { get; private set; }

        public bool Sound
        {
            get
            {
                return preferences.Sound;
            }
        }

        public TimerPhase Phase
        {
            get
            {
                return clock.Phase;
            }
        }

        public bool IsThrowing
        {
            get
            {
                return overlay != null;
            }
        }

        public ThrowSurface Surface
        {
            get
            {
                return overlay == null ? null : overlay.Surface;
            }
        }

        public AppController(Application app, string dataPath)
        {
            this.app = app;
            store = new StateStore(dataPath);
            preferences = store.Read();
            art = Art.TomatoImage(880);
            smallArt = Art.TomatoImage(192);
            Window = new TomatoWindow(this, art);
            Window.Duration = Math.Max(1, Math.Min(86399, preferences.Seconds));
            Window.Left = double.IsNaN(preferences.Left) ? SystemParameters.WorkArea.Right - Window.Width - 50 : preferences.Left;
            Window.Top = double.IsNaN(preferences.Top) ? SystemParameters.WorkArea.Top + 90 : preferences.Top;
            ticker.Tick += Tick;
            initialized = true;
        }

        public void Launch(bool restore)
        {
            Window.Show();
            ClampWindow();
            CreateTray();
            source = HwndSource.FromHwnd(new WindowInteropHelper(Window).Handle);
            source.AddHook(Hook);
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += DisplayChanged;
            ticker.Start();
            if (restore && preferences.Active && preferences.DeadlineTicks > 0 && preferences.DeadlineTicks < DateTime.MaxValue.Ticks)
            {
                clock.Restore(preferences.Seconds, new DateTime(preferences.DeadlineTicks, DateTimeKind.Utc), DateTime.UtcNow);
                if (clock.Phase == TimerPhase.Ringing)
                    BeginAlarm(false);
                else
                    Window.Hide();
            }
        }

        public void DurationChanged()
        {
            if (initialized && clock.Phase == TimerPhase.Editing && !preview)
            {
                preferences.Seconds = Window.Duration;
            }
        }

        public void Preset(int seconds)
        {
            Cancel();
            Window.Duration = seconds;
            Save();
        }

        public void ToggleSound()
        {
            preferences.Sound = !preferences.Sound;
            Save();
        }

        public void Start()
        {
            if (clock.Phase == TimerPhase.Running)
            {
                Window.Hide();
                return;
            }

            if (IsThrowing)
            {
                StopAlarm();
                return;
            }

            Window.Settle();
            if (Window.Duration == 0)
            {
                Window.Error("先设置一点专注时间");
                return;
            }

            clock.Start(Window.Duration, DateTime.UtcNow);
            preferences.Seconds = Window.Duration;
            Save();
            Window.Hide();
            UpdateTray();
        }

        void Tick(object sender, EventArgs e)
        {
            if (preview && DateTime.UtcNow >= previewUntil)
            {
                StopAlarm();
                return;
            }

            if (clock.Tick(DateTime.UtcNow))
                BeginAlarm(false);
            if (clock.Phase == TimerPhase.Running)
            {
                UpdateTray();
                if (Window.IsVisible)
                    Window.ShowCountdown(Format(clock.Remaining(DateTime.UtcNow)));
            }
        }

        static string Format(int seconds)
        {
            return TimeSpan.FromSeconds(seconds).ToString(seconds >= 3600 ? @"hh\:mm\:ss" : @"mm\:ss");
        }

        public void Preview()
        {
            if (clock.Phase == TimerPhase.Running)
            {
                Window.Error("请先取消当前计时，再预览");
                return;
            }

            if (IsThrowing)
                return;
            previewUntil = DateTime.UtcNow.AddSeconds(8);
            BeginAlarm(true);
        }

        void BeginAlarm(bool isPreview)
        {
            if (overlay != null)
                return;
            preview = isPreview;
            editorLeft = Window.Left;
            editorTop = Window.Top;
            var screen = Forms.Screen.FromHandle(new WindowInteropHelper(Window).Handle);
            var matrix = source.CompositionTarget.TransformFromDevice;
            var work = screen.WorkingArea;
            Point corner = matrix.Transform(new Point(work.Right, work.Top));
            Window.SetAlarm(true);
            Window.Left = corner.X - Window.Width - 18;
            Window.Top = corner.Y + 8;
            var desktop = Forms.SystemInformation.VirtualScreen;
            var p = matrix.Transform(new Point(desktop.Left, desktop.Top));
            var size = matrix.Transform(new Vector(desktop.Width, desktop.Height));
            var bounds = new Rect(p.X, p.Y, size.X, size.Y);
            Point stem = Window.ThrowOrigin;
            Point origin = new Point(Window.Left + stem.X - bounds.Left, Window.Top + stem.Y - bounds.Top);
            overlay = new ThrowWindow(smallArt, bounds, origin);
            overlay.Show();
            Window.Show();
            Window.Topmost = false;
            Window.Topmost = true;
            Window.Activate();
            hotkey = Native.RegisterHotKey(source.Handle, 91, 0, 0x1B);
            if (Sound && !isPreview)
                PlayChime();
            Save();
            UpdateTray();
        }

        public void StopAlarm()
        {
            StopAlarmCore(true);
        }

        public void StopAlarmForDrag()
        {
            StopAlarmCore(false);
        }

        void StopAlarmCore(bool restorePosition)
        {
            if (!IsThrowing)
                return;
            overlay.Close();
            overlay = null;
            preview = false;
            if (hotkey)
            {
                Native.UnregisterHotKey(source.Handle, 91);
                hotkey = false;
            }

            if (player != null)
                player.Stop();
            clock.Stop();
            Window.ShowEditor();
            if (restorePosition)
            {
                Window.Left = editorLeft;
                Window.Top = editorTop;
                ClampWindow();
            }

            Save();
            UpdateTray();
        }

        public void Cancel()
        {
            StopAlarm();
            clock.Stop();
            Window.ShowEditor();
            Window.Show();
            Window.Activate();
            Save();
            UpdateTray();
        }

        public void Show()
        {
            Window.Show();
            ClampWindow();
            Window.Activate();
            if (clock.Phase == TimerPhase.Running)
                Window.ShowCountdown(Format(clock.Remaining(DateTime.UtcNow)));
        }

        public void ClampWindow()
        {
            var hwnd = new WindowInteropHelper(Window).Handle;
            double scale = 1;
            var ps = PresentationSource.FromVisual(Window);
            if (ps != null)
                scale = ps.CompositionTarget.TransformFromDevice.M11;
            var screen = hwnd == IntPtr.Zero ? Forms.Screen.PrimaryScreen : Forms.Screen.FromHandle(hwnd);
            var area = screen.WorkingArea;
            Window.Left = Math.Max(area.Left * scale, Math.Min(Window.Left, area.Right * scale - Window.Width));
            Window.Top = Math.Max(area.Top * scale, Math.Min(Window.Top, area.Bottom * scale - Window.Height));
        }

        public void Save()
        {
            if (!initialized)
                return;
            if (clock.Phase == TimerPhase.Editing)
                preferences.Seconds = Window.Duration;
            preferences.Active = clock.Phase != TimerPhase.Editing;
            preferences.DeadlineTicks = clock.DeadlineUtc.Ticks;
            preferences.Left = IsThrowing ? editorLeft : Window.Left;
            preferences.Top = IsThrowing ? editorTop : Window.Top;
            if (!store.Write(preferences) && tray != null)
                tray.Text = "朱果 · 设置保存失败，本次计时继续";
        }

        void CreateTray()
        {
            tray = new Forms.NotifyIcon();
            var png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(smallArt));
            using (var bytes = new MemoryStream())
            {
                png.Save(bytes);
                bytes.Position = 0;
                using (var bmp = new System.Drawing.Bitmap(bytes))
                {
                    var handle = bmp.GetHicon();
                    try
                    {
                        using (var icon = System.Drawing.Icon.FromHandle(handle))
                        {
                            tray.Icon = (System.Drawing.Icon)icon.Clone();
                        }
                    }
                    finally
                    {
                        Native.DestroyIcon(handle);
                    }
                }
            }

            tray.Visible = true;
            tray.Text = "朱果 · 番茄钟";
            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add("显示番茄", null, delegate
            {
                Show();
            });
            menu.Items.Add("取消计时 / 停止提醒", null, delegate
            {
                Cancel();
            });
            menu.Items.Add("预览投掷 · 8 秒", null, delegate
            {
                Preview();
            });
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("退出朱果", null, delegate
            {
                Quit();
            });
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate
            {
                Show();
            };
        }

        void UpdateTray()
        {
            if (tray != null)
                tray.Text = clock.Phase == TimerPhase.Running ? "朱果 · 专注剩余 " + Format(clock.Remaining(DateTime.UtcNow)) : IsThrowing ? "朱果 · 时间到了，拖动番茄结束提醒" : "朱果 · 番茄钟";
        }

        IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312 && wParam.ToInt32() == 91)
            {
                StopAlarm();
                handled = true;
            }

            return IntPtr.Zero;
        }

        void DisplayChanged(object sender, EventArgs e)
        {
            app.Dispatcher.BeginInvoke(new Action(delegate
            {
                if (IsThrowing)
                    StopAlarm();
                ClampWindow();
            }));
        }

        void PlayChime()
        {
            try
            {
                if (player != null)
                {
                    player.Dispose();
                    sound.Dispose();
                }

                sound = new MemoryStream();
                var writer = new BinaryWriter(sound);
                int samples = 44100 * 2;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + samples * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(44100);
                writer.Write(88200);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(samples * 2);
                for (int i = 0; i < samples; i++)
                {
                    double t = i / 44100.0;
                    double v = Math.Sin(2 * Math.PI * 659.25 * t) * Math.Exp(-3 * t);
                    if (t > .23)
                        v += .7 * Math.Sin(2 * Math.PI * 830.61 * (t - .23)) * Math.Exp(-4 * (t - .23));
                    writer.Write((short)(v * 6500 * Math.Min(1, t * 100)));
                }

                writer.Flush();
                sound.Position = 0;
                player = new SoundPlayer(sound);
                player.Play();
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        public void Quit()
        {
            Save();
            Exiting = true;
            ticker.Stop();
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= DisplayChanged;
            if (hotkey)
                Native.UnregisterHotKey(source.Handle, 91);
            if (overlay != null)
                overlay.Close();
            if (source != null)
                source.RemoveHook(Hook);
            if (player != null)
                player.Dispose();
            if (sound != null)
                sound.Dispose();
            if (tray != null)
            {
                tray.Visible = false;
                var icon = tray.Icon;
                tray.Dispose();
                if (icon != null)
                    icon.Dispose();
            }

            Window.Close();
            app.Shutdown();
        }

        internal static void Log(Exception ex)
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TomatoFocus");
                Directory.CreateDirectory(folder);
                string file = Path.Combine(folder, "error.log");
                if (File.Exists(file) && new FileInfo(file).Length > 1024 * 1024)
                    File.WriteAllText(file, "");
                File.AppendAllText(file, DateTime.UtcNow.ToString("o") + " " + ex + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
