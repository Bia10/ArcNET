# ArcNET Modular Editor SDK Review and Roadmap

## Executive Summary

ArcNET should be treated and marketed as a pluggable, UI-agnostic editor SDK for Arcanum. The primary product goal is not to become a full replacement engine. The primary goal is to make frontend work trivial enough that a desktop, web, or hybrid shell can deliver a full-blown Arcanum editor without having to solve binary formats, save orchestration, archive handling, round-trip safety, or authoring-domain rules from scratch.

That changes the architectural bar:

- Frontends should consume stable editor-facing sessions, builders, validators, and preview-ready models.
- Low-level formats remain essential, but they are only the foundation.
- Engine-adjacent functionality is justified only when it directly supports authoring workflows.
- Rendering, audio preview, dialog composition, asset browsing, undo/redo, validation, and project/workspace orchestration are core SDK concerns because they remove work from the frontend.

## Product Positioning

### Primary Goal

Provide a modular SDK from which a full Arcanum editor becomes mostly presentation work.

### Non-Goal

Do not pursue a general-purpose Arcanum runtime or full gameplay engine unless a runtime slice is required to support editor workflows directly.

### Engine-Adjacent Capabilities That Do Belong

- Map, tile, object, and art rendering for authoring previews.
- Sound-effect and music preview.
- Dialog graph composition and validation.
- Script and condition/action authoring support.
- Asset reference resolution and dependency inspection.
- Optional simulation helpers only where needed for editor feedback.

## Current State Review

| Area | Status | Assessment |
|---|---|---|
| Binary formats | Strong | `ArcNET.Formats` already provides broad read/write coverage for Arcanum formats and save-related files. |
| Object model | Strong | `ArcNET.GameObjects` gives typed object surfaces instead of raw byte-only editing. |
| Save editing | Strong | `LoadedSave`, `SaveGameLoader`, `SaveGameEditor`, `SaveGameWriter`, and validators already form a credible save-authoring pipeline. |
| Builder APIs | Strong | `CharacterBuilder`, `SectorBuilder`, `ScriptBuilder`, `DialogBuilder`, and `MobDataBuilder` reduce raw property editing. |
| Patch tooling | Strong | `ArcNET.BinaryPatch` and `ArcNET.Patch` already cover structured patching and install-time workflows. |
| Unified editor session | Partial | Frontends can now open loose or install-backed workspaces with optional save composition plus a parsed asset catalog, winning-source provenance, skipped-load diagnostics, and a first workspace index for maps, messages, protos, scripts, dialogs, and art references. |
| Asset graph / dependency model | Partial | `EditorWorkspace.Index` now covers map ownership, message ownership, proto definition lookup, multi-asset script/dialog definition lookup, plus reverse proto/script/art references, but broader cross-format dependency navigation is still missing. |
| Cross-file validation services | Partial | `EditorWorkspace.Validation` now surfaces first-pass workspace findings for missing proto/script definitions, install-aware proto display-name gaps, broken dialog response targets, and unknown script attachment slots, and `arcnet editor validate` can print that report from live installs, but broader edit-time validation and repair flows are still missing. |
| Preview-ready rendering surfaces | Weak | Formats exist, but there is no dedicated rendering abstraction for map/art/model previews. |
| Audio preview surfaces | Weak | There is no SDK-level audio decode/preview service yet. |
| Dialog authoring surface | Partial | File parsing exists, but there is no graph/editor-domain composition API yet. |
| Frontend-facing orchestration | Partial | The solution has good primitives, but not enough high-level session surfaces for frontend authors. |

## Architectural Direction

ArcNET should commit to a layered editor-SDK architecture.

### Layer 1: Binary and Archive Foundation

- `ArcNET.Core`
- `ArcNET.Archive`
- `ArcNET.Formats`
- `ArcNET.GameObjects`

This layer remains responsible for correctness, performance, and round-trip fidelity.

### Layer 2: Workspace and Asset Graph

This layer should become the canonical frontend entry point.

Responsibilities:

- Load a game workspace from loose content, extracted content, and eventually native DAT-backed installs.
- Load an optional save slot into the same session.
- Track asset provenance, overrides, and source locations.
- Resolve references between sectors, mobs, protos, dialogs, scripts, art, sound, and save state.
- Expose asset catalogs and search/index services.

This session adds the first step in that direction: `EditorWorkspace` plus `EditorWorkspaceLoader` in `ArcNET.Editor`, including native install-backed DAT loading, a first workspace asset catalog, skipped-load diagnostics, and an initial map/message/proto/script/art index slice.

### Layer 3: Authoring Services

This layer should hold editor-domain operations rather than UI code.

Responsibilities:

- Strongly typed builders and mutation services.
- Validation and diagnostics.
- Undo/redo-friendly transactional edits.
- Diff and merge helpers.
- Import/export pipelines.
- Authoring-safe transforms for sectors, dialogs, scripts, and object graphs.

### Layer 4: Preview and Media Adapters

This layer should deliberately support frontend work without turning ArcNET into a full engine.

Responsibilities:

- Render-ready map scene projection.
- Art and animation frame projection.
- Model/material preview projection when the format work reaches that point.
- Audio preview services for sound effects and music.
- Palette, lighting, and animation timing helpers.

These should be SDK services and abstractions, not hardwired UI widgets.

### Layer 5: Frontend Shells

Desktop, web, and tooling frontends should live above the SDK and consume the same editor services.

Responsibilities:

- View layout and interaction.
- Docking, tree views, property grids, command routing.
- Input gestures and workflow polish.
- Host-specific rendering and audio backends.

## Roadmap

## Phase 1 - Frontend Entry Point

Goal: make frontend startup and basic binding trivial.

Deliverables:

- Unified workspace/session surface.
- Clear separation between content data, save data, and installation metadata.
- Stable load options and progress reporting.
- First-class documentation and examples for frontend authors.

Committed implementation from this session:

- `EditorWorkspace`
- `EditorWorkspaceLoadOptions`
- `EditorWorkspaceLoader`
- `EditorWorkspaceLoader.LoadFromGameInstall(...)` / `LoadFromGameInstallAsync(...)`

Outcome target:

- A frontend can open one SDK object and immediately access game data plus an optional save slot.

## Phase 2 - Asset Catalog and Reference Graph

Goal: remove manual file-path plumbing from frontend code.

Deliverables:

- Asset catalog service with typed entries.
- Reverse reference and forward reference queries.
- Provenance model for DAT, loose override, extracted, and generated content.
- Search/index APIs for entities, dialogs, scripts, art, and sectors.

Outcome target:

- Frontends navigate assets by meaning, not by ad hoc path conventions.

## Phase 3 - Authoring Domain Services

Goal: move complex editor behavior out of UI shells.

Deliverables:

- Transactional mutation services.
- Undo/redo command model.
- Cross-file validation services.
- Domain-level helpers for map placement, prototype instancing, dialog edits, and script edits.
- Persisted editor-project metadata for open tabs, bookmarks, view state, and custom tool state.

Outcome target:

- The frontend owns interaction; the SDK owns authoring semantics.

## Phase 4 - Preview and Media Support

Goal: remove the need for frontend authors to build rendering/media pipelines from scratch.

Deliverables:

- Map-scene projection APIs for sectors, tiles, roofs, and placed objects.
- Art preview pipeline with palette-aware frame extraction and animation timing.
- Sound preview abstractions.
- Shared camera/viewport math helpers for map editors.
- Optional render-backend adapters rather than hardcoded rendering UI.

Outcome target:

- A frontend can render previews by plugging into ArcNET-provided scene/media services.

## Phase 5 - Composition Editors

Goal: make full editor feature sets practical, not just possible.

Deliverables:

- Dialog graph composition API.
- Script composition and validation helpers.
- Sector and map composition helpers.
- Object palette and brush services.
- Asset browser and prefab-like reusable authoring primitives.

Outcome target:

- Building a full content editor is primarily a UX/design problem, not a reverse-engineering problem.

## Phase 6 - Plugin and Host Model

Goal: allow ecosystem growth without forking the core SDK.

Deliverables:

- Plugin extension points for asset tools, validators, importers, exporters, and previews.
- Capability discovery for frontend hosts.
- Versioned compatibility guidance for third-party editor plugins.

Outcome target:

- ArcNET becomes the common authoring substrate for multiple Arcanum tools.

## Immediate Technical Priorities

| Priority | Area | Why |
|---|---|---|
| P1 | Broaden the reference graph beyond the current map/message/proto/script/art slice | Frontends still need wider dependency navigation and higher-level asset queries beyond the current catalog, provenance, diagnostics, and initial index. |
| P1 | Preview-ready map and art services | A serious editor needs visual feedback without bespoke rendering logic per frontend. |
| P1 | Dialog authoring model | Parsing alone is not enough for a usable editor. |
| P2 | Audio preview APIs | Sound browsing and preview are part of editor completeness. |
| P2 | Transaction and undo model | Essential for frontend simplicity and correctness. |
| P2 | Public API discoverability | `ArcNET.Editor` surfaces must be as visible and documented as the lower-level packages. |
| P3 | Plugin model | Important, but only after the core editor-domain seams are stable. |

## Success Criteria

ArcNET should consider this strategy successful when all of the following are true:

- A frontend can open one workspace object and discover the full editable world state.
- Frontends do not need bespoke binary-format orchestration.
- Preview surfaces for maps, art, and audio are provided by SDK contracts rather than UI-specific ad hoc code.
- Dialog, script, map, and save editing all use first-class editor-domain APIs.
- Most new editor features can be added without changing the frontend architecture.

## Implementation Started In This Session

This roadmap is not documentation-only. The first SDK-first step was implemented alongside this review:

- `EditorWorkspace` now provides a unified editor-facing session surface.
- `EditorWorkspaceLoader` now composes both loose-content and install-backed DAT loading with optional `LoadedSave` loading.
- `EditorWorkspace.Assets` now exposes a parsed asset inventory with loose-file versus DAT provenance for the winning source of each loaded game-data file.
- `EditorWorkspace.LoadReport` now surfaces skipped archive candidates and skipped winning assets when real installs contain junk root DATs or malformed payloads.
- `EditorWorkspace.Index` now exposes the first higher-level workspace queries: map ownership, message-index ownership, proto definition lookup, multi-asset script/dialog definition lookup, and reverse proto/script/art references from `.mob` and `.sec` assets.
- `EditorWorkspace.Validation` now exposes first-pass cross-file authoring findings for missing proto/script definitions, install-aware proto display-name gaps, broken dialog response targets, and unknown script attachment slots.
- `EditorWorkspaceLoadOptions` gives the workspace a stable growth path for installation-root and save-slot inputs.
- Install-backed workspace loading now tolerates invalid root DAT candidates and skips malformed winning assets so real patched installs can still open successfully.
- Focused editor tests now pin loose-content loading, DAT/module loading, patch precedence, loose override precedence, asset provenance, malformed winning-asset handling, malformed script-reference handling, and save-backed workspace composition.

Documentation and discoverability follow-through now continue that same direction:

- `README.md` now positions ArcNET as a modular editor SDK instead of only a format library.
- `arcnet editor validate` now exposes the workspace validation report through the existing live-install CLI surface with severity, asset-path, and message-text filters.
- The generated public API document now includes `ArcNET.Editor`, so the frontend-facing surface is published alongside the lower-level packages.

That is the correct first move for the stated goal: reduce frontend orchestration before adding more specialized authoring and preview services.