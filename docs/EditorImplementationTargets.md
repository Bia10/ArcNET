# ArcNET Editor SDK Implementation Targets

This document is the short execution plan for moving ArcNET from strong foundations to a full Arcanum editor SDK.

Use this document for implementation priority.
Use [EditorSdkRoadmap.md](EditorSdkRoadmap.md) for the broader review, comparison, and roadmap narrative.

## Working Definition of Success

ArcNET is ready to power a full editor SDK when a host can build an Arcanum editor mostly as presentation work and does not need to:

- parse or rewrite Arcanum binary formats directly
- reverse-engineer asset relationships, map layout rules, or object-placement semantics
- invent its own history, dirty-state, apply/save, or project-restore model
- rebuild the original world editor workflows from scratch just to reach basic parity
- implement its own camera math, hit testing, selection routing, terrain/object brush plumbing, or media preview decoding

For this project, "full editor SDK" means two things at once:

1. Match at least the practical authoring workflows the original Arcanum World Editor exposed.
2. Go beyond the original editor by offering safer transactions, stronger validation/repair, better persistence, and frontend-neutral APIs.

## Audit Snapshot As Of 2026-05-02

The current codebase is materially ahead of the original implementation-target draft.
The foundation is real, but full editor parity is not done yet.

What is already shipped and should be treated as baseline:

- install-backed and loose-content workspace loading through `EditorWorkspaceLoader`
- optional save-slot composition into the same workspace
- asset catalog, provenance, load diagnostics, and first-pass dependency summaries
- transactional dialog, script, and save editing through `EditorWorkspaceSession`
- session apply/save/discard flows plus applied history snapshots
- staged local undo/redo for dialog, script, save, and direct proto/mob/sector edits
- project metadata persistence for open assets, bookmarks, view state, map camera/selection/preview state, and tool state
- map outline and scene preview surfaces, hit testing, and selection projection
- typed sector layer editing for tile art, roof art, blocked tiles, lights, and tile scripts
- typed object placement, erase, replace, move, rotate, pitch-rotate, palette placement, placement sets, and placement presets
- ART preview and WAV preview surfaces for host-side browsers and preview panes
- host capability discovery through `EditorWorkspace.GetCapabilities()` with stable supported-versus-available backend slices, including tracked terrain/object workflow capabilities
- tracked map-view terrain and object-placement tool helpers through `EditorWorkspaceSession` summaries, setters, preview, and apply flows

The main remaining problem is not "no editor SDK exists."
The problem is that the SDK still does not cover enough complete editor workflows, at original-editor parity quality, through one coherent host-facing model.

## Original Editor Parity Review

The legacy Arcanum World Editor clearly demonstrates a parity floor ArcNET still has to meet:

- module/open workflow
- map canvas with isometric and top-down viewing modes
- terrain painting and terrain-art browsing
- item, scenery, and critter prototype browsing
- placed-object stamping and editing
- fast save/reload world-edit workflows

Against that parity floor, ArcNET currently looks like this:

| Original editor workflow | Current ArcNET status | What this means |
|---|---|---|
| Open game/module content | Strong | Install-backed and loose-content workspace loading are real and already frontend-friendly. |
| Save-slot composition | Strong | ArcNET goes beyond the original editor here with integrated save-backed workspace loading and typed save editing. |
| Terrain painting | Partial | Tile art, roof art, and blocked-tile editing exist, but a host still has to assemble more of the actual terrain-tool workflow than it should. |
| Terrain browsing/picking | Partial | Terrain/map property assets are indexed, but there is not yet a host-ready terrain palette/browser flow comparable to the original editor UX. |
| Critter/item/scenery palette workflows | Partial | Proto-backed object palette entries, placement requests, placement sets, and presets exist, but the SDK still lacks a more complete end-to-end placement workflow layer. |
| Object selection and manipulation | Partial | Hit testing, object IDs, move, rotate, pitch rotate, erase, replace, and transform helpers are present, but deeper live-edit semantics still need polish. |
| Lights and tile scripts | Partial | Typed sector light and tile-script mutation seams exist, but higher-level tool workflows around them are still thin. |
| Isometric/top-down scene support | Partial | Camera math, scene preview, and selection projection exist, but not a more opinionated host-facing scene/view contract that clearly reaches parity with the original editor modes. |
| Project reopen/restore | Partial | ArcNET already exceeds the original editor in typed persistence direction, but restore coverage is still not rich enough to declare the workflow done. |
| Undo/redo | Partial | Local staged undo/redo and applied history exist, but the full editing model still feels like coordinated slices rather than one unified editor command system. |

Conclusion:

- ArcNET already surpasses the old editor in foundation quality, save integration, validation direction, and project-model direction.
- ArcNET does not yet match the old editor in turnkey world-editing workflow completeness.
- The next priorities should therefore focus on parity-grade map/object authoring workflows before broader extensibility work.

## Priority Order

Build these targets in order unless a frontend integration proves a different dependency path:

1. Coherent session and history core
2. World-edit parity floor
3. Cross-asset authoring and repair
4. Preview and interaction adapters
5. Project and workspace persistence
6. Host and plugin contract

## Target 1: Coherent Session And History Core

Why this is first:
ArcNET already has real undo/redo and session orchestration pieces, but they still read more like adjacent subsystems than one default editor command model.

Current progress:

- staged local undo/redo exists for dialog, script, save, and direct asset drafts
- session-level staged transaction summaries and staged command summaries exist
- applied history snapshots exist and restore project/session shell state
- apply/save/discard can operate on all staged work or selected staged transactions

Implementation targets:

- unify staged and applied command semantics into one predictable host-facing editing model
- keep transaction grouping first-class for multi-step user actions
- expose clearer dirty baselines independent of history depth
- make default undo/redo routing deterministic for mixed asset workflows
- ensure apply/save/discard/reopen semantics stay coherent across all supported editor scopes

Done when:

- a host can bind one default `Undo` and one default `Redo` action without editor-specific branching
- session changes can be grouped and explained as one reversible user action
- dialog, script, save, and direct world edits all participate in one understandable command model
- focused tests cover staged undo/redo, applied undo/redo, grouped actions, partial apply/save, and restore-after-history flows

## Target 2: World-Edit Parity Floor

Why this is second:
The project goal is a full Arcanum editor SDK, and the original world editor sets the minimum practical parity bar for map authoring.

Current progress:

- tile art, roof art, blocked-tile, light, and tile-script editing seams are real
- object placement, erase, replace, move, rotate, pitch rotate, and combined transforms are real
- object palette search, placement requests, placement sets, and placement presets are real
- scene hit testing, area selection routing, and project-persisted map selection state are real
- tracked terrain-paint and object-placement tool summaries/setters now let hosts bind world-edit state with less manual project-state plumbing

Implementation targets:

- deepen the new tracked terrain/object tool layer so palette browsing and tool binding feel turnkey instead of merely structured
- promote palette browsing into clearer item/scenery/critter/world-object authoring flows
- add higher-level world-edit operations that feel like tools, not just mutation primitives
- define stronger scene/view contracts for parity-grade isometric and top-down editing shells
- cover more end-to-end map editing tasks through typed services instead of host orchestration

Done when:

- a host can build terrain painting, object placement, object editing, and selection tools mostly by binding SDK services
- the SDK exposes enough palette and brush workflows to match the original world editor feature floor
- common world-edit actions do not require hosts to manipulate raw `.sec`, `.mob`, or proto data directly
- focused tests cover mixed terrain-plus-object workflows and end-to-end world-edit round trips

## Target 3: Cross-Asset Authoring And Repair

Why this is next:
Parity with the old editor is not enough; ArcNET should be safer and more automation-friendly than the original tool.

Current progress:

- script retargeting and art-reference replacement already exist as session-level helpers
- dependency summaries already expose outgoing proto/script/art references plus incoming proto/script context
- validation findings already cover several missing-reference and local authoring issues
- repair candidates already exist, but only for a narrow dialog slice

Implementation targets:

- broaden cross-asset edit helpers beyond script/art retargeting into more complete authoring operations
- expand validation from findings-only into a richer repair system
- add dependency-aware pre-save validation across the effective staged session head
- add richer pending-change impact summaries so hosts can explain exactly what a transaction will touch

Done when:

- a host can request a common cross-asset update and receive one coherent staged transaction
- validation can report blocking issues, warnings, and actionable repair candidates across more than dialog editing
- session apply/save fails with precise dependency diagnostics instead of forcing hosts to infer what broke
- tests cover multi-asset edits and validation/repair flows on realistic dependency shapes

## Target 4: Preview And Interaction Adapters

Why this matters:
If hosts still need to reconstruct object projection, depth, media lookup, or selection math themselves, ArcNET is still leaving expensive editor work on the table.

Current progress:

- map outline and scene preview surfaces are real
- shared camera math, point hit testing, and area-hit resolution are real
- ART preview is real
- WAV preview is real

Implementation targets:

- improve object projection fidelity, depth/order semantics, and editor-meaningful scene data
- define clearer adapter seams for host rendering and media playback backends
- extend audio support beyond the current WAV-only practical slice
- connect scene-preview semantics more directly to parity-grade editor tool workflows

Done when:

- a host can render a credible editing scene without reverse-engineering Arcanum object-placement semantics
- pointer input, selection, and tool routing work through SDK helpers instead of host-specific math
- art, map, and audio previews expose stable host-neutral models with enough fidelity for serious editor shells
- tests pin projection math and preview invariants without requiring a UI framework

## Target 5: Project And Workspace Persistence

Why this matters:
A real editor must reopen where the user left off, not merely reload game files.

Current progress:

- `EditorProject` and `EditorProjectStore` already persist workspace references and substantial UI/session metadata
- map camera, selection, and preview state already have typed persisted models
- restore/load flows already reopen workspaces and return restore summaries

Implementation targets:

- persist richer live session state and workflow-specific tool state through the project model
- make restore more complete for complex world-edit workflows
- define versioned migration expectations before the project format grows further
- ensure project persistence is treated as part of the SDK contract, not just host convenience

Done when:

- a host can reopen a project and reliably restore the meaningful editing state for day-to-day workflows
- project persistence survives normal SDK evolution through explicit versioning and migration guidance
- frontend shells do not need parallel persistence formats for core editing state

## Target 6: Host And Plugin Contract

Why this comes last:
Extension points are only useful after the editor-facing model is stable enough to extend safely.

Current progress:

- a minimum capability-discovery contract now exists through `EditorWorkspace.GetCapabilities()`
- the capability model distinguishes SDK-supported backend slices from workspace-available slices
- no meaningful plugin extension surface was found in `ArcNET.Editor`

Implementation targets:

- expand capability discovery so hosts can reason about richer optional slices without probing ad hoc APIs
- add extension points for validators, importers, exporters, and preview providers
- document compatibility/versioning expectations for third-party extensions
- keep extension seams above binary-format code and below UI widgets

Done when:

- a host can query stable, documented backend capabilities and tell which ones are currently actionable for the loaded workspace
- external packages can extend validation, import/export, or preview behavior without forking `ArcNET.Editor`
- the public extension contract is versioned and documented well enough for third-party tooling

## Explicit Non-Targets Right Now

These should not displace the targets above:

- building a full gameplay engine
- embedding a specific desktop or web UI framework into `ArcNET.Editor`
- inventing bespoke widgets before the underlying SDK services exist
- broad plugin infrastructure before the editing core and parity-grade workflows are stable

## Suggested Immediate Work Queue

If the goal is to move toward a full-blown Arcanum editor SDK as quickly as possible, the next slices should be:

1. Finish the coherent session command model so hosts can treat staged and applied history as one editing system.
2. Extend the new tracked terrain workflow layer into richer palette browsing, brush binding, and selection-driven tool flows.
3. Extend the new tracked object-placement workflow layer into clearer palette-browser, preset-library, and edit-tool flows.
4. Expand repair candidates and dependency-aware validation beyond the current narrow dialog slice.
5. Improve scene-preview fidelity and view contracts to better match original-editor world-edit expectations.
6. Persist richer world-edit workflow state through `EditorProject` and then layer plugin seams above the new capability contract.

Those six slices would move ArcNET from "strong editor foundation" toward "credible full editor SDK with original-editor parity and room to exceed it."
