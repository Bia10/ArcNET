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
- invent raw `ObjectField`-level contracts for the old editor's detail windows
- implement its own camera math, hit testing, selection routing, terrain/object brush plumbing, or media preview decoding

For this project, "full editor SDK" means two things at once:

1. Match at least the practical authoring workflows the original Arcanum World Editor exposed.
2. Go beyond the original editor by offering safer transactions, stronger validation/repair, better persistence, and frontend-neutral APIs.

## Audit Snapshot As Of 2026-05-03

The current codebase is materially ahead of the original implementation-target draft.
The foundation is real, but full editor parity is not done yet.

What is already shipped and should be treated as baseline:

- install-backed and loose-content workspace loading through `EditorWorkspaceLoader`
- optional save-slot composition into the same workspace
- asset catalog, provenance, load diagnostics, and first-pass dependency summaries
- transactional dialog, script, and save editing through `EditorWorkspaceSession`
- session apply/save/discard flows plus applied history snapshots
- staged local undo/redo for dialog, script, save, and direct proto/mob/sector edits
- default session command summaries that unify staged-local and applied-history `Undo`/`Redo` routing through one host-facing surface
- host-facing pending-change and staged-transaction impact summaries that aggregate direct targets, related referencing assets, touched maps, and referenced IDs from the effective staged workspace state
- project metadata persistence for open assets, bookmarks, view state, map camera/selection/preview state, tool state, tracked terrain/object tool state, preset libraries, and default world-edit shell preferences
- map outline and scene preview surfaces, hit testing, and selection projection
- typed sector layer editing for tile art, roof art, blocked tiles, lights, and tile scripts
- typed object placement, erase, replace, move, rotate, pitch-rotate, palette placement, placement sets, and placement presets
- tracked terrain palette browser summaries and coordinate-based terrain selection helpers through `EditorWorkspaceSession`
- tracked object palette browser summaries and proto-number-based tracked placement selection through `EditorWorkspaceSession`
- tracked selected-object summaries plus tracked selection brush/transform/convenience helpers through `EditorWorkspaceSession`
- tracked object-placement preset-library lookup, replacement, selection, and removal helpers through `EditorWorkspaceSession`, with project-backed persisted state
- tracked world-edit shell bundles that compose parity-style top-down/isometric scenes, tool/browser summaries, selection state, and tracked placement preview through `EditorWorkspaceSession`
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
- selected-object and prototype detail windows for flags, script attachments, light settings, critter level, spells, skills, generator, and blending
- fast save/reload world-edit workflows

Against that parity floor, ArcNET currently looks like this:

| Original editor workflow | Current ArcNET status | What this means |
|---|---|---|
| Open game/module content | Strong | Install-backed and loose-content workspace loading are real and already frontend-friendly. |
| Save-slot composition | Strong | ArcNET goes beyond the original editor here with integrated save-backed workspace loading and typed save editing. |
| Terrain painting | Partial | Tile art, roof art, and blocked-tile editing exist, but a host still has to assemble more of the actual terrain-tool workflow than it should. |
| Terrain browsing/picking | Partial | Terrain/map property assets are indexed, and tracked terrain palette browser summaries plus coordinate selection now exist, but broader parity-grade browser/tool UX is still unfinished. |
| Critter/item/scenery palette workflows | Strong | Proto-backed object palette entries, tracked object palette browser summaries, proto-number selection, placement requests, placement sets, presets, and tracked shell bundling now cover the practical browser-plus-placement floor. |
| Object selection and manipulation | Strong | Hit testing, object IDs, move, rotate, pitch rotate, erase, replace, transforms, tracked selection summaries, and tracked convenience edit helpers now cover the practical manipulation floor. |
| Object/prototype detail windows | Missing first-class contract | The low-level data layer exists, but the public backend still stops short of a typed inspector read/write surface for the old detail panes. |
| Lights and tile scripts | Partial | Typed sector light and tile-script mutation seams exist, but only sector-light workflows are first-class today; object-light panes are still part of the missing inspector layer. |
| Isometric/top-down scene support | Strong | Camera math, scene preview, selection projection, and tracked world-edit shell bundles now expose a host-facing scene/view contract for parity-style top-down and isometric shells. |
| Project reopen/restore | Partial, stronger | ArcNET already exceeds the original editor in typed persistence direction, now including tracked terrain/object tool state, preset libraries, and default world-edit shell preferences, but restore coverage is still not rich enough to declare the workflow done. |
| Undo/redo | Partial | Local staged undo/redo and applied history now also expose one default session command surface, but dirty-baseline polish and broader editing-model cleanup still remain. |

Conclusion:

- ArcNET already surpasses the old editor in foundation quality, save integration, validation direction, and project-model direction.
- ArcNET now reaches a credible original-editor parity floor for host-driven map/object authoring through typed SDK services.
- The next priorities should therefore shift from basic parity-floor plumbing toward the missing object/proto inspector backend layer, then toward richer cross-asset authoring, repair, preview fidelity, and persistence depth.

## Frontend Construction Readiness

You can start frontend work now for:

- module/open flows
- object palette browsers with search, category, and art preview
- top-down and isometric map shells
- placement, selection, move/rotate/replace/erase workflows
- apply/save/reopen loops for normal world-edit work

Do not bind the following windows directly to the current public SDK yet:

- selected-object and proto inspector shells
- flags panes
- script-attachment panes
- critter level, spells, and skills panes
- object-light panes
- generator panes
- blending panes

Those windows need one explicit backend inspector layer so the frontend does not have to invent raw `ObjectField` or direct proto/mob mutation semantics.

## Priority Order

Build these targets in order unless a frontend integration proves a different dependency path:

1. Coherent session and history core
2. Object/proto inspector contract
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
- default session command summaries now unify staged-local and applied-history `Undo`/`Redo` routing for hosts that want one bindable command surface
- applied history snapshots exist and restore project/session shell state
- apply/save/discard can operate on all staged work or selected staged transactions

Implementation targets:

- keep transaction grouping first-class for multi-step user actions
- expose clearer dirty baselines independent of history depth
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
- tracked terrain palette browser summaries and coordinate-based terrain selection now let hosts build a terrain picker without dropping into raw workspace lookups
- tracked object palette browser summaries, persisted browser filters/selection, and proto-number-based tracked placement selection now let hosts build an object picker without dropping into raw workspace lookups
- tracked selected-object summaries plus tracked selection brush/transform/convenience helpers now let hosts drive basic live object edits from persisted map-view selection state
- tracked object-placement preset libraries now have first-class lookup, replacement, selection, and removal helpers instead of host-managed bookkeeping
- tracked browser-selected palette entries can now be promoted into placement-set composition while preserving inactive single-placement and preset context
- tracked world-edit shell bundles plus first-class shell-preference setters now let hosts bind parity-style top-down/isometric scenes, persist shell defaults, tool/browser state, selection, and tracked placement preview without rewriting raw map-view DTOs
- scene hit testing, area selection routing, and project-persisted map selection state are real
- tracked terrain-paint and object-placement tool summaries/setters now let hosts bind world-edit state with less manual project-state plumbing

Parity-floor assessment:

- hosts can now build terrain painting, object placement, object editing, selection, and parity-style top-down/isometric map shells mostly by binding SDK services
- common world-edit actions no longer require hosts to manipulate raw `.sec`, `.mob`, or proto data directly
- the remaining work in this area is now mostly workflow polish and richer authoring semantics, not missing parity-floor primitives

Done when:

- a host can build terrain painting, object placement, object editing, and selection tools mostly by binding SDK services
- the SDK exposes enough palette and brush workflows to match the original world editor feature floor
- common world-edit actions do not require hosts to manipulate raw `.sec`, `.mob`, or proto data directly
- focused tests cover mixed terrain-plus-object workflows and end-to-end world-edit round trips

Status:

- Target 2 is now effectively complete at the parity-floor level described above.
- The next frontend blocker is not more placement plumbing; it is the missing object/proto inspector layer captured in Target 2A below.

## Target 2A: Object Inspector And Prototype Authoring

Why this is next:
The map/palette shell is far enough along that the biggest blocker to building the old editor's remaining windows is now the missing inspector layer, not placement or scene plumbing.

Current progress:

- `EditorWorkspace.FindProto(...)` and object palette entries already expose raw proto-backed reads.
- `EditorMapObjectSelectionSummary` already gives stable selected-object identity, owning sector paths, and preview metadata.
- `EditorWorkspace.FindObjectInspectorSummary(...)` and `EditorWorkspaceSession.GetTrackedObjectInspectorSummary(...)` now expose one presentation-neutral selected-object/proto inspector summary, including pane-readiness metadata and staged proto display-name resolution.
- `EditorWorkspace.FindObjectInspectorFlagsSummary(...)`, `EditorWorkspaceSession.GetTrackedObjectInspectorFlagsSummary(...)`, `SetTrackedObjectInspectorFlags(...)`, and `SetProtoInspectorFlags(...)` now expose one typed flags contract and staged edit surface above raw `ObjectField` plumbing.
- `EditorWorkspace.FindObjectInspectorScriptAttachmentsSummary(...)`, `EditorWorkspaceSession.GetTrackedObjectInspectorScriptAttachmentsSummary(...)`, and the new set/clear helpers now expose one typed script-attachment contract with missing/unknown-slot reporting for proto and selected-object workflows.
- `EditorWorkspace.FindObjectInspectorCritterProgressionSummary(...)`, `EditorWorkspaceSession.GetTrackedObjectInspectorCritterProgressionSummary(...)`, `SetTrackedObjectInspectorCritterProgression(...)`, and `SetProtoInspectorCritterProgression(...)` now expose one typed critter progression contract and staged edit surface.
- `EditorWorkspace.FindObjectInspectorLightSummary(...)`, `EditorWorkspaceSession.GetTrackedObjectInspectorLightSummary(...)`, `SetTrackedObjectInspectorLight(...)`, and `SetProtoInspectorLight(...)` now expose one typed object-light contract and staged edit surface.
- `EditorWorkspace.FindObjectInspectorGeneratorSummary(...)`, `EditorWorkspaceSession.GetTrackedObjectInspectorGeneratorSummary(...)`, `SetTrackedObjectInspectorGenerator(...)`, and `SetProtoInspectorGenerator(...)` now expose one typed generator contract and staged edit surface.
- `EditorWorkspace.FindObjectInspectorBlendingSummary(...)`, `EditorWorkspaceSession.GetTrackedObjectInspectorBlendingSummary(...)`, `SetTrackedObjectInspectorBlending(...)`, and `SetProtoInspectorBlending(...)` now expose one typed blending/material contract and staged edit surface.
- `MobDataBuilder`, `CharacterBuilder`, `CharacterRecord`, and `SectorBuilder` already provide the low-level data/mutation substrate.
- sector light editing, script asset authoring, and save-backed world-asset persistence already cover adjacent workflows this layer can reuse.

Implementation targets:

- persist the broader inspector workflow cleanly now that the remaining non-transform object/proto panes have typed contracts
- harden selected-object preview/apply workflows around the remaining raw-property versus object-codec array mismatches
- broaden end-to-end save/reopen coverage across the full inspector slice

Per-window backend contract expectations:

| Window | Contract expectation |
|---|---|
| Flags | Done: typed boolean/enum groups by object type plus `SetTrackedObjectInspectorFlags(...)` / `SetProtoInspectorFlags(...)` now exist. |
| Script attachments | Done: typed known attachment points plus set/clear/retarget helpers and validation-friendly missing/unknown-slot reporting now exist. |
| Light | Done: keep sector-light list editing separate from object-light properties; the object-light pane now has typed read/write DTOs and staged helpers. |
| Level, spells, skills | Done: critter progression summaries and staged update helpers now sit above the existing `CharacterBuilder` / `CharacterRecord` substrate. |
| Generator | Done: typed spawn/generator settings plus staged edit helpers now exist. |
| Blending | Done: typed blending/material settings plus staged edit helpers now exist. |

Done when:

- the frontend can render the old detail windows without raw `ObjectField` plumbing
- selected-object and proto editing go through first-class session APIs instead of frontend-assembled direct-asset rewrites
- capability discovery can truthfully advertise which inspector panes are supported in the loaded workspace
- focused tests cover read-model composition plus staged apply/save for each pane group

## Target 3: Cross-Asset Authoring And Repair

Why this is next:
Parity with the old editor is not enough; ArcNET should be safer and more automation-friendly than the original tool.

Current progress:

- script retargeting and art-reference replacement already exist as session-level helpers
- proto reference retargeting now lets hosts migrate mob and sector object links to another loaded proto through `EditorWorkspaceSession`
- proto display-name authoring can now stage message-file edits, including creating `oemes/oname.mes` or updating `mes/description.mes`, through `EditorWorkspaceSession`
- save-backed workspaces now compose matching save-sector and save-mob overrides into the live workspace map view, and direct sector or mob saves target the loaded save slot instead of loose content when the save owns that asset path
- dependency summaries already expose outgoing proto/script/art references plus incoming proto/script context
- pending session and staged-transaction summaries now aggregate direct targets, direct/related asset categories, related referencing assets, touched maps, referenced IDs, and the current staged repair inventory for host-side impact explanations, with pending target summaries grouping repairs per staged target
- blocked apply/save validation exceptions now include the staged impact summary plus the scoped repair inventory so hosts can explain what the failed commit attempt was trying to touch and how it could be repaired
- validation findings already cover several missing-reference and local authoring issues
- repair candidates now cover dialog entry fixes, duplicate dialog-entry renumbering, disk-safe script-description normalization, unknown script attachment-slot cleanup, missing proto display-name entry authoring, and per-asset cleanup of broken direct-asset script/proto references through one session repair surface

Implementation targets:

- broaden cross-asset edit helpers beyond the current script/art/proto retargeting, art replacement, proto-display-name authoring, cleanup operations, and matching save-backed world-asset persistence into more complete authoring flows
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
- tracked terrain/object world-edit tool state, persisted object-palette browser state, preset libraries, and default tracked shell preferences now round-trip through `EditorProject` / `EditorProjectStore`
- restore/load flows already reopen workspaces and return restore summaries

Implementation targets:

- persist deeper live session state beyond the current terrain/object tool, object-palette browser, preset-library, and shell-preference layer through the project model
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

1. Persist deeper world-edit and inspector workflow state through `EditorProject`.
2. Improve scene-preview fidelity and rendering semantics beyond the current shell contract.
3. Resolve the remaining selected-object preview mismatches for array-backed staged inspector edits.
4. Harden end-to-end inspector apply/save coverage across the now-complete pane contract set.

Those four slices would move ArcNET from "strong editor foundation" toward "front-end-ready editor SDK that can honestly back the old editor's browser and detail windows."
