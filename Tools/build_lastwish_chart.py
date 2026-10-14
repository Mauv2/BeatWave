"""Rebuild the authored chart for the 168.996712 s original recording.

152 BPM / 14 ms onset phase measured from the original FLAC's spectral flux.
No random notes: phrase motifs follow the arrangement's increasing density.
Run from the repository root with Python 3 (standard library only).
"""
import json
from pathlib import Path

BPM, OFFSET, DURATION = 152, 0.014, 7452755 / 44100
GATES = [(104, 112), (296, 304), (368, 376)]
LEG_ENDS = [16,32,48,60,64,80,96,104,112,120,136,152,168,172,
            184,200,216,232,236,248,264,280,296,304,312,328,340,
            344,360,368,376,380,396,412]
HOLDS = {12,28,44,76,92,132,148,180,212,244,276,288,324,356,388,404}
notes = []
for phrase in range(0, 416, 16):
    if phrase < 80:
        motif = [0,2,4,6,8,10,12,14]
    elif phrase < 168:
        motif = [0,2,3,4,6,8,10,11,12,14,15]
    elif phrase < 304:
        motif = [0,1,2,3,4,6,7,8,9,10,11,12,14,15]
    elif phrase < 368:
        motif = [0,1,2,3,3.5,4,5,6,7,8,9,10,11,11.5,12,13,14,15]
    else:
        motif = [0,4,8,12]
    for j, step in enumerate(motif):
        beat = phrase + step
        if beat < 4 or beat > 408 or any(a-0.5 <= beat <= b+0.5 for a,b in GATES):
            continue
        # The first collapse is a 22 m drop. Let the camera reveal the landing
        # before resuming input two beats after touchdown (beat 120).
        if 112 < beat < 122:
            continue
        # A beat of breathing room around each reversal makes the new direction readable.
        if any(abs(beat-turn)<=.5 for turn in (184,216)):
            continue
        # Leg-contained holds remain straight and aligned with the player's path.
        duration = 1.5 if beat in HOLDS else 0
        if any(beat < boundary < beat+duration for boundary in LEG_ENDS):
            duration = 0
        lane = (j + phrase//16) % 2
        start = OFFSET + beat*60/BPM
        length = duration*60/BPM
        if any(n['lane']==lane and start < n['time']+n['duration']+.18 for n in notes[-6:]):
            continue
        notes.append(dict(time=round(start,6), lane=lane, duration=round(length,6)))

assert all(n['time']+n['duration'] < DURATION for n in notes)
for lane in (0,1):
    ns = [n for n in notes if n['lane']==lane]
    assert all(a['time']+a['duration']+.17 < b['time'] for a,b in zip(ns,ns[1:]))
chart = dict(title='LAST WISH — Original', bpm=BPM, offset=OFFSET, duration=DURATION, notes=notes)
Path('Assets/Stage3/Data/LastWishChart.json').write_text(json.dumps(chart,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print(f'{len(notes)} notes, {sum(n["duration"]>0 for n in notes)} holds, {len(GATES)} mash gates; {DURATION:.6f}s')
