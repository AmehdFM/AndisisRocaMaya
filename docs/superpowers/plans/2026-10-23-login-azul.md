# Login azul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the empty WPF main window with a polished blue login screen.

**Architecture:** Keep the view self-contained in `MainWindow.xaml` using WPF controls, shapes, and local styles. The login is a visual-only screen; no credential validation or data connection is added.

**Tech Stack:** WPF, .NET 8, XAML.

**Spec:** `docs/superpowers/specs/2026-10-23-login-azul-design.md`

## Global Constraints

- Use the existing WPF and .NET 8 project.
- Keep `MainWindow.xaml` as the entry view.
- Add no package or external image dependencies.
- Present the login only; authentication is out of scope.

## Review Focus

- At the existing 800 × 450 window size, the card and controls remain visible and centered.
- Username and password labels remain clear against the blue palette.
- Password input uses a password control rather than plain text.
- Decorative elements do not intercept input.
- The layout remains usable when resized moderately.

---

### Task 1: Build the login view

**Files:**
- Modify: `SistemaRocaMaya/MainWindow.xaml`

**Interfaces:**
- Consumes: existing WPF window and project resources.
- Produces: centered blue login card with username, password, and a primary sign-in button.

- [x] Replace the empty root grid with the approved visual composition: blue background, subtle graphic accent, centered light card, title/supporting copy, labeled username and password inputs, primary button, and restrained secondary footer text.
- [x] Use local XAML styles and controls only; ensure decorative graphics do not capture pointer input and maintain readable contrast.
- [x] Review the resulting XAML against the design spec and confirm the project’s WPF markup compiles.

**Execution notes:** The first visual check showed the card clipped at 800 × 450, so vertical spacing was reduced and the preview was checked again with all controls visible. Review feedback prompted a visible keyboard-focus border on the primary button and darker small text. Final command: `dotnet build .\SistemaRocaMaya\SistemaRocaMaya.csproj --no-restore` — succeeded with 0 errors and 2 NU1701 compatibility warnings for the existing `SkiaSharp.Views.WPF` dependency. The last requested screenshot refresh was stopped by the user pressing Escape.
