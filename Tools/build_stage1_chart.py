"""Rebuild Stage1 from Unity's imported PCM, exported by Inspect Rhythm Layout.

Requires numpy/scipy. The imported PCM is intentional: MP3 encoder padding differs
between Unity's importer and external gapless decoders by tens of milliseconds.
The music has swing and expressive timing; a single rigid BPM grid is not enough.
"""
import json
from pathlib import Path
import numpy as np
from scipy import signal

ROOT = Path(__file__).resolve().parents[1]
audio = np.fromfile(ROOT / 'Temp/Stage1-imported-audio.f32', dtype='<f4')
rate = int((ROOT / 'Temp/Stage1-imported-audio-rate.txt').read_text())
mono = signal.resample_poly(audio, 1, 4)
_, times, spectrum = signal.stft(mono, rate / 4, nperseg=512, noverlap=448)
flux = np.maximum(0, np.diff(np.log1p(abs(spectrum) * 100), axis=1)).sum(axis=0)
times = times[1:]
peaks, _ = signal.find_peaks(flux, distance=int(.09 * rate / 4 / 64), prominence=.6)
period = 60 / 138.5  # analysis pulse, not a claim that the performance has a fixed tempo
centers = np.arange(0, 103, 3.)
phases = [np.angle(np.sum(flux * np.exp(-.5 * ((times-c)/5)**2) *
                         np.exp(1j * times / period * 2*np.pi))) for c in centers]
offsets = np.unwrap(phases) / (2*np.pi) * period
grid = np.arange(235) * period + offsets[0]
grid += np.interp(grid, centers, offsets) - offsets[0]
beats = []
for predicted in grid:
    nearby = peaks[abs(times[peaks] - predicted) < .12]
    if not len(nearby):
        continue  # do not create inputs during a rest just to fill a grid
    score = flux[nearby] * np.exp(-.5 * ((times[nearby]-predicted)/.045)**2)
    k = nearby[np.argmax(score)]
    actual = float(times[k] - .005)  # flux window centre to attack compensation
    if .2 < actual < 99.3:
        beats.append(dict(time=round(actual,6), strength=round(float(flux[k]),3)))

def attack(target):
    return min(beats, key=lambda b: abs(b['time']-target))['time']

# Phrase-dependent density. The opening follows the clear piano attacks, while
# the fuller arrangement adds responses. No regular notes cross the fixed event.
selected = []
for b in beats:
    t = b['time']
    if t < 1.1 or 38.3 < t < 50.2 or t > 99.0:
        continue
    if t < 12.0:
        use = abs((t-.3433) / (60/69.3) - round((t-.3433)/(60/69.3))) < .13
    elif t < 16.0:
        use = b['strength'] > 5
    elif t < 38.3:
        use = b['strength'] > 6.5
    elif t < 55:
        use = b['strength'] > 7
    elif t < 75:
        use = b['strength'] > 9
    else:
        use = b['strength'] > 8
    if use and (not selected or t-selected[-1]['time'] >= .30):
        selected.append(b)

holds = [(13.89,15.64),(19.95,20.72),(65.05,65.916),(51.108,52.838),
         (63.31,64.18),(67.26,68.01),(79.2,80.05),(83.98,84.85)]
holds = [(attack(a),attack(b)) for a,b in holds]
notes=[]
pattern=[0,1,0,0,1,0,1,1]
for i,b in enumerate(selected):
    t=b['time']
    if any(a+.01 < t < end+.24 for a,end in holds):
        continue
    notes.append(dict(noteType=pattern[i%len(pattern)],noteKind=0,createTime=t,
                      holdTime=0.,needHitCount=0,mashLimitTime=0.))
for i,(start,end) in enumerate(holds):
    notes=[n for n in notes if abs(n['createTime']-start)>.24]
    notes.append(dict(noteType=i%2,noteKind=1,createTime=start,
                      holdTime=round(end-start,6),needHitCount=0,mashLimitTime=0.))
notes.sort(key=lambda n:n['createTime'])
events=[attack(t) for t in [40.734,41.594,42.453,43.318,45.048,46.783,47.654,48.513]]
config=dict(duration=len(audio)/rate,eventEntry=attack(38.633),
            caveResume=attack(49.378),eventNotes=[dict(showTime=round(t-.68,6),
            judgeTime=t,noteKind=0,holdTime=0.) for t in events],
            analysisPulseBpm=138.5,regularNotes=len(notes),holds=sum(n['noteKind']==1 for n in notes),
            attackTimes=[b['time'] for b in beats])
output=ROOT/'Assets/notedata JSON';output.mkdir(exist_ok=True)
(output/'Stage1Notes.json').write_text(json.dumps(notes,indent=2)+'\n',encoding='utf-8')
(output/'Stage1Timing.json').write_text(json.dumps(config,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in config.items() if k!='attackTimes'},indent=2))
