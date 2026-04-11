# Agent Guide: Bia.ValueBuffers Expansion + Consumer Migration

> **Status**: Phases 1–5 shipped in **v0.2.0** (2026-04-11).
> Commits `1bbf116`, `52f878d`, `6d1a7fc` on `main` — push `main` and tag `0.2.0` to publish.
> Phases 6–17 (ArcNET + OutbreakTracker2 migration) are pending.
>
> **Purpose**: Step-by-step instructions for an AI agent to implement five new ValueBuffer
> types/enhancements in `Bia.ValueBuffers`, then migrate `ArcNET` and `OutbreakTracker2` to use them.
>
> **Source repos** (all on disk, no cloning needed):
>
> | Repo | Local path |
> |------|-----------|
> | `Bia.ValueBuffers` | `C:\Users\Bia\source\repos\Bia.ValueBuffers` |
> | `ArcNET` | `C:\Users\Bia\source\repos\ArcNET` |
> | `OutbreakTracker2` | `C:\Users\Bia\source\repos\OutbreakTracker2` |
>
> **Prerequisite reading** before starting — read these files in full:
>
> - `src\Bia.ValueBuffers\ValueStringBuilder.cs`
> - `src\Bia.ValueBuffers\ValueRingBuffer.cs`
> - `src\Bia.ValueBuffers\ValueBuffers.cs`
> - `src\Bia.ValueBuffers.Test\ValueStringBuilderTests.cs`
> - `src\Bia.ValueBuffers.Test\ValueRingBufferTests.cs`

---

## Coding standards (apply to every file touched)

- All new public types: `[StructLayout(LayoutKind.Auto)]` on `ref struct` types.
- Stack-first, ArrayPool-fallback pattern (`Grow` private method) — mirror `ValueStringBuilder`.
- Methods that only read state must be `readonly`.
- Aggressive-inline all hot paths — `[MethodImpl(MethodImplOptions.AggressiveInlining)]`.
- One type per file. File name matches type name exactly.
- After all changes: run `dotnet csharpier format .` then `dotnet format` from the repo root.
- Never use `2>nul` — use `2>/dev/null` (Bash/MSYS2 environment).

---

## Phase 1 — `ValueByteBuffer` (new type) ✅ DONE

### 1.1 Purpose

A `ref struct` byte buffer equivalent of `ValueStringBuilder`. Stack-allocated initial storage, transparent `ArrayPool<byte>` overflow.  Eliminates the recurring `stackalloc`-or-`ArrayPool.Rent/Return` boilerplate pattern found throughout both consumer repos.

### 1.2 Create the file

**Path:** `src\Bia.ValueBuffers\ValueByteBuffer.cs`

### 1.3 Full specification

```
Namespace : Bia.ValueBuffers
Type      : public ref struct ValueByteBuffer
```

**Fields (private):**
```
byte[]? _arrayFromPool   // rented array; null while on stack
Span<byte> _bytes        // view of the active backing store
```

**Properties:**

| Name | Type | Description |
|------|------|-------------|
| `Length` | `int` (public get, private set) | Count of bytes written. |
| `Capacity` | `readonly int` | `_bytes.Length` |
| `WrittenSpan` | `readonly ReadOnlySpan<byte>` | `_bytes[..Length]` |
| `IsEmpty` | `readonly bool` | `Length == 0` |

**Constructor:**
```csharp
public ValueByteBuffer(Span<byte> initialBuffer)
// Sets _bytes = initialBuffer, Length = 0
```

**Write methods (all AggressiveInlining):**

```csharp
void Write(byte value)
void Write(ReadOnlySpan<byte> data)
void WriteUInt16LittleEndian(ushort value)   // BinaryPrimitives
void WriteInt16LittleEndian(short value)
void WriteUInt32LittleEndian(uint value)
void WriteInt32LittleEndian(int value)
void WriteUInt64LittleEndian(ulong value)
void WriteInt64LittleEndian(long value)
void WriteSingleLittleEndian(float value)
void WriteDoubleLittleEndian(double value)

void WriteUnmanaged<T>(in T value) where T : unmanaged
// MemoryMarshal.Write(_bytes[Length..], in value); Length += Unsafe.SizeOf<T>()
// Grow before writing if needed.
```

**Read/output methods:**
```csharp
byte[] ToArray()               // WrittenSpan.ToArray()
void CopyTo(Span<byte> dest)   // WrittenSpan.CopyTo(dest)
void Clear()                   // Length = 0; does NOT return pool array
void Dispose()                 // returns _arrayFromPool to ArrayPool<byte>.Shared if non-null; sets Length = 0
```

**Private growth:**
```csharp
[MethodImpl(MethodImplOptions.NoInlining)]
private void Grow(int additionalBytes)
// Double capacity until it fits. Rent new array, copy existing bytes, return old rented array.
```

**Usage pattern (mirror ValueStringBuilder pattern exactly):**
```csharp
using var buf = new ValueByteBuffer(stackalloc byte[256]);
buf.WriteUInt32LittleEndian(0xDEADBEEF);
buf.Write(someSpan);
var result = buf.ToArray(); // only allocates if needed
```

**Imports needed:** `System.Buffers`, `System.Buffers.Binary`, `System.Runtime.CompilerServices`, `System.Runtime.InteropServices`

### 1.4 Unit tests

**Path:** `src\Bia.ValueBuffers.Test\ValueByteBufferTests.cs`

Write tests covering:
- `Write(byte)` — single byte appears at correct index
- `Write(ReadOnlySpan<byte>)` — bulk data matches input
- `WriteUInt32LittleEndian` — verify bytes via `BinaryPrimitives.ReadUInt32LittleEndian`
- `WriteInt32LittleEndian` — same with signed value
- `WriteUnmanaged<int>` — result equals `WriteInt32LittleEndian` for the same value
- Growth past initial buffer — `ToArray()` still returns all bytes
- `Clear()` — `Length` resets, subsequent writes start from 0
- `Dispose()` — calling after pool graduation doesn't throw
- `CopyTo` — destination span matches `WrittenSpan`
- `WrittenSpan` empty on construction — `IsEmpty` is true
- Round-trip: write several primitives, read back with `BinaryPrimitives` at correct offsets

---

## Phase 2 — `ValueTokenizer` (new type) ✅ DONE

### 2.1 Purpose

A `ref struct` that scans a `ReadOnlySpan<char>` for delimiter-bounded tokens and yields each as a `ReadOnlySpan<char>` — zero allocation. Replaces `List<string>` + `.ToString()` per-token patterns in ArcNET's text-format parsers.

### 2.2 Create the file

**Path:** `src\Bia.ValueBuffers\ValueTokenizer.cs`

### 2.3 Full specification

```
Namespace : Bia.ValueBuffers
Type      : public ref struct ValueTokenizer
```

**Fields (private):**
```
ReadOnlySpan<char> _source    // full original input
int _pos                      // current scan position
char _open                    // opening delimiter
char _close                   // closing delimiter
```

**Constructor:**
```csharp
public ValueTokenizer(ReadOnlySpan<char> input, char open = '{', char close = '}')
// For parsers that scan flat {token} sequences.
```

**Properties:**

| Name | Type | Description |
|------|------|-------------|
| `Position` | `readonly int` | Current scan index |
| `IsAtEnd` | `readonly bool` | `_pos >= _source.Length` |

**Methods:**

```csharp
// Finds the next {…} token (no nesting support). Advances _pos past the close delimiter.
// Returns false when no more tokens exist.
bool TryReadNext(out ReadOnlySpan<char> token)

// Same but respects nested delimiters: {outer {inner} still outer} is a single token.
// Advances _pos past the outer close delimiter.
bool TryReadNested(out ReadOnlySpan<char> token)

// Resets _pos to 0.
void Reset()
```

**`TryReadNext` algorithm:**
1. Scan forward from `_pos` until `_open` character found.
2. Record `start = pos + 1`.
3. Find next `_close` from `start`.
4. If not found return `false`.
5. Set `token = _source[start..closeIdx]`, advance `_pos = closeIdx + 1`, return `true`.

**`TryReadNested` algorithm:**
1. Scan forward from `_pos` until `_open` character found; record position.
2. Enter depth-counting loop: `depth = 1`, `i = openIdx + 1`.
3. For each char: if `_open` → depth++; if `_close` → depth--; if `depth == 0` → break.
4. If loop ended without reaching depth 0 → return `false`.
5. `token = _source[(openIdx + 1)..i]` (excludes outer delimiters), `_pos = i + 1`, return `true`.

**Usage example:**
```csharp
// Line: {42}{sound_id}{Some text here}
var tokenizer = new ValueTokenizer(line.AsSpan());
while (tokenizer.TryReadNext(out var token))
    process(token); // token is a span slice, zero allocation
```

### 2.4 Unit tests

**Path:** `src\Bia.ValueBuffers.Test\ValueTokenizerTests.cs`

Write tests covering:
- Empty input — `TryReadNext` returns false immediately
- Single token — `{hello}` yields `hello`
- Multiple flat tokens — `{1}{2}{3}` yields three tokens in order
- Token with spaces — `{hello world}` yields `hello world` intact
- `TryReadNested` — `{outer {inner} end}` yields `outer {inner} end` as one token
- Deeply nested — `{a {b {c} b} a}` yields `a {b {c} b} a`
- `Position` advances correctly after each read
- `IsAtEnd` true after consuming all tokens
- `Reset()` — reads same tokens again after reset
- Mixed text outside tokens — `prefix{one}middle{two}` — reads `one` then `two`, ignores non-token chars
- Missing close delimiter — `TryReadNested` returns false
- Default delimiters are `{` and `}`

---

## Phase 3 — `ValueStringBuilder` enhancements ✅ DONE

### 3.1 Add `WrittenSpan` property

**File:** `src\Bia.ValueBuffers\ValueStringBuilder.cs`

Find the `Length` property. Directly below it, add:

```csharp
/// <summary>
/// Returns the written characters as a <see cref="ReadOnlySpan{T}"/> without allocating a string.
/// Use this to pass directly to APIs accepting <see cref="ReadOnlySpan{char}"/>
/// (e.g. <c>Encoding.GetBytes</c>, <c>TextWriter.Write</c>).
/// </summary>
public readonly ReadOnlySpan<char> WrittenSpan => _chars[..Length];
```

### 3.2 Add delegate-based `AppendJoin<T>` overload

**File:** `src\Bia.ValueBuffers\ValueStringBuilder.cs`

Find the last existing `AppendJoin` overload. Directly after it, add:

```csharp
/// <summary>
/// Appends each element from <paramref name="values"/> separated by <paramref name="separator"/>,
/// formatting each element via the caller-supplied <paramref name="append"/> delegate.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <param name="separator">The string placed between consecutive elements.</param>
/// <param name="values">The elements to join.</param>
/// <param name="append">Delegate that appends one element to this builder.</param>
public void AppendJoin<T>(ReadOnlySpan<char> separator, IEnumerable<T> values, AppendElement<T> append)
{
    bool first = true;
    foreach (T item in values)
    {
        if (!first)
            Append(separator);
        append(ref this, item);
        first = false;
    }
}

/// <summary>
/// Appends each element from <paramref name="values"/> separated by <paramref name="separator"/>,
/// formatting each element via the caller-supplied <paramref name="append"/> delegate.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
/// <param name="separator">A single character placed between consecutive elements.</param>
/// <param name="values">The elements to join.</param>
/// <param name="append">Delegate that appends one element to this builder.</param>
public void AppendJoin<T>(char separator, IEnumerable<T> values, AppendElement<T> append)
{
    bool first = true;
    foreach (T item in values)
    {
        if (!first)
            Append(separator);
        append(ref this, item);
        first = false;
    }
}
```

Add the delegate type at namespace scope in the same file (outside the `ref struct`):

```csharp
/// <summary>Delegate used by <see cref="ValueStringBuilder.AppendJoin{T}(char,IEnumerable{T},AppendElement{T})"/>.</summary>
public delegate void AppendElement<T>(ref ValueStringBuilder builder, T item);
```

### 3.3 Unit tests

**File:** `src\Bia.ValueBuffers.Test\ValueStringBuilderTests.cs`

Add tests:
- `WrittenSpan` returns correct slice without calling `ToString()`
- `WrittenSpan` empty (`Length == 0`) returns empty span
- `AppendJoin<T>(char, IEnumerable<T>, delegate)` — joins integers with `,` separator
- `AppendJoin<T>(ReadOnlySpan<char>, IEnumerable<T>, delegate)` — joins strings with `" | "` separator
- Empty sequence — delegate overloads produce empty string (no trailing separator)

---

## Phase 4 — `ValueRingBuffer<T>` enhancements ✅ DONE

### 4.1 Add `this[int index]` readonly indexer

**File:** `src\Bia.ValueBuffers\ValueRingBuffer.cs`

Find the `IsFull` property. Directly below it add:

```csharp
/// <summary>
/// Gets the element at <paramref name="index"/> in FIFO order (0 = oldest).
/// </summary>
/// <exception cref="ArgumentOutOfRangeException">
/// <paramref name="index"/> is negative or &gt;= <see cref="Count"/>.
/// </exception>
public readonly T this[int index]
{
    get
    {
        if ((uint)index >= (uint)Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be non-negative and less than Count.");
        return _buffer[(_tail + index) % _buffer.Length];
    }
}
```

### 4.2 Add `Drain(Span<T> destination)` method

**File:** `src\Bia.ValueBuffers\ValueRingBuffer.cs`

Find the `CopyTo` method. Directly after its closing brace add:

```csharp
/// <summary>
/// Copies all buffered elements in FIFO order (oldest first) into
/// <paramref name="destination"/> and resets <see cref="Count"/> to zero.
/// </summary>
/// <param name="destination">
/// The destination span. Must be at least <see cref="Count"/> elements long.
/// </param>
/// <exception cref="ArgumentException">
/// <paramref name="destination"/> has fewer than <see cref="Count"/> elements.
/// </exception>
public void Drain(Span<T> destination)
{
    CopyTo(destination);
    _head = 0;
    _tail = 0;
    Count = 0;
}
```

### 4.3 Add `ToArray()` method

**File:** `src\Bia.ValueBuffers\ValueRingBuffer.cs`

Directly after `Drain` add:

```csharp
/// <summary>
/// Returns a new array containing all buffered elements in FIFO order (oldest first).
/// </summary>
public readonly T[] ToArray()
{
    if (Count == 0)
        return [];
    var result = new T[Count];
    CopyTo(result);
    return result;
}
```

### 4.4 Unit tests

**File:** `src\Bia.ValueBuffers.Test\ValueRingBufferTests.cs`

Add tests:
- Indexer `[0]` on a full buffer returns oldest element
- Indexer `[Count - 1]` returns newest element
- Indexer throws `ArgumentOutOfRangeException` for index < 0
- Indexer throws `ArgumentOutOfRangeException` for index >= Count
- Indexer works correctly after wrap-around (write more than Capacity elements)
- `Drain` moves all elements to destination in insertion order
- `Drain` resets Count to 0 after call
- `Drain` throws `ArgumentException` when destination is too small
- Subsequent `Write` after `Drain` starts from index 0 again
- `ToArray` returns empty array when buffer is empty
- `ToArray` returns correct FIFO order
- `ToArray` after partial fill (Count < Capacity)
- `ToArray` after wrap-around

---

## Phase 5 — Build and test the library ✅ DONE

```shell
cd C:\Users\Bia\source\repos\Bia.ValueBuffers
dotnet build src\Bia.ValueBuffers\Bia.ValueBuffers.csproj
dotnet test src\Bia.ValueBuffers.Test\Bia.ValueBuffers.Test.csproj
```

Fix every compile error and every test failure before proceeding.  
**Do not proceed to Phase 6 if tests are red.**

---

## Phase 6 — ArcNET: `PrefixedString.Write` migration

### 6.1 Context

**File:** `C:\Users\Bia\source\repos\ArcNET\src\Core\ArcNET.Core\Primitives\PrefixedString.cs`

Current code manually branches on `StackAllocPolicy.MaxStackAllocBytes` (256), doing either `stackalloc` or `ArrayPool.Rent/Return`. Replace with `ValueByteBuffer`.

### 6.2 Add NuGet reference

**File:** `C:\Users\Bia\source\repos\ArcNET\src\Directory.Packages.props`

The entry already exists:
```xml
<PackageVersion Include="Bia.ValueBuffers" Version="0.1.0" />
```

**File:** `C:\Users\Bia\source\repos\ArcNET\src\Core\ArcNET.Core\ArcNET.Core.csproj`

Add a `PackageReference` if not already present:
```xml
<PackageReference Include="Bia.ValueBuffers" />
```

### 6.3 Rewrite `PrefixedString.Write`

Replace the entire `Write` method body with:

```csharp
public void Write(ref SpanWriter writer)
{
    var byteCount = Encoding.ASCII.GetByteCount(Value);
    writer.WriteUInt16((ushort)byteCount);
    using var buf = new ValueByteBuffer(stackalloc byte[Core.StackAllocPolicy.MaxStackAllocBytes]);
    if (byteCount > Core.StackAllocPolicy.MaxStackAllocBytes)
        buf = new ValueByteBuffer(byteCount); // heap-only path for over-limit strings
    Encoding.ASCII.GetBytes(Value, buf._bytes[..byteCount]); // write directly into backing store
    buf.Length = byteCount;
    writer.WriteBytes(buf.WrittenSpan);
}
```

> **Note to agent**: `ValueByteBuffer` does not expose `_bytes` or `Length` as settable from outside. Instead, use one of two approaches:
>
> **Preferred approach** — Use `Write(ReadOnlySpan<byte>)` to write the pre-encoded bytes:
>
> ```csharp
> public void Write(ref SpanWriter writer)
> {
>     var byteCount = Encoding.ASCII.GetByteCount(Value);
>     writer.WriteUInt16((ushort)byteCount);
>     using var buf = new ValueByteBuffer(stackalloc byte[Core.StackAllocPolicy.MaxStackAllocBytes]);
>     Span<byte> scratch = byteCount <= Core.StackAllocPolicy.MaxStackAllocBytes
>         ? stackalloc byte[byteCount]
>         : new byte[byteCount];
>     Encoding.ASCII.GetBytes(Value, scratch);
>     buf.Write(scratch);
>     writer.WriteBytes(buf.WrittenSpan);
> }
> ```
>
> Or even more directly — since `ValueByteBuffer` wraps encoding boilerplate, you can
> just encode straight into `buf` via `buf.Write`:
>
> ```csharp
> public void Write(ref SpanWriter writer)
> {
>     var byteCount = Encoding.ASCII.GetByteCount(Value);
>     writer.WriteUInt16((ushort)byteCount);
>     // stackalloc covers strings <= 256 bytes; larger allocate on heap only for getbytes step
>     Span<byte> scratch = byteCount <= Core.StackAllocPolicy.MaxStackAllocBytes
>         ? stackalloc byte[byteCount]
>         : System.Buffers.ArrayPool<byte>.Shared.Rent(byteCount).AsSpan(0, byteCount);
>     Encoding.ASCII.GetBytes(Value, scratch);
>     writer.WriteBytes(scratch);
>     if (byteCount > Core.StackAllocPolicy.MaxStackAllocBytes)
>         System.Buffers.ArrayPool<byte>.Shared.Return(/* original rented array */);
> }
> ```
>
> **Simplest correct rewrite** — `ValueByteBuffer` eliminates the branch entirely:
>
> ```csharp
> public void Write(ref SpanWriter writer)
> {
>     var byteCount = Encoding.ASCII.GetByteCount(Value);
>     writer.WriteUInt16((ushort)byteCount);
>     Span<byte> initial = stackalloc byte[Core.StackAllocPolicy.MaxStackAllocBytes];
>     using var buf = new ValueByteBuffer(initial);
>     // Grow if needed so WrittenSpan covers exactly byteCount bytes
>     if (byteCount > buf.Capacity)
>         buf.Write(new byte[byteCount]); // triggers Grow; alternatively use EnsureCapacity if added
>     // Write encoded bytes via a span the buffer owns
>     Span<byte> dest = initial.Length >= byteCount ? initial[..byteCount] : buf.ToArray().AsSpan();
>     Encoding.ASCII.GetBytes(Value, dest);
>     writer.WriteBytes(byteCount <= initial.Length ? dest : buf.WrittenSpan);
> }
> ```
>
> The cleanest approach is to add an `EnsureCapacity(int)` method to `ValueByteBuffer`
> (see Phase 6a) and then use:
>
> ```csharp
> public void Write(ref SpanWriter writer)
> {
>     var byteCount = Encoding.ASCII.GetByteCount(Value);
>     writer.WriteUInt16((ushort)byteCount);
>     using var buf = new ValueByteBuffer(stackalloc byte[Core.StackAllocPolicy.MaxStackAllocBytes]);
>     buf.EnsureCapacity(byteCount);
>     var span = buf.GetWritableSpan(byteCount); // returns slice from current Length
>     Encoding.ASCII.GetBytes(Value, span);
>     buf.AdvanceLength(byteCount);
>     writer.WriteBytes(buf.WrittenSpan);
> }
> ```

### 6.3a — Add `EnsureCapacity` and `GetWritableSpan` + `AdvanceLength` to `ValueByteBuffer`

These methods allow callers to request a contiguous writable slice without needing to
pre-build bytes separately. Add to `ValueByteBuffer`:

```csharp
/// <summary>Ensures the buffer can hold at least <paramref name="capacity"/> bytes total.</summary>
public void EnsureCapacity(int capacity)
{
    if (capacity > _bytes.Length)
        Grow(capacity - _bytes.Length);
}

/// <summary>
/// Returns a writable slice of <paramref name="count"/> bytes starting at <see cref="Length"/>.
/// Does NOT advance <see cref="Length"/>; call <see cref="AdvanceLength"/> after writing.
/// </summary>
public Span<byte> GetWritableSpan(int count)
{
    if (Length + count > _bytes.Length)
        Grow(count);
    return _bytes[Length..(Length + count)];
}

/// <summary>Advances <see cref="Length"/> by <paramref name="count"/> bytes.</summary>
public void AdvanceLength(int count) => Length += count;
```

### 6.4 Remove the old `using System.Buffers;` import from `PrefixedString.cs` if it is no longer needed after the rewrite.

### 6.5 Verify ArcNET.Core builds

```shell
dotnet build C:\Users\Bia\source\repos\ArcNET\src\Core\ArcNET.Core\ArcNET.Core.csproj
```

---

## Phase 7 — ArcNET: `SarEncoding.BuildSarBytes` migration

### 7.1 Context

**File:** `C:\Users\Bia\source\repos\ArcNET\src\Formats\ArcNET.Formats\SarEncoding.cs`

`BuildSarBytes` currently allocates `new byte[totalSize]` for every SAR serialization.

### 7.2 Rewrite `BuildSarBytes(int, int, int, ReadOnlySpan<byte>)`

Replace the method body to use `ValueByteBuffer`:

```csharp
internal static byte[] BuildSarBytes(int elementSize, int elementCount, int bitsetId, ReadOnlySpan<byte> elements)
{
    var bitsetCnt = (uint)((elementCount + BitsPerWord - 1) / BitsPerWord);
    var totalSize = 1 + SaHeaderSize + elements.Length + 4 + (int)(bitsetCnt * 4);

    using var buf = new ValueByteBuffer(stackalloc byte[512]);
    buf.EnsureCapacity(totalSize);

    buf.Write(1);                                          // presence
    buf.WriteUInt32LittleEndian((uint)elementSize);        // sa.size
    buf.WriteUInt32LittleEndian((uint)elementCount);       // sa.count
    buf.WriteUInt32LittleEndian((uint)bitsetId);           // sa.bitset_id
    buf.Write(elements);                                   // data

    buf.WriteUInt32LittleEndian(bitsetCnt);                // bitset count

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
}
```

Add `using Bia.ValueBuffers;` at the top of the file.

### 7.3 Verify

```shell
dotnet build C:\Users\Bia\source\repos\ArcNET\src\Formats\ArcNET.Formats\ArcNET.Formats.csproj
```

---

## Phase 8 — ArcNET: `TextDataFormat.Write` — remove double allocation

### 8.1 Context

**File:** `C:\Users\Bia\source\repos\ArcNET\src\Formats\ArcNET.Formats\TextDataFormat.cs`

Current code does `writer.WriteBytes(s_encoding.GetBytes(sb.ToString()))`.  
`sb.ToString()` allocates a string; `GetBytes(string)` then allocates a byte array.  
With `WrittenSpan`, both allocations are eliminated.

### 8.2 Change

In the `Write` method, replace:

```csharp
writer.WriteBytes(s_encoding.GetBytes(sb.ToString()));
```

with:

```csharp
writer.WriteBytes(s_encoding.GetBytes(sb.WrittenSpan));
```

> `Encoding` has a `GetBytes(ReadOnlySpan<char>)` overload that avoids the intermediate string.

### 8.3 Verify

```shell
dotnet build C:\Users\Bia\source\repos\ArcNET\src\Formats\ArcNET.Formats\ArcNET.Formats.csproj
```

---

## Phase 9 — ArcNET: `MessageFormat` — zero-allocation token parsing

### 9.1 Context

**File:** `C:\Users\Bia\source\repos\ArcNET\src\Formats\ArcNET.Formats\MessageFormat.cs`

`ReadAllBracedTokens(ReadOnlySpan<char>)` builds a `List<string>` calling `.ToString()` per token.
`ParseLine` then casts these strings with `int.TryParse`. Replace with `ValueTokenizer`.

### 9.2 Rewrite `ReadAllBracedTokens` → inline into `ParseLine`

Remove `ReadAllBracedTokens` entirely. Rewrite `ParseLine` as:

```csharp
private static MessageEntry? ParseLine(ReadOnlySpan<char> line)
{
    var tokenizer = new ValueTokenizer(line);

    if (!tokenizer.TryReadNext(out var tok0))
        return null;
    if (!int.TryParse(tok0, out var index))
        return null;

    if (!tokenizer.TryReadNext(out var tok1))
        return null;

    // Peek for a third token — if present, tok1 is the sound id.
    if (tokenizer.TryReadNext(out var tok2))
        return new MessageEntry(index, tok1.ToString(), tok2.ToString());

    return new MessageEntry(index, null, tok1.ToString());
}
```

> `tok1.ToString()` / `tok2.ToString()` are intentional — `MessageEntry` stores `string`
> and these allocations happen exactly once per entry. The previous code allocated
> strings for every token even when only two were needed.

Remove the now-unused `ReadAllBracedTokens` method and the `using Bia.ValueBuffers;` import is already present.

### 9.3 Verify

```shell
dotnet build C:\Users\Bia\source\repos\ArcNET\src\Formats\ArcNET.Formats\ArcNET.Formats.csproj
dotnet test C:\Users\Bia\source\repos\ArcNET\src\Formats\ArcNET.Formats.Tests\ArcNET.Formats.Tests.csproj
```

---

## Phase 10 — ArcNET: `DialogFormat` — zero-allocation field parsing

### 10.1 Context

**File:** `C:\Users\Bia\source\repos\ArcNET\src\Formats\ArcNET.Formats\DialogFormat.cs`

The local function `NextField()` scans for `{…}` with nesting support using index-based char iteration. Replace with `ValueTokenizer.TryReadNested`.

### 10.2 Refactor `ParseText`

Inspect the current `ParseText(string text)` implementation. It maintains a `pos` variable and calls the local `NextField()` function repeatedly to extract the 7 fields of each entry.

Rewrite the entry-parsing inner loop to:

```csharp
private static DlgFile ParseText(string text)
{
    var entries = new List<DialogEntry>();
    var remaining = text.AsSpan();
    int consumed = 0;

    while (consumed < text.Length)
    {
        var tokenizer = new ValueTokenizer(remaining, '{', '}');

        // Each entry requires exactly 7 nested fields.
        if (!tokenizer.TryReadNested(out var fNum)) break;
        if (!tokenizer.TryReadNested(out var fStr)) break;
        if (!tokenizer.TryReadNested(out var fGender)) break;
        if (!tokenizer.TryReadNested(out var fIq)) break;
        if (!tokenizer.TryReadNested(out var fCond)) break;
        if (!tokenizer.TryReadNested(out var fResp)) break;
        if (!tokenizer.TryReadNested(out var fAct)) break;

        if (!int.TryParse(fNum, out var num)) break;
        if (!int.TryParse(fIq, out var iq)) break;
        if (!int.TryParse(fResp, out var resp)) break;

        entries.Add(new DialogEntry
        {
            Num = num,
            Text = fStr.ToString(),
            GenderField = fGender.ToString(),
            Iq = iq,
            Conditions = fCond.ToString(),
            ResponseVal = resp,
            Actions = fAct.ToString(),
        });

        consumed += tokenizer.Position;
        remaining = text.AsSpan(consumed);
    }

    entries.Sort(static (a, b) => a.Num.CompareTo(b.Num));
    return new DlgFile { Entries = entries };
}
```

> **Important**: `ValueTokenizer.Position` must accurately track how far through the source
> span the tokenizer has consumed. Verify this against the existing test suite.

Remove the local `NextField` function and the `pos` field after the rewrite.

### 10.3 Verify

```shell
dotnet build C:\Users\Bia\source\repos\ArcNET\src\Formats\ArcNET.Formats\ArcNET.Formats.csproj
dotnet test C:\Users\Bia\source\repos\ArcNET\src\Formats\ArcNET.Formats.Tests\ArcNET.Formats.Tests.csproj
```

---

## Phase 11 — ArcNET: full build + test

```shell
cd C:\Users\Bia\source\repos\ArcNET
dotnet build src\
dotnet test src\
```

Fix any remaining failures before proceeding.

---

## Phase 12 — OutbreakTracker2: add NuGet reference

### 12.1 Add package version entry

**File:** `C:\Users\Bia\source\repos\OutbreakTracker2\src\Directory.Packages.props`

Add inside the `<ItemGroup>`:
```xml
<PackageVersion Include="Bia.ValueBuffers" Version="0.1.0" />
```

### 12.2 Add reference to Application project

**File:** `C:\Users\Bia\source\repos\OutbreakTracker2\src\application\OutbreakTracker2.Application\OutbreakTracker2.Application.csproj`

Add:
```xml
<PackageReference Include="Bia.ValueBuffers" />
```

### 12.3 Add reference to Memory lib

**File:** `C:\Users\Bia\source\repos\OutbreakTracker2\src\libs\OutbreakTracker2.Memory\OutbreakTracker2.Memory.csproj`

Add:
```xml
<PackageReference Include="Bia.ValueBuffers" />
```

### 12.4 Verify restore

```shell
dotnet restore C:\Users\Bia\source\repos\OutbreakTracker2\src\OutbreakTracker2.slnx
```

---

## Phase 13 — OutbreakTracker2: `MarkdownRunReportWriter` migration

### 13.1 `FormatContributions` — replace `StringBuilder`

**File:** `C:\Users\Bia\source\repos\OutbreakTracker2\src\application\OutbreakTracker2.Application\Services\Reports\MarkdownRunReportWriter.cs`

Current:
```csharp
private static string FormatContributions(IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> players)
{
    if (players.Count == 0)
        return string.Empty;

    StringBuilder sb = new(" — by ");
    for (int i = 0; i < players.Count; i++)
    {
        if (i > 0)
            sb.Append(", ");
        sb.Append("**").Append(players[i].PlayerName).Append("**");
    }

    return sb.ToString();
}
```

Replace with:
```csharp
private static string FormatContributions(IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> players)
{
    if (players.Count == 0)
        return string.Empty;

    Span<char> buf = stackalloc char[256];
    var sb = new ValueStringBuilder(buf);
    sb.Append(" \u2014 by ");
    for (int i = 0; i < players.Count; i++)
    {
        if (i > 0)
            sb.Append(", ");
        sb.Append("**");
        sb.Append(players[i].PlayerName);
        sb.Append("**");
    }
    return sb.ToString();
}
```

Add `using Bia.ValueBuffers;` at the top of the file.

### 13.2 `BuildMarkdown` — replace `StringBuilder`

The `BuildMarkdown` method builds a large markdown document using `StringBuilder sb = new()`.  
The method is `private static` (no async boundary — safe to use `ref struct`).

Replace:
```csharp
StringBuilder sb = new();
```
with:
```csharp
Span<char> mdBuf = stackalloc char[512];
var sb = new ValueStringBuilder(mdBuf);
```

Also remove all `CultureInfo.InvariantCulture` interpolated-string delegations where they appear as:
```csharp
sb.Append(CultureInfo.InvariantCulture, $"...")
```
These are `StringBuilder`-specific overloads. Replace each with the equivalent sequence of `sb.Append(...)` calls building the string from spans/primitives, or keep them as `sb.Append($"...")` if the interpolated string does not allocate frequently (acceptable for a report-generation path that runs once per session).

> **Agent note**: `MarkdownRunReportWriter.BuildMarkdown` is called once per run report —
> it is not a hot path. The goal is consistency (remove `StringBuilder` heap object), not
> micro-optimisation. It is acceptable to leave interpolated string literals on non-numeric
> lines and only switch numeric formatting to direct `Append(int/double)` overloads.

### 13.3 `AppendMonsterLog` — update signature and body

`AppendMonsterLog` takes `StringBuilder sb` as a parameter. After the `BuildMarkdown` change, its
parameter type must change to `ref ValueStringBuilder sb`. Update the method signature:

```csharp
private static void AppendMonsterLog(ref ValueStringBuilder sb, RunReport report)
```

And update the call site in `BuildMarkdown`:
```csharp
AppendMonsterLog(ref sb, report);
```

### 13.4 Verify

```shell
dotnet build C:\Users\Bia\source\repos\OutbreakTracker2\src\application\OutbreakTracker2.Application\OutbreakTracker2.Application.csproj
```

---

## Phase 14 — OutbreakTracker2: `RunReportService` — player summary builder

**File:** `C:\Users\Bia\source\repos\OutbreakTracker2\src\application\OutbreakTracker2.Application\Services\Reports\RunReportService.cs`

Locate the `AutoStartSession` method. Find:
```csharp
System.Text.StringBuilder sb = new();
bool first = true;
foreach (DecodedInGamePlayer p in _activePlayers.Values)
{
    if (!first)
        sb.Append(", ");
    sb.Append(p.Name)
        .Append(" HP:")
        .Append(p.CurHealth)
        .Append('/')
        .Append(p.MaxHealth)
        .Append(System.Globalization.CultureInfo.InvariantCulture, $" Virus:{p.VirusPercentage:F1}%");
    first = false;
}
playerSummary = sb.ToString();
```

Replace with:
```csharp
Span<char> psBuf = stackalloc char[512];
var sb = new ValueStringBuilder(psBuf);
bool first = true;
foreach (DecodedInGamePlayer p in _activePlayers.Values)
{
    if (!first)
        sb.Append(", ");
    sb.Append(p.Name);
    sb.Append(" HP:");
    sb.Append(p.CurHealth);
    sb.Append('/');
    sb.Append(p.MaxHealth);
    sb.Append(" Virus:");
    sb.Append(p.VirusPercentage, "F1");
    sb.Append('%');
    first = false;
}
playerSummary = sb.ToString();
```

Add `using Bia.ValueBuffers;` if not already present.

### 14.1 Verify

```shell
dotnet build C:\Users\Bia\source\repos\OutbreakTracker2\src\application\OutbreakTracker2.Application\OutbreakTracker2.Application.csproj
```

---

## Phase 15 — OutbreakTracker2: `SafeMemoryReader` — replace `ArrayPool` boilerplate

### 15.1 Context

**File:** `C:\Users\Bia\source\repos\OutbreakTracker2\src\libs\OutbreakTracker2.Memory\SafeMemory\SafeMemoryReader.cs`

Both `Read<T>` and `ReadStruct<T>` follow the pattern:
```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
try { ... }
finally { ArrayPool<byte>.Shared.Return(buffer); }
```

Replace with `ValueByteBuffer` - the `Dispose` call on the `using var` handles pool return automatically.

### 15.2 Rewrite the buffer section of `Read<T>`

Locate the section in `Read<T>` that rents a buffer. Replace:
```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(size);

try
{
    if (!SafeNativeMethods.ReadProcessMemory(hProcess, address, buffer, size, out int bytesRead))
    { ... }

    if (bytesRead != size)
    { ... }

    Span<byte> bufferSpan = buffer.AsSpan(0, size);
    ref T result = ref MemoryMarshal.Cast<byte, T>(bufferSpan)[0];

    return result;
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

With:
```csharp
using var valueBuf = new ValueByteBuffer(stackalloc byte[128]);
valueBuf.EnsureCapacity(size);
var span = valueBuf.GetWritableSpan(size);

if (!SafeNativeMethods.ReadProcessMemory(hProcess, address, span, size, out int bytesRead))
{
    // ... existing error logging and throw
}

if (bytesRead != size)
{
    // ... existing error logging and throw
}

valueBuf.AdvanceLength(size);
ref T result = ref MemoryMarshal.Cast<byte, T>(valueBuf.WrittenSpan)[0];
return result;
```

> **Note**: check whether `SafeNativeMethods.ReadProcessMemory` accepts a `Span<byte>` parameter
> or requires a `byte[]`. If it requires `byte[]`, the `ValueByteBuffer` approach does not apply
> cleanly to this particular call site — leave it as-is and add a comment noting the P/Invoke
> constraint. The goal is to remove the manual `ArrayPool.Rent/Return` boilerplate; if the
> P/Invoke signature forces `byte[]`, use `ArrayPool<byte>.Shared.Rent` but wrap the
> return in a helper to avoid leaking it.

### 15.3 Apply same pattern to `ReadStruct<T>`

Repeat the same substitution for the analogous try/finally block in `ReadStruct<T>`.

### 15.4 Add `using Bia.ValueBuffers;`

### 15.5 Verify

```shell
dotnet build C:\Users\Bia\source\repos\OutbreakTracker2\src\libs\OutbreakTracker2.Memory\OutbreakTracker2.Memory.csproj
```

---

## Phase 16 — OutbreakTracker2: `StringReader.Read` — replace `List<byte>` accumulator

### 16.1 Context

**File:** `C:\Users\Bia\source\repos\OutbreakTracker2\src\libs\OutbreakTracker2.Memory\String\StringReader.cs`

`Read` accumulates raw bytes from successive `ReadProcessMemory` calls into a `List<byte> bytes = new(chunkSize)`. The `List<byte>` grows element-by-element with potential multiple re-allocations.

Replace with `ValueByteBuffer`:

```csharp
const int chunkSize = 256;
using var bytesBuf = new ValueByteBuffer(stackalloc byte[chunkSize]);
byte[] chunk = new byte[chunkSize];
```

Then replace `bytes.Add(chunk[i])` with `bytesBuf.Write(chunk[i])` (single byte write),
and replace `bytes.Count` with `bytesBuf.Length` throughout the method.

When the final result is decoded, replace `[.. bytes]` (which spread-copies the list to array)
with `bytesBuf.WrittenSpan` in any call to `encoding.GetString(...)`.

> **Note on `encoding.GetString`**: check whether `encoding.GetString()` accepts `ReadOnlySpan<byte>`.
> It does in .NET 6+. Replace all `encoding.GetString([.. bytes])` with `encoding.GetString(bytesBuf.WrittenSpan)`.

Replace refs to `bytes.Count` in:
- `int toRead = Math.Min(chunkSize, maxSafeLength - bytes.Count);` → `bytesBuf.Length`
- `while (bytes.Count < maxSafeLength)` → `while (bytesBuf.Length < maxSafeLength)`

Also replace the `ListByteDetails` call argument: `offset, bytes` → update signature
or pass `bytesBuf.WrittenSpan` as needed.

### 16.2 Update `LogByteDetails` call site

`LogByteDetails(chunk[i], bytes.Count - 1, bytes, encoding)` passes `List<byte>`.  
Change the `LogByteDetails` parameter type from `List<byte> bytes` to `ReadOnlySpan<byte> bytes`
(it only reads from the list — look at the method body, which accesses `bytes[offset - 1]`).

### 16.3 Verify

```shell
dotnet build C:\Users\Bia\source\repos\OutbreakTracker2\src\libs\OutbreakTracker2.Memory\OutbreakTracker2.Memory.csproj
```

---

## Phase 17 — Full solution builds and final checks

### ArcNET
```shell
cd C:\Users\Bia\source\repos\ArcNET
dotnet build src\
dotnet test src\
dotnet csharpier format src\
dotnet format src\
```

### OutbreakTracker2
```shell
cd C:\Users\Bia\source\repos\OutbreakTracker2
dotnet build src\
dotnet test src\
dotnet csharpier format src\
dotnet format src\
```

### Bia.ValueBuffers
```shell
cd C:\Users\Bia\source\repos\Bia.ValueBuffers
dotnet build src\
dotnet test src\
dotnet csharpier format src\
dotnet format src\
```

---

## Checklist for each Phase before marking complete

- [ ] Code compiles with zero errors
- [ ] All pre-existing tests still pass
- [ ] New tests written and passing (Phases 1–5)
- [ ] `dotnet csharpier format` produces no diff
- [ ] No dead code left behind (removed methods/fields actually gone)
- [ ] No `using` directives left unused

---

## What NOT to migrate (do not touch)

| Location | Reason |
|----------|--------|
| `OutbreakTracker2.Application\Services\Embedding\WindowsWindowEmbedder.cs` | `new StringBuilder(256)` passed directly to `GetClassName` / `GetWindowText` P/Invoke — Win32 contract requires `StringBuilder` |
| `OutbreakTracker2.Application\Services\LogStorage\ReadOnlyObservableFixedSizeRingBuffer<T>` | Heap lifetime required; wraps `ObservableFixedSizeRingBuffer<T>` which is a UI-observable reference type |
| `OutbreakTracker2.Memory.String\StringReader.GetByteInterpretations` | Trace-only diagnostic path; called under `LogTrace` level; allocation cost is irrelevant |
| Any `async` method body | `ref struct` cannot cross `await` boundaries |

---

## Cross-reference: new API surface summary

### `ValueByteBuffer` (Phase 1)
```
ctor(Span<byte> initialBuffer)
int Length { get; }
int Capacity { get; }
ReadOnlySpan<byte> WrittenSpan { get; }
bool IsEmpty { get; }
void Write(byte)
void Write(ReadOnlySpan<byte>)
void WriteUInt16LittleEndian(ushort)
void WriteInt16LittleEndian(short)
void WriteUInt32LittleEndian(uint)
void WriteInt32LittleEndian(int)
void WriteUInt64LittleEndian(ulong)
void WriteInt64LittleEndian(long)
void WriteSingleLittleEndian(float)
void WriteDoubleLittleEndian(double)
void WriteUnmanaged<T>(in T) where T : unmanaged
void EnsureCapacity(int)
Span<byte> GetWritableSpan(int)
void AdvanceLength(int)
byte[] ToArray()
void CopyTo(Span<byte>)
void Clear()
void Dispose()
```

### `ValueTokenizer` (Phase 2)
```
ctor(ReadOnlySpan<char> input, char open = '{', char close = '}')
int Position { get; }
bool IsAtEnd { get; }
bool TryReadNext(out ReadOnlySpan<char> token)
bool TryReadNested(out ReadOnlySpan<char> token)
void Reset()
```

### `ValueStringBuilder` additions (Phase 3)
```
ReadOnlySpan<char> WrittenSpan { get; }
void AppendJoin<T>(char, IEnumerable<T>, AppendElement<T>)
void AppendJoin<T>(ReadOnlySpan<char>, IEnumerable<T>, AppendElement<T>)
delegate void AppendElement<T>(ref ValueStringBuilder, T)
```

### `ValueRingBuffer<T>` additions (Phase 4)
```
T this[int index] { get; }  -- readonly indexer
void Drain(Span<T> destination)
T[] ToArray()
```
