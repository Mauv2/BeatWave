# Shared Mash migration

The existing Stage3MashGate script was moved to `Assets/Script/MashNote.cs` and renamed `MashNote`. Its Unity script GUID remains `fb0743731d11aae45936ee2f44b97a75`, preserving the three Stage3 scene component references. The temporary MashNoteController base class and Stage3 wrapper were removed.

The original Stage3 behavior is shared: music continues, W/S presses remain accepted after the target hit count, each press emits the existing gold/cyan particle effect and visual pulse, and the fixed release time reports one Perfect/Miss. Stage3's three authored presentations and transition references remain stage-specific. The old JudgeManager flow-pausing Mash branch was replaced with delegation to this script. Tap and Hold judgment rules are unchanged.

Stage1's current chart contains 95 Tap and 7 Hold notes, with no Mash. Its chart and timestamps need no adjustment. Future Mash entries use the shared implementation through the existing NoteMapPlacer/JudgeManager registration.

Stage2's one Mash uses its existing time and count: 27.359716–29.359716 seconds, 5 required hits. The next Tap is at 29.681711 seconds (0.321995 seconds after release; its early judgment window opens 0.201995 seconds after release). No other note or Hold crosses the reserved Mash interval, so no note timestamps were shifted.

Stage2 traversal holds at the gate during this interval while the song continues. A smooth spatial recovery rejoins the original route at 32.841732 seconds, 0.5 seconds before the first jump. Ground is sampled from the same original spatial path; BPM, beat, energy and judgment times remain on the song clock. Gate/camera release follows the Mash end time. Recovery temporarily increases traversal speed locally; later movement and all jump times retain their original timing.

Timing audit: `Reports/Shared-mash-timing-review.txt`.
Playback checks: `Reports/Stage2-causeway-validation.txt`, `Reports/Stage3-fracture-validation.txt`.
These use synthetic judgment API input, not physical keyboard input.

## Verification

- Stage2: full 170.064-second playback, 234 judgments with zero misses, 15 Hold completions and 21 jumps. The shared Mash continued past its hit threshold, generated an impact for every press, held traversal while audio advanced, and resolved once. All 231 sampled note approaches were visible; Hold alignment error was at most 0.0180m. Seven delayed editor callbacks adjusted synthetic input timestamps only.
- Stage3 first Mash: success and failure, pause/resume, repeated input, original fracture visuals and the first three post-landing Tap notes passed. A first-press visual assertion failed on the initial run; rerunning with elapsed-time diagnostics passed without changing the presentation logic. The assertion samples a very short pulse and is sensitive to editor-frame timing.
- Stage3 ASCEND and LAST WISH: each accepted 25 presses, spawned 25 impacts, resolved successfully at its original release time, and did not score twice when resolve was called again. See `Reports/Shared-mash-stage3-gates.txt`.
- Compilation/runtime console reported no errors after the final checks.
- The broader pre-existing Stage3 scene audit still reports two unrelated UI checks: strict Stage1 PauseMenu equality and an expected ResumeGame button binding. Both saved Stage1 and Stage3 scenes currently bind ResumeButton to PauseAudioSettingsUI.OpenAudioSettings; this migration did not change the menu. Pause/resume behavior itself passed the runtime Mash check.
- The editor's unsaved Stage3 scene was retained; validation did not save or replace its pending edits.
