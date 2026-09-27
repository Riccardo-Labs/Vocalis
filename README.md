# Vocalis

> 🚧 **Work in progress.** Personal project, actively being developed. This README will be updated once the work is done.

A voice dictation app for Windows: click a mouse button, speak, click again, and the transcribed text (Whisper, running locally on GPU) gets pasted into the active app. No audio or text ever leaves the PC, except for the model download.

Personal use, single PC — not meant to be installed by others, but the code is public.

## Current status

- [x] App skeleton, tray icon, single instance
- [x] Microphone recording (NAudio, Whisper-compatible format)
- [x] Global mouse activation (side button, toggle, Esc cancels)
- [ ] Transcription with Whisper (GPU via Vulkan)
- [ ] Auto-paste into the active app
- [ ] On-screen overlay
- [ ] Settings
- [ ] Transcription history
- [ ] Packaging for everyday use

## Stack

.NET 10, WPF (UI), WinForms (tray icon only), NAudio (microphone), Whisper.net (transcription, once it lands), SQLite + EF Core (history, once it lands).

## Build & run

```powershell
dotnet build Vocalis.slnx
dotnet run --project Vocalis
dotnet test
```
