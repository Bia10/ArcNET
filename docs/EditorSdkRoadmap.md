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
- The remaining work is now mostly about the object/proto inspector layer, workflow completeness around the original detail windows, and stronger host-facing integration seams.

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
  - object-detail panes for flags, script attachments, light settings, level, spells, skills, generator, and blending

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
| Unified workspace/session | Stronger, still evolving | `EditorWorkspaceLoader`, `EditorWorkspace`, and `EditorWorkspaceSession` now expose one default staged-versus-applied command surface, but the broader editing model still needs polish around grouped semantics and richer workflow state. |
| Asset graph / dependency model | Partial | Asset catalogs, search, typed details, and dependency summaries exist, but graph traversal and broader relationship walking are still narrower than a full editor wants. |
| Validation and repair | Partial | Workspace validation is real, staged repair candidates now span dialog/script/direct-asset fixes including unknown script attachment-slot cleanup and broken proto cleanup, typed proto display-name authoring can stage message-file edits, and proto-reference retargeting now lets hosts migrate mob/sector links intentionally, but broader repair coverage is still missing. |
| Map/object authoring services | Partial, meaningful | Tile/roof/blocked/lights/tile-scripts and object placement/erase/replace/move/rotate/pitch/transform APIs are already real, and matching save-backed sector or mob edits now round-trip through the loaded save slot, but they still need higher-level workflow shaping. |
| Preview and interaction adapters | Partial | Map scene preview, camera math, hit testing, area selection, ART preview, and WAV preview are all real, but scene fidelity and media breadth remain incomplete. |
| Project/workspace persistence | Partial, stronger | `EditorProject` and restore flows already persist meaningful shell state, including tracked terrain/object tool state, preset libraries, and default world-edit shell preferences, but deeper world-edit workflow persistence is still missing. |
| Plugin / host model | Partial, early | `EditorWorkspace.GetCapabilities()` now provides a minimum host capability-discovery contract, but meaningful plugin extension seams and compatibility guidance are still missing. |

## Original Editor Comparison

The original Arcanum World Editor is not a model for architecture, but it is a valid parity target for practical workflow coverage.
ArcNET should at least match those workflows and then exceed them with safer and richer SDK behavior.

| Legacy editor workflow | ArcNET today | Assessment |
|---|---|---|
| Open game/module content | Strong | Install-backed and loose-content workspace loading already cover this well. |
| Save world edits | Partial | Save and content persistence exist, but the full host-ready world-edit workflow around them is still thin. |
| Terrain art selection and painting | Partial | Tile and roof editing are real, and tracked terrain palette browser summaries plus coordinate selection now exist, but fuller parity-grade terrain-tool workflow shaping is still missing. |
| Critter/item/scenery palette browsing | Strong | Proto-backed object palette browsing, tracked placement selection, and shell bundling now provide a host-ready browser/tool floor. |
| Place and edit objects in scene | Strong | Object placement and editing now include tracked selection summaries plus convenience edit helpers above the raw brush primitives. |
| Isometric/top-down map views | Strong | Camera math and scene projection now also ship with a tracked world-edit shell contract for parity-style top-down and isometric host editors. |
| Selection-driven editing | Strong | Point and area selection routing, persisted selection state, tracked summaries, and tracked edit helpers now cover the practical editing floor. |
| Terrain/object workflow completeness | Meaningful partial to strong | The complete world-editor tool experience is now substantially host-bindable, with the remaining work concentrated in polish and richer semantics. |

Main takeaway:

- ArcNET is already ahead of the original editor in foundations, safety, persistence direction, and save integration.
- ArcNET is still behind the original editor in turnkey object-detail workflow completeness even though the map/palette shell floor is largely there.
- The next roadmap should explicitly close that inspector/backend gap instead of treating all remaining work as generic future polish.

## Screenshot-Driven Frontend Readiness

If the immediate question is "can I start writing the editor UI now?", the honest answer is:

- yes for the outer shell: workspace/module open flows, palette browsing, placement, selection/manipulation, top-down/isometric scene shells, and normal apply/save/reopen loops already have backend seams that a frontend can bind directly
- no for the old detail windows as they existed in the original editor: ArcNET still lacks one first-class object/proto inspector contract that exposes those panes without raw property plumbing

The current capability model already reflects that split: `EditorWorkspace.GetCapabilities()` advertises object palette browsing, placement, tracked object workflows, object transforms, sector-light editing, and sector tile-script editing, but it does not yet advertise a general object/proto inspector or property-window editing slice.

| Frontend component | Backend readiness | Existing backend surface | Remaining backend work |
|---|---|---|---|
| Module/open dialog | Ready now | Workspace loaders already open loose content, installs, modules, and optional save slots. | Frontend only. |
| Object palette browser | Ready now | Proto-backed palette entries, search, category grouping, display names, descriptions, ART detail/preview, and tracked browser summaries already exist. | Frontend only. |
| Placement/stamping workflows | Ready now | Placement requests, sets, presets, tracked placement summaries, live previews, and apply helpers already exist. | Frontend only. |
| Map canvas shell | Ready now | Tracked world-edit shell bundles already compose top-down/isometric scenes, tool summaries, palette summaries, selection, and tracked placement preview. | Frontend only. |
| Selection/move/rotate/replace/erase | Ready now | Hit testing, persisted selection state, tracked selected-object summaries, and tracked transform/brush helpers already exist. | Frontend only. |
| Apply/save/reload/reopen loop | Ready now for normal world-edit flows | Pending/apply/save summaries, save-backed world-asset persistence, project restore, and shell/tool persistence already exist. | Mostly frontend polish plus deeper restore coverage over time. |
| Selected-object/proto inspector shell | Thin backend layer still required | Raw proto lookup, selection summaries, staged direct-asset history, and builder APIs already exist underneath. | Add one first-class inspector read model plus one staged write surface for non-transform object/proto edits. |
| Flags window | Thin backend layer still required | The lower object-property layer can represent the data, but no typed flag grouping is exposed to hosts. | Add typed flag projection/edit helpers above raw object fields. |
| Script attachments window | Thin backend layer still required | Script assets already have authoring/validation surfaces, and object script references are indexed, but there is no object-bound script-slot editor contract. | Add typed script-attachment summaries plus set/clear helpers for proto and selected-object workflows. |
| Light window | Split: sector lights ready, object-light panes not ready | Sector light add/replace/remove plus light preview already exist. | Add an object-light inspector contract and keep sector-light editing as the map-level companion workflow. |
| Level, spells, and skills windows | Data model exists, frontend contract missing | `CharacterBuilder` and `CharacterRecord` already model critter progression data cleanly. | Add selected-object/proto critter summaries plus staged update helpers above the current builders. |
| Generator window | Missing first-class backend contract | Only the lower object-property layer is available today. | Add typed field mapping, staged edit routing, and validation for generator settings. |
| Blending window | Missing first-class backend contract | Only the lower object-property layer is available today. | Add typed field mapping plus preview-aware edit helpers for blending/material settings. |

What this means in practice:

- you can start building the real editor frontend now if the first UI milestone is the browser/placement/scene/save shell shown in the screenshots
- you should not let the frontend invent the data contract for flags/scripts/light/progression/generator/blending panes; the SDK needs one explicit inspector slice first so those dialogs stay backend-owned and presentation-neutral

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
- default session command summaries now unify staged-local and applied-history `Undo`/`Redo` routing
- partial apply/save/discard by selected transaction exists
- applied history snapshots restore project/session shell state

Still missing:

- clearer dirty-baseline semantics
- stronger session-lifecycle guidance for hosts

Best next steps:

1. Tighten apply/save/discard/reopen semantics for mixed asset workflows.
2. Expose clearer dirty-baseline semantics independent of history depth.
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
- tracked terrain palette browser summaries and coordinate-based terrain selection helpers on `EditorWorkspaceSession`
- tracked object palette browser summaries and proto-number-based tracked placement selection on `EditorWorkspaceSession`
- tracked object-palette browser filters/selection now persist through tracked object-placement tool state, and the tracked browser selection can now be promoted into placement-set composition without losing inactive single-placement or preset context
- tracked selected-object summaries and tracked selection brush/transform/convenience helpers on `EditorWorkspaceSession`
- tracked object-placement preset libraries now have first-class lookup, replacement, selection, and removal helpers on `EditorWorkspaceSession`
- tracked world-edit shell bundles on `EditorWorkspaceSession` now compose parity-style top-down/isometric scenes, tool/browser summaries, selection, and tracked placement preview in one host-facing model, and shell preferences now have a first-class setter instead of raw map-view DTO rewrites
- scene hit testing and persisted map selection state
- tracked map-view terrain/object tool summaries and setters for host-ready tool binding, preview, and apply

Parity-floor status:

- hosts can now build terrain painting, object placement, object editing, selection, and parity-style top-down/isometric map shells mostly by binding SDK services
- common world-edit actions no longer require direct `.sec`, `.mob`, or proto manipulation
- the remaining work is now mainly the selected-object/proto inspector layer, richer authoring semantics, and persistence depth rather than missing map/palette shell primitives

Best next steps:

1. Add first-class selected-object/proto inspector contracts above the current map/palette shell floor.
2. Persist deeper world-edit workflow state beyond the current tool, browser, and shell-preference layer as the new shell contract is adopted by hosts.
3. Improve scene-preview fidelity and rendering semantics beyond the new parity-floor shell contract.

## Phase 3A - Object Inspector And Prototype Authoring

Goal:

- make the original editor's detail windows bindable through SDK-owned read/write contracts instead of raw object-property plumbing

Working now:

- `EditorWorkspace.FindProto(...)` and object palette entries already expose raw proto-backed read access
- tracked selected-object summaries already expose stable selection identity, owning sector paths, and preview metadata
- `MobDataBuilder`, `CharacterBuilder`, `CharacterRecord`, and `SectorBuilder` already provide the low-level mutation/data-model foundation
- save-backed world-asset persistence and staged direct-asset history already exist for the edits this layer would stage

Still missing:

- one host-facing inspector read model for a selected placed object or proto definition
- one public staged write surface for non-transform object/proto edits
- typed pane models for flags, script attachments, critter progression, object-light settings, generator settings, and blending settings
- capability discovery and project persistence guidance for those new inspector slices

Best next steps:

1. Add one selected-object/proto inspector summary that groups properties by editor pane rather than raw object field.
2. Add staged update helpers for flags, script attachments, critter progression, light settings, generator settings, and blending settings.
3. Extend capability discovery and project/session persistence once the inspector surface becomes stable enough for hosts to bind directly.

## Phase 4 - Cross-Asset Validation And Repair

Goal:

- make ArcNET safer and more automation-friendly than the original editor

Working now:

- first-pass workspace validation
- staged validation inspection for selected transactions
- whole-session pending summaries and blocked apply/save exceptions now expose staged repair candidates inline with validation and impact context, and pending target summaries now group those repairs by staged target
- staged repair candidates for dialog entry fixes, duplicate dialog-entry renumbering, disk-safe script-description normalization, unknown script attachment-slot cleanup, missing proto display-name entry authoring, and per-asset cleanup of broken direct-asset script/proto references
- script retargeting, proto retargeting, art replacement, proto display-name message-authoring, broken-proto cleanup helpers, and matching save-backed world-asset persistence
- host-facing pending-change and staged-transaction impact summaries that aggregate direct targets, direct/related asset categories, related referencing assets, touched maps, and referenced IDs
- blocked apply/save validation exceptions now carry both the staged impact summary and the scoped repair inventory for the failed commit attempt instead of only the raw blocking report

Still missing:

- broader repair coverage
- richer multi-asset impact summaries beyond the new direct-target, category, and related-asset layer
- more generalized cross-asset authoring helpers beyond the current script/proto/art/proto-display-name/cleanup/save-backed world layer

Best next steps:

1. Expand repair candidates beyond the current dialog-plus-script plus duplicate-dialog, unknown-attachment-slot, and proto-display-name repair slice.
2. Promote dependency-aware validation and impact reporting from the new impact-summary layer into broader repair and pre-save workflows.
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
- tracked terrain/object world-edit tool state, persisted object-palette browser state, preset libraries, and default tracked shell preferences now round-trip through `EditorProject` / `EditorProjectStore`
- project restore summaries and bootstrap summaries
- a minimum host capability contract through `EditorWorkspace.GetCapabilities()`

Still missing:

- deeper workflow persistence beyond the current terrain/object tool, object-palette browser, and shell-preference layer
- validators/importers/exporters/preview-provider plugin seams
- compatibility guidance for third-party extensions

Best next steps:

1. Persist deeper world-edit workflow state beyond the current tool, browser, and shell-preference layer.
2. Extend the new capability-discovery contract as more optional backend slices become versioned SDK features.
3. Add extension points only after the host-facing model is stable enough to version safely.

## Immediate Technical Priorities

| Priority | Area | Why |
|---|---|---|
| P1 | Land the object/proto inspector contract | This is the current blocker between frontend-ready map shells and the old editor's detail windows. |
| P1 | Finish the coherent editing core | This keeps the new frontend-bound workflows on one trustworthy undo/apply/save model. |
| P1 | Harden save/reload/reopen world-edit loops | Frontend work should be able to treat editing as one persistent product flow, not a demo path. |
| P2 | Improve scene fidelity and media support | Better preview fidelity directly reduces frontend reimplementation pressure. |
| P2 | Expand validation and repair | Still important, but no longer the first blocker for starting the editor UI. |
| P2 | Broaden dependency navigation | Larger content tooling still needs wider graph and relationship support. |
| P3 | Define the plugin/host model | This should happen before too many host-specific seams accumulate accidentally. |

## Next 10 Tasks

These are the best concrete tasks to move ArcNET toward a full Arcanum editor SDK:

| Rank | Task | Why now |
|---|---|---|
| 1 | Add a first-class selected-object/proto inspector summary | This is the main missing backend seam for the old editor's detail windows. |
| 2 | Add typed flags and script-attachment pane contracts | These are the most immediate inspector panes the frontend will need once the shell exists. |
| 3 | Add typed critter level, spells, and skills pane contracts | The underlying character data model already exists, so this is high-leverage frontend unblocker work. |
| 4 | Add typed light, generator, and blending pane contracts | These panes still need both field modeling and stable staged edit routing. |
| 5 | Persist deeper world-edit and inspector session state through `EditorProject` / `EditorProjectStore` | Tool, browser, and shell defaults now survive reopen, but richer workflow context still needs to survive across sessions. |
| 6 | Harden save/reload/reopen coverage for end-to-end world-edit frontend loops | The frontend should be able to trust day-to-day editing and reopen behavior before it grows larger. |
| 7 | Improve scene-preview object depth/order fidelity | Better scene fidelity lowers frontend complexity immediately. |
| 8 | Expand repair candidates beyond the current dialog-plus-script practical slice | This remains the clearest path to exceed the legacy editor once frontend-blocking seams are in place. |
| 9 | Add broader graph traversal and semantic navigation helpers | Asset relationships are already useful but still incomplete. |
| 10 | Extend capability discovery and define a minimum plugin surface around the new inspector slices | Extensibility should grow on top of the stabilized frontend-facing contract, not ahead of it. |

## Done Definition

ArcNET should only be described as supporting "most full editor needs" once all of the following are true:

- a host can open, browse, edit, validate, preview, apply, save, and reopen common Arcanum workflows without bespoke binary-format orchestration
- common terrain, object, dialog, script, and save workflows run through first-class editor-domain services rather than frontend-only orchestration
- legacy object-detail windows bind through typed SDK inspector contracts rather than raw object-field plumbing
- the session/history model feels coherent across cross-asset editing, not only within local editor scopes
- preview/media services are good enough that hosts do not need to recreate core scene/audio/art logic themselves
- the SDK reaches at least the practical workflow floor of the original Arcanum World Editor
- extension and host capability seams are explicit enough that multiple editor shells can grow on top of the same SDK without forking

## Success Criteria

ArcNET should consider this strategy successful when all of the following are true:

- a frontend can open one workspace object and discover the full editable world state
- frontends do not need bespoke binary-format orchestration or raw object-field plumbing for the original detail windows
- preview surfaces for maps, art, and audio are provided by SDK contracts rather than UI-specific ad hoc code
- world-edit, dialog, script, and save editing all use first-class editor-domain APIs
- most new editor features can be added without changing frontend architecture
