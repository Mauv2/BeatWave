"""Build the full Stage2 chart/route from Unity-imported PCM (numpy/scipy).

Export with BeatWave/Stage2/Inspect Music and Route before running this script.
The recording is never sped up; all coordinates are integrated on its timeline.
"""
import json
from pathlib import Path
import numpy as np
from scipy import signal, ndimage

ROOT = Path(__file__).resolve().parents[1]
audio = np.fromfile(ROOT / 'Temp/Stage2-imported-audio.f32', dtype='<f4')
rate = int((ROOT / 'Temp/Stage2-imported-audio-rate.txt').read_text())
duration = len(audio) / rate
mono = signal.resample_poly(audio, 1, 4)
_, times, spectrum = signal.stft(mono, rate / 4, nperseg=1024, noverlap=896)
flux = np.maximum(0, np.diff(np.log1p(abs(spectrum) * 100), axis=1)).sum(axis=0)
times = times[1:]
flux = np.maximum(0, flux - signal.savgol_filter(flux, 87, 2))
steady = (times > 31) & (times < 160)
# Autocorrelation gives a ~129 BPM candidate; refine its phase over the steady song.
candidates = np.arange(127., 133., .005)
strength = [abs(np.sum(flux[steady] * np.exp(2j*np.pi*times[steady]*b/60))) for b in candidates]
bpm = float(candidates[np.argmax(strength)])
period = 60 / bpm
phase = float(np.angle(np.sum(flux[steady]*np.exp(2j*np.pi*times[steady]/period))) * period/(2*np.pi))
beat = lambda time: phase + round((time-phase)/period)*period

boundaries = [0., beat(16), beat(31), beat(47), beat(78), beat(108), beat(124), beat(160), duration]
names = ['Moonlit approach', 'Quiet nave', 'Ascending arcade', 'First chase',
         'Broken gallery', 'Gathering lights', 'Final chase', 'Starlit coda']
sections = [dict(name=name, start=round(boundaries[i], 6), end=round(boundaries[i+1], 6),
                 background=[0, 1, 2, 2, 1, 0, 3, 3][i]) for i, name in enumerate(names)]
sample_times = np.r_[np.arange(0, duration, .05), duration]
window = int(rate * 1.5)
power = np.r_[0., np.cumsum(audio.astype(np.float64)**2)]
left = np.maximum(0, (sample_times*rate).astype(int)-window)
right = np.minimum(len(audio), (sample_times*rate).astype(int)+window+1)
rms = np.sqrt((power[right]-power[left])/np.maximum(1, right-left))
low, high = np.percentile(rms, [15, 90])
energy = np.clip((rms-low)/max(high-low, 1e-6), 0, 1)
energy = ndimage.gaussian_filter1d(energy, 14)
base_speeds = [5.2, 4.5, 6.0, 7.4, 5.9, 6.6, 7.9, 5.0]
section_index = np.minimum(7, np.searchsorted(boundaries[1:], sample_times, side='right'))
target = np.array(base_speeds)[section_index] * (.96 + .08*energy)
speed = ndimage.gaussian_filter1d(target, 24, mode='nearest')
outro = np.clip((sample_times-(duration-6))/6, 0, 1)
speed = speed*(1-outro*outro) + 2.2*outro*outro
x = -4.7 + np.r_[0, np.cumsum((speed[:-1]+speed[1:])*.5*np.diff(sample_times))]

targets = [11.2,34,39,44,50,53,56,62,66,70,74,83,92,100,110,118,127,131,135,139,145,150,155,160]
levels = [-3.176,-2.5,-1.5,-.8,-.8,-.1,.6,.6,-.2,-1,-1,-2,-2.6,-2.6,-1.4,-.2,.4,1.2,1.2,.5,-.3,-1,-1.8,-2.4]
jumps = []
ground = -3.176
for i, (target_time, level) in enumerate(zip(targets, levels)):
    start = beat(target_time)
    end = start + 2*period
    jumps.append(dict(start=round(start, 6), end=round(end, 6), fromY=ground, toY=level,
                      height=round(1.5 + min(1, abs(level-ground))*.45 + (.2 if i%3==0 else 0), 3)))
    ground = level
y = np.full_like(sample_times, -3.176)
for jump in jumps:
    y[sample_times >= jump['end']] = jump['toY']
    mask = (sample_times >= jump['start']) & (sample_times < jump['end'])
    u = (sample_times[mask]-jump['start'])/(jump['end']-jump['start'])
    y[mask] = jump['fromY']*(1-u) + jump['toY']*u + 4*jump['height']*u*(1-u)
frames = [dict(time=round(float(t), 6), x=round(float(px), 6), y=round(float(py), 6),
               speed=round(float(v), 5), bpm=round(bpm, 3), beat=round(float((t-phase)/period), 6),
               energy=round(float(e), 5)) for t,px,py,v,e in zip(sample_times,x,y,speed,energy)]

peaks, _ = signal.find_peaks(flux, distance=8, prominence=.35)
def attack(predicted, tolerance=.075):
    nearby = peaks[abs(times[peaks]-predicted) < tolerance]
    if not len(nearby): return None
    k = nearby[np.argmax(flux[nearby]*np.exp(-.5*((times[nearby]-predicted)/.035)**2))]
    return round(float(times[k]-.005), 6)

notes = []
lanes = [0,1,0,0,1,0,1,1]
for i, predicted in enumerate(np.arange(phase, duration, period)):
    if predicted < 1.2 or predicted > duration-3.5: continue
    section = min(7, int(np.searchsorted(boundaries[1:], predicted, side='right')))
    every = [2, 4, 2, 1, 2, 1, 1, 2][section]
    if i % every != 0: continue
    actual = attack(predicted)
    if actual is None or (notes and actual-notes[-1]['createTime']<.27): continue
    notes.append(dict(noteType=lanes[(i//every)%8], noteKind=0, createTime=actual,
                      holdTime=0., needHitCount=0, mashLimitTime=0.))

for i, target_time in enumerate([5.5,18,23,32,41,58,68,80,88,96,103,113,120,129,137,147,157,163]):
    start = attack(beat(target_time)); end = attack(beat(target_time)+2*period)
    if start is None or end is None: continue
    if any(start-.2 < j['end'] and end+.2 > j['start'] for j in jumps): continue
    notes = [n for n in notes if not start-.22 < n['createTime'] < end+.25]
    notes.append(dict(noteType=i%2, noteKind=1, createTime=start, holdTime=round(end-start, 6), needHitCount=0, mashLimitTime=0.))

# Retain a short Mash interaction in the sparse early passage, using the shared judge.
mash_time = attack(beat(27.4))
if mash_time is not None:
    notes = [n for n in notes if abs(n['createTime']-mash_time)>.65]
    notes.append(dict(noteType=0, noteKind=2, createTime=mash_time, holdTime=0., needHitCount=5, mashLimitTime=2.))
notes.sort(key=lambda n:n['createTime'])
out = ROOT/'Assets/Stage2/Data'; out.mkdir(parents=True, exist_ok=True)
(out/'Stage2Route.json').write_text(json.dumps(dict(duration=duration, referenceBpm=bpm, phase=phase,
    frames=frames, jumps=jumps, sections=sections), separators=(',', ':'))+'\n', encoding='utf-8')
(out/'Stage2Notes.json').write_text(json.dumps(notes, indent=2)+'\n', encoding='utf-8')
summary = dict(duration=duration, analysisPulseBpm=bpm, phase=phase, speedRange=[float(min(speed)),float(max(speed))],
               distance=float(x[-1]-x[0]), notes=len(notes), holds=sum(n['noteKind']==1 for n in notes),
               mash=sum(n['noteKind']==2 for n in notes), jumps=len(jumps), sections=sections)
(ROOT/'Temp/Stage2-music-summary.json').write_text(json.dumps(summary, indent=2)+'\n', encoding='utf-8')
print(json.dumps(summary, indent=2))
