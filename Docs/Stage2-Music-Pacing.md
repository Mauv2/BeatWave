# Stage2: full-song pacing

Stage2 now uses the full 170.064-second Unity-imported recording of `별빛 술래잡기 (off vocal).mp3`.
The measured pulse is approximately 129.985 BPM. The recording's pitch and playback rate are unchanged.

## Musical progression

| Song time | Section | Target travel speed before energy adjustment |
| --- | --- | --- |
| 0–15.8s | Moonlit approach | 5.2 |
| 15.8–31.0s | Quiet nave | 4.5 |
| 31.0–47.2s | Ascending arcade | 6.0 |
| 47.2–78.1s | First chase | 7.4 |
| 78.1–108.1s | Broken gallery | 5.9 |
| 108.1–123.8s | Gathering lights | 6.6 |
| 123.8–159.8s | Final chase | 7.9 |
| 159.8–170.064s | Starlit coda | 5.0, easing to 2.2 |

Speed follows phrase energy with smooth transitions, reaching roughly 8.22 units/second in the finale.
Distance is approximately 1,104 world units. The route contains 24 two-beat jumps and uses the existing Stage2 gothic artwork.
The chart contains 233 notes: 216 Tap, 16 Hold, and one Mash in the early quiet passage.
The shared Mash behavior temporarily pauses the music and resumes its original song time.

## Scope and bindings

- `Stage2NoteMapPlacer` creates notes on the same sampled route used by the player. The legacy `NoteMapPlacer` component remains in the Stage2 scene but is disabled. Its source code is unchanged by this task.
- `Stage2MusicRoute` controls Stage2's route, run animation cadence, camera framing, and background placement. It does not change the shared music clock or judge.
- `Stage2Tempo.controller` is a separate copy with a speed multiplier on the run state only.
- The only shared path addition is `RhythmPath.FromSamples`, an additive constructor for a supplied 10ms trajectory. Existing `RhythmPath.Build` behavior is unchanged.
- Existing prototype art is retained under the inactive `Stage2 Original Layout (archived)` root. The generated level is under `Stage2 Music Stage`.
- Existing sound assignments and HUD/pause-menu bindings are retained. The route keeps running through the music's outro before the shared result transition.

## Rebuilding and validation

1. Open `Assets/Scenes/Stage2.unity` in edit mode.
2. To reanalyse changed music, run `BeatWave/Stage2/Inspect Music and Route`, then `Tools/build_stage2_music.py` with NumPy and SciPy available.
3. Run `BeatWave/Stage2/Apply Full Music Stage` to connect the generated data and rebuild the Stage2 terrain.
4. Run `BeatWave/Stage2/Validate Music Layout` or `BeatWave/Stage2/Validate Full Music Playback`.

The layout validation checks chart overlap, Hold/jump separation, full ground coverage, music length, speed range, and scene bindings.
Full playback uses synthetic presses through the shared judge API; held-key input is not injected, and Hold completion is invoked at the authored tail time.
If an editor callback is delayed past the input window, the harness temporarily adjusts only its synthetic input timestamp and records the count. The production music clock and input settings are not changed.
It checks every note position, Hold body alignment, pause/resume, approach visibility, all jumps, and the final result after the full song.
Results are written to `Reports/Stage2-music-validation.txt`, with section screenshots under `Docs/Preview/Stage2-Music`.

The completed full-song run passed: 233 judged notes with zero synthetic-input misses, all 16 Hold tails and 24 jumps, pause/resume, and the result transition after 170.064 seconds. All 231 sampled note approaches were visible. Three delayed editor callbacks required the synthetic timestamp adjustment described above. The Unity console contained no errors after the run. Physical key handling and subjective chart feel still require a manual playthrough.

## Archived ground revision

The generated stage now uses the archived original ground sprites: the stone platform at the opening, broken stone bridges in the middle, and candlelit platforms at the end. Source renderer settings and collider-top offsets are reused; the old individual-art bridge/landmark generation is replaced. Ground segments are fitted to the existing 170-second music route. The archive remains the source reference.

After this geometry revision, layout validation passed for the entire route (maximum ground-height mismatch 0.00046 units), and editor camera previews were checked at the opening, 60s, and 140s. The earlier full playback result above predates this geometry revision. The previously reported Hold effect shader error is a separate unresolved issue; this revision does not claim a clean full playthrough.

## Original-layout restoration (supersedes the archived-ground revision)

The archived original root is now ACTIVE at its existing positions/scales. The generated `Stage2 Music Stage` is inactive. `Stage2 Original Map Extension` contains two translated copies of the complete original layout, with boundary heights aligned, and the active chart root. Small original platform colliders now use ground layer 6.

`Stage2OriginalRoute.json` follows the original ground surfaces and gaps over the existing 170.064-second horizontal music timeline. Original trigger locations are used where they do not overlap required gap jumps; jump trajectories are recalculated for the current speed. There are 39 jumps. `Stage2OriginalNotes.json` contains 234 notes: one Hold crossing a jump was converted to head/tail taps. This is a continuation of the original layout, not bespoke new arrangements for the two added copies.

Validation: 17,008 path samples remain finite and supported by terrain or gap jumps; time/x advance monotonically; jump intervals do not overlap; retained Holds avoid jumps. Editor previews were inspected. A new full manual playback has not been completed, and the reported Hold shader issue remains unresolved.

The extension root was subsequently merged into `Stage2 Original Layout (archived)`: both continuation groups and `Music notes` are direct children of the active original root. The empty `Stage2 Original Map Extension` root was removed. All 99 descendant world positions and route/chart/note-root references were checked during the merge. The expansion tool also detects continuations inside the merged root to prevent duplicate generation.
