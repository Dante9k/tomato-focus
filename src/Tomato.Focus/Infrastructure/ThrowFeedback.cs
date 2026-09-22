using System;
using System.Diagnostics;
using System.IO;

namespace Tomato
{
    public sealed class ThrowFeedback : IDisposable
    {
        readonly float[][] launches = new float[3][], impacts = new float[3][];
        readonly EffectMixer mixer = new EffectMixer();
        NativeWaveOutput output;
        int launchIndex, impactIndex;
        public bool IsActive
        {
            get
            {
                return output != null;
            }
        }

        public bool AudioReady
        {
            get
            {
                return output != null && output.Ready;
            }
        }

        public ThrowFeedback()
        {
            for (int i = 0; i < 3; i++)
            {
                launches[i] = CreateSound(FlightSoundKind.Launch, i);
                impacts[i] = CreateSound(FlightSoundKind.Impact, i);
            }
        }

        public void Start()
        {
            Stop();
            output = new NativeWaveOutput(mixer);
        }

        public void Play(FlightSoundEventArgs cue)
        {
            if (output == null || output.Failed)
                return;
            bool impact = cue.Kind == FlightSoundKind.Impact;
            var sample = impact ? impacts[impactIndex++ % 3] : launches[launchIndex++ % 3];
            impactIndex %= 3;
            launchIndex %= 3;
            mixer.Add(sample, GainFor(cue), cue.Pan, impact, Now());
        }

        public static double GainFor(FlightSoundEventArgs cue)
        {
            double weight = Math.Max(.5, Math.Min(1, cue.Size / 100));
            double strength = Math.Max(.2, Math.Min(1, cue.Speed / 1050));
            return cue.Kind == FlightSoundKind.Impact ? (.22 + .25 * strength) * weight * (cue.Settling ? .35 : 1) : (.14 + .10 * strength) * weight;
        }

        internal static double Now()
        {
            return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        }

        public void Stop()
        {
            if (output != null)
                output.Dispose();
            output = null;
            mixer.Clear();
        }

        public void Dispose()
        {
            Stop();
        }

        // Preloaded CC0 soft-contact samples plus a short original air gesture. No tonal impact oscillator.
        // Resources are decoded once at startup, never on a collision.
        public static float[] CreateSound(FlightSoundKind kind, int variant)
        {
            if (variant < 0 || variant > 2)
                throw new ArgumentOutOfRangeException("variant");
            if (kind == FlightSoundKind.Impact)
                return LoadImpact(variant);
            double duration = .075 + .005 * variant;
            var samples = new float[(int)(EffectMixer.SampleRate * duration)];
            var random = new Random(1700 + variant);
            double low = 0, medium = 0, previous = 0, highpass = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                double t = i / (double)EffectMixer.SampleRate;
                double noise = random.NextDouble() * 2 - 1;
                low += .085 * (noise - low);
                medium += .24 * (noise - medium);
                double envelope = Math.Pow(Math.Sin(Math.PI * t / duration), 2);
                double sample = ((medium - low) * .25 + low * .04) * envelope;
                highpass = .994 * (highpass + sample - previous);
                previous = sample;
                double fade = Math.Min(1, (samples.Length - 1 - i) / (EffectMixer.SampleRate * .012));
                samples[i] = (float)(highpass * fade);
            }

            return samples;
        }

        static float[] LoadImpact(int variant)
        {
            string name = "Tomato.Audio.impact-soft-" + variant + ".wav";
            using (var stream = typeof(ThrowFeedback).Assembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                    throw new InvalidDataException("Missing embedded audio: " + name);
                using (var reader = new BinaryReader(stream))
                {
                    if (reader.ReadUInt32() != 0x46464952 || reader.ReadUInt32() != stream.Length - 8 || reader.ReadUInt32() != 0x45564157 || reader.ReadUInt32() != 0x20746d66 || reader.ReadInt32() != 16 || reader.ReadInt16() != 1 || reader.ReadInt16() != 1 || reader.ReadInt32() != EffectMixer.SampleRate || reader.ReadInt32() != EffectMixer.SampleRate * 2 || reader.ReadInt16() != 2 || reader.ReadInt16() != 16 || reader.ReadUInt32() != 0x61746164)
                        throw new InvalidDataException("Unexpected audio format: " + name);
                    int bytes = reader.ReadInt32();
                    if (bytes <= 0 || bytes % 2 != 0 || bytes != stream.Length - 44 || bytes > EffectMixer.SampleRate)
                        throw new InvalidDataException("Unexpected audio size: " + name);
                    var samples = new float[bytes / 2];
                    for (int i = 0; i < samples.Length; i++)
                        samples[i] = reader.ReadInt16() / 32768f;
                    return samples;
                }
            }
        }
    }
}
