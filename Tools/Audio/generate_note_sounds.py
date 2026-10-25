"""Generate BeatWave's original glass/crystal judgment sounds (no sampled audio)."""
import math
from pathlib import Path
import random
import struct
import wave

RATE = 48000
OUTPUT = Path(__file__).resolve().parents[2] / "Assets/Audio/NoteJudgment"


def render(name, duration, modes, click, peak, seed):
    rng = random.Random(seed)
    samples = []
    previous_noise = 0.0
    for i in range(round(duration * RATE)):
        t = i / RATE
        attack = 1.0 - math.exp(-t / 0.0007)
        # Inharmonic, quickly decaying modes suggest glass without a long melody.
        tone = sum(weight * math.sin(math.tau * frequency * t) * math.exp(-t / decay)
                   for frequency, weight, decay in modes)
        noise = rng.uniform(-1.0, 1.0)
        transient = (noise - previous_noise) * click * math.exp(-t / 0.003)
        previous_noise = noise
        fade = min(1.0, (duration - t) / 0.015)
        samples.append(math.tanh((tone + transient) * attack) * fade)
    scale = peak / max(abs(value) for value in samples)
    pcm = b"".join(struct.pack("<h", round(value * scale * 32767)) for value in samples)
    with wave.open(str(OUTPUT / name), "wb") as file:
        file.setparams((1, 2, RATE, len(samples), "NONE", "not compressed"))
        file.writeframes(pcm)
    rms = math.sqrt(sum((value * scale) ** 2 for value in samples) / len(samples))
    print(f"{name}: {duration:.3f}s, peak={peak:.2f}, RMS={20 * math.log10(rms):.1f}dBFS")


def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    render("BeatWave_Tap.wav", 0.120,
           [(1240, .65, .014), (2180, .23, .022), (3470, .09, .010)], .10, .60, 1)
    render("BeatWave_HoldHead.wav", 0.150,
           [(1040, .65, .018), (1810, .23, .030), (2920, .08, .016)], .065, .55, 2)
    render("BeatWave_HoldEnd.wav", 0.230,
           [(1560, .55, .017), (2670, .25, .045), (4230, .07, .027)], .075, .60, 3)
    render("BeatWave_Mash.wav", 0.075,
           [(930, .7, .009), (1730, .21, .013), (2840, .06, .007)], .12, .42, 4)


if __name__ == "__main__":
    main()
