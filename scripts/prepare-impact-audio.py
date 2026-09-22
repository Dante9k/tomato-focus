"""Offline asset preparation only. Requires numpy and soundfile; not used by the app/build."""
from pathlib import Path
import hashlib
import json
import math
import wave
import numpy as np
import soundfile as sf

ROOT = Path(__file__).resolve().parents[1]
RATE = 44100


def lowpass(samples, cutoff):
    amount = 1 - math.exp(-2 * math.pi * cutoff / RATE)
    state = 0.0
    result = np.empty_like(samples)
    for i, value in enumerate(samples):
        state += amount * (value - state)
        result[i] = state
    return result


records = []
for variant in range(3):
    source = ROOT / f"assets/audio/source/impactSoft_medium_{variant:03d}.ogg"
    samples, rate = sf.read(source, always_2d=True)
    samples = samples.mean(axis=1)
    active = np.flatnonzero(np.abs(samples) > max(.001, np.max(np.abs(samples)) * .025))
    samples = samples[max(0, active[0] - int(rate * .002)):active[-1] + 1]
    # Slightly lighter material; keep the natural irregular contact, without a pitched oscillator.
    speed = (1.06, 1.10, 1.08)[variant]
    positions = np.arange(0, len(samples) - 1, rate * speed / RATE)
    samples = np.interp(positions, np.arange(len(samples)), samples)
    samples = lowpass(samples - lowpass(samples, 120), 4300)
    samples = samples[:int(RATE * .155)]
    samples -= samples.mean()
    attack = min(int(RATE * .0015), len(samples))
    release = min(int(RATE * .014), len(samples))
    samples[:attack] *= np.linspace(0, 1, attack)
    samples[-release:] *= np.linspace(1, 0, release) ** 2
    gain = min(.085 / np.sqrt(np.mean(samples ** 2)), .38 / np.max(np.abs(samples)))
    samples *= gain
    output = ROOT / f"assets/audio/impact-soft-{variant}.wav"
    with wave.open(str(output), "wb") as stream:
        stream.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        stream.writeframes(np.rint(samples * 32767).astype("<i2").tobytes())
    records.append({"source": source.name, "source_sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
                    "output": output.name, "output_sha256": hashlib.sha256(output.read_bytes()).hexdigest(),
                    "seconds": round(len(samples) / RATE, 4), "peak": round(float(np.max(np.abs(samples))), 4)})
(ROOT / "assets/audio/processing.json").write_text(json.dumps(records, indent=2) + "\n", encoding="utf-8")
print(json.dumps(records, indent=2))
