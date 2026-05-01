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
| Save editing | Strong | `LoadedSave`, `SaveGameLoader`, `SaveGameEditor`, `SaveGameWriter`, and validators already form a credible save-authoring pipeline, and `SaveGameEditor` now exposes first-class staged `JmpFile` and `MapProperties` editing instead of leaving those save-side formats stranded behind raw registry support. |
| Builder APIs | Strong | `CharacterBuilder`, `SectorBuilder`, `ScriptBuilder`, `DialogBuilder`, and `MobDataBuilder` reduce raw property editing, and the script/dialog authoring surfaces now expose typed composition, transactional dialog editing, flow insertion helpers, operand helpers, and local validation hooks for authoring-time checks before content is written back out. |
| Patch tooling | Strong | `ArcNET.BinaryPatch` and `ArcNET.Patch` already cover structured patching and install-time workflows. |
| Unified editor session | Partial | `EditorWorkspaceLoader` now opens loose or install-backed workspaces synchronously or asynchronously, supports weighted progress for combined game-data and save loading, and returns one `EditorWorkspace` with assets, index, validation, load diagnostics, install metadata, and optional save composition. `EditorWorkspace.CreateSession()` now lifts that snapshot into an `EditorWorkspaceSession` that can keep dialog/script/save editors alive, report pending changes across the workspace, apply those staged edits into a refreshed workspace snapshot, persist the current session surfaces back to the configured content/save paths, and undo/redo applied change groups together with the live session project state. Dialog/script/save editors plus direct proto/mob/sector session workflows now expose one host-facing staged-history surface through the session, including merged local undo/redo and preferred-scope helpers for active hosts before apply/save, and `GetPendingChangeSummary()` now carries per-target dependency context from the effective staged workspace state. The remaining gap is deeper cross-asset coordination around those transactions and a broader authoring model above the current per-scope editors. |
| Asset graph / dependency model | Partial | `EditorWorkspace.Assets` now exposes basic asset-path text search, and `EditorWorkspace.Index` now covers map ownership, asset-path dependency summaries for defined proto/script/dialog IDs, outgoing proto/script/art references, incoming proto/script graph context for one-asset lookups, sector summaries, sector environment-scheme lookups, lightweight map projection lookup, message ownership, proto definition lookup, multi-asset script/dialog definition lookup, direct workspace lookups for message/proto/jump/map-properties/terrain/facade-walk assets, indexed jump/map-properties/terrain/facade-walk detail/search surfaces, text search across maps/sectors/script descriptions/dialog text, plus reverse proto/script/art references, but broader cross-format dependency navigation is still missing. |
| Cross-file validation services | Partial | `EditorWorkspace.Validation` now surfaces first-pass workspace findings for missing proto/script definitions, install-aware proto display-name gaps, dialog-local authoring problems such as broken response targets and negative IQ values, and unknown script attachment slots, and `arcnet editor validate` can print that report from live installs, but broader edit-time validation and repair flows are still missing. |
| Preview-ready rendering surfaces | Partial | `EditorWorkspace.Index` now exposes a lightweight map projection model plus normalized sector preview flags and map-local density bands, `EditorMapPreviewBuilder` now projects those into reusable dense preview rows for hosts, `EditorMapScenePreviewBuilder` now lifts loaded `.sec` payloads into tile/roof/block/light/script/object layer data for GUI-oriented hosts, and `EditorMapCameraMath` now provides shared tile-space viewport bounds, projection/unprojection helpers, scene hit-testing that maps viewport points back into the current persisted selection model, and stacked-object hit results for tile-level discrimination. `.art` assets also load into `EditorWorkspace` and can be previewed by asset path through `EditorArtPreviewBuilder`. The remaining gaps are richer object/material rendering backends and deeper viewport interaction helpers. |
| Audio preview surfaces | Partial | `EditorWorkspace` now discovers loose and install-backed `.wav` assets, exposes them through `AudioAssets`, and can build metadata-plus-sample previews through `EditorAudioPreviewBuilder`. Broader codec coverage, cue/scheme resolution, and playback backend adapters are still missing. |
| Script authoring surface | Partial | `ScriptBuilder` now exposes typed condition/action composition, typed operand setters, and local validation, `ScriptEditor` now adds the same staged-versus-committed transactional layer that dialogs already had, local staged command undo/redo, `EditorWorkspaceSession` can now apply staged script edits back into a refreshed workspace snapshot and persist them to the configured loose-content path, and `EditorWorkspace.Index.FindScriptDetails(...)` exposes attachment summaries. What is still missing is broader script-domain authoring semantics, richer edit-time repair flows, and deeper command/history integration beyond the current local staged editor stack. |
| Dialog authoring surface | Partial | `EditorWorkspace.Index.FindDialogDetails(...)` now exposes node-level dialog graph data with entry kinds, roots, transitions, and missing-target flags, `DialogBuilder` now exposes graph-aware composition helpers for NPC replies, PC options, control entries, and response rewiring, `DialogEditor` now adds a transactional current-versus-pending edit layer with commit/discard semantics, local staged command undo/redo, and insert-after graph splicing on top of that builder surface, and `DialogValidator` covers duplicate entry numbers, negative IQ values, and missing positive targets, but broader dialog-authoring rules are still missing. |
| Undo/redo model | Partial | `EditorWorkspaceSession` can now keep `DialogEditor`, `ScriptEditor`, and `SaveGameEditor` instances alive, summarize pending changes across assets, apply/persist labeled change groups through refreshed workspace snapshots, and undo/redo those snapshots while restoring tracked open assets, active asset state, and typed map-view state. `DialogEditor`, `ScriptEditor`, and `SaveGameEditor` now expose local command-level undo/redo for staged edits, direct proto/mob/sector session workflows now expose local staged draft undo/redo through `UndoDirectAssetChanges()` / `RedoDirectAssetChanges()`, and `EditorWorkspaceSession.GetStagedHistoryScopes()` plus scoped and parameterless `UndoStagedChanges(...)` / `RedoStagedChanges(...)` now give hosts one session-level way to inspect, prefer, and drive those local histories. The remaining gap is deeper coordination between the merged staged-history layer and broader workspace-level authoring transactions. |
| Project/workspace metadata | Partial | `EditorProject`, `EditorProjectWorkspaceReference`, and `EditorProjectStore` now provide a host-neutral persisted model for workspace source, optional save selection, open assets, bookmarks, view state, and tool state, and can reopen that workspace through `EditorWorkspaceLoader`. `EditorWorkspaceSession` now gives hosts a live in-memory mutation layer beside that metadata. The remaining gap is persisting richer typed session state and wiring project metadata directly into live session workflows. |
| Plugin / host model | Weak | No plugin extension points, capability discovery surface, or compatibility contract was found in `ArcNET.Editor`. |
| Frontend-facing orchestration | Partial | `EditorWorkspace`, `EditorProject`, the read-only index interfaces, and `EditorWorkspaceSession` now form a credible UI-agnostic anchor for load/open flows plus dialog/script/save editor lifetime, dirty-change reporting, apply-back into refreshed workspace snapshots, persistence of the current session-editable surfaces to workspace paths, and reversible apply/save history that restores session-bound project state. Dialog/script/save editors plus direct proto/mob/sector session workflows now all support local staged undo/redo in their respective scopes, the session now exposes a host-facing staged-history surface to inspect, prioritize, and route those local operations, and the pending-change summary now exposes per-target dependency context for multi-asset staged workflows. The remaining gap is deeper cross-asset coordination and a richer workspace-wide command model above the current transactional slices. |

## Review Basis

This update was checked against the current codebase as of 2026-05-01:

- `ArcNET.Editor` public surfaces such as `EditorWorkspace`, `EditorWorkspaceSession`, `EditorProject`, `EditorProjectStore`, `EditorAssetIndex`, validation reports, `DialogEditor`, `ScriptEditor`, `DialogBuilder`, `ScriptBuilder`, and the save-editing pipeline.
- `ArcNET.App` host-side mirrors of those surfaces, including the live-install CLI whose outline command now consumes the shared editor-side map preview builder.
- `ArcNET.Formats.ArtFormat`, which already provides low-level ART frame and palette decoding at the format layer.
- `ArcNET.Editor.Tests`, which now pin most of the implemented workspace, validation, projection, dialog, and script slices.

Main conclusion:

- Phase 1 is mostly real for load/open scenarios.
- Phase 2 and the first parts of Phase 3 are meaningfully underway, including an initial persisted project-metadata slice.
- Phase 4 now has concrete editor-side map-outline and ART preview seams, but richer map/audio/model preview coverage is still incomplete.
- Phase 5 and Phase 6 remain mostly roadmap, not implementation.

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

The current codebase already contains the first usable shape of this layer: `EditorWorkspace`, `EditorWorkspaceLoader`, `EditorWorkspace.Assets`, `EditorWorkspace.Index`, `EditorWorkspace.Validation`, and `EditorWorkspace.LoadReport` in `ArcNET.Editor`. That is enough for install-backed or loose-content inspection, provenance tracking, first-pass dependency navigation, and workspace diagnostics, but it is not yet a full mutable editor session.

### Layer 3: Authoring Services

This layer should hold editor-domain operations rather than UI code.

Responsibilities:

- Strongly typed builders and mutation services.
- Validation and diagnostics.
- Undo/redo-friendly transactional edits.
- Diff and merge helpers.
- Import/export pipelines.
- Authoring-safe transforms for sectors, dialogs, scripts, and object graphs.

The codebase now has early Layer 3 pieces through `SaveGameEditor`, `DialogBuilder`, `DialogEditor`, `ScriptBuilder`, `ScriptEditor`, `DialogValidator`, `ScriptValidator`, `EditorWorkspaceSession`, `EditorProject`, and `EditorProjectStore`. What is still missing is richer cross-asset authoring services plus broader transaction-history and repair flows that would let frontends treat the current merged staged-history layer and apply/save history as one coherent editing model.

### Layer 4: Preview and Media Adapters

This layer should deliberately support frontend work without turning ArcNET into a full engine.

Responsibilities:

- Render-ready map scene projection.
- Art and animation frame projection.
- Model/material preview projection when the format work reaches that point.
- Audio preview services for sound effects and music.
- Palette, lighting, and animation timing helpers.

These should be SDK services and abstractions, not hardwired UI widgets.

The current foothold is still partial: `EditorMapProjection` and `EditorMapSectorProjection` live in `ArcNET.Editor`, `EditorMapPreviewBuilder` turns those projections into reusable dense preview rows, `EditorMapScenePreviewBuilder` now exposes richer tile/roof/block/light/script/object data from loaded sectors, `EditorWorkspace` can resolve `.art` and `.wav` assets and build previews by asset path, and `EditorArtPreviewBuilder` plus `EditorAudioPreviewBuilder` project those media assets into host-neutral preview models. Richer render backends plus broader audio codec support are still missing.

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

Codebase status as of 2026-05-01:

- Mostly delivered for load/open scenarios via `EditorWorkspaceLoader`, `EditorWorkspaceLoadOptions`, install-backed loading, weighted progress, and optional save composition.
- Still missing: richer host-neutral workspace lifecycle/orchestration services plus deeper coordination between local staged history and higher-level workspace workflows.

## Phase 2 - Asset Catalog and Reference Graph

Goal: remove manual file-path plumbing from frontend code.

Deliverables:

- Asset catalog service with typed entries.
- Reverse reference and forward reference queries.
- Provenance model for DAT, loose override, extracted, and generated content.
- Search/index APIs for entities, dialogs, scripts, art, and sectors.

Outcome target:

- Frontends navigate assets by meaning, not by ad hoc path conventions.

Codebase status as of 2026-05-01:

- Meaningfully underway via `EditorWorkspace.Assets`, `EditorWorkspace.Index`, `EditorAssetDependencySummary`, path/text search over assets/maps/sectors/scripts/dialogs, map/sector/scheme/message/proto/script/dialog/art queries, direct workspace lookups for message/proto/jump/map-properties/terrain/facade-walk assets, indexed jump/map-properties/terrain/facade-walk detail/search metadata, and provenance/load-diagnostic reporting.
- Still missing: broader cross-format traversal, richer semantic search beyond the current text-plus-ID slice, generated-content provenance, and deeper relationships beyond the current proto/script/dialog/art-centric slice despite the newly surfaced jump/map-properties/terrain/facade-walk coverage.

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

Codebase status as of 2026-05-01:

- Early partial via `LoadedSave` / `SaveGameEditor`, `DialogBuilder`, `DialogEditor`, `ScriptBuilder`, `ScriptEditor`, `DialogValidator`, `ScriptValidator`, `EditorWorkspaceSession`, `EditorWorkspace.Validation`, `EditorProject`, and `EditorProjectStore`, with `SaveGameEditor` now also exposing first-class staged `jmp/prp` editing instead of relying on hidden format-registry plumbing.
- Still missing: diff/merge helpers and broader cross-asset coordination on top of the current merged staged-history layer, richer pending change summaries, and session apply/save history.

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

Codebase status as of 2026-05-01:

- Early partial. `EditorMapProjection` / `EditorMapSectorProjection` plus preview flags and density bands already exist in `ArcNET.Editor`, `EditorMapPreviewBuilder` now exposes a host-neutral dense preview overlay reused by `arcnet editor outline`, `EditorMapScenePreviewBuilder` now exposes richer sector tile/roof/block/light/script/object layer data for GUI hosts including object offset/depth metadata from existing mob fields and optional conservative sprite bounds, `EditorWorkspace` can already build packed-pixel ART previews by asset path and the art index now exposes browser-friendly art detail/search metadata for those loaded ART assets, `EditorWorkspace` now also exposes a built-in `EditorArtResolver` binding surface that resolves known `ArtId -> loaded art asset` mappings back into scene preview construction without requiring raw resolver delegates from hosts, `EditorMapCameraMath` now exposes shared tile-space camera math plus scene hit-testing that can return either the current `EditorProjectMapSelectionState` shape, the full stacked object hits on a tile, one ordered set of tile hits across a viewport drag box, a persisted area-selection state with map-local bounds and stable selected object identities for one drag box, one resolved set of positioned-sector hits from that persisted area selection, or those resolved hits grouped back under their positioned sectors for bulk host workflows, and `EditorWorkspaceSession` now exposes bulk sector tile-art and blocked-tile helpers that consume those grouped scene-sector hits directly. The persisted selection DTOs now expose helper semantics for hosts to interpret point versus area object selection directly and enumerate drag-box tiles in stable screen order. Same-tile object hits now use object offsets plus optional resolved sprite bounds as the current depth heuristic, with preview order retained as the final tie-break.
- Still missing: broader sound/music cue resolution, model/material preview, automatic engine-style `ArtId` decoding into asset paths plus exact render-order or sprite-bounds-aware object depth beyond the current conservative heuristic, grounded scene-hit-to-roof-cell mapping for bulk roof editing, and richer render-backend adapters beyond the current dense sector-scene surfaces.

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

Codebase status as of 2026-05-01:

- Early partial. Dialog graph composition and script condition/action composition now exist, map/sector composition now includes grouped bulk tile-art and blocked-tile helpers driven from shared scene-hit grouping, and object composition now includes grouped proto-instancing plus grouped object erasure from those same grouped scene hits together with a typed object-brush request/result contract that now covers multi-tile stamp, erase, and replace workflows. Asset browsing also now has a first explicit art-browser seam through indexed art details plus search, but broader map/sector/object composition is still mostly low-level builder work.
- Still missing: broader sector/map composition helpers including roof bulk-edit mapping, higher-level object palette/brush services beyond the current typed stamp/erase/replace contract, broader asset-browser abstractions beyond the current art slice, and prefab-like reusable authoring primitives.

## Phase 6 - Plugin and Host Model

Goal: allow ecosystem growth without forking the core SDK.

Deliverables:

- Plugin extension points for asset tools, validators, importers, exporters, and previews.
- Capability discovery for frontend hosts.
- Versioned compatibility guidance for third-party editor plugins.

Outcome target:

- ArcNET becomes the common authoring substrate for multiple Arcanum tools.

Codebase status as of 2026-04-30:

- Not started in code. No plugin extension points, capability discovery API, or compatibility contract were found in `ArcNET.Editor`.

## Immediate Technical Priorities

| Priority | Area | Why |
|---|---|---|
| P1 | Mutable editor session and undo model | `EditorWorkspaceSession` now tracks live dialog/script/save editors plus pending changes and can undo/redo applied workspace snapshots together with tracked session project state, while `DialogEditor`, `ScriptEditor`, and `SaveGameEditor` have local staged command undo/redo, direct proto/mob/sector session workflows have local staged draft undo/redo, and the session now exposes one host-facing staged-history surface with merged local undo/redo plus preferred-scope helpers. Remaining work is to keep those local histories coherent with richer cross-asset authoring workflows inside one mutable workspace state. |
| P1 | Promote preview and media to SDK contracts | Map outline preview, richer sector-scene preview data, shared tile-space camera math, first-pass scene hit-testing with stacked-object discrimination, ART preview, and WAV preview now live in `ArcNET.Editor`, but richer rendering backends, area-selection helpers, and broader audio support are still missing. |
| P1 | Broaden graph and navigation APIs | `EditorAssetIndex` is useful and now exposes incoming proto/script context in per-asset dependency summaries, but frontends still need wider cross-format traversal, search, and dependency navigation beyond the current slice. |
| P2 | Expand authoring services beyond dialogs and scripts | Save editing is strong and dialog/script helpers exist, while sector composition plus direct object edits and a typed grouped object-brush contract with stamp/erase/replace semantics now exist, but broader map placement, richer palette/brush semantics, and workspace-level mutation services are still missing. |
| P2 | Broaden project/workspace metadata | The first host-neutral project model now exists and a live session layer now exists beside it, but frontends still need richer typed location/view conventions and persisted session integration. |
| P3 | Plugin and host capability model | No extension seams or compatibility model exist yet, so ecosystem growth would still require forking or app-specific wiring. |

## Success Criteria

ArcNET should consider this strategy successful when all of the following are true:

- A frontend can open one workspace object and discover the full editable world state.
- Frontends do not need bespoke binary-format orchestration.
- Preview surfaces for maps, art, and audio are provided by SDK contracts rather than UI-specific ad hoc code.
- Dialog, script, map, and save editing all use first-class editor-domain APIs.
- Most new editor features can be added without changing the frontend architecture.

## Implementation Landed So Far

This roadmap is no longer documentation-only. The current codebase already contains the following SDK-first slices:

- `EditorWorkspace` now provides a unified editor-facing session surface.
- `EditorWorkspaceLoader` now composes both loose-content and install-backed DAT loading with optional `LoadedSave` loading.
- `EditorWorkspaceLoader.LoadAsync(...)` and `LoadFromGameInstallAsync(...)` now expose weighted progress reporting so hosts can surface one combined workspace-open operation.
- `EditorWorkspace.Assets` now exposes a parsed asset inventory with loose-file versus DAT provenance for the winning source of each loaded game-data file.
- `EditorWorkspace.AudioAssets` now exposes loose-file versus DAT provenance for loaded `.wav` assets, and `EditorWorkspace.CreateAudioPreview(...)` can build metadata-plus-sample previews from those workspace assets by path.
- `EditorWorkspace.LoadReport` now surfaces skipped archive candidates and skipped winning assets when real installs contain junk root DATs or malformed payloads.
- `EditorWorkspace.Assets.Search(...)` now exposes a first host-neutral asset-path text search surface, and `EditorWorkspace.Index` now exposes `SearchMapNames(...)`, `SearchSectors(...)`, `SearchScriptDetails(...)`, and `SearchDialogDetails(...)` so frontends can discover current workspace content without manually scanning every collection.
- `EditorWorkspace.Index` now exposes the first higher-level workspace queries: map ownership, asset-path dependency summaries for defined proto/script/dialog IDs plus forward proto/script/art references, sector summaries, sector environment-scheme lookups, lightweight map projection lookup plus normalized sector preview flags and density bands, message-index ownership, proto definition lookup, multi-asset script/dialog definition lookup, node-level dialog graph summaries, and reverse proto/script/art references from `.mob` and `.sec` assets.
- `GameDataLoader`, install-backed workspace loading, and the editor asset catalog now surface `.jmp`, `.prp`, `.tdf`, and `facwalk.*` assets through the normal editor workspace boundary instead of dropping those parsed formats before they reached host-facing APIs.
- `EditorWorkspace` now exposes direct `FindMessageFile(...)`, `FindProto(...)`, `FindJumpFile(...)`, `FindMapProperties(...)`, `FindTerrain(...)`, and `FindFacadeWalk(...)` lookups so hosts can consume those parsed assets without going back through raw `GameDataStore` internals.
- `EditorAssetIndex` now exposes browser-friendly jump/map-properties/terrain/facade-walk detail and search surfaces through `EditorJumpDefinition`, `EditorMapPropertiesDefinition`, `EditorTerrainDefinition`, and `EditorFacadeWalkDefinition`.
- `GameDataLoader`, install-backed workspace loading, and `EditorWorkspace` now materialize `.art` assets, so hosts can resolve art sources through the normal asset catalog and build previews with `EditorWorkspace.CreateArtPreview(...)` by asset path.
- `EditorArtPreviewBuilder` now projects `ArtFile` content into `EditorArtPreview` / `EditorArtPreviewFrame` models with palette-slot selection, RGBA/BGRA packed pixels, optional vertical flip, and frame-timing metadata so hosts can display ART previews without reimplementing palette application.
- `IArtIndex` / `EditorAssetIndex` now expose `FindArtDetail(...)` and `SearchArtDetails(...)`, and `EditorArtDefinition` now exposes browser-friendly ART metadata so hosts can build an art viewer or art browser on top of indexed workspace assets without hand-scanning the raw catalog before calling `CreateArtPreview(...)`.
- `EditorMapPreviewBuilder` now projects `EditorMapProjection` data into reusable dense preview rows with shared legends and combined/objects/roofs/lights/blocked/scripts classification, so hosts can reuse map-outline semantics without duplicating sector-priority logic.
- `EditorMapScenePreviewBuilder` now lifts loaded `.sec` payloads into richer per-sector tile, roof, blocked-tile, light, tile-script, and placed-object preview data, and `EditorWorkspace.CreateMapScenePreview(...)` exposes that richer map scene surface by map name.
- `EditorAudioPreviewBuilder` now parses RIFF/WAVE payloads into preview-ready sample metadata plus extracted data-chunk bytes so hosts can inspect or hand off supported audio assets without implementing WAV parsing themselves.
- `EditorAssetIndex` now exposes smaller read-only query interfaces (`IMapIndex`, `IAssetDependencyIndex`, `ISchemeIndex`, `IMessageIndex`, `IProtoIndex`, `IScriptIndex`, `IDialogIndex`, `IArtIndex`) so hosts can depend on narrower editor-facing capabilities instead of one monolithic surface.
- `ScriptBuilder` now exposes typed condition/action composition for the common zero-operand case plus `ScriptOperand`-based operand setters for condition, action, and else-action slots, so frontends can assemble common script entries without hand-constructing raw `ScriptConditionData`, `ScriptActionData`, `OpTypeBuffer`, or `OpValueBuffer` payloads.
- `EditorWorkspace.FindDialog(...)`, `FindScript(...)`, `CreateDialogEditor(...)`, and `CreateScriptEditor(...)` now let hosts open transactional editors directly from loaded workspace asset paths instead of re-parsing those assets themselves.
- `SaveGameEditor` now mirrors that staged command-history model for save editing, exposing `CanUndo`, `CanRedo`, `Undo()`, and `Redo()` across player, save-info, typed save-global, and raw-file edits while preserving the exact pending save-info/player-sync state until commit, discard, or session baseline reset.
- `SaveGameEditor` now also exposes first-class staged `jmp/prp` editing through `GetCurrentJumpFile(...)`, `GetPendingJumpFile(...)`, `WithJumpFile(...)`, `GetCurrentMapProperties(...)`, `GetPendingMapProperties(...)`, and `WithMapProperties(...)`, with those pending assets included in save-editor snapshot capture/restore.
- `ScriptEditor` now adds a transactional script-edit session on top of `ScriptBuilder`, exposing staged versus committed script views plus commit/discard flows so frontends can queue multi-step script edits without rebuilding the whole file by hand.
- `ScriptValidator` now exposes local script-authoring validation for description truncation risk, non-ASCII description loss, and unknown non-empty attachment slots, and `ScriptBuilder.Validate()` wires that into the fluent builder surface.
- `DialogBuilder` now exposes graph-aware composition helpers for NPC replies, PC options, control entries, and response-target rewiring so frontends do not need to hand-assemble raw `DialogEntry` records for common dialog-edit flows.
- `DialogEditor` now adds a transactional dialog-edit session on top of `DialogBuilder`, exposing staged versus committed dialog views plus commit/discard flows and insert-after graph-splicing helpers so frontends can queue multi-step dialog edits and insert intermediate dialog nodes without manually coordinating add-plus-rewire operations.
- `DialogValidator` now exposes local dialog-authoring validation for duplicate entry numbers, negative IQ values, and missing positive response targets before a frontend writes the file back out.
- `EditorWorkspace.Validation` now exposes first-pass cross-file authoring findings for missing proto/script definitions, install-aware proto display-name gaps, dialog-local authoring problems such as broken response targets and negative IQ values, and unknown script attachment slots.
- `EditorProject`, `EditorProjectWorkspaceReference`, and `EditorProjectStore` now provide a persisted editor-project model for workspace source, optional save selection, open assets, bookmarks, view states, and tool states, and `EditorWorkspace.CreateProject()` can seed that model from a live workspace.
- `EditorWorkspace.CreateSession()` now lifts a loaded workspace into `EditorWorkspaceSession`, which keeps dialog/script/save editors alive and surfaces one `GetPendingChanges()` summary across those staged edits for frontend hosts.
- Direct proto/mob/sector session workflows now keep their own staged draft history through `CanUndoDirectAssetChanges`, `CanRedoDirectAssetChanges`, `UndoDirectAssetChanges()`, and `RedoDirectAssetChanges()`, covering sector composition helpers plus bulk script/art retarget workflows until apply, save, or discard resets the session baseline.
- `EditorWorkspaceSession.GetStagedHistoryScopes()` now exposes one host-facing inventory of tracked dialog/script/save/direct-asset local history scopes, and `UndoStagedChanges(...)` / `RedoStagedChanges(...)` now route those local undo/redo operations through the session without forcing hosts to keep editor-specific references.
- `EditorWorkspaceSession` now exposes direct placed-object add/move/remove helpers plus `AddSectorObjectsFromProto(IReadOnlyList<EditorMapSceneSectorHitGroup>, int)`, `RemoveSectorObjects(IReadOnlyList<EditorMapSceneSectorHitGroup>)`, and `ApplySectorObjectBrush(IReadOnlyList<EditorMapSceneSectorHitGroup>, EditorMapObjectBrushRequest)`, so hosts can stamp, erase, or replace objects across grouped scene hits through either lower-level helpers or one typed brush contract without mutating raw `.sec` payloads themselves.
- Applied or saved `EditorWorkspaceSession` change groups now record undo/redo history snapshots that restore both the workspace content and the live session project state, including tracked open assets, the active asset, and typed map-view metadata.
- `EditorProjectWorkspaceReference.Load(...)` / `LoadAsync(...)` can reopen either loose-content or install-backed workspaces through the existing `EditorWorkspaceLoader` entry points.
- `EditorWorkspaceLoadOptions` gives the workspace a stable growth path for installation-root and save-slot inputs.
- Install-backed workspace loading now tolerates invalid root DAT candidates and skips malformed winning assets so real patched installs can still open successfully.
- Focused editor tests now pin loose-content loading, DAT/module loading, patch precedence, loose override precedence, asset provenance, malformed winning-asset handling, malformed script-reference handling, and save-backed workspace composition.
- `arcnet editor sector`, `arcnet editor scheme`, and `arcnet editor outline` now mirror the new workspace sector-summary, sector environment lookup, and map projection queries through the live-install CLI surface, with the outline command consuming `EditorMapPreviewBuilder` instead of carrying its own mode-classification logic.

Documentation and discoverability follow-through now continue that same direction:

- `README.md` now positions ArcNET as a modular editor SDK instead of only a format library.
- `arcnet editor validate` now exposes the workspace validation report through the existing live-install CLI surface with severity, asset-path, and message-text filters.
- The generated public API document now includes `ArcNET.Editor`, so the frontend-facing surface is published alongside the lower-level packages.

That is the correct first move for the stated goal: reduce frontend orchestration before adding more specialized authoring and preview services.