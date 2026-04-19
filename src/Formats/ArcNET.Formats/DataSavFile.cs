using System.Buffers.Binary;

namespace ArcNET.Formats;

/// <summary>
/// Parsed contents of a <c>data.sav</c> save-global file.
/// The current typed surface is structural rather than semantic: it exposes the verified
/// 8-byte header, aligned INT32[4] rows, and trailing remainder while preserving all bytes verbatim.
/// </summary>
public sealed class DataSavFile
{
    private const int HeaderIntCount = 2;
    private const int QuadWidthInts = 4;

    /// <summary>
    /// Original file bytes preserved verbatim. Structural mutation helpers patch only the
    /// requested INT32 slots and keep every other byte unchanged.
    /// </summary>
    public required byte[] RawBytes { get; init; }

    /// <summary>First INT32 header word.</summary>
    public int Header0 => ReadInt32(RawBytes, 0);

    /// <summary>Second INT32 header word.</summary>
    public int Header1 => ReadInt32(RawBytes, 1);

    /// <summary>Total number of full INT32 values present in the file.</summary>
    public int TotalInts => RawBytes.Length / 4;

    /// <summary>Trailing bytes after the last full INT32, if any.</summary>
    public int TrailingBytes => RawBytes.Length % 4;

    /// <summary>Number of full aligned INT32[4] rows after the 8-byte header.</summary>
    public int QuadRowCount
    {
        get
        {
            if (TotalInts < HeaderIntCount)
                return 0;

            return (TotalInts - HeaderIntCount) / QuadWidthInts;
        }
    }

    /// <summary>
    /// Number of full INT32 remainder values after the last aligned row.
    /// This excludes any trailing non-INT32 bytes counted by <see cref="TrailingBytes"/>.
    /// </summary>
    public int RemainderIntCount
    {
        get
        {
            if (TotalInts < HeaderIntCount)
                return 0;

            return (TotalInts - HeaderIntCount) % QuadWidthInts;
        }
    }

    /// <summary>Returns the aligned INT32[4] row at <paramref name="rowIndex"/>.</summary>
    public DataSavQuadRow GetQuadRow(int rowIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);
        if (rowIndex >= QuadRowCount)
            throw new ArgumentOutOfRangeException(nameof(rowIndex));

        var baseInt = HeaderIntCount + rowIndex * QuadWidthInts;
        return new DataSavQuadRow(
            ReadInt32(RawBytes, baseInt),
            ReadInt32(RawBytes, baseInt + 1),
            ReadInt32(RawBytes, baseInt + 2),
            ReadInt32(RawBytes, baseInt + 3)
        );
    }

    /// <summary>
    /// Copies a contiguous aligned INT32[4] row range into <paramref name="destination"/>,
    /// starting at <paramref name="startRowIndex"/>.
    /// </summary>
    public void CopyQuadRows(int startRowIndex, Span<DataSavQuadRow> destination)
    {
        ValidateCopyRange(startRowIndex, destination.Length, QuadRowCount, nameof(startRowIndex));

        var baseInt = HeaderIntCount + startRowIndex * QuadWidthInts;
        for (var index = 0; index < destination.Length; index++)
        {
            var rowInt = baseInt + index * QuadWidthInts;
            destination[index] = new DataSavQuadRow(
                ReadInt32(RawBytes, rowInt),
                ReadInt32(RawBytes, rowInt + 1),
                ReadInt32(RawBytes, rowInt + 2),
                ReadInt32(RawBytes, rowInt + 3)
            );
        }
    }

    /// <summary>Returns the INT32 remainder value at <paramref name="index"/>.</summary>
    public int GetRemainderInt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index >= RemainderIntCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        return ReadInt32(RawBytes, HeaderIntCount + QuadRowCount * QuadWidthInts + index);
    }

    /// <summary>
    /// Returns a mutable builder for batching multiple structural edits with copy-on-write semantics.
    /// Prefer this over chaining multiple <c>With*</c> calls when updating several regions of the
    /// same <c>data.sav</c> payload, because the builder clones the raw bytes at most once before
    /// <see cref="Builder.Build"/>.
    /// </summary>
    public Builder ToBuilder() => new(this);

    /// <summary>
    /// Copies a contiguous remainder INT32 range into <paramref name="destination"/>,
    /// starting at <paramref name="startIndex"/>.
    /// </summary>
    public void CopyRemainderInts(int startIndex, Span<int> destination)
    {
        ValidateCopyRange(startIndex, destination.Length, RemainderIntCount, nameof(startIndex));

        var baseInt = HeaderIntCount + QuadRowCount * QuadWidthInts + startIndex;
        for (var index = 0; index < destination.Length; index++)
            destination[index] = ReadInt32(RawBytes, baseInt + index);
    }

    /// <summary>
    /// Returns a new file instance with the two header INT32 values replaced.
    /// All aligned rows, remainder ints, and trailing bytes remain unchanged.
    /// </summary>
    public DataSavFile WithHeader(int header0, int header1)
    {
        var updated = RawBytes.ToArray();
        WriteInt32(updated, 0, header0);
        WriteInt32(updated, 1, header1);
        return new DataSavFile { RawBytes = updated };
    }

    /// <summary>
    /// Returns a new file instance with the aligned INT32[4] row at <paramref name="rowIndex"/>
    /// replaced. All other bytes remain unchanged.
    /// </summary>
    public DataSavFile WithQuadRow(int rowIndex, DataSavQuadRow row)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);
        if (rowIndex >= QuadRowCount)
            throw new ArgumentOutOfRangeException(nameof(rowIndex));

        var updated = RawBytes.ToArray();
        var baseInt = HeaderIntCount + rowIndex * QuadWidthInts;
        WriteInt32(updated, baseInt, row.A);
        WriteInt32(updated, baseInt + 1, row.B);
        WriteInt32(updated, baseInt + 2, row.C);
        WriteInt32(updated, baseInt + 3, row.D);
        return new DataSavFile { RawBytes = updated };
    }

    /// <summary>
    /// Returns a new file instance with a contiguous aligned INT32[4] row range replaced.
    /// All other bytes remain unchanged.
    /// </summary>
    public DataSavFile WithQuadRows(int startRowIndex, ReadOnlySpan<DataSavQuadRow> rows)
    {
        ValidatePatchRange(startRowIndex, rows.Length, QuadRowCount, nameof(startRowIndex));

        var updated = RawBytes.ToArray();
        var baseInt = HeaderIntCount + startRowIndex * QuadWidthInts;
        for (var index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            var rowInt = baseInt + index * QuadWidthInts;
            WriteInt32(updated, rowInt, row.A);
            WriteInt32(updated, rowInt + 1, row.B);
            WriteInt32(updated, rowInt + 2, row.C);
            WriteInt32(updated, rowInt + 3, row.D);
        }

        return new DataSavFile { RawBytes = updated };
    }

    /// <summary>
    /// Returns a new file instance with one full-INT32 remainder value replaced.
    /// Trailing non-INT32 bytes remain unchanged.
    /// </summary>
    public DataSavFile WithRemainderInt(int index, int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index >= RemainderIntCount)
            throw new ArgumentOutOfRangeException(nameof(index));

        var updated = RawBytes.ToArray();
        var intIndex = HeaderIntCount + QuadRowCount * QuadWidthInts + index;
        WriteInt32(updated, intIndex, value);
        return new DataSavFile { RawBytes = updated };
    }

    /// <summary>
    /// Returns a new file instance with a contiguous remainder INT32 range replaced.
    /// Trailing non-INT32 bytes remain unchanged.
    /// </summary>
    public DataSavFile WithRemainderInts(int startIndex, ReadOnlySpan<int> values)
    {
        ValidatePatchRange(startIndex, values.Length, RemainderIntCount, nameof(startIndex));

        var updated = RawBytes.ToArray();
        var baseInt = HeaderIntCount + QuadRowCount * QuadWidthInts + startIndex;
        for (var index = 0; index < values.Length; index++)
            WriteInt32(updated, baseInt + index, values[index]);

        return new DataSavFile { RawBytes = updated };
    }

    private static void ValidateCopyRange(int startIndex, int count, int availableCount, string paramName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (startIndex + count > availableCount)
            throw new ArgumentOutOfRangeException(paramName);
    }

    private static void ValidatePatchRange(int startIndex, int count, int availableCount, string paramName)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        if (count == 0)
            throw new ArgumentException("At least one value is required.", nameof(count));

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
    /// Fluent copy-on-write builder for <see cref="DataSavFile"/>.
    /// Use <see cref="DataSavFile.ToBuilder"/> to batch multiple structural edits into one final
    /// <see cref="Build"/> step without cloning the raw payload once per mutation.
    /// </summary>
    public sealed class Builder
    {
        private byte[] _rawBytes;
        private bool _sharesBytes;

        /// <summary>Creates a builder pre-populated from an existing <see cref="DataSavFile"/>.</summary>
        public Builder(DataSavFile from)
        {
            ArgumentNullException.ThrowIfNull(from);

            _rawBytes = from.RawBytes;
            _sharesBytes = true;
        }

        /// <summary>First INT32 header word.</summary>
        public int Header0 => ReadInt32(_rawBytes, 0);

        /// <summary>Second INT32 header word.</summary>
        public int Header1 => ReadInt32(_rawBytes, 1);

        /// <summary>Total number of full INT32 values present in the file.</summary>
        public int TotalInts => _rawBytes.Length / 4;

        /// <summary>Trailing bytes after the last full INT32, if any.</summary>
        public int TrailingBytes => _rawBytes.Length % 4;

        /// <summary>Number of full aligned INT32[4] rows after the 8-byte header.</summary>
        public int QuadRowCount
        {
            get
            {
                if (TotalInts < HeaderIntCount)
                    return 0;

                return (TotalInts - HeaderIntCount) / QuadWidthInts;
            }
        }

        /// <summary>Number of full INT32 remainder values after the last aligned row.</summary>
        public int RemainderIntCount
        {
            get
            {
                if (TotalInts < HeaderIntCount)
                    return 0;

                return (TotalInts - HeaderIntCount) % QuadWidthInts;
            }
        }

        /// <summary>Returns the aligned INT32[4] row at <paramref name="rowIndex"/>.</summary>
        public DataSavQuadRow GetQuadRow(int rowIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);
            if (rowIndex >= QuadRowCount)
                throw new ArgumentOutOfRangeException(nameof(rowIndex));

            var baseInt = HeaderIntCount + rowIndex * QuadWidthInts;
            return new DataSavQuadRow(
                ReadInt32(_rawBytes, baseInt),
                ReadInt32(_rawBytes, baseInt + 1),
                ReadInt32(_rawBytes, baseInt + 2),
                ReadInt32(_rawBytes, baseInt + 3)
            );
        }

        /// <summary>
        /// Copies a contiguous aligned INT32[4] row range into <paramref name="destination"/>,
        /// starting at <paramref name="startRowIndex"/>.
        /// </summary>
        public void CopyQuadRows(int startRowIndex, Span<DataSavQuadRow> destination)
        {
            ValidateCopyRange(startRowIndex, destination.Length, QuadRowCount, nameof(startRowIndex));

            var baseInt = HeaderIntCount + startRowIndex * QuadWidthInts;
            for (var index = 0; index < destination.Length; index++)
            {
                var rowInt = baseInt + index * QuadWidthInts;
                destination[index] = new DataSavQuadRow(
                    ReadInt32(_rawBytes, rowInt),
                    ReadInt32(_rawBytes, rowInt + 1),
                    ReadInt32(_rawBytes, rowInt + 2),
                    ReadInt32(_rawBytes, rowInt + 3)
                );
            }
        }

        /// <summary>Returns the INT32 remainder value at <paramref name="index"/>.</summary>
        public int GetRemainderInt(int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            if (index >= RemainderIntCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            return ReadInt32(_rawBytes, HeaderIntCount + QuadRowCount * QuadWidthInts + index);
        }

        /// <summary>
        /// Copies a contiguous remainder INT32 range into <paramref name="destination"/>,
        /// starting at <paramref name="startIndex"/>.
        /// </summary>
        public void CopyRemainderInts(int startIndex, Span<int> destination)
        {
            ValidateCopyRange(startIndex, destination.Length, RemainderIntCount, nameof(startIndex));

            var baseInt = HeaderIntCount + QuadRowCount * QuadWidthInts + startIndex;
            for (var index = 0; index < destination.Length; index++)
                destination[index] = ReadInt32(_rawBytes, baseInt + index);
        }

        /// <summary>Replaces the two header INT32 values.</summary>
        public Builder WithHeader(int header0, int header1)
        {
            EnsureWritable();
            WriteInt32(_rawBytes, 0, header0);
            WriteInt32(_rawBytes, 1, header1);
            return this;
        }

        /// <summary>Replaces one aligned INT32[4] row.</summary>
        public Builder WithQuadRow(int rowIndex, DataSavQuadRow row)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(rowIndex);
            if (rowIndex >= QuadRowCount)
                throw new ArgumentOutOfRangeException(nameof(rowIndex));

            EnsureWritable();
            var baseInt = HeaderIntCount + rowIndex * QuadWidthInts;
            WriteInt32(_rawBytes, baseInt, row.A);
            WriteInt32(_rawBytes, baseInt + 1, row.B);
            WriteInt32(_rawBytes, baseInt + 2, row.C);
            WriteInt32(_rawBytes, baseInt + 3, row.D);
            return this;
        }

        /// <summary>Replaces a contiguous aligned INT32[4] row range.</summary>
        public Builder WithQuadRows(int startRowIndex, ReadOnlySpan<DataSavQuadRow> rows)
        {
            ValidatePatchRange(startRowIndex, rows.Length, QuadRowCount, nameof(startRowIndex));

            EnsureWritable();
            var baseInt = HeaderIntCount + startRowIndex * QuadWidthInts;
            for (var index = 0; index < rows.Length; index++)
            {
                var row = rows[index];
                var rowInt = baseInt + index * QuadWidthInts;
                WriteInt32(_rawBytes, rowInt, row.A);
                WriteInt32(_rawBytes, rowInt + 1, row.B);
                WriteInt32(_rawBytes, rowInt + 2, row.C);
                WriteInt32(_rawBytes, rowInt + 3, row.D);
            }

            return this;
        }

        /// <summary>Replaces one full-INT32 remainder value.</summary>
        public Builder WithRemainderInt(int index, int value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            if (index >= RemainderIntCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            EnsureWritable();
            var intIndex = HeaderIntCount + QuadRowCount * QuadWidthInts + index;
            WriteInt32(_rawBytes, intIndex, value);
            return this;
        }

        /// <summary>Replaces a contiguous remainder INT32 range.</summary>
        public Builder WithRemainderInts(int startIndex, ReadOnlySpan<int> values)
        {
            ValidatePatchRange(startIndex, values.Length, RemainderIntCount, nameof(startIndex));

            EnsureWritable();
            var baseInt = HeaderIntCount + QuadRowCount * QuadWidthInts + startIndex;
            for (var index = 0; index < values.Length; index++)
                WriteInt32(_rawBytes, baseInt + index, values[index]);

            return this;
        }

        /// <summary>
        /// Builds an immutable <see cref="DataSavFile"/> snapshot from the current builder state.
        /// Subsequent mutations on this builder use copy-on-write so previously built snapshots stay immutable.
        /// </summary>
        public DataSavFile Build()
        {
            _sharesBytes = true;
            return new DataSavFile { RawBytes = _rawBytes };
        }

        private void EnsureWritable()
        {
            if (!_sharesBytes)
                return;

            _rawBytes = _rawBytes.ToArray();
            _sharesBytes = false;
        }
    }
}
