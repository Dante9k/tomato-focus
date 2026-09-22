using Tomato;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Tomato.Tests
{
    internal static class Verification
    {
        internal static int SmokeExitCode;
        static string Output(string name)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
        }

        static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
        }

        public static int Run()
        {
            var lines = new List<string>();
            try
            {
                var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                var clock = new Countdown();
                bool rejected = false;
                try
                {
                    clock.Start(0, now);
                }
                catch (ArgumentOutOfRangeException)
                {
                    rejected = true;
                }

                Assert(rejected, "零时长必须拒绝");
                lines.Add("PASS 零时长保护");
                rejected = false;
                try
                {
                    clock.Start(86400, now);
                }
                catch (ArgumentOutOfRangeException)
                {
                    rejected = true;
                }

                Assert(rejected, "24 小时上界");
                lines.Add("PASS 时长上界");
                clock.Start(1500, now);
                Assert(clock.Remaining(now.AddMilliseconds(1)) == 1500, "向上取整");
                Assert(!clock.Tick(now.AddSeconds(1499.9)), "不得提前响铃");
                Assert(clock.Tick(now.AddSeconds(1500)), "到期触发");
                Assert(!clock.Tick(now.AddSeconds(1501)), "响铃仅触发一次");
                lines.Add("PASS 截止时刻及单次触发");
                clock.Start(5, now);
                Assert(clock.Tick(now.AddHours(2)), "休眠唤醒");
                lines.Add("PASS 休眠跨越截止时间");
                clock.Restore(1500, now.AddMinutes(25), now.AddMinutes(2));
                Assert(clock.Phase == TimerPhase.Running && clock.Remaining(now.AddMinutes(2)) == 1380, "重启恢复");
                clock.Restore(1500, now, now.AddHours(1));
                Assert(clock.Phase == TimerPhase.Ringing, "重启时过期");
                clock.Stop();
                Assert(clock.Phase == TimerPhase.Editing, "取消");
                lines.Add("PASS 未到期 / 已到期恢复及取消");
                string testPath = Output("verification-state.xml");
                var store = new StateStore(testPath);
                var pref = new Preferences
                {
                    Seconds = 3599,
                    Active = true,
                    DeadlineTicks = now.Ticks,
                    Left = -900,
                    Top = 100
                };
                Assert(store.Write(pref), "保存失败");
                var restored = store.Read();
                Assert(restored.Seconds == 3599 && restored.Active && restored.Left == -900, "保存恢复不一致");
                pref.Seconds = 42;
                Assert(store.Write(pref) && store.Read().Seconds == 42, "原子覆盖");
                File.WriteAllText(testPath, "broken xml");
                Assert(store.Read().Seconds == 1500, "损坏文件恢复");
                lines.Add("PASS 设置保存、覆盖及损坏恢复");
                var flight = new Flight
                {
                    X = 600,
                    Y = 120,
                    Vx = -300,
                    Vy = -250,
                    Size = 90,
                    Spin = 220
                };
                double expectedX = 600 - 300 * .5;
                for (int i = 0; i < 50; i++)
                    flight.Step(.01, 1920, 1080);
                Assert(Math.Abs(flight.X - expectedX) < .0001, "水平轨迹");
                Assert(Math.Abs(flight.Y - (120 - 250 * .5 + .5 * 920 * .25)) < .001, "重力轨迹");
                for (int i = 0; i < 200; i++)
                    flight.Step(.01, 1920, 1080);
                Assert(flight.Bounces == 1, "落地反弹");
                Assert(!flight.Step(7, 1920, 1080), "寿命回收");
                lines.Add("PASS 抛物线、反弹与粒子回收");
                var wheel = new TimeWheel(60, "秒");
                wheel.Value = 60;
                Assert(wheel.Value == 0, "向上环绕");
                wheel.Value = -1;
                Assert(wheel.Value == 59, "向下环绕");
                wheel.Value = 25;
                wheel.Settle();
                Assert(wheel.Value == 25, "吸附");
                lines.Add("PASS 时间滚轮循环与吸附");
                var texture = Art.TomatoImage(192);
                var surface = new ThrowSurface(texture)
                {
                    Origin = new Point(1660, 160)
                };
                surface.Measure(new Size(1920, 1080));
                surface.Arrange(new Rect(0, 0, 1920, 1080));
                var watch = Stopwatch.StartNew();
                for (int i = 0; i < 60 * 120; i++)
                {
                    surface.Advance(1.0 / 60, 1920, 1080);
                    if (i % 60 == 0)
                        surface.Draw();
                }

                watch.Stop();
                Assert(surface.PeakParticles <= 36 && surface.PeakParticles > 0, "粒子数量");
                lines.Add("PASS 120 秒动画模拟，峰值 " + surface.PeakParticles + " 个，模拟耗时 " + watch.ElapsedMilliseconds + " ms");
                lines.Add("全部验证通过。 " + DateTime.UtcNow.ToString("o"));
                File.WriteAllLines(Output("test-results.txt"), lines, System.Text.Encoding.UTF8);
                return 0;
            }
            catch (Exception ex)
            {
                lines.Add("FAIL " + ex);
                File.WriteAllLines(Output("test-results.txt"), lines, System.Text.Encoding.UTF8);
                return 1;
            }
        }

        public static int Render()
        {
            try
            {
                var app = new Application
                {
                    ShutdownMode = ShutdownMode.OnExplicitShutdown
                };
                var controller = new AppController(app, Output("preview-state.xml"));
                controller.Window.Duration = 1500;
                double width = controller.Window.Width, height = controller.Window.Height;
                var root = (FrameworkElement)controller.Window.Content;
                root.Measure(new Size(width, height));
                root.Arrange(new Rect(0, 0, width, height));
                root.UpdateLayout();
                Art.Save(root, (int)Math.Ceiling(width), (int)Math.Ceiling(height), Output("tomato-widget.png"));
                var iconPng = new System.Windows.Media.Imaging.PngBitmapEncoder();
                iconPng.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(Art.TomatoImage(256)));
                using (var bytes = new MemoryStream())
                {
                    iconPng.Save(bytes);
                    byte[] payload = bytes.ToArray();
                    using (var writer = new BinaryWriter(File.Create(Output("Tomato.ico"))))
                    {
                        writer.Write((short)0);
                        writer.Write((short)1);
                        writer.Write((short)1);
                        writer.Write((byte)0);
                        writer.Write((byte)0);
                        writer.Write((byte)0);
                        writer.Write((byte)0);
                        writer.Write((short)1);
                        writer.Write((short)32);
                        writer.Write(payload.Length);
                        writer.Write(22);
                        writer.Write(payload);
                    }
                }

                var visual = new DrawingVisual();
                using (var dc = visual.RenderOpen())
                {
                    dc.DrawRectangle(Art.Brush("#EAE9DF"), null, new Rect(0, 0, 1440, 960));
                    dc.DrawRectangle(Art.Brush("#F5F4ED"), null, new Rect(30, 30, 1380, 900));
                    Art.Text(dc, "朱果", 34, Art.Brush("#243C30"), 87, 79, "Microsoft YaHei UI", true);
                    Art.Text(dc, "T O M A T O   /   F O C U S", 11, Art.Brush("#648069"), 88, 135, "Segoe UI", false);
                    Art.Text(dc, "让时间，", 64, Art.Brush("#233D2E"), 87, 281, "Microsoft YaHei UI", true);
                    Art.Text(dc, "慢慢成熟。", 64, Art.Brush("#233D2E"), 87, 365, "Microsoft YaHei UI", true);
                    Art.Text(dc, "一颗番茄，一段完整的专注。", 17, Art.Brush("#657665"), 93, 480, "Microsoft YaHei UI", false);
                    Art.Text(dc, "滚动设置时间。双击绿蒂，然后进入心流。", 14, Art.Brush("#7E8B79"), 93, 520, "Microsoft YaHei UI", false);
                    dc.PushTransform(new TranslateTransform(853, 248));
                    dc.PushTransform(new ScaleTransform(1.22, 1.22));
                    dc.DrawRectangle(new VisualBrush(root), null, new Rect(0, 0, width, height));
                    dc.Pop();
                    dc.Pop();
                    dc.DrawLine(new Pen(Art.Brush("#D8DED1"), 1), new Point(90, 726), new Point(1347, 726));
                    string[] ns =
                    {
                        "01",
                        "02",
                        "03"
                    };
                    string[] titles =
                    {
                        "设定节奏",
                        "进入专注",
                        "收获休息"
                    };
                    string[] desc =
                    {
                        "时 · 分 · 秒，轻滑即定",
                        "双击绿蒂，番茄悄然隐去",
                        "番茄跃入桌面，提醒适时停下"
                    };
                    for (int i = 0; i < 3; i++)
                    {
                        double x = 92 + i * 427;
                        Art.Text(dc, ns[i], 12, Art.Brush("#C3553C"), x, 770, "Segoe UI", true);
                        Art.Text(dc, titles[i], 19, Art.Brush("#284333"), x + 38, 764, "Microsoft YaHei UI", true);
                        Art.Text(dc, desc[i], 13, Art.Brush("#7D8977"), x + 38, 809, "Microsoft YaHei UI", false);
                    }

                    Art.Text(dc, "DESKTOP EDITION     /     1.0", 10, Art.Brush("#899580"), 92, 887, "Segoe UI", false);
                    Art.Text(dc, "专注有时，休息有声。", 11, Art.Brush("#899580"), 1210, 883, "Microsoft YaHei UI", false);
                }

                Art.Save(visual, 1440, 960, Output("preview.png"));
                controller.Window.SetAlarm(true);
                root.UpdateLayout();
                Art.Save(root, (int)Math.Ceiling(width), (int)Math.Ceiling(height), Output("tomato-alarm.png"));
                File.WriteAllText(Output("render-results.txt"), "PASS actual WPF widget and alarm render");
                return 0;
            }
            catch (Exception ex)
            {
                File.WriteAllText(Output("render-results.txt"), ex.ToString());
                return 1;
            }
        }

        public static void Smoke(AppController controller)
        {
            var watch = Stopwatch.StartNew();
            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            int stage = 0;
            var log = new List<string>();
            timer.Tick += delegate
            {
                try
                {
                    double elapsed = watch.Elapsed.TotalSeconds;
                    if (stage == 0 && elapsed > .5)
                    {
                        controller.Window.Duration = 2;
                        controller.Start();
                        Assert(controller.Phase == TimerPhase.Running && !controller.Window.IsVisible, "启动后隐藏");
                        log.Add("PASS 启动后窗口隐藏");
                        stage++;
                    }
                    else if (stage == 1 && elapsed > 3)
                    {
                        Assert(controller.IsThrowing && controller.Window.IsVisible && controller.Window.AlarmMode, "到期提醒");
                        log.Add("PASS 到期自动显示番茄与动画");
                        stage++;
                    }
                    else if (stage == 2 && elapsed > 7)
                    {
                        Assert(controller.Surface.Frames > 20, "动画渲染");
                        log.Add("PASS 实际动画帧=" + controller.Surface.Frames + "，平均帧间隔=" + controller.Surface.MeanFrameMs.ToString("F2") + " ms，粒子峰值=" + controller.Surface.PeakParticles + "，渲染层级=" + controller.Surface.RenderTier + "，最大帧间隔=" + controller.Surface.MaxFrameMs.ToString("F2") + " ms，每帧应用更新=" + (controller.Surface.UpdateMs / controller.Surface.Frames).ToString("F3") + " ms");
                        controller.StopAlarmForDrag();
                        Assert(!controller.IsThrowing && controller.Phase == TimerPhase.Editing, "拖动停止");
                        log.Add("PASS 拖动停止路径与动画回收");
                        controller.Preview();
                        stage++;
                    }
                    else if (stage == 3 && elapsed > 16)
                    {
                        Assert(!controller.IsThrowing, "预览自动停止");
                        log.Add("PASS 8 秒预览自动结束");
                        controller.Window.Duration = 1500;
                        controller.Start();
                        controller.Cancel();
                        Assert(controller.Window.IsVisible && controller.Phase == TimerPhase.Editing, "托盘取消");
                        log.Add("PASS 取消与重新编辑");
                        timer.Stop();
                        File.WriteAllLines(Output("smoke-results.txt"), log, System.Text.Encoding.UTF8);
                        controller.Quit();
                    }
                }
                catch (Exception ex)
                {
                    SmokeExitCode = 1;
                    timer.Stop();
                    log.Add("FAIL " + ex);
                    File.WriteAllLines(Output("smoke-results.txt"), log, System.Text.Encoding.UTF8);
                    controller.Quit();
                }
            };
            timer.Start();
        }
    }
}
