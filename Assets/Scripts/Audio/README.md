# Audio

Sounds are data: one `SoundEvent` asset per sound in `Assets/Resources/Audio/SoundEvents/`; the file name is the key.

```csharp
AudioManager.Play("brick_pickup");
```

`AudioManager` bootstraps itself before the first scene (no scene wiring) and keeps a pool of 8 sources.

## Change what a sound sounds like
Select the asset (e.g. `ui_click.asset`) and edit its clip list / volume / pitch ranges. No code change. A random clip is picked each play (never the same one twice in a row).

## Add a new sound
1. Create asset: `Assets > Create > Overclocked > Sound Event`, named with the key (e.g. `door_open`), into `Resources/Audio/SoundEvents/`.
2. Assign clips + bus (`Sfx` / `Ui` / `Music`).
3. Call `AudioManager.Play("door_open")`. A missing key logs one warning.

`Tools > Audio > Generate Default Sound Events` (`Assets/Editor/SoundEventGenerator.cs`) creates the initial set from Kenney clips; it never overwrites existing assets. Add rows there for new defaults.

## UI sounds
`UiSounds.Attach(root)` gives every `Button` / `.clickable` element `ui_click` + `ui_hover`. Already attached in `HelpOverlay.Build` and `EndScreenUI`. `Toast.Show` plays `toast_*`. The `ui_open`/`ui_close` assets exist but nothing plays them (modal open/close sounds were too loud); re-enable by calling them from `HelpOverlay.ShowModal`.

## Mixer (optional)
Create `Assets/Resources/Audio/MainMixer.mixer` with groups `Master > Music, SFX, UI`. `AudioManager` routes by each event's bus; without it sounds play ungrouped. Master volume is still `AudioListener.volume` (`GameSettings.cs`).

## Music
`music_main.asset` (bus Music, clip `Assets/Audio/AfternoonPings.ogg`, base volume 0.3) loops from boot on a dedicated source in `AudioManager` and survives scene loads. `AudioManager.PlayMusic("key")` switches tracks (no-op if the clip is already playing). Not yet done: per-level `music` field in level JSON.

## Volume sliders (Settings modal)
`GameSettings.MasterVolume` (-> `AudioListener.volume`), `MusicVolume` (scales the music source) and `SfxVolume` (scales every non-music sound, including UI and the movement hum). Plain code gains, so no exposed mixer parameters are needed.

## Sources
`Assets/Audio/kenney_interface-sounds/` - Kenney Interface Sounds, CC0. `Assets/Audio/kenney_interface-sounds/sound-preview.html` loop-previews the pack in a browser. `Computer.mp3` is the team's own recording.
