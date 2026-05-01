using System.Runtime.Versioning;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class RuntimeInspectionCommands
{
    public static int RunDumpPointerGraph(string[] args, Func<Func<ProcessMemory, int>, int> runWithProcess)
    {
        if (args.Length < 2)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab dump-pointer-graph <address|module+rva> <depth> [int-count]"
            );
        }

        return runWithProcess(memory =>
        {
            var start = LiveLabCli.ParseAddress(memory, args[0]);
            var depth = LiveLabCli.ParseInt32(args[1]);
            if (depth < 0)
                throw new InvalidOperationException("depth must be non-negative.");

            var intCount = args.Length >= 3 ? LiveLabCli.ParseInt32(args[2]) : 16;
            if (intCount < 1)
                throw new InvalidOperationException("int-count must be at least 1.");

            var graph = BuildPointerGraph(memory, start, depth, intCount, new HashSet<uint>());
            LiveLabCli.WriteJson(graph);
            return 0;
        });
    }

    public static int RunFindInt32Sequence(string[] args, Func<Func<ProcessMemory, int>, int> runWithProcess)
    {
        if (args.Length < 3)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab find-int32-sequence <start-address|module+rva> <byte-count> <value1> [value2] [value3] ..."
            );
        }

        return runWithProcess(memory =>
        {
            var start = LiveLabCli.ParseAddress(memory, args[0]);
            var byteCount = LiveLabCli.ParseInt32(args[1]);
            if (byteCount < sizeof(int))
                throw new InvalidOperationException("byte-count must be at least 4.");

            var expected = args[2..].Select(LiveLabCli.ParseInt32).ToArray();
            var bytes = memory.ReadBytes(start, byteCount);
            var matches = Int32RuntimeScanner.FindSequenceMatches(bytes, start, expected);

            LiveLabCli.WriteJson(
                new
                {
                    Start = ProcessMemory.FormatAddress(start),
                    ByteCount = byteCount,
                    Sequence = expected,
                    MatchCount = matches.Length,
                    Matches = matches,
                }
            );
            return 0;
        });
    }

    public static int RunFindInt32SequenceGlobal(string[] args, Func<Func<ProcessMemory, int>, int> runWithProcess)
    {
        if (args.Length < 1)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab find-int32-sequence-global <value1> [value2] [value3] ..."
            );
        }

        return runWithProcess(memory =>
        {
            const int chunkSize = 1 * 1024 * 1024;
            const int maxMatches = 256;

            var expected = args.Select(LiveLabCli.ParseInt32).ToArray();
            var (matches, regionsScanned, bytesScanned, skippedRegions) = Int32RuntimeScanner.FindSequenceMatchesGlobal(
                memory,
                expected,
                chunkSize,
                maxMatches
            );

            LiveLabCli.WriteJson(
                new
                {
                    Sequence = expected,
                    RegionsScanned = regionsScanned,
                    BytesScanned = bytesScanned,
                    SkippedRegions = skippedRegions,
                    MatchCount = matches.Length,
                    ReachedMatchLimit = matches.Length >= maxMatches,
                    Matches = matches,
                }
            );
            return 0;
        });
    }

    public static int RunScanInt32Records(string[] args, Func<Func<ProcessMemory, int>, int> runWithProcess)
    {
        if (args.Length < 4)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab scan-int32-records <start-address|module+rva> <byte-count> <record-int-count> <header1> [header2] [header3] ..."
            );
        }

        return runWithProcess(memory =>
        {
            const int maxMatches = 256;

            var start = LiveLabCli.ParseAddress(memory, args[0]);
            var byteCount = LiveLabCli.ParseInt32(args[1]);
            if (byteCount < sizeof(int))
                throw new InvalidOperationException("byte-count must be at least 4.");

            var recordIntCount = LiveLabCli.ParseInt32(args[2]);
            if (recordIntCount < 1)
                throw new InvalidOperationException("record-int-count must be at least 1.");

            var header = args[3..].Select(LiveLabCli.ParseInt32).ToArray();
            if (header.Length > recordIntCount)
                throw new InvalidOperationException("record-int-count must be at least the header length.");

            var bytes = memory.ReadBytes(start, byteCount);
            var matches = Int32RuntimeScanner.ScanRecordMatches(bytes, start, recordIntCount, header, maxMatches);

            LiveLabCli.WriteJson(
                new
                {
                    Start = ProcessMemory.FormatAddress(start),
                    ByteCount = byteCount,
                    RecordIntCount = recordIntCount,
                    Header = header,
                    MatchCount = matches.Length,
                    ReachedMatchLimit = matches.Length >= maxMatches,
                    Matches = matches,
                }
            );
            return 0;
        });
    }

    public static int RunPatchInt32RecordField(string[] args, Func<Func<ProcessMemory, int>, int> runWithProcess)
    {
        if (args.Length < 7)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab patch-int32-record-field <start-address|module+rva> <byte-count> <record-int-count> <field-index> <expected-value> <new-value> <header1> [header2] [header3] ..."
            );
        }

        return runWithProcess(memory =>
        {
            const int maxWrites = 256;

            var start = LiveLabCli.ParseAddress(memory, args[0]);
            var byteCount = LiveLabCli.ParseInt32(args[1]);
            if (byteCount < sizeof(int))
                throw new InvalidOperationException("byte-count must be at least 4.");

            var recordIntCount = LiveLabCli.ParseInt32(args[2]);
            if (recordIntCount < 1)
                throw new InvalidOperationException("record-int-count must be at least 1.");

            var fieldIndex = LiveLabCli.ParseInt32(args[3]);
            if (fieldIndex < 0 || fieldIndex >= recordIntCount)
                throw new InvalidOperationException("field-index must be within the record.");

            var expectedValue = LiveLabCli.ParseInt32(args[4]);
            var newValue = LiveLabCli.ParseInt32(args[5]);
            var header = args[6..].Select(LiveLabCli.ParseInt32).ToArray();
            if (header.Length > recordIntCount)
                throw new InvalidOperationException("record-int-count must be at least the header length.");

            var bytes = memory.ReadBytes(start, byteCount);
            var writes = Int32RuntimeScanner.PatchRecordFields(
                memory,
                start,
                bytes,
                recordIntCount,
                fieldIndex,
                expectedValue,
                newValue,
                header,
                maxWrites
            );

            LiveLabCli.WriteJson(
                new
                {
                    Start = ProcessMemory.FormatAddress(start),
                    ByteCount = byteCount,
                    RecordIntCount = recordIntCount,
                    FieldIndex = fieldIndex,
                    ExpectedValue = expectedValue,
                    NewValue = newValue,
                    Header = header,
                    WriteCount = writes.Length,
                    ReachedWriteLimit = writes.Length >= maxWrites,
                    Writes = writes,
                }
            );
            return 0;
        });
    }

    private static PointerGraphNodeSnapshot BuildPointerGraph(
        ProcessMemory memory,
        nint address,
        int depth,
        int intCount,
        HashSet<uint> visited
    )
    {
        if (!memory.TryGetReadableRegion(address, out var region))
        {
            return new PointerGraphNodeSnapshot
            {
                Address = ProcessMemory.FormatAddress(address),
                Readable = false,
                Values = [],
                Pointers = [],
            };
        }

        var addressValue = memory.ToUInt32Address(address);
        var firstVisit = visited.Add(addressValue);
        var regionBase = memory.ToUInt32Address(region.BaseAddress);
        var offsetInRegion = checked((int)(addressValue - regionBase));
        var availableIntCount = Math.Max(0, (int)((ulong)region.Size - (ulong)offsetInRegion) / sizeof(int));
        var count = Math.Min(intCount, availableIntCount);

        var values = Enumerable
            .Range(0, count)
            .Select(index =>
            {
                var offset = index * sizeof(int);
                var valueAddress = address + offset;
                var value = memory.ReadInt32(valueAddress);
                return new PointerGraphValueSnapshot
                {
                    Index = index,
                    Offset = offset,
                    Address = ProcessMemory.FormatAddress(valueAddress),
                    Value = value,
                    Hex = $"0x{unchecked((uint)value):X8}",
                };
            })
            .ToArray();

        var pointers = new List<PointerGraphEdgeSnapshot>();
        if (firstVisit && depth > 0)
        {
            var expandedTargets = new HashSet<uint>();
            foreach (var value in values)
            {
                var target = unchecked((uint)value.Value);
                if (!IsCandidatePointer(memory, target, out var targetAddress))
                    continue;

                PointerGraphNodeSnapshot? targetNode = null;
                if (expandedTargets.Add(target))
                    targetNode = BuildPointerGraph(memory, targetAddress, depth - 1, intCount, visited);

                pointers.Add(
                    new PointerGraphEdgeSnapshot
                    {
                        Index = value.Index,
                        Offset = value.Offset,
                        SourceAddress = value.Address,
                        TargetAddress = ProcessMemory.FormatAddress(targetAddress),
                        TargetNode = targetNode,
                    }
                );
            }
        }

        return new PointerGraphNodeSnapshot
        {
            Address = ProcessMemory.FormatAddress(address),
            Readable = true,
            RegionBase = ProcessMemory.FormatAddress(region.BaseAddress),
            RegionSize = (int)region.Size,
            Protection = region.Protect.ToString(),
            AlreadyVisited = !firstVisit,
            Values = values,
            Pointers = pointers.ToArray(),
        };
    }

    private static bool IsCandidatePointer(ProcessMemory memory, uint value, out nint address)
    {
        address = default;
        if (value < 0x0010_0000 || (value & 0x3) != 0)
            return false;

        address = (nint)(long)value;
        return memory.TryGetReadableRegion(address, out _);
    }

    private sealed class PointerGraphNodeSnapshot
    {
        public required string Address { get; init; }

        public bool Readable { get; init; }

        public string? RegionBase { get; init; }

        public int? RegionSize { get; init; }

        public string? Protection { get; init; }

        public bool AlreadyVisited { get; init; }

        public required PointerGraphValueSnapshot[] Values { get; init; }

        public required PointerGraphEdgeSnapshot[] Pointers { get; init; }
    }

    private sealed class PointerGraphValueSnapshot
    {
        public int Index { get; init; }

        public int Offset { get; init; }

        public required string Address { get; init; }

        public int Value { get; init; }

        public required string Hex { get; init; }
    }

    private sealed class PointerGraphEdgeSnapshot
    {
        public int Index { get; init; }

        public int Offset { get; init; }

        public required string SourceAddress { get; init; }

        public required string TargetAddress { get; init; }

        public PointerGraphNodeSnapshot? TargetNode { get; init; }
    }
}
