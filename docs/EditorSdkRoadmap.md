# ArcNET Modular Editor SDK Review And Roadmap

## Executive Summary

ArcNET should be treated and marketed as a pluggable, UI-agnostic editor SDK for Arcanum.
The product goal is not a replacement gameplay engine.
The product goal is to make a desktop, web, or hybrid Arcanum editor mostly presentation work by moving the hard parts into stable SDK services.

As of 2026-05-02, the codebase already contains a real editor-SDK foundation:

- one workspace/session entry point for loose-content or install-backed loading
- optional save-slot composition into the same workspace
- transactional dialog, script, save, and direct world-asset editing
- first-pass staged/apply history and project/session restore state
- asset indexing, dependency summaries, validation, and preview builders
- typed map/object editing seams that are much more than raw format mutation
- a minimum capability-discovery contract through `EditorWorkspace.GetCapabilities()`, now including tracked terrain/object workflow slices

The most important honest conclusion is this:

- ArcNET is already a serious editor foundation.
- ArcNET is not yet a full-blown Arcanum editor SDK.
- The remaining work is mostly about workflow completeness, parity with the original world editor, and stronger host-facing integration seams.

## Review Basis

This review was checked against the current codebase and the original editor reference material:

- `ArcNET.Editor` public surfaces such as `EditorWorkspace`, `EditorWorkspaceSession`, `EditorProject`, `EditorProjectStore`, `EditorAssetIndex`, `DialogEditor`, `ScriptEditor`, `SaveGameEditor`, and the preview builders
- `ArcNET.Editor.Tests`, which already pin workspace loading, project restore, scene-preview, audio-preview, dialog/script/save editing, and session history behavior
- the current docs in `docs/`
- the original Arcanum World Editor screenshots provided for this review, which show the practical parity floor:
  - module/open workflow
  - terrain art browsing
  - critter/item/scenery palette browsing
  - object placement into the world view
  - isometric/top-down editing views

## Product Positioning

### Primary Goal

Provide a modular SDK from which a full Arcanum editor becomes mostly presentation and workflow polish.

### Non-Goal

Do not pursue a general-purpose Arcanum runtime or full gameplay engine unless a runtime slice is directly required for authoring workflows.

### Engine-Adjacent Capabilities That Do Belong

- map, tile, object, and art rendering for authoring previews
- sound-effect and music preview
- dialog graph composition and validation
- script and condition/action authoring support
- asset reference resolution and dependency inspection
- optional simulation helpers only where needed for editor feedback

## Current State Review

| Area | Status | Assessment |
|---|---|---|
| Binary formats | Strong | `ArcNET.Formats` already provides broad read/write coverage for Arcanum formats and save-related files. |
| Object model | Strong | `ArcNET.GameObjects` gives typed object surfaces instead of raw byte-only editing. |
| Save editing | Strong | `LoadedSave`, `SaveGameLoader`, `SaveGameEditor`, `SaveGameWriter`, and validators form a credible save-authoring pipeline, including staged `jmp` and `prp` editing. |
| Builder APIs | Strong | `CharacterBuilder`, `SectorBuilder`, `ScriptBuilder`, `DialogBuilder`, and `MobDataBuilder` reduce raw property editing substantially. |
| Unified workspace/session | Partial but real | `EditorWorkspaceLoader`, `EditorWorkspace`, and `EditorWorkspaceSession` already give hosts a serious UI-agnostic entry point, but the editing model still feels like coordinated slices rather than one default command system. |
| Asset graph / dependency model | Partial | Asset catalogs, search, typed details, and dependency summaries exist, but graph traversal and broader relationship walking are still narrower than a full editor wants. |
| Validation and repair | Partial | Workspace validation is real, and staged repair candidates exist, but repair coverage is still narrow and mostly dialog-focused. |
| Map/object authoring services | Partial, meaningful | Tile/roof/blocked/lights/tile-scripts and object placement/erase/replace/move/rotate/pitch/transform APIs are already real, but they still need higher-level workflow shaping. |
| Preview and interaction adapters | Partial | Map scene preview, camera math, hit testing, area selection, ART preview, and WAV preview are all real, but scene fidelity and media breadth remain incomplete. |
| Project/workspace persistence | Partial | `EditorProject` and restore flows already persist meaningful shell state, but richer world-edit workflow persistence is still missing. |
| Plugin / host model | Partial, early | `EditorWorkspace.GetCapabilities()` now provides a minimum host capability-discovery contract, but meaningful plugin extension seams and compatibility guidance are still missing. |

## Original Editor Comparison

The original Arcanum World Editor is not a model for architecture, but it is a valid parity target for practical workflow coverage.
ArcNET should at least match those workflows and then exceed them with safer and richer SDK behavior.

| Legacy editor workflow | ArcNET today | Assessment |
|---|---|---|
| Open game/module content | Strong | Install-backed and loose-content workspace loading already cover this well. |
| Save world edits | Partial | Save and content persistence exist, but the full host-ready world-edit workflow around them is still thin. |
| Terrain art selection and painting | Partial | Tile and roof editing are real, but the SDK still lacks a more complete terrain-tool and terrain-browser story. |
| Critter/item/scenery palette browsing | Partial | Proto-backed object palette search and preview bindings exist, but not yet a more opinionated host-ready browser/tool workflow. |
| Place and edit objects in scene | Partial | Object placement, erase, replace, move, rotate, pitch rotate, and transforms already exist, but more editing semantics should be lifted above brush primitives. |
| Isometric/top-down map views | Partial | Camera math and scene projection are real, but ArcNET still lacks a clearer parity-grade scene/view contract for hosts. |
| Selection-driven editing | Partial | Point and area selection routing exist, including persisted selection state, but deeper live-edit behavior still needs refinement. |
| Terrain/object workflow completeness | Weak to partial | The low-level and mid-level primitives are increasingly good, but the complete "world editor tool" experience is still not mostly solved by the SDK. |

Main takeaway:

- ArcNET is already ahead of the original editor in foundations, safety, persistence direction, and save integration.
- ArcNET is still behind the original editor in turnkey world-edit workflow completeness.
- The next roadmap should explicitly close that gap instead of treating all remaining work as generic future polish.

## Readiness Snapshot

This is the shortest honest answer to "does ArcNET already support most of what a full Arcanum editor needs?":

| Phase | Readiness | What that means in practice |
|---|---|---|
| Phase 1 - Frontend entry point | Mostly delivered | Frontends can already open one workspace object, inspect assets, and attach a live editor session. |
| Phase 2 - Asset catalog and reference graph | Partial, useful | Asset browsing, lookups, and dependency summaries exist, but the graph is not yet broad enough for large-scale content tooling. |
| Phase 3 - Authoring domain services | Partial, meaningful | Save/dialog/script/session work is real, and map/object seams are increasingly real, but workflow completeness is still missing. |
| Phase 4 - Preview and media support | Partial | Map, art, and WAV preview are usable, but richer rendering fidelity and broader media coverage are still missing. |
| Phase 5 - World-edit parity workflows | Early partial | The SDK now has enough primitives to start parity-grade map/object tooling, but not enough complete workflows to declare parity. |
| Phase 6 - Plugin and host model | Early partial | A minimum capability-discovery API now exists, but there is still no formal extension model or compatibility guidance for third-party hosts/plugins. |

Overall product read:

- ArcNET is already a strong editor-SDK foundation with multiple real vertical slices.
- ArcNET should not yet be described as "supports most full editor needs."
- The biggest remaining gaps are coherent editing workflows, world-editor parity, deeper preview/media support, and the missing plugin/host model.

## Architectural Direction

ArcNET should commit to a layered editor-SDK architecture.

### Layer 1: Binary And Archive Foundation

- `ArcNET.Core`
- `ArcNET.Archive`
- `ArcNET.Formats`
- `ArcNET.GameObjects`

This layer remains responsible for correctness, performance, and round-trip fidelity.

### Layer 2: Workspace And Asset Graph

Responsibilities:

- load a game workspace from loose content or DAT-backed installs
- load an optional save slot into the same workspace
- track provenance, overrides, and source locations
- expose catalogs, typed lookups, and dependency summaries

This layer already has a usable shape through `EditorWorkspace`, `EditorWorkspaceLoader`, `EditorWorkspace.Assets`, `EditorWorkspace.Index`, `EditorWorkspace.Validation`, and `EditorWorkspace.LoadReport`.

### Layer 3: Authoring Services

Responsibilities:

- strongly typed builders and mutation services
- validation and repair
- undo/redo-friendly editing
- authoring-safe transforms for sectors, dialogs, scripts, saves, and object graphs

This layer is now materially underway, but it still needs to grow from "good primitives" into "host-ready workflows."

### Layer 4: Preview And Interaction Adapters

Responsibilities:

- render-ready map scene projection
- shared camera and hit-testing math
- ART and audio preview services
- adapter seams for host rendering and playback backends

This layer is real enough to be useful already, but not yet rich enough to keep all serious hosts from rebuilding key semantics.

### Layer 5: Frontend Shells

Desktop, web, and tooling frontends should live above the SDK and consume the same editor services.

Responsibilities:

- view layout and interaction
- docking, trees, property grids, and tool routing
- workflow polish
- host-specific rendering and audio backends

## Revised Roadmap

## Phase 1 - Coherent Editing Core

Goal:

- make the current session/history/project model feel like one editor system instead of several adjacent transactional slices

Working now:

- staged local undo/redo exists for dialog, script, save, and direct asset drafts
- staged transaction summaries and staged command summaries exist
- partial apply/save/discard by selected transaction exists
- applied history snapshots restore project/session shell state

Still missing:

- one clearly dominant host-facing command model
- clearer dirty-baseline semantics
- stronger session-lifecycle guidance for hosts

Best next steps:

1. Finish the default command model for staged and applied history.
2. Tighten apply/save/discard/reopen semantics for mixed asset workflows.
3. Keep changes additive and compatibility-friendly for existing hosts.

## Phase 2 - Asset Catalog And Dependency Graph

Goal:

- remove manual file-path and relationship plumbing from hosts

Working now:

- asset catalogs and typed detail/search surfaces
- dependency summaries with outgoing proto/script/art and incoming proto/script context
- map ownership, sector summaries, and scheme lookups

Still missing:

- broader graph traversal helpers
- richer semantic navigation
- more cross-format relationship coverage

Best next steps:

1. Add higher-level related-asset traversal helpers.
2. Add search/filter surfaces that understand asset kind and ownership semantics.

## Phase 3 - World-Edit Parity Workflows

Goal:

- reach and then exceed the practical feature floor of the original Arcanum World Editor

Working now:

- sector layer editing for tile art, roof art, blocked tiles, lights, and tile scripts
- object placement, erase, replace, move, rotate, pitch rotate, and transforms
- proto-backed palette entries plus placement requests, sets, and presets
- scene hit testing and persisted map selection state
- tracked map-view terrain/object tool summaries and setters for host-ready tool binding, preview, and apply

Still missing:

- richer terrain palette and terrain-tool workflows beyond the current tracked-tool baseline
- more complete object browser and placement-tool workflows beyond the current tracked-tool baseline
- more opinionated scene/view contracts for parity-grade host editors
- more complete end-to-end world-edit operations

Best next steps:

1. Extend the new tracked terrain-tool helpers into stronger palette browsing and brush routing flows.
2. Extend the new tracked object-placement helpers into clearer placement-browser and preset-library workflows.
3. Persist richer world-edit workflow state through the project model as those tool flows deepen.

## Phase 4 - Cross-Asset Validation And Repair

Goal:

- make ArcNET safer and more automation-friendly than the original editor

Working now:

- first-pass workspace validation
- staged validation inspection for selected transactions
- narrow dialog repair candidates
- script retargeting and art replacement helpers

Still missing:

- broader repair coverage
- richer multi-asset impact summaries
- more generalized cross-asset authoring helpers

Best next steps:

1. Expand repair candidates beyond dialog fixes.
2. Promote dependency-aware validation and impact reporting in host-facing APIs.
3. Add more cross-asset update helpers for common authoring operations.

## Phase 5 - Preview And Media Completion

Goal:

- keep hosts from reconstructing preview/render/media logic themselves

Working now:

- map outline and scene preview
- camera math and hit testing
- ART preview
- WAV preview and typed audio browsing

Still missing:

- better object depth/order fidelity
- broader audio/media coverage
- cleaner backend adapter seams

Best next steps:

1. Improve object scene projection fidelity.
2. Expand audio/media coverage beyond WAV.
3. Decide which preview adapters belong in-core versus optional packages.

## Phase 6 - Project Persistence And Extensibility

Goal:

- make workspaces restorable and extensions safe before multiple shells grow around ad hoc seams

Working now:

- persisted workspace references
- typed open assets, bookmarks, view states, tool states, and map view state
- project restore summaries and bootstrap summaries
- a minimum host capability contract through `EditorWorkspace.GetCapabilities()`

Still missing:

- richer workflow persistence for world-edit tools
- validators/importers/exporters/preview-provider plugin seams
- compatibility guidance for third-party extensions

Best next steps:

1. Persist more world-edit workflow state first.
2. Extend the new capability-discovery contract as more optional backend slices become versioned SDK features.
3. Add extension points only after the host-facing model is stable enough to version safely.

## Immediate Technical Priorities

| Priority | Area | Why |
|---|---|---|
| P1 | Finish the coherent editing core | This unlocks trustworthy host command routing, mixed-workflow undo/redo, and cleaner apply/save behavior. |
| P1 | Reach original world-editor parity in world-edit workflows | Matching terrain/object editing completeness is now the most important product gap. |
| P1 | Expand validation and repair | ArcNET should exceed the old editor here and turn validation into editing assistance, not only reporting. |
| P2 | Improve scene fidelity and media support | Better preview fidelity directly reduces frontend reimplementation pressure. |
| P2 | Broaden dependency navigation | Larger content tooling still needs wider graph and relationship support. |
| P3 | Define the plugin/host model | This should happen before too many host-specific seams accumulate accidentally. |

## Next 12 Tasks

These are the best concrete tasks to move ArcNET toward a full Arcanum editor SDK:

| Rank | Task | Why now |
|---|---|---|
| 1 | Finish one default staged/applied command model for `EditorWorkspaceSession` | Hosts still need a clearer "just bind undo/redo" story. |
| 2 | Extend tracked terrain-tool workflows above the current layer-brush APIs | The first tracked workflow layer now exists; parity needs richer palette/tool routing on top. |
| 3 | Extend tracked object placement/edit workflows above current palette and brush primitives | The first tracked workflow layer now exists; parity needs stronger browser/preset/edit semantics. |
| 4 | Persist preset libraries and world-edit tool state through `EditorProject` / `EditorProjectStore` | World-edit workflows become much more usable when they survive across sessions. |
| 5 | Expand repair candidates beyond the current dialog-only practical slice | This is the clearest path to exceed the legacy editor. |
| 6 | Expand pending-change impact summaries with richer dependency context | Hosts need this before larger cross-asset operations feel safe. |
| 7 | Add more generalized cross-asset authoring helpers | Current script retarget/art replacement helpers are valuable but too narrow. |
| 8 | Improve scene-preview object depth/order fidelity | Better scene fidelity lowers frontend complexity immediately. |
| 9 | Expand audio/media support beyond the current WAV-only slice | Real editors need broader preview/media coverage. |
| 10 | Add broader graph traversal and semantic navigation helpers | Asset relationships are already useful but still incomplete. |
| 11 | Extend capability discovery to cover future optional validators/importers/exporters/preview slices | The minimum contract now exists; the next job is to keep it aligned with future extensibility. |
| 12 | Define a minimum plugin surface and compatibility guidance | Extensibility should now be layered on top of the capability contract before ecosystem growth begins. |

## Done Definition

ArcNET should only be described as supporting "most full editor needs" once all of the following are true:

- a host can open, browse, edit, validate, preview, apply, save, and reopen common Arcanum workflows without bespoke binary-format orchestration
- common terrain, object, dialog, script, and save workflows run through first-class editor-domain services rather than frontend-only orchestration
- the session/history model feels coherent across cross-asset editing, not only within local editor scopes
- preview/media services are good enough that hosts do not need to recreate core scene/audio/art logic themselves
- the SDK reaches at least the practical workflow floor of the original Arcanum World Editor
- extension and host capability seams are explicit enough that multiple editor shells can grow on top of the same SDK without forking

## Success Criteria

ArcNET should consider this strategy successful when all of the following are true:

- a frontend can open one workspace object and discover the full editable world state
- frontends do not need bespoke binary-format orchestration
- preview surfaces for maps, art, and audio are provided by SDK contracts rather than UI-specific ad hoc code
- world-edit, dialog, script, and save editing all use first-class editor-domain APIs
- most new editor features can be added without changing frontend architecture
