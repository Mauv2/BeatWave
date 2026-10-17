"""Export a smooth Stage1 pulse/dynamics profile from Unity-imported PCM.

Uses the same 138.5 BPM analysis pulse and local phase method as the chart tool.
Does not rewrite note timestamps or change the recording's playback rate.
"""
import json
from pathlib import Path
import numpy as np

root = Path(__file__).resolve().parents[1]
audio = np.fromfile(root / 'Temp/Stage1-imported-audio.f32', dtype='<f4')
rate = int((root / 'Temp/Stage1-imported-audio-rate.txt').read_text())
# Four-sample averaging and a centered Hann STFT need only NumPy.
mono = audio[:len(audio)//4*4].reshape(-1, 4).mean(axis=1)
window = np.hanning(513)[:-1]
frames = np.lib.stride_tricks.sliding_window_view(np.pad(mono, (256, 256)), 512)[::64]
spectrum = np.fft.rfft(frames*window, axis=1)/window.sum()
flux = np.maximum(0, np.diff(np.log1p(abs(spectrum) * 100), axis=0)).sum(axis=1)
times = np.arange(1, len(frames))*64/(rate/4)
bpm = 138.5
period = 60 / bpm
centers = np.arange(0, 105, 3.)
phases = [np.angle(np.sum(flux * np.exp(-.5 * ((times-c)/5)**2) *
                         np.exp(1j * times / period * 2*np.pi))) for c in centers]
offsets = np.unwrap(phases) / (2*np.pi) * period
grid = np.arange(-4, 245) * period + offsets[0]
grid += np.interp(grid, centers, offsets) - offsets[0]
# Phrase energy spans multiple beats, so isolated transients do not jerk the camera.
sample_times = np.arange(0, len(audio)/rate + .25, .25)
rms = np.array([np.sqrt(np.mean(audio[max(0, int((t-1.3)*rate)):
                  min(len(audio), max(1, int((t+1.3)*rate)))]**2)) for t in sample_times])
lo, hi = np.percentile(rms, [15, 90])
energy = np.clip((rms-lo)/max(hi-lo, 1e-6), 0, 1)
phase = np.interp(sample_times, grid, np.arange(len(grid)))
tempo = np.clip(np.gradient(phase, .25)*60, bpm*.88, bpm*1.12)
frames = [dict(time=round(float(t), 6), bpm=round(float(b), 6),
               energy=round(float(e), 6), beat=round(float(p), 6))
          for t, b, e, p in zip(sample_times, tempo, energy, phase)]
out = root/'Assets/notedata JSON/Stage1Tempo.json'
out.write_text(json.dumps(dict(referenceBpm=bpm, duration=len(audio)/rate, frames=frames), indent=2)+'\n', encoding='utf-8')
print(f'{len(frames)} frames; local analysis pulse {min(tempo):.2f}-{max(tempo):.2f} BPM; {out}')
