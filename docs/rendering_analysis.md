# Rendering Pipeline Fix — Verification Checklist

> **Commit:** `be59fbc fix(editor): preserve object insertion order and restore rendering pipeline fixes`  
> **Date:** 2026-05-19  
> **Source:** Restored from `fix/map-rendering` branch (commit `aec2e8d`)  

All 24 rendering regressions identified below have been verified present in the current codebase as of the commit above.

---

## EditorMapFloorRenderBuilder.cs

| # | Fix | Status |
|---|-----|--------|
| 1 | `ScaleObjectOffsets` uses `+40`/`+20` isometric pixel offsets — `(objectPreview.OffsetX + 40) * scaleX, (objectPreview.OffsetY + 20) * scaleY` | ✅ `line 978` |
| 2 | `TieBreakerSortKey` removed from `SortRawItems` object comparator — wall rendering draw order restored | ✅ `lines 809-816` |
| 3 | `GetLayoutSpriteCenter` simplified to `return (spriteBounds.MaxFrameCenterX, spriteBounds.MaxFrameCenterY)` — no wall/portal rotation adjustment | ✅ `line 1156` |
| 4 | `GetLayoutSpriteCenter` visibility changed from `internal` to `public` | ✅ `lines 1145, 1150` |
| 5 | `ProcessSector` uses `request.IncludeEmptyTiles` instead of `rowMask == 0` | ✅ `line 426` |

---

## EditorMapRenderSprite.cs

| # | Fix | Status |
|---|-----|--------|
| 6 | `IEditorMapRenderSpriteSource.PreloadAsync` interface method added | ✅ `line 101` |
| 7 | `EditorWorkspaceMapRenderSpriteSource.PreloadAsync` + `TryGetArtId` implementation | ✅ `lines 252-273` |
| 8 | Facade MES fallback in `TryResolveAssetPath` — resolves `art/facade/facadename.mes` for floor tile families | ✅ `lines 306-340` |
| 9 | `IsCompatibleFamily` added — filters sector art paths by render item kind (tile ↔ tile/facade, roof ↔ roof, else compatible) | ✅ `lines 529-548` |
| 10 | `AdjustSpriteCenter` simplified to `=> (centerX, centerY)` — no hotspot adjustment | ✅ `line 472` |
| 11 | `RequiresCeWallPortalHotspotAdjustment` removed — uses inline `StartsWith` checks in `UsesArtIdRotationForWallPortal` | ✅ (absent from file) |

---

## EditorWorkspace.cs

| # | Fix | Status |
|---|-----|--------|
| 12 | `PreloadArtsAsync` method — parallel batch preloading of ART files | ✅ `line 1228` |
| 13 | `_openArchives` `ConcurrentDictionary<string, DatArchive>` — caches open archive handles per source path | ✅ `line 30` |
| 14 | `IDisposable` + `Dispose` — properly disposes all cached archive handles | ✅ `line 24`, `lines 1319-1325` |

---

## EditorWorkspaceSession.cs

| # | Fix | Status |
|---|-----|--------|
| 15 | `CreateMapWorldEditSceneCoreAsync` is truly `async Task<...>` | ✅ `line 2970` |
| 16 | `PreloadAsync` call before `PaintableSceneBuilder.Build` in the pipeline | ✅ `line 3010` |
| 17 | Sync `CreateMapWorldEditScene` overloads removed — only `Async` variants remain | ✅ (absent from file) |
| 18 | `CreateTrackedMapWorldEditShellCore` (sync) removed — unified under async pipeline | ✅ (absent from file) |
| 19 | `0x3F` bitmask tile coordinate normalization in `IsSameTrackedObject` | ✅ `lines 12182-12184` |

---

## EditorMapScenePreviewBuilder.cs

| # | Fix | Status |
|---|-----|--------|
| 20 | `TryGetNormalizedSectorObjectLocation` uses `tileX & 0x3F` / `tileY & 0x3F` engine-accurate bitmask instead of sector-coordinate subtraction | ✅ `lines 363-364` |

---

## EditorMapSceneRenderSpaceMath.cs

| # | Fix | Status |
|---|-----|--------|
| 21 | `ContainsRenderPoint(EditorMapObjectRenderItem, ...)` added — object hit-testing uses sprite bounds instead of per-sector tile matching | ✅ `line 358` |
| 22 | `HitTestSceneSelection` uses `LastOrDefault()` for ObjectId — returns topmost rendered object | ✅ `line 308` |

---

## EditorProject.cs

| # | Fix | Status |
|---|-----|--------|
| 23 | Sync `LoadSession` / `LoadSessionWithRestoreResult` removed — only async variants remain | ✅ (absent, only `LoadSessionAsync` at `line 89`) |

---

## docs/rendering_analysis.md

| # | Fix | Status |
|---|-----|--------|
| 24 | Documentation file created with full verification checklist | ✅ (this file) |

---

## Quick Verification Commands

```powershell
# Check all 24 fixes at once
Select-String -Path src/Editor/ArcNET.Editor/EditorMapFloorRenderBuilder.cs -Pattern "(OffsetX \+ 40).*scaleX" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorMapFloorRenderBuilder.cs -Pattern "public static.*GetLayoutSpriteCenter" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorMapRenderSprite.cs -Pattern "PreloadAsync" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorMapRenderSprite.cs -Pattern "facadename" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorMapRenderSprite.cs -Pattern "IsCompatibleFamily" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorMapRenderSprite.cs -Pattern "=> \(centerX, centerY\)" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorWorkspace.cs -Pattern "PreloadArtsAsync" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorWorkspace.cs -Pattern "_openArchives" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorWorkspace.cs -Pattern "IDisposable" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorWorkspaceSession.cs -Pattern "CreateMapWorldEditSceneCoreAsync" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorWorkspaceSession.cs -Pattern "\.PreloadAsync\(" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorWorkspaceSession.cs -Pattern "0x3F" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorMapScenePreviewBuilder.cs -Pattern "0x3F" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorMapSceneRenderSpaceMath.cs -Pattern "ContainsRenderPoint\(EditorMapObjectRenderItem" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorMapSceneRenderSpaceMath.cs -Pattern "LastOrDefault" -NoEmphasis
Select-String -Path src/Editor/ArcNET.Editor/EditorProject.cs -Pattern "LoadSession" -NoEmphasis
```
