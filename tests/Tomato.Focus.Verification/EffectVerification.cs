using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows;
using Tomato;

namespace Tomato.Tests
{
    internal static class EffectVerification
    {
        static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new Exception(message);
        }

        public static void Run(List<string> log)
        {
            var flight = new Flight
            {
                X = 400,
                Y = 410,
                Size = 80,
                Vy = 500
            };
            int contacts = 0;
            double firstSpeed = 0;
            for (int i = 0; i < 3000; i++)
            {
                bool alive = flight.Step(.001, 1000, 500);
                if (flight.Impacted)
                {
                    contacts++;
                    Assert(Math.Abs(flight.Y + flight.Size * .39 - 492) < .0001, "声音事件必须发生在真实地面接触");
                    if (contacts == 1)
                        firstSpeed = flight.ImpactSpeed;
                    else
                        Assert(flight.ImpactSpeed < firstSpeed && flight.Resting, "第二次触地更轻且进入停留");
                }

                if (!alive)
                    break;
            }

            Assert(contacts == 2 && flight.Bounces == 1, "每次实际触地仅产生一个事件");
            Assert(flight.Resting && flight.RestAge >= .32, "第二次落地不穿过地面并及时回收");
            var cue = new FlightSoundEventArgs(FlightSoundKind.Impact, 0, 1000, 90, 900, false);
            var soft = new FlightSoundEventArgs(FlightSoundKind.Impact, 1000, 1000, 90, 450, true);
            Assert(cue.Pan < 0 && soft.Pan > 0 && ThrowFeedback.GainFor(soft) < ThrowFeedback.GainFor(cue), "位置声像和反弹音量");
            log.Add("PASS 实际落地与二次触地事件、反弹轻重、声像及落地回收");
            foreach (FlightSoundKind kind in new[]
            {
                FlightSoundKind.Launch,
                FlightSoundKind.Impact
            }

            )
                for (int variant = 0; variant < 3; variant++)
                {
                    var samples = ThrowFeedback.CreateSound(kind, variant);
                    Assert(samples.Length <= EffectMixer.SampleRate * (kind == FlightSoundKind.Impact ? .16 : .09), "短促干声，不保留长尾堆叠");
                    double sum = 0, energy = 0;
                    foreach (float sample in samples)
                    {
                        Assert(!float.IsNaN(sample) && !float.IsInfinity(sample) && Math.Abs(sample) < .5, "拟音有限且峰值受控");
                        sum += sample;
                        energy += sample * sample;
                    }

                    Assert(samples[0] == 0 && samples[samples.Length - 1] == 0, "拟音首尾无断点");
                    Assert(Math.Abs(sum / samples.Length) < .002, "无明显直流偏移");
                    Assert(Math.Sqrt(energy / samples.Length) > .003, "拟音有效能量");
                }

            log.Add("PASS 六组投掷与碰撞拟音的能量、首尾、直流与峰值");
            var mixer = new EffectMixer();
            var clip = new float[EffectMixer.FramesPerBlock * 2];
            for (int i = 0; i < clip.Length; i++)
                clip[i] = .5f;
            var block = new short[EffectMixer.FramesPerBlock * 2];
            mixer.Add(clip, .5, -1, false, 0);
            mixer.Render(block, 0);
            Assert(block[0] > 0 && block[1] == 0, "左侧声像");
            short single = block[0];
            mixer.Add(clip, .5, -1, true, .01);
            mixer.Render(block, .01);
            Assert(block[0] > single, "碰撞和飞行混音而非打断");
            mixer.Clear();
            mixer.Add(clip, .5, 1, true, 0);
            mixer.Render(block, 0);
            Assert(block[0] == 0 && block[1] > 0, "右侧声像");
            mixer.Clear();
            for (int i = 0; i < 100; i++)
                mixer.Add(clip, 1, 0, false, 0);
            Assert(mixer.ActiveVoices == EffectMixer.VoiceLimit && mixer.Add(clip, 1, 0, true, 0), "并发受限且优先碰撞");
            mixer.Render(block, 0);
            foreach (short sample in block)
                Assert(Math.Abs((int)sample) < 27852, "高并发限幅不削波");
            mixer.Clear();
            mixer.Render(block, .01);
            foreach (short sample in block)
                Assert(sample == 0, "停止后无残留");
            mixer.Add(clip, 1, 0, true, 0);
            mixer.Render(block, .2);
            Assert(mixer.ActiveVoices == 0 && block[0] == 0, "不补播过期事件");
            using (var unavailable = new NativeWaveOutput(mixer, uint.MaxValue - 1))
            {
                for (int i = 0; i < 200 && !unavailable.Stopped; i++)
                    Thread.Sleep(5);
                Assert(unavailable.Failed && unavailable.Stopped, "无效音频设备安全退出");
            }

            log.Add("PASS 立体声并发混音、16声部上限、碰撞优先、限幅、停止清空、过期丢弃与设备失败降级");
        }

        public static void RenderPreview(string path)
        {
            var mixer = new EffectMixer();
            var surface = new ThrowSurface(Art.TomatoImage(96))
            {
                Origin = new Point(1360, 120)
            };
            var launches = new float[3][];
            var impacts = new float[3][];
            for (int i = 0; i < 3; i++)
            {
                launches[i] = ThrowFeedback.CreateSound(FlightSoundKind.Launch, i);
                impacts[i] = ThrowFeedback.CreateSound(FlightSoundKind.Impact, i);
            }

            double time = 0;
            int sequence = 0;
            surface.SoundCue += delegate (object sender, FlightSoundEventArgs cue)
            {
                bool impact = cue.Kind == FlightSoundKind.Impact;
                mixer.Add((impact ? impacts : launches)[sequence++ % 3], ThrowFeedback.GainFor(cue), cue.Pan, impact, time);
            };
            const int blocks = 650;
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                int bytes = blocks * EffectMixer.FramesPerBlock * 4;
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + bytes);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)2);
                writer.Write(EffectMixer.SampleRate);
                writer.Write(EffectMixer.SampleRate * 4);
                writer.Write((short)4);
                writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(bytes);
                var block = new short[EffectMixer.FramesPerBlock * 2];
                for (int i = 0; i < blocks; i++)
                {
                    time = i * .01;
                    if (i < 600)
                        surface.Advance(.01, 1536, 864);
                    mixer.Render(block, time);
                    foreach (short sample in block)
                        writer.Write(sample);
                }
            }
        }
    }
}
