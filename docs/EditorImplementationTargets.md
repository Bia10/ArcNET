# ArcNET Editor SDK Implementation Targets

This document is the short execution plan for moving ArcNET from strong editor foundations to a frontend-ready SDK.

Use this document for implementation priority.
Use [EditorSdkRoadmap.md](EditorSdkRoadmap.md) for the broader product framing only.

## Working Definition of Success

ArcNET is editor-SDK ready when a host can build an Arcanum editor mostly as presentation work and does not need to:

- parse or rewrite binary formats directly
- reverse-engineer asset relationships or path conventions
- invent its own transaction, history, or dirty-state model
- build its own validation and repair layer for common authoring mistakes
- build its own preview math for maps, art, audio, and object placement

## Current Foundation

The following foundation is already real and should be treated as baseline, not future roadmap:

- install-backed and loose-content workspace loading through `EditorWorkspaceLoader`
- optional save-slot composition into the same workspace
- asset catalog, provenance, load diagnostics, and workspace validation
- transactional dialog, script, and save editing through `EditorWorkspaceSession`
- session apply and persistence back to content/save paths
- initial map, art, and audio preview surfaces
- live-data hardening for malformed sectors, mobs, protos, and decorated save-slot companions

## Priority Order

Build these targets in order unless a frontend integration proves a different dependency path.

1. Reversible session core
2. Cross-asset authoring services
3. Map and object composition
4. Preview and interaction adapters
5. Project and workspace persistence
6. Host and plugin contract

## Target 1: Reversible Session Core

Why this is first:
Without real undo/redo, every frontend has to invent the most failure-prone part of editor behavior.

Implementation targets:

- add a generalized history model on top of `EditorWorkspaceSession`
- support undo and redo across dialog, script, save, and future map/object edits
- support transaction grouping so one user action can contain multiple low-level mutations
- expose dirty baselines independently from undo history depth
- make apply and persist operate on the current history head instead of ad hoc staged state

Done when:

- a host can call `Undo()` and `Redo()` on one live session regardless of edited asset type
- session changes can be grouped into one reversible command
- save, dialog, and script edits all use the same history contract
- focused tests cover undo, redo, grouped actions, apply after undo, and persist after redo

## Target 2: Cross-Asset Authoring Services

Why this is next:
Editors need safe changes that span more than one file, not just isolated file editors.

Implementation targets:

- add cross-asset edit helpers for common operations such as script retargeting, dialog retargeting, and asset-reference replacement
- expand validation from findings-only into actionable repair candidates
- add dependency-aware pre-save validation that runs across the whole session
- add diff summaries for pending multi-asset changes so hosts can explain what will be written

Done when:

- a host can ask the SDK to update a reference and receive all affected asset changes in one session transaction
- validation can report both blocking errors and fixable issues
- session-level apply can fail with precise dependency diagnostics instead of partial writes
- tests cover multi-file edits and validation failures on real dependency shapes

## Target 3: Map and Object Composition

Why this is the main feature gap:
ArcNET can inspect maps and sectors well enough to support browsers and previews, but not yet well enough to power a real map editor shell.

Implementation targets:

- add high-level map and sector editing services rather than raw format mutation only
- add placed-object creation, deletion, movement, rotation, and proto-instancing helpers
- add tile, roof, block-mask, light, and tile-script editing helpers
- add reusable brush and palette primitives for frontend tools
- add selection-safe object identity and handle management for live editing

Done when:

- a host can place, move, and delete objects without touching raw `.mob` or `.sec` payloads
- a host can edit core sector layers through typed services
- the same map-edit APIs work inside the session history model
- focused tests cover map/object round-trip and mixed object-plus-sector edits

## Target 4: Preview and Interaction Adapters

Why this matters:
If frontend authors still need to build camera math, hit testing, and render-ready object projections themselves, ArcNET is not yet doing the expensive editor work for them.

Implementation targets:

- add shared camera and viewport math for map scenes
- add hit-testing and selection projection helpers for sectors, tiles, and placed objects
- enrich object preview data so placed entities can be rendered without host-specific reverse engineering
- extend audio support from wav samples to editor-meaningful cue and scheme resolution where possible
- define optional adapter seams for hosts that want to plug in their own render/audio backends

Done when:

- a host can render a map scene with camera transforms and layer toggles using ArcNET projection data
- a host can map pointer input back to tiles and objects through SDK helpers
- art, map, and audio previews all expose stable host-neutral models
- tests pin preview math and projection invariants without depending on a UI framework

## Target 5: Project and Workspace Persistence

Why this matters:
Real editors need to reopen exactly where the user left off, not just reload content.

Implementation targets:

- connect `EditorProject` and `EditorProjectStore` to live session state instead of only load metadata
- persist open assets, selections, bookmarks, tool state, and preview/camera state with typed models
- persist enough session metadata for hosts to restore an editing workspace without bespoke glue
- define versioned project-data migration rules

Done when:

- a host can reopen a project and restore the live workspace plus useful editing state
- project persistence survives normal SDK evolution through explicit version handling
- the project model is rich enough that frontend shells do not need parallel persistence formats for core editing state

## Target 6: Host and Plugin Contract

Why this comes last:
Extension points are only useful after the core editing model is stable enough to extend.

Implementation targets:

- define capability discovery for host shells
- add extension points for validators, importers, exporters, and preview providers
- define compatibility guidance for third-party extensions
- keep the plugin surface above the binary-format layer and below UI-specific widgets

Done when:

- a host can ask ArcNET what editor capabilities are available
- external packages can contribute validators or content handlers without forking `ArcNET.Editor`
- the extension contract is versioned and documented well enough for third-party tooling

## Explicit Non-Targets Right Now

These should not displace the targets above:

- building a full gameplay engine
- embedding a specific desktop or web UI framework into `ArcNET.Editor`
- inventing bespoke widgets before the underlying SDK services exist
- broad plugin infrastructure before the editing core is stable

## Suggested Immediate Work Queue

If the goal is to support editor frontends as fast as possible, the next slices should be:

1. Add generalized undo/redo and transaction grouping to `EditorWorkspaceSession`.
2. Add session-level dependency validation plus repair candidates for cross-asset edits.
3. Add high-level map/object composition services for placed objects and core sector layers.

Those three slices would move ArcNET from strong foundations into the first genuinely frontend-reducing editor-SDK tier.