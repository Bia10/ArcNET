# Agent Guide: ArcNET — Bia.ValueBuffers Migration

> **Status**: Phases 0–4 pending implementation.
>
> **Purpose**: Step-by-step instructions for migrating all remaining ArcNET call sites
> to use `Bia.ValueBuffers` v0.2.0 (`ValueByteBuffer`, `ValueStringBuilder.WrittenSpan`,
> `ValueTokenizer`).
>
> **Prerequisite reading**: `docs/ValueBuffers_Expansion_AgentGuide.md`
> (describes the library API; Phases 1–5 of that guide are already shipped in v0.2.0).

---

## Already migrated (do not touch)

| File | Method | Type |
|------|--------|------|
| `MessageFormat.cs` | `Serialize()` | `ValueStringBuilder` |
| `DialogFormat.cs` | `Write()` | `ValueStringBuilder` |
| `TextDataFormat.cs` | `Write()` | `ValueStringBuilder` (except `sb.ToString()` → Phase 2) |
| `SectorDumper.cs` | outer `Dump()` | `ValueStringBuilder` |
| `SaveDumper.cs` | public `Dump()` | `ValueStringBuilder` |

---

## Phase 0 — Prerequisites (must run first)

### 0.1 — Bump package version

**File:** `src/Directory.Packages.props`

Change:
```xml
<PackageVersion Include="Bia.ValueBuffers" Version="0.1.0" />
```
to:
```xml
<PackageVersion Include="Bia.ValueBuffers" Version="0.2.0" />
```

> v0.2.0 adds `ValueByteBuffer.EnsureCapacity` / `GetWritableSpan` / `AdvanceLength`,
> `ValueStringBuilder.WrittenSpan`, delegate `AppendJoin<T>`, and `ValueTokenizer`.

### 0.2 — Add reference to ArcNET.Core

**File:** `src/Core/ArcNET.Core/ArcNET.Core.csproj`

Add inside the empty `<ItemGroup>` at line ~30:
```xml
<PackageReference Include="Bia.ValueBuffers" />
```

### 0.3 — Add reference to ArcNET.Archive

**File:** `src/Archive/ArcNET.Archive/ArcNET.Archive.csproj`

Add a new `<ItemGroup>` before the existing `<ProjectReference>` group:
```xml
<ItemGroup>
  <PackageReference Include="Bia.ValueBuffers" />
</ItemGroup>
```

### 0.4 — Verify restore

```shell
dotnet restore src\
```

---

## Phase 1 — `ValueByteBuffer` sites

### Phase 1a — `PrefixedString.Write()`

**File:** `src/Core/ArcNET.Core/Primitives/PrefixedString.cs`

**Current code** — manual two-branch pattern:
```csharp
// Use stackalloc for short strings; fall back to ArrayPool for long ones.
if (byteCount <= Core.StackAllocPolicy.MaxStackAllocBytes)
{
    Span<byte> buf = stackalloc byte[byteCount];
    Encoding.ASCII.GetBytes(Value, buf);
    writer.WriteBytes(buf);
}
else
{
    var buf = System.Buffers.ArrayPool<byte>.Shared.Rent(byteCount);
    try
    {
        Encoding.ASCII.GetBytes(Value, buf);
        writer.WriteBytes(buf.AsSpan(0, byteCount));
    }
    finally
    {
        System.Buffers.ArrayPool<byte>.Shared.Return(buf);
    }
}
```

**Replace with:**
```csharp
Span<byte> initial = stackalloc byte[Core.StackAllocPolicy.MaxStackAllocBytes];
using var buf = new ValueByteBuffer(initial);
buf.EnsureCapacity(byteCount);
var dest = buf.GetWritableSpan(byteCount);
Encoding.ASCII.GetBytes(Value, dest);
buf.AdvanceLength(byteCount);
writer.WriteBytes(buf.WrittenSpan);
```

Add `using Bia.ValueBuffers;` at the top of the file.
Remove `using System.Buffers;` (no longer needed — `ArrayPool` references are gone).

### 1.5 — Verify

```shell
dotnet build src\Core\ArcNET.Core\ArcNET.Core.csproj
```

---

### Phase 1b — `DatPacker` path encoding loop

**File:** `src/Archive/ArcNET.Archive/DatPacker.cs`

**Current code** — pre-allocated stack buffer + if/else ArrayPool fallback:
```csharp
Span<byte> stackNameBuf = stackalloc byte[StackAllocPolicy.MaxStackAllocBytes];
foreach (var (virtualPath, size, startOffset) in entryInfos)
{
    var byteCount = Encoding.ASCII.GetByteCount(virtualPath);
    var nameLen = byteCount + 1; // includes null terminator
    dirWriter.WriteInt32(nameLen);
    if (byteCount <= StackAllocPolicy.MaxStackAllocBytes)
    {
        var nameBuf = stackNameBuf[..byteCount];
        Encoding.ASCII.GetBytes(virtualPath, nameBuf);
        dirWriter.WriteBytes(nameBuf);
    }
    else
    {
        var rented = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            Encoding.ASCII.GetBytes(virtualPath, rented.AsSpan(0, byteCount));
            dirWriter.WriteBytes(rented.AsSpan(0, byteCount));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
    dirWriter.WriteByte(0); // null terminator
```

**Replace with:**
```csharp
Span<byte> nameBufStorage = stackalloc byte[StackAllocPolicy.MaxStackAllocBytes];
using var pathBuf = new ValueByteBuffer(nameBufStorage);
foreach (var (virtualPath, size, startOffset) in entryInfos)
{
    var byteCount = Encoding.ASCII.GetByteCount(virtualPath);
    var nameLen = byteCount + 1; // includes null terminator
    dirWriter.WriteInt32(nameLen);
    pathBuf.EnsureCapacity(byteCount);
    var dest = pathBuf.GetWritableSpan(byteCount);
    Encoding.ASCII.GetBytes(virtualPath, dest);
    pathBuf.AdvanceLength(byteCount);
    dirWriter.WriteBytes(pathBuf.WrittenSpan);
    pathBuf.Clear();
    dirWriter.WriteByte(0); // null terminator
```

Add `using Bia.ValueBuffers;` at the top of the file.
Keep `using System.Buffers;` — `ArrayBufferWriter<byte>` still uses it.
Remove the explicit `ArrayPool<byte>.Shared.Rent/Return` calls only.

### 1b verification

```shell
dotnet build src\Archive\ArcNET.Archive\ArcNET.Archive.csproj
```

---

### Phase 1c — `SarEncoding.BuildSarBytes`

**File:** `src/Formats/ArcNET.Formats/SarEncoding.cs`

**Current code** — single heap allocation then indexed writes:
```csharp
var totalSize = 1 + SaHeaderSize + elements.Length + 4 + (int)(bitsetCnt * 4);
var bytes = new byte[totalSize];
bytes[PresenceOffset] = 1;
BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(SizeFieldOffset), (uint)elementSize);
BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(CountFieldOffset), (uint)elementCount);
BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(BitsetIdOffset), (uint)bitsetId);
elements.CopyTo(bytes.AsSpan(DataOffset));
var postOffset = DataOffset + elements.Length;
BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(postOffset), bitsetCnt);
for (var i = 0; i < (int)bitsetCnt; i++)
{
    ...
    BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(postOffset + 4 + i * 4), word);
}
return bytes;
```

**Replace the body of the main overload** with sequential `ValueByteBuffer` writes:
```csharp
var bitsetCnt = (uint)((elementCount + BitsPerWord - 1) / BitsPerWord);
Span<byte> initial = stackalloc byte[256];
using var buf = new ValueByteBuffer(initial);
buf.Write((byte)1);                                      // presence
buf.WriteUInt32LittleEndian((uint)elementSize);          // sa.size
buf.WriteUInt32LittleEndian((uint)elementCount);         // sa.count
buf.WriteUInt32LittleEndian((uint)bitsetId);             // sa.bitset_id
buf.Write(elements);                                     // element data
buf.WriteUInt32LittleEndian(bitsetCnt);                  // bitset_cnt
for (var i = 0; i < (int)bitsetCnt; i++)
{
    uint word;
    if (i < (int)bitsetCnt - 1)
        word = 0xFFFFFFFF;
    else
    {
        var rem = elementCount % BitsPerWord;
        word = rem == 0 ? 0xFFFFFFFF : (1u << rem) - 1u;
    }
    buf.WriteUInt32LittleEndian(word);
}
return buf.ToArray();
```

Add `using Bia.ValueBuffers;` at the top of the file.
Remove `using System.Buffers.Binary;` only if it becomes unused (it stays — `BinaryPrimitives` constants are still used for the constants block). Actually verify whether `BinaryPrimitives` is still used after the change; if only used for the constants `BitsPerWord` etc., those are just `const int` not from BinaryPrimitives. Check whether the `using` is needed at all after rewrite — remove only if safe.

> **Note**: The `stackalloc byte[256]` covers headers up to 256 bytes (1 + 12 + 4 + bitset words).
> The element data `buf.Write(elements)` will trigger `Grow` for any non-trivial element payload,
> transparently promoting to `ArrayPool<byte>`. This is equivalent to the current `new byte[totalSize]`
> for large payloads.

### 1c verification

```shell
dotnet build src\Formats\ArcNET.Formats\ArcNET.Formats.csproj
```

---

## Phase 2 — `ValueStringBuilder.WrittenSpan` one-liner

### Phase 2a — `TextDataFormat.Write()`

**File:** `src/Formats/ArcNET.Formats/TextDataFormat.cs`

**Current code:**
```csharp
writer.WriteBytes(s_encoding.GetBytes(sb.ToString()));
```

**Replace with:**
```csharp
writer.WriteBytes(s_encoding.GetBytes(sb.WrittenSpan));
```

Rationale: `sb.ToString()` allocates a `string` object; `Encoding.GetBytes(string)` then allocates a
`byte[]`. Using `WrittenSpan` + `GetBytes(ReadOnlySpan<char>)` eliminates both intermediate allocations.

> **Note**: After calling `sb.WrittenSpan`, the builder is still valid and `sb.Dispose()` must still be
> called (or the builder scope must close). `ToString()` both returns the string AND disposes the builder,
> so removing it means no explicit dispose is needed here only if the builder is stack-scoped with no
> ArrayPool growth. To be safe, ensure the `ValueStringBuilder` variable goes out of scope at the end of
> the method (it does — it's declared inside `Write`). No `using` is required since it's purely stack-backed
> at 512 char initial size for typical text data files.

### 2a verification

```shell
dotnet build src\Formats\ArcNET.Formats\ArcNET.Formats.csproj
```

---

## Phase 3 — `ValueTokenizer` sites

### Phase 3a — `MessageFormat.ParseLine` / `ReadAllBracedTokens`

**File:** `src/Formats/ArcNET.Formats/MessageFormat.cs`

**Current code** — `ParseLine` + `ReadAllBracedTokens`:
```csharp
private static MessageEntry? ParseLine(ReadOnlySpan<char> line)
{
    var tokens = ReadAllBracedTokens(line);
    if (tokens.Count < 2)
        return null;

    if (!int.TryParse(tokens[0], out var index))
        return null;

    if (tokens.Count >= 3)
        return new MessageEntry(index, tokens[1], tokens[^1]);

    return new MessageEntry(index, null, tokens[^1]);
}

private static List<string> ReadAllBracedTokens(ReadOnlySpan<char> span)
{
    var tokens = new List<string>(3);
    var pos = 0;
    while (pos < span.Length)
    {
        if (span[pos] != '{') { pos++; continue; }
        var closeIdx = span[pos..].IndexOf('}');
        if (closeIdx < 0) break;
        tokens.Add(span[(pos + 1)..(pos + closeIdx)].ToString());
        pos += closeIdx + 1;
    }
    return tokens;
}
```

**Replace both methods** with one `ParseLine` using `ValueTokenizer`:
```csharp
private static MessageEntry? ParseLine(ReadOnlySpan<char> line)
{
    var tok = new ValueTokenizer(line);

    if (!tok.TryReadNext(out var t0))
        return null;

    if (!int.TryParse(t0, out var index))
        return null;

    if (!tok.TryReadNext(out var t1))
        return null;

    // Check for optional third token (sound id between index and text).
    if (tok.TryReadNext(out var t2))
        return new MessageEntry(index, t1.ToString(), t2.ToString());

    return new MessageEntry(index, null, t1.ToString());
}
```

Remove `ReadAllBracedTokens` entirely.
`using Bia.ValueBuffers;` is already present in the file.

### 3a verification

```shell
dotnet build src\Formats\ArcNET.Formats\ArcNET.Formats.csproj
dotnet test src\Formats\ArcNET.Formats.Tests\ArcNET.Formats.Tests.csproj
```

---

### Phase 3b — `DialogFormat.ParseText` / `NextField`

**File:** `src/Formats/ArcNET.Formats/DialogFormat.cs`

**Current code** — captured-variable closure with depth-counting:
```csharp
private static DlgFile ParseText(string text)
{
    var entries = new List<DialogEntry>();
    var pos = 0;

    string? NextField()
    {
        var start = text.IndexOf('{', pos);
        if (start < 0) return null;
        var depth = 1;
        var i = start + 1;
        while (i < text.Length && depth > 0)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}') depth--;
            i++;
        }
        if (depth != 0) return null;
        pos = i;
        return text[(start + 1)..(i - 1)];
    }

    while (true)
    {
        var numStr = NextField(); if (numStr is null) break;
        var str    = NextField(); if (str    is null) break;
        var gender = NextField(); if (gender is null) break;
        var iqStr  = NextField(); if (iqStr  is null) break;
        var cond   = NextField(); if (cond   is null) break;
        var respStr = NextField(); if (respStr is null) break;
        var acts   = NextField(); if (acts   is null) break;

        if (!int.TryParse(numStr.AsSpan().Trim(), out var num)
            || !int.TryParse(iqStr.AsSpan().Trim(), out var iq)
            || !int.TryParse(respStr.AsSpan().Trim(), out var resp))
            continue;

        entries.Add(new DialogEntry { Num = num, Text = str, GenderField = gender,
            Iq = iq, Conditions = cond, ResponseVal = resp, Actions = acts });
    }

    entries.Sort(static (a, b) => a.Num.CompareTo(b.Num));
    return new DlgFile { Entries = entries };
}
```

**Replace `ParseText`** — inline `ValueTokenizer.TryReadNested`, remove `NextField` closure,
parse numeric spans directly without `.AsSpan()`:
```csharp
private static DlgFile ParseText(string text)
{
    var entries = new List<DialogEntry>();
    var tok = new ValueTokenizer(text.AsSpan());

    while (true)
    {
        if (!tok.TryReadNested(out var numSpan)) break;
        if (!tok.TryReadNested(out var strSpan)) break;
        if (!tok.TryReadNested(out var genderSpan)) break;
        if (!tok.TryReadNested(out var iqSpan)) break;
        if (!tok.TryReadNested(out var condSpan)) break;
        if (!tok.TryReadNested(out var respSpan)) break;
        if (!tok.TryReadNested(out var actsSpan)) break;

        if (!int.TryParse(numSpan.Trim(), out var num)
            || !int.TryParse(iqSpan.Trim(), out var iq)
            || !int.TryParse(respSpan.Trim(), out var resp))
            continue;

        entries.Add(new DialogEntry
        {
            Num = num,
            Text = strSpan.ToString(),
            GenderField = genderSpan.ToString(),
            Iq = iq,
            Conditions = condSpan.ToString(),
            ResponseVal = resp,
            Actions = actsSpan.ToString(),
        });
    }

    entries.Sort(static (a, b) => a.Num.CompareTo(b.Num));
    return new DlgFile { Entries = entries };
}
```

Remove the `var pos = 0;` variable and the `NextField` local function.
`using Bia.ValueBuffers;` is already present.

### 3b verification

```shell
dotnet build src\Formats\ArcNET.Formats\ArcNET.Formats.csproj
dotnet test src\Formats\ArcNET.Formats.Tests\ArcNET.Formats.Tests.csproj
```

---

## Phase 4 — `ValueStringBuilder` heap replacements

### Phase 4a — `SectorDumper.counterParts`

**File:** `src/Dumpers/ArcNET.Dumpers/SectorDumper.cs`

**Current code** — `new StringBuilder()` inside `Dump()` which already owns a `ValueStringBuilder`:
```csharp
if (hasNonZeroCounter)
{
    var counterParts = new StringBuilder();
    for (var ci = 0; ci < 4; ci++)
    {
        var b = (byte)(counters >> (ci * 8));
        if (b != 0)
        {
            if (counterParts.Length > 0)
                counterParts.Append(", ");
            counterParts.Append($"[{ci}]=0x{b:X2}");
        }
    }
    vsb.AppendLine($"    Counters   : {counterParts}");
}
```

**Replace with** a stack-backed `ValueStringBuilder`:
```csharp
if (hasNonZeroCounter)
{
    Span<char> cpBuf = stackalloc char[64];
    var counterParts = new ValueStringBuilder(cpBuf);
    for (var ci = 0; ci < 4; ci++)
    {
        var b = (byte)(counters >> (ci * 8));
        if (b != 0)
        {
            if (counterParts.Length > 0)
                counterParts.Append(", ");
            counterParts.Append('[');
            counterParts.Append(ci);
            counterParts.Append("]=0x");
            counterParts.Append(b, "X2");
        }
    }
    vsb.Append("    Counters   : ");
    vsb.Append(counterParts.WrittenSpan);
    vsb.AppendLine();
    counterParts.Dispose();
}
```

> `counterParts` is at most 4 × `"[N]=0xXX, "` (10 chars) = 40 chars — well within
> the `stackalloc char[64]` initial buffer; `Grow` is never called.
> The inner `new StringBuilder()` heap object is eliminated.
> `using Bia.ValueBuffers;` is already present in `SectorDumper.cs`.

### 4a verification

```shell
dotnet build src\Dumpers\ArcNET.Dumpers\ArcNET.Dumpers.csproj
```

---

### Phase 4b — `SaveDumper` — 6 private helpers

**File:** `src/Dumpers/ArcNET.Dumpers/SaveDumper.cs`

All 6 private static methods currently begin with `var sb = new StringBuilder()`.
Replace each with a stack-backed `ValueStringBuilder`.

#### Pattern for methods that end with `return sb.ToString()`

Replace:
```csharp
var sb = new StringBuilder();
```
with:
```csharp
Span<char> sbBuf = stackalloc char[512];
var sb = new ValueStringBuilder(sbBuf);
```

Keep `return sb.ToString()` as-is — this both materialises the string and handles pool cleanup.

#### Methods affected

1. **`DumpNarrative(SaveInfo, IReadOnlyDictionary<string, byte[]>)`** — `var sb = new StringBuilder();`
   - Large potential output (section per map); `stackalloc char[512]` grows to ArrayPool automatically.
   - All `sb.AppendLine(...)` and `sb.Append(...)` calls map 1:1 to `ValueStringBuilder`.

2. **`DumpMultipleMobs(ReadOnlyMemory<byte>)`** — `var sb = new StringBuilder();`
   - Final return is `$"... {sb}"` — change to `$"... {sb.ToString()}"` to materialize and dispose.
   - Keep the early `return "No dynamic mobile objects"` unchanged; add `sb.Dispose()` before it.

3. **`DumpCompactDif(byte[])`** — `var sb = new StringBuilder();`
   - Two distinct early-return paths exist (Variant C and Variant A/B); both end with `return sb.ToString()`.

4. **`DumpDestroyedObjects(ReadOnlyMemory<byte>)`** — `var sb = new StringBuilder();`
   - Simple append loop; single `return sb.ToString()` at the end.

5. **`DumpModifiedObjects(ReadOnlyMemory<byte>)`** — `var sb = new StringBuilder();`
   - Final return is `count == 0 ? "No modified objects" : $"... {sb}"` — change to
     `count == 0 ? "No modified objects" : $"... {sb.ToString()}"`.
   - Add `sb.Dispose()` before the ternary return (or rely on `sb.ToString()` evaluating before disposal).

6. **`DumpTimeEvents(ReadOnlyMemory<byte>)`** — `var sb = new StringBuilder();`
   - Simple append loop + `return sb.ToString()`.

#### Remove `using System.Text;` from SaveDumper.cs if nothing else uses `StringBuilder` after migration.

Check usages before removing — `Encoding` is also in `System.Text`, so the `using` stays.

### 4b verification

```shell
dotnet build src\Dumpers\ArcNET.Dumpers\ArcNET.Dumpers.csproj
```

---

## Phase 5 — Full build + test

```shell
dotnet build src\
dotnet test src\
dotnet csharpier format src\
dotnet format src\
```

Fix every error and test failure before committing.

---

## Phase 6 — Probe: `BinaryDiff.PrintHexDiff` — 4x `StringBuilder` per row

**File:** `src/Probe/BinaryDiff.cs`
**Method:** `PrintHexDiff`
**Lines:** ~124–127

### 6.1 Problem

Four `new System.Text.StringBuilder()` instances are created inside a nested loop
(outer: diff regions; inner: 8-byte rows). For large binary diffs this means 4 heap
allocations per row.

```csharp
var sbHexA = new System.Text.StringBuilder();
var sbHexB = new System.Text.StringBuilder();
var sbAscA = new System.Text.StringBuilder();
var sbAscB = new System.Text.StringBuilder();
```

### 6.2 Replace

Declare four `ValueStringBuilder` instances before the row loop with `stackalloc char[64]`
each, and `Clear()` them at the top of every row iteration:

```csharp
Span<char> hexABuf = stackalloc char[64];
Span<char> hexBBuf = stackalloc char[64];
Span<char> ascABuf = stackalloc char[64];
Span<char> ascBBuf = stackalloc char[64];
var sbHexA = new ValueStringBuilder(hexABuf);
var sbHexB = new ValueStringBuilder(hexBBuf);
var sbAscA = new ValueStringBuilder(ascABuf);
var sbAscB = new ValueStringBuilder(ascBBuf);

// At the top of each row iteration:
sbHexA.Clear(); sbHexB.Clear(); sbAscA.Clear(); sbAscB.Clear();
```

After the row loop ends, call `.Dispose()` on each builder (or scope them in `using`).

Add `using Bia.ValueBuffers;` at the top of `BinaryDiff.cs`.

### 6.3 Verify

```shell
dotnet build src\Probe\Probe.csproj
```

---

## Phase 7 — Probe: `SarUtils` — multiple allocation patterns

**File:** `src/Probe/SarUtils.cs`

### Phase 7a — `FormatQuestStateBits` — `List<string>` + `string.Join`

**Method:** `FormatQuestStateBits`
**Line:** ~215

**Current code:**
```csharp
private static string FormatQuestStateBits(int state)
{
    var flags = new List<string>();
    if ((state & 0x001) != 0) flags.Add("active");
    ...
    return $"{string.Join("|", flags)} [0x{state:X3}]";
}
```

**Replace with:**
```csharp
private static string FormatQuestStateBits(int state)
{
    Span<char> buf = stackalloc char[128];
    var sb = new ValueStringBuilder(buf);
    bool any = false;
    if ((state & 0x001) != 0) { if (any) sb.Append('|'); sb.Append("active");      any = true; }
    // ... repeat for each flag ...
    sb.Append(" [0x");
    sb.Append(state, "X3");
    sb.Append(']');
    return sb.ToString();
}
```

Eliminates the `List<string>` and internal `string.Join` intermediary.

---

### Phase 7b — `FormatElements` — `new int[N]` scratch + `.ToArray()` on spans

**Method:** `FormatElements`
**Lines:** ~239, 247, 252

**Pattern 1** — scratch int array:
```csharp
var vals = new int[showCnt]; // showCnt <= 32
```
Replace with `stackalloc int[32]` (128 bytes < `MaxStackAllocBytes`); slice to `[..showCnt]`.

**Pattern 2** — unnecessary `.ToArray()` before hex string:
```csharp
Convert.ToHexString(raw.AsSpan(dataOff, ...).ToArray())
```
`Convert.ToHexString` accepts `ReadOnlySpan<byte>` directly in .NET 7+. Remove `.ToArray()`:
```csharp
Convert.ToHexString(raw.AsSpan(dataOff, showBytes))
```

No new `using` needed — this is entirely `stackalloc` + removing unnecessary heap calls.

---

### Phase 7c — `FormatSlotList` — `string.Join(",", intList)`

**Method:** `FormatSlotList`
**Lines:** ~171–172

**Current code:**
```csharp
return "[" + string.Join(",", slots) + "]";
return "[" + string.Join(",", slots.Take(maxShow)) + $",+{slots.Count - maxShow} more]";
```

`string.Join` boxes each `int` to `ToString()` and allocates an internal array.

**Replace with:**
```csharp
private static string FormatSlotList(List<int> slots, int maxShow)
{
    Span<char> buf = stackalloc char[256];
    var sb = new ValueStringBuilder(buf);
    sb.Append('[');
    var limit = Math.Min(slots.Count, maxShow);
    for (var i = 0; i < limit; i++)
    {
        if (i > 0) sb.Append(',');
        sb.Append(slots[i]);
    }
    if (slots.Count > maxShow)
    {
        sb.Append(",+");
        sb.Append(slots.Count - maxShow);
        sb.Append(" more");
    }
    sb.Append(']');
    return sb.ToString();
}
```

---

### Phase 7d — `BuildQuestLookup` — chain of Replace + Split + Join

**Method:** `BuildQuestLookup`
**Line:** ~816

**Current code:**
```csharp
var normalized = string.Join(
    ' ',
    entry.Text.Replace('\r', ' ')
              .Replace('\n', ' ')
              .Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries)
);
```

Two `Replace` calls produce two heap strings; `Split` produces a `string[]`; `Join` produces a fourth.

**Replace with:**
```csharp
Span<char> normBuf = stackalloc char[512];
var sb = new ValueStringBuilder(normBuf);
bool inWord = false;
foreach (var ch in entry.Text.AsSpan())
{
    if (ch is ' ' or '\t' or '\r' or '\n')
    {
        if (inWord) { sb.Append(' '); inWord = false; }
    }
    else
    {
        sb.Append(ch);
        inWord = true;
    }
}
// Trim trailing space if any was appended as word separator
if (sb.Length > 0 && sb.WrittenSpan[sb.Length - 1] == ' ')
    sb.Length--;
var normalized = sb.ToString();
```

Single pass, single final allocation.

### 7 Verify

```shell
dotnet build src\Probe\Probe.csproj
```

---

## Phase 8 — Formats: minor allocation removals

### Phase 8a — `ScriptFormat.Parse` — remove `.ToArray()` before ASCII decode

**File:** `src/Formats/ArcNET.Formats/ScriptFormat.cs`
**Method:** `Parse`
**Line:** ~101

**Current code:**
```csharp
var descBytes = reader.ReadBytes(DescriptionLength).ToArray();
var description = Encoding.ASCII.GetString(descBytes).TrimEnd('\0');
```

`DescriptionLength = 40` bytes. The `.ToArray()` allocates a 40-byte heap array only to
immediately pass it to `GetString`. `Encoding.ASCII.GetString` accepts `ReadOnlySpan<byte>`:

**Replace with:**
```csharp
var description = Encoding.ASCII.GetString(reader.ReadBytes(DescriptionLength)).TrimEnd('\0');
```

One-liner. No import changes needed.

---

### Phase 8b — `TextDataFormat.Parse` — span-based line iteration

**File:** `src/Formats/ArcNET.Formats/TextDataFormat.cs`
**Method:** `Parse`
**Line:** ~47

**Current code:**
```csharp
var text = s_encoding.GetString(reader.ReadBytes(reader.Remaining));
return new TextDataFile { Entries = ParseLines(text.Split('\n')) };
```

`GetString` decodes to a `string`; `Split('\n')` allocates a `string[]`.

**Replace with:**
```csharp
var text = s_encoding.GetString(reader.ReadBytes(reader.Remaining));
return new TextDataFile { Entries = ParseText(text.AsSpan()) };
```

Add a private span-based overload mirroring `MessageFormat.ParseText`:
```csharp
private static List<TextEntry> ParseText(ReadOnlySpan<char> text)
{
    var entries = new List<TextEntry>();
    foreach (var line in text.EnumerateLines())
    {
        // existing per-line logic from ParseLines, but operating on ReadOnlySpan<char>
    }
    return entries;
}
```

Keep `ParseLines(IEnumerable<string>)` for any external callers.

### 8 Verify

```shell
dotnet build src\Formats\ArcNET.Formats\ArcNET.Formats.csproj
dotnet test src\Formats\ArcNET.Formats.Tests\ArcNET.Formats.Tests.csproj
```

---

## Phase 9 — Dumpers: `MobDumper` inline append patterns

**File:** `src/Dumpers/ArcNET.Dumpers/MobDumper.cs`

### Phase 9a — `AppendFlagNames<T>` — remove `List<string>` intermediate

**Method:** `AppendFlagNames<T>`
**Lines:** ~454–468

**Current code:**
```csharp
var names = new List<string>();
foreach (var flag in Enum.GetValues<T>())
    if (flagVal != 0 && (value & flagVal) == flagVal)
        names.Add(flag.ToString());
if (names.Count > 0)
    vsb.AppendJoin(" | ".AsSpan(), (IEnumerable<string?>)names);
```

**Replace with:**
```csharp
bool firstFlag = true;
foreach (var flag in Enum.GetValues<T>())
{
    var flagVal = Convert.ToInt64(flag);
    if (flagVal == 0 || (value & flagVal) != flagVal) continue;
    if (!firstFlag) vsb.Append(" | ");
    vsb.Append(flag.ToString());
    firstFlag = false;
}
```

Eliminates the `List<string>` object entirely; `vsb` is already in scope.

---

### Phase 9b — `AppendSarValue` — remove LINQ chain

**Method:** `AppendSarValue`
**Line:** ~389

**Current code:**
```csharp
vsb.Append(string.Join(", ", vals.Take(8).Select(v => v.ToString())));
```

**Replace with:**
```csharp
var limit = Math.Min(vals.Length, 8);
for (var i = 0; i < limit; i++)
{
    if (i > 0) vsb.Append(", ");
    vsb.Append(vals[i]);
}
```

Eliminates LINQ iterator, boxed `ToString()`, and `string.Join` allocation.

### 9 Verify

```shell
dotnet build src\Dumpers\ArcNET.Dumpers\ArcNET.Dumpers.csproj
```

---

## Phase 10 — `SaveDumper` — byte-prepend anti-pattern

**File:** `src/Dumpers/ArcNET.Dumpers/SaveDumper.cs`
**Methods:** `DumpModifiedObjects`, `CountModifiedObjects`
**Lines:** ~309–312, ~723–726

### 10.1 Problem

Each method allocates a buffer the size of the entire remaining span just to prepend 4 bytes:

```csharp
var versionBytes = new byte[4];
BinaryPrimitives.WriteInt32LittleEndian(versionBytes, version);
var remaining = span.Slice(pos);
var combined = new byte[4 + remaining.Length]; // potentially 100s of KB
versionBytes.CopyTo(combined, 0);
remaining.CopyTo(combined.AsSpan(4));
var reader = new SpanReader(combined);
```

### 10.2 Replace with `ValueByteBuffer`

```csharp
var remaining = span.Slice(pos);
Span<byte> initial = stackalloc byte[256];
using var combined = new ValueByteBuffer(initial);
combined.WriteInt32LittleEndian(version);
combined.Write(remaining);
var reader = new SpanReader(combined.WrittenSpan);
```

For typical modified-object records (small blobs) the buffer stays on the stack.
For large records it falls back to `ArrayPool<byte>` via `ValueByteBuffer.Grow`.
Both cases are handled automatically.

Add `using Bia.ValueBuffers;` at the top of the file if not already present.

### 10.3 Verify

```shell
dotnet build src\Dumpers\ArcNET.Dumpers\ArcNET.Dumpers.csproj
```

---

## Phase 11 — Full build + test (post-Phase 6–10)

```shell
dotnet build src\
dotnet test src\
dotnet csharpier format src\
dotnet format src\
```

---

## What NOT to migrate

| Location | Reason |
|----------|--------|
| `TerrainFormat.cs` row buffers | Runtime-sized rows; always heap-scale; no benefit |
| `DocTest/PublicApiTest.cs` | Test infrastructure; do not change |
| Any P/Invoke `[DllImport]` that takes `StringBuilder` | Win32 contract; `StringBuilder` required |
