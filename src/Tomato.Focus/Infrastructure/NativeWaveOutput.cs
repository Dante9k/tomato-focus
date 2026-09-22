using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Tomato
{
    // Three reusable 10ms stereo buffers; the UI only adds voices to the bounded mixer.
    // WaveOut owns each prepared buffer until DONE/reset. Only the audio thread releases it.
    public sealed class NativeWaveOutput : IDisposable
    {
        readonly EffectMixer mixer;
        readonly AutoResetEvent wake = new AutoResetEvent(false);
        readonly Thread thread;
        readonly uint deviceId;
        int stopping;
        volatile bool ready, failed, stopped;
        public bool Ready
        {
            get
            {
                return ready;
            }
        }

        public bool Failed
        {
            get
            {
                return failed;
            }
        }

        public bool Stopped
        {
            get
            {
                return stopped;
            }
        }

        public NativeWaveOutput(EffectMixer mixer) : this(mixer, uint.MaxValue)
        {
        }

        public NativeWaveOutput(EffectMixer mixer, uint deviceId)
        {
            this.mixer = mixer;
            this.deviceId = deviceId;
            thread = new Thread(Run)
            {
                IsBackground = true,
                Name = "Tomato effects audio"
            };
            thread.Start();
        }

        void Run()
        {
            IntPtr device = IntPtr.Zero;
            var buffers = new Buffer[3];
            try
            {
                var format = new WaveFormat
                {
                    FormatTag = 1,
                    Channels = 2,
                    SamplesPerSecond = EffectMixer.SampleRate,
                    BytesPerSecond = EffectMixer.SampleRate * 4,
                    BlockAlign = 4,
                    BitsPerSample = 16
                };
                Check(waveOutOpen(out device, deviceId, ref format, wake.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, 0x00050000));
                for (int i = 0; i < buffers.Length; i++)
                    buffers[i] = new Buffer(device);
                ready = true;
                while (Interlocked.CompareExchange(ref stopping, 0, 0) == 0)
                {
                    foreach (var buffer in buffers)
                    {
                        if (Interlocked.CompareExchange(ref stopping, 0, 0) != 0)
                            break;
                        if (buffer.Queued && !buffer.Done)
                            continue;
                        mixer.Render(buffer.Samples, ThrowFeedback.Now());
                        buffer.Submit(device);
                    }

                    wake.WaitOne(20);
                }
            }
            catch (Exception ex)
            {
                failed = true;
                // An unavailable device is nonfatal; effects fail silent, animation keeps running.
                if (deviceId == uint.MaxValue)
                    AppController.Log(ex);
            }
            finally
            {
                ready = false;
                if (device != IntPtr.Zero)
                {
                    uint reset = waveOutReset(device);
                    if (reset != 0)
                        AppController.Log(new InvalidOperationException("Effects audio reset: " + reset));
                    foreach (var buffer in buffers)
                        if (buffer != null)
                            buffer.Release(device);
                    uint close = waveOutClose(device);
                    if (close != 0)
                        AppController.Log(new InvalidOperationException("Effects audio close: " + close));
                }

                wake.Dispose();
                stopped = true;
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref stopping, 1) != 0)
                return;
            try
            {
                wake.Set();
            }
            catch (ObjectDisposedException)
            {
            }

            // Driver calls normally finish immediately. Never hold the UI indefinitely on a broken driver.
            thread.Join(200);
        }

        static void Check(uint result)
        {
            if (result != 0)
                throw new InvalidOperationException("Effects audio device returned " + result);
        }

        sealed class Buffer
        {
            public readonly short[] Samples = new short[EffectMixer.FramesPerBlock * 2];
            IntPtr data, header;
            bool prepared;
            public bool Queued;
            static readonly uint HeaderSize = (uint)Marshal.SizeOf(typeof(WaveHeader));
            static readonly int FlagsOffset = (int)Marshal.OffsetOf(typeof(WaveHeader), "Flags");
            public bool Done
            {
                get
                {
                    return (Marshal.ReadInt32(header, FlagsOffset) & 1) != 0;
                }
            }

            public Buffer(IntPtr device)
            {
                try
                {
                    data = Marshal.AllocHGlobal(Samples.Length * 2);
                    header = Marshal.AllocHGlobal((int)HeaderSize);
                    Marshal.StructureToPtr(new WaveHeader { Data = data, BufferLength = (uint)(Samples.Length * 2) }, header, false);
                    Check(waveOutPrepareHeader(device, header, HeaderSize));
                    prepared = true;
                }
                catch
                {
                    if (header != IntPtr.Zero)
                        Marshal.FreeHGlobal(header);
                    if (data != IntPtr.Zero)
                        Marshal.FreeHGlobal(data);
                    throw;
                }
            }

            public void Submit(IntPtr device)
            {
                Marshal.Copy(Samples, 0, data, Samples.Length);
                Check(waveOutWrite(device, header, HeaderSize));
                Queued = true;
            }

            public void Release(IntPtr device)
            {
                uint result = 0;
                for (int attempt = 0; prepared && attempt < 4; attempt++)
                {
                    result = waveOutUnprepareHeader(device, header, HeaderSize);
                    if (result == 0)
                    {
                        prepared = false;
                        break;
                    }

                    if (result != 33)
                        break; // WAVERR_STILLPLAYING
                    Thread.Sleep(10);
                }

                if (prepared)
                {
                    // Do not free memory while a faulty driver still owns it.
                    AppController.Log(new InvalidOperationException("Effects audio buffer still owned by driver: " + result));
                    return;
                }

                Marshal.FreeHGlobal(header);
                Marshal.FreeHGlobal(data);
                header = data = IntPtr.Zero;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        struct WaveFormat
        {
            public ushort FormatTag, Channels;
            public uint SamplesPerSecond, BytesPerSecond;
            public ushort BlockAlign, BitsPerSample, ExtraSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct WaveHeader
        {
            public IntPtr Data;
            public uint BufferLength, BytesRecorded;
            public UIntPtr User;
            public uint Flags, Loops;
            public IntPtr Next;
            public UIntPtr Reserved;
        }

        [DllImport("winmm.dll")]
        static extern uint waveOutOpen(out IntPtr output, uint deviceId, ref WaveFormat format, IntPtr callback, IntPtr instance, uint flags);
        [DllImport("winmm.dll")]
        static extern uint waveOutPrepareHeader(IntPtr output, IntPtr header, uint size);
        [DllImport("winmm.dll")]
        static extern uint waveOutWrite(IntPtr output, IntPtr header, uint size);
        [DllImport("winmm.dll")]
        static extern uint waveOutReset(IntPtr output);
        [DllImport("winmm.dll")]
        static extern uint waveOutUnprepareHeader(IntPtr output, IntPtr header, uint size);
        [DllImport("winmm.dll")]
        static extern uint waveOutClose(IntPtr output);
    }
}
