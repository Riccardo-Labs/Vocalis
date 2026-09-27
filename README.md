# Vocalis

> 🚧 **Still evolving.** Core dictation flow (v1) works end to end and is used daily. Settings, history and a local-LLM refinement pass are planned for v2 — see roadmap below.

A voice dictation app for Windows: click a mouse button, speak, click again, and the transcribed text (Whisper, running locally on GPU) gets pasted into the active app. No audio or text ever leaves the PC, except for the one-time model download.

Personal use, single PC — not meant to be installed by others, but the code is public.

## Current status

- [x] App skeleton, tray icon, single instance
- [x] Microphone recording (NAudio, Whisper-compatible format)
- [x] Global mouse activation (side button, toggle, Esc cancels)
- [x] Transcription with Whisper (GPU via Vulkan, CPU fallback)
- [x] Auto-paste into the active app, with clipboard restore
- [x] On-screen overlay (animated, follows the active monitor)
- [x] Custom icon + self-contained publish
- [ ] Settings (v2)
- [ ] Transcription history (v2)
- [ ] Local-LLM text refinement pass for long dictations (v2)

## How it works

Clicking the mouse side button anywhere on the system starts recording; clicking it again stops and transcribes. Each piece runs independently and only talks to the others through events — nothing reaches across components directly:

- **Activation** — a low-level Win32 mouse/keyboard hook (`WH_MOUSE_LL`/`WH_KEYBOARD_LL`) on its own dedicated thread, so it can never stall the system's mouse. It blocks the side button's default action (no more browser "back") and lets Esc cancel an in-progress recording.
- **Audio** — WASAPI (via NAudio) captures the mic in shared mode, letting Windows resample straight to the 16kHz mono PCM Whisper wants.
- **Transcription** — Whisper.net loads the model once and reuses it; a small pure-logic filter strips the hallucinated text Whisper sometimes produces on silence (`[BLANK_AUDIO]`, stock "subtitles by…" phrases).
- **Paste** — the result is written to the clipboard (marked to skip Windows' clipboard history and cloud sync), pasted via a synthetic `Ctrl+V` (`SendInput`), and the clipboard's previous content is restored right after.
- **Overlay** — a borderless, focus-less WPF window (`WS_EX_NOACTIVATE`) shows the current state (listening / transcribing) without ever stealing keyboard focus from whatever you're typing into.
- **Coordinator** — a single class (`CoordinatoreDettatura`) wires the above through events and drives a small state machine (`Inattivo → Registrazione → Trascrizione → Inattivo`); nothing else in the app manages that transition.

## Stack

.NET 10, WPF (UI), WinForms (tray icon only), NAudio (microphone), Whisper.net (transcription, Vulkan GPU + CPU fallback). SQLite + EF Core are planned for the v2 history feature, not used yet.

## Build & run

```powershell
dotnet build Vocalis.slnx
dotnet run --project Vocalis
dotnet test
```

## Publish (self-contained, no .NET runtime needed on the target machine)

```powershell
dotnet publish Vocalis -c Release -r win-x64 --self-contained
```
