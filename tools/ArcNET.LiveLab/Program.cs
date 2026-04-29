using System.Buffers.Binary;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal static class Program
{
    private static readonly nint[] InventoryGoldAddresses =
    [
        (nint)0x10888ABC,
        (nint)0x108EFD34,
        (nint)0x108EFD74,
        (nint)0x113B4BB4,
    ];

    private static readonly nint[] InventoryArrowsAddresses =
    [
        (nint)0x1087ACDC,
        (nint)0x1088437C,
        (nint)0x108AE584,
        (nint)0x108E92F4,
        (nint)0x108E9374,
        (nint)0x108E93B4,
        (nint)0x108EC9C8,
    ];

    private static readonly nint[] InventoryBulletsAddresses =
    [
        (nint)0x108887A0,
        (nint)0x10888890,
        (nint)0x108ED200,
        (nint)0x108EBB74,
        (nint)0x108EFFA0,
        (nint)0x108E2250,
        (nint)0x108E27F8,
        (nint)0x108E3698,
        (nint)0x108E3C30,
        (nint)0x108EC5E8,
        (nint)0x108EF1B8,
        (nint)0x108F1630,
    ];

    private static readonly int[] InventoryHighHeapHeader = [-950191472, 144998066, 2518, 2, 40, 50];

    private static async Task<int> Main(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("ArcNET.LiveLab is Windows-only.");
            return 1;
        }

        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintUsage();
            return 0;
        }

        try
        {
            return await RunAsync(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static async Task<int> RunAsync(string[] args)
    {
        var command = args[0].ToLowerInvariant();
        return command switch
        {
            "status" => RunWithProcess(memory =>
            {
                Console.WriteLine($"Process: {memory.ProcessName} ({memory.ProcessId})");
                Console.WriteLine($"Module:  {ProcessMemory.FormatAddress(memory.ModuleBase)}  {memory.ModulePath}");
                Console.WriteLine(
                    $"OpenedCharacterId: 0x{memory.ReadUInt32(memory.ResolveRva(ArcanumRuntimeOffsets.CurrentCharacterSheetIdRva)):X8}"
                );
                Console.WriteLine(
                    $"ActionPoints: {memory.ReadInt32(memory.ResolveRva(ArcanumRuntimeOffsets.ActionPointsRva))}"
                );
                return 0;
            }),
            "ap" => RunActionPoints(args[1..]),
            "read-int32" => RunReadInt32(args[1..]),
            "dump-int32s" => RunDumpInt32s(args[1..]),
            "dump-pointer-graph" => RunDumpPointerGraph(args[1..]),
            "write-int32" => RunWriteInt32(args[1..]),
            "write-int32-many" => RunWriteInt32Many(args[1..]),
            "find-int32-sequence" => RunFindInt32Sequence(args[1..]),
            "find-int32-sequence-global" => RunFindInt32SequenceGlobal(args[1..]),
            "scan-int32-records" => RunScanInt32Records(args[1..]),
            "patch-int32-record-field" => RunPatchInt32RecordField(args[1..]),
            "inventory" => RunInventory(args[1..]),
            "sheet" => RunSheet(args[1..]),
            _ => throw new InvalidOperationException($"Unknown command '{args[0]}'. Use 'help' for usage."),
        };
    }

    private static int RunActionPoints(string[] args)
    {
        return RunWithProcess(memory =>
        {
            var address = memory.ResolveRva(ArcanumRuntimeOffsets.ActionPointsRva);
            if (args.Length == 0 || args[0].Equals("get", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"ActionPoints: {memory.ReadInt32(address)}");
                return 0;
            }

            if (args[0].Equals("set", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length < 2)
                    throw new InvalidOperationException("Usage: ArcNET.LiveLab ap set <value>");

                var value = ParseInt32(args[1]);
                var before = memory.ReadInt32(address);
                memory.WriteInt32(address, value);
                var after = memory.ReadInt32(address);
                Console.WriteLine($"ActionPoints: {before} -> {after}");
                return 0;
            }

            throw new InvalidOperationException("Usage: ArcNET.LiveLab ap [get|set <value>]");
        });
    }

    private static int RunReadInt32(string[] args)
    {
        if (args.Length < 1)
            throw new InvalidOperationException("Usage: ArcNET.LiveLab read-int32 <address|module+rva>");

        return RunWithProcess(memory =>
        {
            var address = ParseAddress(memory, args[0]);
            var value = memory.ReadInt32(address);
            Console.WriteLine($"{ProcessMemory.FormatAddress(address)} = {value} (0x{unchecked((uint)value):X8})");
            return 0;
        });
    }

    private static int RunDumpInt32s(string[] args)
    {
        if (args.Length < 2)
            throw new InvalidOperationException("Usage: ArcNET.LiveLab dump-int32s <address|module+rva> <count>");

        return RunWithProcess(memory =>
        {
            var start = ParseAddress(memory, args[0]);
            var count = ParseInt32(args[1]);
            if (count < 1)
                throw new InvalidOperationException("count must be at least 1.");

            var values = Enumerable
                .Range(0, count)
                .Select(index =>
                {
                    var offset = index * sizeof(int);
                    var address = start + offset;
                    var value = memory.ReadInt32(address);
                    return new
                    {
                        Index = index,
                        Offset = offset,
                        Address = ProcessMemory.FormatAddress(address),
                        Value = value,
                        Hex = $"0x{unchecked((uint)value):X8}",
                    };
                })
                .ToArray();

            WriteJson(
                new
                {
                    Start = ProcessMemory.FormatAddress(start),
                    Count = count,
                    Values = values,
                }
            );
            return 0;
        });
    }

    private static int RunDumpPointerGraph(string[] args)
    {
        if (args.Length < 2)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab dump-pointer-graph <address|module+rva> <depth> [int-count]"
            );
        }

        return RunWithProcess(memory =>
        {
            var start = ParseAddress(memory, args[0]);
            var depth = ParseInt32(args[1]);
            if (depth < 0)
                throw new InvalidOperationException("depth must be non-negative.");

            var intCount = args.Length >= 3 ? ParseInt32(args[2]) : 16;
            if (intCount < 1)
                throw new InvalidOperationException("int-count must be at least 1.");

            var graph = BuildPointerGraph(memory, start, depth, intCount, new HashSet<uint>());
            WriteJson(graph);
            return 0;
        });
    }

    private static int RunWriteInt32(string[] args)
    {
        if (args.Length < 2)
            throw new InvalidOperationException("Usage: ArcNET.LiveLab write-int32 <address|module+rva> <value>");

        return RunWithProcess(memory =>
        {
            var address = ParseAddress(memory, args[0]);
            var value = ParseInt32(args[1]);
            var before = memory.ReadInt32(address);
            memory.WriteInt32(address, value);
            var after = memory.ReadInt32(address);
            Console.WriteLine($"{ProcessMemory.FormatAddress(address)}: {before} -> {after}");
            return 0;
        });
    }

    private static int RunWriteInt32Many(string[] args)
    {
        if (args.Length < 2)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab write-int32-many <value> <address|module+rva> [address|module+rva] ..."
            );
        }

        return RunWithProcess(memory =>
        {
            var value = ParseInt32(args[0]);
            var writes = args[1..]
                .Select(text => ParseAddress(memory, text))
                .Select(address =>
                {
                    var before = memory.ReadInt32(address);
                    memory.WriteInt32(address, value);
                    var after = memory.ReadInt32(address);
                    return new
                    {
                        Address = ProcessMemory.FormatAddress(address),
                        Before = before,
                        After = after,
                    };
                })
                .ToArray();

            WriteJson(new { Value = value, Writes = writes });
            return 0;
        });
    }

    private static int RunFindInt32Sequence(string[] args)
    {
        if (args.Length < 3)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab find-int32-sequence <start-address|module+rva> <byte-count> <value1> [value2] [value3] ..."
            );
        }

        return RunWithProcess(memory =>
        {
            var start = ParseAddress(memory, args[0]);
            var byteCount = ParseInt32(args[1]);
            if (byteCount < sizeof(int))
                throw new InvalidOperationException("byte-count must be at least 4.");

            var expected = args[2..].Select(ParseInt32).ToArray();
            var bytes = memory.ReadBytes(start, byteCount);
            var matches = new List<object>();

            var neededBytes = checked(expected.Length * sizeof(int));
            for (var offset = 0; offset <= bytes.Length - neededBytes; offset += sizeof(int))
            {
                var matched = true;
                for (var index = 0; index < expected.Length; index++)
                {
                    var current = BinaryPrimitives.ReadInt32LittleEndian(
                        bytes.AsSpan(offset + index * sizeof(int), sizeof(int))
                    );
                    if (current != expected[index])
                    {
                        matched = false;
                        break;
                    }
                }

                if (!matched)
                    continue;

                matches.Add(
                    new
                    {
                        Address = ProcessMemory.FormatAddress(start + offset),
                        Offset = offset,
                        Values = expected,
                    }
                );
            }

            WriteJson(
                new
                {
                    Start = ProcessMemory.FormatAddress(start),
                    ByteCount = byteCount,
                    Sequence = expected,
                    MatchCount = matches.Count,
                    Matches = matches,
                }
            );
            return 0;
        });
    }

    private static int RunFindInt32SequenceGlobal(string[] args)
    {
        if (args.Length < 1)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab find-int32-sequence-global <value1> [value2] [value3] ..."
            );
        }

        return RunWithProcess(memory =>
        {
            const int chunkSize = 1 * 1024 * 1024;
            const int maxMatches = 256;

            var expected = args.Select(ParseInt32).ToArray();
            var overlapBytes = Math.Max(0, checked(expected.Length * sizeof(int)) - sizeof(int));
            var matches = new List<object>();
            ulong bytesScanned = 0;
            var regionsScanned = 0;
            var skippedRegions = 0;

            foreach (var region in memory.EnumerateCommittedReadableRegions())
            {
                if ((ulong)region.Size < (ulong)(expected.Length * sizeof(int)))
                    continue;

                regionsScanned++;
                bytesScanned += (ulong)region.Size;

                try
                {
                    ScanRegionForInt32Sequence(memory, region, expected, chunkSize, overlapBytes, matches, maxMatches);
                }
                catch
                {
                    skippedRegions++;
                }

                if (matches.Count >= maxMatches)
                    break;
            }

            WriteJson(
                new
                {
                    Sequence = expected,
                    RegionsScanned = regionsScanned,
                    BytesScanned = bytesScanned,
                    SkippedRegions = skippedRegions,
                    MatchCount = matches.Count,
                    ReachedMatchLimit = matches.Count >= maxMatches,
                    Matches = matches,
                }
            );
            return 0;
        });
    }

    private static int RunScanInt32Records(string[] args)
    {
        if (args.Length < 4)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab scan-int32-records <start-address|module+rva> <byte-count> <record-int-count> <header1> [header2] [header3] ..."
            );
        }

        return RunWithProcess(memory =>
        {
            const int maxMatches = 256;

            var start = ParseAddress(memory, args[0]);
            var byteCount = ParseInt32(args[1]);
            if (byteCount < sizeof(int))
                throw new InvalidOperationException("byte-count must be at least 4.");

            var recordIntCount = ParseInt32(args[2]);
            if (recordIntCount < 1)
                throw new InvalidOperationException("record-int-count must be at least 1.");

            var header = args[3..].Select(ParseInt32).ToArray();
            if (header.Length > recordIntCount)
                throw new InvalidOperationException("record-int-count must be at least the header length.");

            var bytes = memory.ReadBytes(start, byteCount);
            var matches = new List<object>();

            var headerBytes = checked(header.Length * sizeof(int));
            var recordBytes = checked(recordIntCount * sizeof(int));
            for (var offset = 0; offset <= bytes.Length - recordBytes && matches.Count < maxMatches; offset += sizeof(int))
            {
                var matched = true;
                for (var index = 0; index < header.Length; index++)
                {
                    var current = BinaryPrimitives.ReadInt32LittleEndian(
                        bytes.AsSpan(offset + index * sizeof(int), sizeof(int))
                    );
                    if (current != header[index])
                    {
                        matched = false;
                        break;
                    }
                }

                if (!matched)
                    continue;

                var values = Enumerable
                    .Range(0, recordIntCount)
                    .Select(index =>
                    {
                        var valueOffset = index * sizeof(int);
                        var value = BinaryPrimitives.ReadInt32LittleEndian(
                            bytes.AsSpan(offset + valueOffset, sizeof(int))
                        );

                        return new
                        {
                            Index = index,
                            Offset = valueOffset,
                            Address = ProcessMemory.FormatAddress(start + offset + valueOffset),
                            Value = value,
                            Hex = $"0x{unchecked((uint)value):X8}",
                        };
                    })
                    .ToArray();

                matches.Add(
                    new
                    {
                        Address = ProcessMemory.FormatAddress(start + offset),
                        Offset = offset,
                        Values = values,
                    }
                );
            }

            WriteJson(
                new
                {
                    Start = ProcessMemory.FormatAddress(start),
                    ByteCount = byteCount,
                    RecordIntCount = recordIntCount,
                    Header = header,
                    MatchCount = matches.Count,
                    ReachedMatchLimit = matches.Count >= maxMatches,
                    Matches = matches,
                }
            );
            return 0;
        });
    }

    private static int RunPatchInt32RecordField(string[] args)
    {
        if (args.Length < 7)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab patch-int32-record-field <start-address|module+rva> <byte-count> <record-int-count> <field-index> <expected-value> <new-value> <header1> [header2] [header3] ..."
            );
        }

        return RunWithProcess(memory =>
        {
            const int maxWrites = 256;

            var start = ParseAddress(memory, args[0]);
            var byteCount = ParseInt32(args[1]);
            if (byteCount < sizeof(int))
                throw new InvalidOperationException("byte-count must be at least 4.");

            var recordIntCount = ParseInt32(args[2]);
            if (recordIntCount < 1)
                throw new InvalidOperationException("record-int-count must be at least 1.");

            var fieldIndex = ParseInt32(args[3]);
            if (fieldIndex < 0 || fieldIndex >= recordIntCount)
                throw new InvalidOperationException("field-index must be within the record.");

            var expectedValue = ParseInt32(args[4]);
            var newValue = ParseInt32(args[5]);
            var header = args[6..].Select(ParseInt32).ToArray();
            if (header.Length > recordIntCount)
                throw new InvalidOperationException("record-int-count must be at least the header length.");

            var bytes = memory.ReadBytes(start, byteCount);
            var writes = new List<object>();

            var recordBytes = checked(recordIntCount * sizeof(int));
            for (var offset = 0; offset <= bytes.Length - recordBytes && writes.Count < maxWrites; offset += sizeof(int))
            {
                var matched = true;
                for (var index = 0; index < header.Length; index++)
                {
                    var current = BinaryPrimitives.ReadInt32LittleEndian(
                        bytes.AsSpan(offset + index * sizeof(int), sizeof(int))
                    );
                    if (current != header[index])
                    {
                        matched = false;
                        break;
                    }
                }

                if (!matched)
                    continue;

                var fieldOffset = checked(fieldIndex * sizeof(int));
                var before = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + fieldOffset, sizeof(int)));
                if (before != expectedValue)
                    continue;

                var fieldAddress = start + offset + fieldOffset;
                memory.WriteInt32(fieldAddress, newValue);
                var after = memory.ReadInt32(fieldAddress);

                writes.Add(
                    new
                    {
                        RecordAddress = ProcessMemory.FormatAddress(start + offset),
                        FieldIndex = fieldIndex,
                        FieldAddress = ProcessMemory.FormatAddress(fieldAddress),
                        Before = before,
                        After = after,
                    }
                );
            }

            WriteJson(
                new
                {
                    Start = ProcessMemory.FormatAddress(start),
                    ByteCount = byteCount,
                    RecordIntCount = recordIntCount,
                    FieldIndex = fieldIndex,
                    ExpectedValue = expectedValue,
                    NewValue = newValue,
                    Header = header,
                    WriteCount = writes.Count,
                    ReachedWriteLimit = writes.Count >= maxWrites,
                    Writes = writes,
                }
            );
            return 0;
        });
    }

    private static int RunInventory(string[] args)
    {
        if (args.Length == 0)
            throw new InvalidOperationException("Usage: ArcNET.LiveLab inventory <snapshot|resources>");

        var subcommand = args[0].ToLowerInvariant();
        return RunWithProcess(memory =>
            subcommand switch
            {
                "snapshot" => RunInventorySnapshot(memory, args[1..]),
                "resources" => RunInventoryResources(memory, args[1..]),
                _ => throw new InvalidOperationException("Usage: ArcNET.LiveLab inventory <snapshot|resources> ..."),
            }
        );
    }

    private static int RunInventoryResources(ProcessMemory memory, string[] args)
    {
        if (args.Length == 0)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab inventory resources set <gold> <arrows> <bullets>"
            );
        }

        var subcommand = args[0].ToLowerInvariant();
        return subcommand switch
        {
            "set" => RunInventoryResourcesSet(memory, args[1..]),
            _ => throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab inventory resources set <gold> <arrows> <bullets>"
            ),
        };
    }

    private static int RunInventoryResourcesSet(ProcessMemory memory, string[] args)
    {
        if (args.Length < 3)
        {
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab inventory resources set <gold> <arrows> <bullets>"
            );
        }

        var gold = ParseInt32(args[0]);
        var arrows = ParseInt32(args[1]);
        var bullets = ParseInt32(args[2]);

        var writes = new List<object>();
        writes.AddRange(PatchInt32Addresses(memory, gold, InventoryGoldAddresses));
        writes.AddRange(PatchInt32Addresses(memory, arrows, InventoryArrowsAddresses));
        writes.AddRange(PatchInt32Addresses(memory, bullets, InventoryBulletsAddresses));

        writes.AddRange(
            PatchInt32RecordFieldInRange(
                memory,
                (nint)0x11284000,
                65536,
                10,
                7,
                memory.ReadInt32((nint)0x11284814),
                bullets,
                InventoryHighHeapHeader
            )
        );
        writes.AddRange(
            PatchInt32RecordFieldInRange(
                memory,
                (nint)0x11284000,
                65536,
                10,
                9,
                memory.ReadInt32((nint)0x112849AC),
                bullets,
                InventoryHighHeapHeader
            )
        );
        writes.AddRange(
            PatchInt32RecordFieldInRange(
                memory,
                (nint)0x11284000,
                65536,
                10,
                7,
                memory.ReadInt32((nint)0x1128EFDC),
                arrows,
                InventoryHighHeapHeader
            )
        );
        writes.AddRange(
            PatchInt32RecordFieldInRange(
                memory,
                (nint)0x11284000,
                65536,
                10,
                8,
                memory.ReadInt32((nint)0x112849A8),
                arrows,
                InventoryHighHeapHeader
            )
        );
        writes.AddRange(
            PatchInt32RecordFieldInRange(
                memory,
                (nint)0x11390000,
                262144,
                8,
                0,
                memory.ReadInt32((nint)0x1139B054),
                gold,
                [memory.ReadInt32((nint)0x1139B054), 288948984, -1, 5, 0, 0]
            )
        );
        writes.AddRange(
            PatchInt32RecordFieldInRange(
                memory,
                (nint)0x11390000,
                262144,
                7,
                0,
                memory.ReadInt32((nint)0x11393B84),
                gold,
                [memory.ReadInt32((nint)0x11393B84), -1050854762, 146112180, 285227596, 171003200, 0]
            )
        );

        WriteJson(
            new
            {
                Gold = gold,
                Arrows = arrows,
                Bullets = bullets,
                WriteCount = writes.Count,
                Writes = writes,
            }
        );

        return 0;
    }

    private static int RunInventorySnapshot(ProcessMemory memory, string[] args)
    {
        var aggregateRootOverride = ParseOptionalAddressArgument(memory, args, "--aggregate-root");
        if (aggregateRootOverride != 0)
        {
            var aggregateSnapshot = InventoryPageCapture.CreateFromAggregateRoot(memory, aggregateRootOverride);
            WriteJson(aggregateSnapshot);
            return 0;
        }

        var timeout = ParseTimeout(args);
        Console.Error.WriteLine(
            $"[livelab] Installing CE-derived character-sheet hooks. Open the character sheet and click the stats page within {timeout.TotalSeconds:0.#}s to capture the current character for inventory analysis."
        );

        using var hooks = CharacterSheetHookSession.Install(memory);
        var pointers = WaitForCharacterCapture(hooks, timeout);
        if (pointers.Character == 0)
            throw new InvalidOperationException("No character pointer was captured before timeout.");

        var snapshot = InventoryPageCapture.Create(memory, pointers);
        WriteJson(snapshot);
        return 0;
    }

    private static int RunSheet(string[] args)
    {
        if (args.Length == 0)
            throw new InvalidOperationException("Usage: ArcNET.LiveLab sheet <snapshot|write>");

        var subcommand = args[0].ToLowerInvariant();
        return RunWithProcess(memory =>
            subcommand switch
            {
                "snapshot" => RunSheetSnapshot(memory, args[1..]),
                "write" => RunSheetWrite(memory, args[1..]),
                _ => throw new InvalidOperationException("Usage: ArcNET.LiveLab sheet <snapshot|write> ..."),
            }
        );
    }

    private static int RunSheetSnapshot(ProcessMemory memory, string[] args)
    {
        var timeout = ParseTimeout(args);
        Console.Error.WriteLine(
            $"[livelab] Installing CE-derived character-sheet hooks. Open the character sheet and click the stats / skills / tech / spells pages within {timeout.TotalSeconds:0.#}s."
        );

        using var hooks = CharacterSheetHookSession.Install(memory);
        var pointers = hooks.WaitForCapture(timeout);
        if (!pointers.HasAnyCapture)
            throw new InvalidOperationException("No character-sheet pointers were captured before timeout.");

        var snapshot = CharacterSheetCapture.Create(memory, pointers);
        WriteJson(snapshot);
        return 0;
    }

    private static int RunSheetWrite(ProcessMemory memory, string[] args)
    {
        if (args.Length < 2)
            throw new InvalidOperationException(
                "Usage: ArcNET.LiveLab sheet write <field> <value> [--timeout-ms <ms>]"
            );

        var fieldName = args[0];
        var value = ParseInt32(args[1]);
        var timeout = ParseTimeout(args[2..]);

        Console.Error.WriteLine(
            $"[livelab] Installing CE-derived character-sheet hooks. Open the relevant character-sheet page for '{fieldName}' within {timeout.TotalSeconds:0.#}s."
        );

        using var hooks = CharacterSheetHookSession.Install(memory);
        var pointers = hooks.WaitForCapture(timeout);
        if (!CharacterSheetCapture.TryResolveIntField(memory, pointers, fieldName, out var field))
        {
            throw new InvalidOperationException(
                $"Unable to resolve field '{fieldName}'. Open the matching character-sheet page and try again."
            );
        }

        var before = field.Value;
        memory.WriteInt32(field.Address, value);
        var after = memory.ReadInt32(field.Address);

        WriteJson(
            new
            {
                Field = field.Name,
                Address = ProcessMemory.FormatAddress(field.Address),
                Before = before,
                After = after,
            }
        );

        return 0;
    }

    private static int RunWithProcess(Func<ProcessMemory, int> action)
    {
        using var memory = ProcessMemory.Attach(ArcanumRuntimeOffsets.ProcessName);
        return action(memory);
    }

    private static bool IsHelp(string arg) => arg is "help" or "--help" or "-h" or "/?";

    private static nint ParseAddress(ProcessMemory memory, string text)
    {
        var trimmed = text.Trim();
        var plusIndex = trimmed.IndexOf('+');
        if (plusIndex >= 0)
        {
            var moduleName = trimmed[..plusIndex];
            if (
                !moduleName.Equals(ArcanumRuntimeOffsets.ModuleName, StringComparison.OrdinalIgnoreCase)
                && !moduleName.Equals(ArcanumRuntimeOffsets.ProcessName, StringComparison.OrdinalIgnoreCase)
            )
            {
                throw new InvalidOperationException(
                    $"Unsupported module '{moduleName}'. Expected {ArcanumRuntimeOffsets.ModuleName}."
                );
            }

            return memory.ResolveRva(ParseInt32(trimmed[(plusIndex + 1)..]));
        }

        return (nint)(long)ParseUInt32(trimmed);
    }

    private static TimeSpan ParseTimeout(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--timeout-ms", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                    throw new InvalidOperationException("Missing value for --timeout-ms.");

                return TimeSpan.FromMilliseconds(ParseInt32(args[i + 1]));
            }
        }

        return TimeSpan.FromSeconds(15);
    }

    private static nint ParseOptionalAddressArgument(ProcessMemory memory, string[] args, string name)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                    throw new InvalidOperationException($"Missing value for {name}.");

                return ParseAddress(memory, args[i + 1]);
            }
        }

        return 0;
    }

    private static CapturedPointers WaitForCharacterCapture(CharacterSheetHookSession hooks, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        var last = hooks.ReadPointers();
        while (DateTime.UtcNow < deadline)
        {
            last = hooks.ReadPointers();
            if (last.Character != 0)
                return last;

            Thread.Sleep(100);
        }

        return last;
    }

    private static int ParseInt32(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            return unchecked((int)ParseUInt32(trimmed));

        return int.Parse(trimmed, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static uint ParseUInt32(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.Parse(
                trimmed[2..],
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture
            );
        }

        return uint.Parse(trimmed, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void WriteJson<T>(T value)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        Console.WriteLine(JsonSerializer.Serialize(value, options));
    }

    private static void PrintUsage()
    {
        Console.WriteLine("ArcNET.LiveLab - live Arcanum runtime research harness");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  ArcNET.LiveLab status");
        Console.WriteLine("  ArcNET.LiveLab ap [get|set <value>]");
        Console.WriteLine("  ArcNET.LiveLab read-int32 <address|Arcanum.exe+rva>");
        Console.WriteLine("  ArcNET.LiveLab dump-int32s <address|Arcanum.exe+rva> <count>");
        Console.WriteLine("  ArcNET.LiveLab dump-pointer-graph <address|Arcanum.exe+rva> <depth> [int-count]");
        Console.WriteLine("  ArcNET.LiveLab write-int32 <address|Arcanum.exe+rva> <value>");
        Console.WriteLine(
            "  ArcNET.LiveLab write-int32-many <value> <address|Arcanum.exe+rva> [address|Arcanum.exe+rva] ..."
        );
        Console.WriteLine(
            "  ArcNET.LiveLab find-int32-sequence <start-address|Arcanum.exe+rva> <byte-count> <value1> [value2] ..."
        );
        Console.WriteLine("  ArcNET.LiveLab find-int32-sequence-global <value1> [value2] ...");
        Console.WriteLine(
            "  ArcNET.LiveLab scan-int32-records <start-address|Arcanum.exe+rva> <byte-count> <record-int-count> <header1> [header2] ..."
        );
        Console.WriteLine(
            "  ArcNET.LiveLab patch-int32-record-field <start-address|Arcanum.exe+rva> <byte-count> <record-int-count> <field-index> <expected-value> <new-value> <header1> [header2] ..."
        );
        Console.WriteLine(
            "  ArcNET.LiveLab inventory snapshot [--timeout-ms <ms>] [--aggregate-root <address|Arcanum.exe+rva>]"
        );
        Console.WriteLine("  ArcNET.LiveLab inventory resources set <gold> <arrows> <bullets>");
        Console.WriteLine("  ArcNET.LiveLab sheet snapshot [--timeout-ms <ms>]");
        Console.WriteLine("  ArcNET.LiveLab sheet write <field> <value> [--timeout-ms <ms>]");
        Console.WriteLine();
        Console.WriteLine("Notes:");
        Console.WriteLine("  - sheet commands install ephemeral CE-derived hooks and remove them before exit.");
        Console.WriteLine(
            "  - sheet snapshot/write expects the Arcanum character sheet to be open while the tool is waiting."
        );
        Console.WriteLine(
            "  - field names are case-insensitive and ignore punctuation, e.g. Strength, hp-loss, GunSmithy."
        );
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

    private static void ScanRegionForInt32Sequence(
        ProcessMemory memory,
        ProcessMemory.MemoryRegion region,
        int[] expected,
        int chunkSize,
        int overlapBytes,
        List<object> matches,
        int maxMatches
    )
    {
        var regionBase = (ulong)memory.ToUInt32Address(region.BaseAddress);
        var regionSize = (ulong)region.Size;
        var cursor = 0UL;
        byte[] carry = [];

        while (cursor < regionSize && matches.Count < maxMatches)
        {
            var remaining = regionSize - cursor;
            var readSize = (int)Math.Min((ulong)chunkSize, remaining);
            var address = (nint)(long)(regionBase + cursor);
            var bytes = memory.ReadBytes(address, readSize);

            var combined = new byte[carry.Length + bytes.Length];
            carry.CopyTo(combined, 0);
            bytes.CopyTo(combined, carry.Length);

            var combinedBase = regionBase + cursor - (ulong)carry.Length;
            ScanBufferForInt32Sequence(combined, combinedBase, expected, matches, maxMatches);

            if (overlapBytes == 0)
            {
                carry = [];
            }
            else
            {
                var preserved = Math.Min(overlapBytes, combined.Length);
                carry = combined[^preserved..];
            }

            cursor += (ulong)readSize;
        }
    }

    private static void ScanBufferForInt32Sequence(
        byte[] buffer,
        ulong bufferBase,
        int[] expected,
        List<object> matches,
        int maxMatches
    )
    {
        var neededBytes = checked(expected.Length * sizeof(int));
        for (var offset = 0; offset <= buffer.Length - neededBytes && matches.Count < maxMatches; offset += sizeof(int))
        {
            var matched = true;
            for (var index = 0; index < expected.Length; index++)
            {
                var current = BinaryPrimitives.ReadInt32LittleEndian(
                    buffer.AsSpan(offset + index * sizeof(int), sizeof(int))
                );
                if (current != expected[index])
                {
                    matched = false;
                    break;
                }
            }

            if (!matched)
                continue;

            matches.Add(
                new
                {
                    Address = ProcessMemory.FormatAddress((nint)(long)(bufferBase + (ulong)offset)),
                    Offset = offset,
                    Values = expected,
                }
            );
        }
    }

    private static object[] PatchInt32Addresses(ProcessMemory memory, int newValue, IReadOnlyList<nint> addresses)
    {
        return addresses
            .Select(address =>
            {
                var before = memory.ReadInt32(address);
                memory.WriteInt32(address, newValue);
                var after = memory.ReadInt32(address);

                return (object)new
                {
                    Kind = "address",
                    Address = ProcessMemory.FormatAddress(address),
                    Before = before,
                    After = after,
                };
            })
            .ToArray();
    }

    private static object[] PatchInt32RecordFieldInRange(
        ProcessMemory memory,
        nint start,
        int byteCount,
        int recordIntCount,
        int fieldIndex,
        int expectedValue,
        int newValue,
        int[] header
    )
    {
        const int maxWrites = 256;

        var bytes = memory.ReadBytes(start, byteCount);
        var writes = new List<object>();
        var recordBytes = checked(recordIntCount * sizeof(int));
        var fieldOffset = checked(fieldIndex * sizeof(int));

        for (var offset = 0; offset <= bytes.Length - recordBytes && writes.Count < maxWrites; offset += sizeof(int))
        {
            var matched = true;
            for (var index = 0; index < header.Length; index++)
            {
                var current = BinaryPrimitives.ReadInt32LittleEndian(
                    bytes.AsSpan(offset + index * sizeof(int), sizeof(int))
                );
                if (current != header[index])
                {
                    matched = false;
                    break;
                }
            }

            if (!matched)
                continue;

            var before = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(offset + fieldOffset, sizeof(int)));
            if (before != expectedValue)
                continue;

            var fieldAddress = start + offset + fieldOffset;
            memory.WriteInt32(fieldAddress, newValue);
            var after = memory.ReadInt32(fieldAddress);

            writes.Add(
                new
                {
                    Kind = "record",
                    RecordAddress = ProcessMemory.FormatAddress(start + offset),
                    FieldIndex = fieldIndex,
                    FieldAddress = ProcessMemory.FormatAddress(fieldAddress),
                    Before = before,
                    After = after,
                }
            );
        }

        return writes.ToArray();
    }
}
