using System.Buffers.Binary;

namespace ArcNET.Formats;

/// <summary>
/// Parsed contents of a <c>data2.sav</c> save-global file.
/// The unresolved bytes remain preserved verbatim in <see cref="RawBytes"/>, while the
/// verified alternating <c>[state, 50000+ id]</c> table is exposed as <see cref="IdPairs"/>.
/// </summary>
public sealed class Data2SavFile
{
    /// <summary>
    /// Original file bytes preserved verbatim. Writes patch only the decoded value slots in the
    /// verified ID pair table and keep every other byte unchanged.
    /// </summary>
    public required byte[] RawBytes { get; init; }

    /// <summary>
    /// INT32 index where the verified alternating <c>[value, id]</c> table begins.
    /// </summary>
    public required int IdPairTableStartInt { get; init; }

    /// <summary>
    /// Decoded <c>50000+</c> ID table entries in on-disk order.
    /// </summary>
    public IReadOnlyList<Data2SavIdPairEntry> IdPairs { get; init; } = [];

    /// <summary>First INT32 header word.</summary>
    public int Header0 => RawBytes.Length >= 4 ? BinaryPrimitives.ReadInt32LittleEndian(RawBytes) : 0;

    /// <summary>Second INT32 header word.</summary>
    public int Header1 => RawBytes.Length >= 8 ? BinaryPrimitives.ReadInt32LittleEndian(RawBytes.AsSpan(4, 4)) : 0;

    /// <summary>Total number of full INT32 values present in the file.</summary>
    public int TotalInts => RawBytes.Length / 4;

    /// <summary>Trailing bytes after the last full INT32, if any.</summary>
    public int TrailingBytes => RawBytes.Length % 4;

    /// <summary>
    /// Number of full INT32 values before the verified <c>50000+</c> ID pair table.
    /// This unresolved prefix region remains structurally addressable without treating the
    /// whole file as an untyped raw blob again.
    /// </summary>
    public int PrefixIntCount => Math.Max(0, IdPairTableStartInt);

    /// <summary>Last INT32 index occupied by the decoded ID pair table.</summary>
    public int IdPairTableEndInt => IdPairTableStartInt + IdPairs.Count * 2 - 1;

    /// <summary>
    /// Number of full INT32 values after the verified <c>50000+</c> ID pair table.
    /// Trailing non-INT32 bytes are excluded and still counted by <see cref="TrailingBytes"/>.
    /// </summary>
    public int SuffixIntCount => Math.Max(0, TotalInts - IdPairTableEndInt - 1);

    /// <summary>Returns one unresolved prefix INT32 value before the decoded pair table.</summary>
    public int GetPrefixInt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index >= PrefixIntCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return ReadInt32(RawBytes, index);
    }

    /// <summary>Returns one unresolved suffix INT32 value after the decoded pair table.</summary>
    public int GetSuffixInt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index >= SuffixIntCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return ReadInt32(RawBytes, IdPairTableEndInt + 1 + index);
    }

    /// <summary>
    /// Copies a contiguous unresolved prefix INT32 range into <paramref name="destination"/>,
    /// starting at <paramref name="startIndex"/>.
    /// </summary>
    public void CopyPrefixInts(int startIndex, Span<int> destination)
    {
        ValidateRegionCopyRange(startIndex, destination.Length, PrefixIntCount, nameof(startIndex));

        for (var index = 0; index < destination.Length; index++)
            destination[index] = ReadInt32(RawBytes, startIndex + index);
    }

    /// <summary>
    /// Copies a contiguous unresolved suffix INT32 range into <paramref name="destination"/>,
    /// starting at <paramref name="startIndex"/>.
    /// </summary>
    public void CopySuffixInts(int startIndex, Span<int> destination)
    {
        ValidateRegionCopyRange(startIndex, destination.Length, SuffixIntCount, nameof(startIndex));

        var baseIndex = IdPairTableEndInt + 1 + startIndex;
        for (var index = 0; index < destination.Length; index++)
            destination[index] = ReadInt32(RawBytes, baseIndex + index);
    }

    /// <summary>Returns the decoded value for <paramref name="id"/> when present.</summary>
    public bool TryGetIdPairValue(int id, out int value)
    {
        foreach (var entry in IdPairs)
        {
            if (entry.Id != id)
                continue;

            value = entry.Value;
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// Returns a mutable builder for batching multiple structural edits with copy-on-write semantics.
    /// Prefer this over chaining multiple <c>With*</c> calls when updating several decoded or
    /// unresolved regions of the same <c>data2.sav</c> payload.
    /// </summary>
    public Builder ToBuilder() => new(this);

    /// <summary>
    /// Returns a new file instance with the decoded value for <paramref name="id"/> replaced.
    /// The table shape and all unresolved bytes remain unchanged.
    /// </summary>
    public Data2SavFile WithIdPairValue(int id, int value)
    {
        var updated = IdPairs.ToArray();
        for (var index = 0; index < updated.Length; index++)
        {
            if (updated[index].Id != id)
                continue;

            updated[index] = updated[index] with { Value = value };
            return new Data2SavFile
            {
                RawBytes = RawBytes,
                IdPairTableStartInt = IdPairTableStartInt,
                IdPairs = updated,
            };
        }

        throw new KeyNotFoundException($"data2.sav does not contain decoded ID {id}.");
    }

    /// <summary>
    /// Returns a new file instance with one unresolved prefix INT32 replaced.
    /// The verified pair table shape and all other bytes remain unchanged.
    /// </summary>
    public Data2SavFile WithPrefixInt(int index, int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index >= PrefixIntCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return WithPatchedInt(index, value);
    }

    /// <summary>
    /// Returns a new file instance with a contiguous unresolved prefix INT32 range replaced.
    /// The verified pair table shape and all other bytes remain unchanged.
    /// </summary>
    public Data2SavFile WithPrefixInts(int startIndex, ReadOnlySpan<int> values)
    {
        ValidateRegionPatchRange(startIndex, values.Length, PrefixIntCount, nameof(startIndex));
        return WithPatchedInts(startIndex, values);
    }

    /// <summary>
    /// Returns a new file instance with one unresolved suffix INT32 replaced.
    /// The verified pair table shape and all other bytes remain unchanged.
    /// </summary>
    public Data2SavFile WithSuffixInt(int index, int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index >= SuffixIntCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return WithPatchedInt(IdPairTableEndInt + 1 + index, value);
    }

    /// <summary>
    /// Returns a new file instance with a contiguous unresolved suffix INT32 range replaced.
    /// The verified pair table shape and all other bytes remain unchanged.
    /// </summary>
    public Data2SavFile WithSuffixInts(int startIndex, ReadOnlySpan<int> values)
    {
        ValidateRegionPatchRange(startIndex, values.Length, SuffixIntCount, nameof(startIndex));
        return WithPatchedInts(IdPairTableEndInt + 1 + startIndex, values);
    }

    private Data2SavFile WithPatchedInt(int intIndex, int value)
    {
        var updatedBytes = RawBytes.ToArray();
        WriteInt32(updatedBytes, intIndex, value);
        return new Data2SavFile
        {
            RawBytes = updatedBytes,
            IdPairTableStartInt = IdPairTableStartInt,
            IdPairs = IdPairs,
        };
    }

    private Data2SavFile WithPatchedInts(int startIntIndex, ReadOnlySpan<int> values)
    {
        var updatedBytes = RawBytes.ToArray();
        for (var index = 0; index < values.Length; index++)
            WriteInt32(updatedBytes, startIntIndex + index, values[index]);

        return new Data2SavFile
        {
            RawBytes = updatedBytes,
            IdPairTableStartInt = IdPairTableStartInt,
            IdPairs = IdPairs,
        };
    }

    private static void ValidateRegionCopyRange(int startIndex, int count, int availableCount, string paramName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (startIndex + count > availableCount)
            throw new ArgumentOutOfRangeException(paramName);
    }

    private static void ValidateRegionPatchRange(int startIndex, int count, int availableCount, string paramName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        if (count == 0)
            throw new ArgumentException("At least one INT32 value is required.", nameof(count));

        if (startIndex + count > availableCount)
            throw new ArgumentOutOfRangeException(paramName);
    }

    private static int ReadInt32(byte[] bytes, int intIndex)
    {
        var start = intIndex * 4;
        return start >= 0 && start + 4 <= bytes.Length
            ? BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(start, 4))
            : 0;
    }

    private static void WriteInt32(byte[] bytes, int intIndex, int value)
    {
        var start = intIndex * 4;
        if (start < 0 || start + 4 > bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(intIndex));

        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(start, 4), value);
    }

    /// <summary>
    /// Fluent copy-on-write builder for <see cref="Data2SavFile"/>.
    /// Use <see cref="Data2SavFile.ToBuilder"/> to batch multiple pair-table and unresolved-region
    /// edits before one final <see cref="Build"/>.
    /// </summary>
    public sealed class Builder
    {
        private byte[] _rawBytes;
        private bool _sharesBytes;
        private Data2SavIdPairEntry[] _idPairs;
        private bool _sharesPairs;

        /// <summary>Creates a builder pre-populated from an existing <see cref="Data2SavFile"/>.</summary>
        public Builder(Data2SavFile from)
        {
            ArgumentNullException.ThrowIfNull(from);

            _rawBytes = from.RawBytes;
            _sharesBytes = true;
            _idPairs = [.. from.IdPairs];
            _sharesPairs = true;
            IdPairTableStartInt = from.IdPairTableStartInt;
        }

        /// <summary>INT32 index where the verified alternating <c>[value, id]</c> table begins.</summary>
        public int IdPairTableStartInt { get; }

        /// <summary>Decoded <c>50000+</c> ID table entries in on-disk order.</summary>
        public IReadOnlyList<Data2SavIdPairEntry> IdPairs => _idPairs;

        /// <summary>First INT32 header word.</summary>
        public int Header0 => _rawBytes.Length >= 4 ? BinaryPrimitives.ReadInt32LittleEndian(_rawBytes) : 0;

        /// <summary>Second INT32 header word.</summary>
        public int Header1 =>
            _rawBytes.Length >= 8 ? BinaryPrimitives.ReadInt32LittleEndian(_rawBytes.AsSpan(4, 4)) : 0;

        /// <summary>Total number of full INT32 values present in the file.</summary>
        public int TotalInts => _rawBytes.Length / 4;

        /// <summary>Trailing bytes after the last full INT32, if any.</summary>
        public int TrailingBytes => _rawBytes.Length % 4;

        /// <summary>Number of full INT32 values before the verified ID pair table.</summary>
        public int PrefixIntCount => Math.Max(0, IdPairTableStartInt);

        /// <summary>Last INT32 index occupied by the decoded ID pair table.</summary>
        public int IdPairTableEndInt => IdPairTableStartInt + _idPairs.Length * 2 - 1;

        /// <summary>Number of full INT32 values after the verified ID pair table.</summary>
        public int SuffixIntCount => Math.Max(0, TotalInts - IdPairTableEndInt - 1);

        /// <summary>Returns one unresolved prefix INT32 value before the decoded pair table.</summary>
        public int GetPrefixInt(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            if (index >= PrefixIntCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            return ReadInt32(_rawBytes, index);
        }

        /// <summary>Returns one unresolved suffix INT32 value after the decoded pair table.</summary>
        public int GetSuffixInt(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            if (index >= SuffixIntCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            return ReadInt32(_rawBytes, IdPairTableEndInt + 1 + index);
        }

        /// <summary>
        /// Copies a contiguous unresolved prefix INT32 range into <paramref name="destination"/>.
        /// </summary>
        public void CopyPrefixInts(int startIndex, Span<int> destination)
        {
            ValidateRegionCopyRange(startIndex, destination.Length, PrefixIntCount, nameof(startIndex));

            for (var index = 0; index < destination.Length; index++)
                destination[index] = ReadInt32(_rawBytes, startIndex + index);
        }

        /// <summary>
        /// Copies a contiguous unresolved suffix INT32 range into <paramref name="destination"/>.
        /// </summary>
        public void CopySuffixInts(int startIndex, Span<int> destination)
        {
            ValidateRegionCopyRange(startIndex, destination.Length, SuffixIntCount, nameof(startIndex));

            var baseIndex = IdPairTableEndInt + 1 + startIndex;
            for (var index = 0; index < destination.Length; index++)
                destination[index] = ReadInt32(_rawBytes, baseIndex + index);
        }

        /// <summary>Returns the decoded value for <paramref name="id"/> when present.</summary>
        public bool TryGetIdPairValue(int id, out int value)
        {
            foreach (var entry in _idPairs)
            {
                if (entry.Id != id)
                    continue;

                value = entry.Value;
                return true;
            }

            value = 0;
            return false;
        }

        /// <summary>Replaces the decoded value for <paramref name="id"/>.</summary>
        public Builder WithIdPairValue(int id, int value)
        {
            EnsureWritablePairs();
            for (var index = 0; index < _idPairs.Length; index++)
            {
                if (_idPairs[index].Id != id)
                    continue;

                _idPairs[index] = _idPairs[index] with { Value = value };
                return this;
            }

            throw new KeyNotFoundException($"data2.sav does not contain decoded ID {id}.");
        }

        /// <summary>Replaces one unresolved prefix INT32 value.</summary>
        public Builder WithPrefixInt(int index, int value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            if (index >= PrefixIntCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            EnsureWritableBytes();
            WriteInt32(_rawBytes, index, value);
            return this;
        }

        /// <summary>Replaces a contiguous unresolved prefix INT32 range.</summary>
        public Builder WithPrefixInts(int startIndex, ReadOnlySpan<int> values)
        {
            ValidateRegionPatchRange(startIndex, values.Length, PrefixIntCount, nameof(startIndex));

            EnsureWritableBytes();
            for (var index = 0; index < values.Length; index++)
                WriteInt32(_rawBytes, startIndex + index, values[index]);

            return this;
        }

        /// <summary>Replaces one unresolved suffix INT32 value.</summary>
        public Builder WithSuffixInt(int index, int value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            if (index >= SuffixIntCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            EnsureWritableBytes();
            WriteInt32(_rawBytes, IdPairTableEndInt + 1 + index, value);
            return this;
        }

        /// <summary>Replaces a contiguous unresolved suffix INT32 range.</summary>
        public Builder WithSuffixInts(int startIndex, ReadOnlySpan<int> values)
        {
            ValidateRegionPatchRange(startIndex, values.Length, SuffixIntCount, nameof(startIndex));

            EnsureWritableBytes();
            var baseIndex = IdPairTableEndInt + 1 + startIndex;
            for (var index = 0; index < values.Length; index++)
                WriteInt32(_rawBytes, baseIndex + index, values[index]);

            return this;
        }

        /// <summary>
        /// Builds an immutable <see cref="Data2SavFile"/> snapshot from the current builder state.
        /// Subsequent mutations on this builder use copy-on-write so previously built snapshots stay immutable.
        /// </summary>
        public Data2SavFile Build()
        {
            _sharesBytes = true;
            _sharesPairs = true;
            return new Data2SavFile
            {
                RawBytes = _rawBytes,
                IdPairTableStartInt = IdPairTableStartInt,
                IdPairs = _idPairs,
            };
        }

        private void EnsureWritableBytes()
        {
            if (!_sharesBytes)
                return;

            _rawBytes = _rawBytes.ToArray();
            _sharesBytes = false;
        }

        private void EnsureWritablePairs()
        {
            if (!_sharesPairs)
                return;

            _idPairs = [.. _idPairs];
            _sharesPairs = false;
        }
    }
}
