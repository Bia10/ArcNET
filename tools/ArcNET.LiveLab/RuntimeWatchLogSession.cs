using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using ZLogger;
using ZLogger.Formatters;

namespace ArcNET.LiveLab;

[SupportedOSPlatform("windows")]
internal sealed class RuntimeWatchLogSession : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly RuntimeWatchObjectResolver _resolver;

    private RuntimeWatchLogSession(
        string logFilePath,
        ILoggerFactory loggerFactory,
        RuntimeWatchObjectResolver resolver
    )
    {
        LogFilePath = logFilePath;
        _loggerFactory = loggerFactory;
        _resolver = resolver;
        _logger = loggerFactory.CreateLogger("ArcNET.LiveLab.Watch");
    }

    public string LogFilePath { get; }

    public static RuntimeWatchLogSession Create(string logFilePath, RuntimeWatchObjectResolver resolver)
    {
        var fullPath = Path.GetFullPath(logFilePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var loggerFactory = LoggerFactory.Create(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddZLoggerFile(fullPath, static options => ConfigureStructuredJson(options));
        });

        return new RuntimeWatchLogSession(fullPath, loggerFactory, resolver);
    }

    public void LogWatchStart(
        string[] selectors,
        TimeSpan pollInterval,
        TimeSpan? duration,
        bool consoleEvents,
        bool includeNoise,
        TimeSpan summaryInterval,
        ProcessMemory memory,
        IReadOnlyList<RuntimeWatchHookDefinition> hooks
    )
    {
        var durationMs = duration is { } value ? (int?)value.TotalMilliseconds : null;
        var selectorsText = string.Join(",", selectors);
        var hooksText = string.Join(",", hooks.Select(static hook => hook.Key));
        _logger.ZLogInformation(
            $"{WatchStartType:@Type} {LogFilePath:@LogFilePath} {selectorsText:@Selectors} {hooksText:@Hooks} {(int)pollInterval.TotalMilliseconds:@PollMs} {durationMs:@DurationMs} {!duration.HasValue:@RunsIndefinitely} {consoleEvents:@ConsoleEvents} {includeNoise:@IncludeNoise} {(int)summaryInterval.TotalMilliseconds:@SummaryMs} {memory.ProcessId:@ProcessId} {ProcessMemory.FormatAddress(memory.ModuleBase):@ModuleBase} {_resolver.NamesEnabled:@NameResolutionEnabled} {_resolver.GameDirectory:@GameDirectory} {_resolver.StatusText:@NameResolutionStatus}"
        );
    }

    public void LogWatchOverflow(int droppedEvents, uint writeSequence)
    {
        _logger.ZLogWarning($"{WatchOverflowType:@Type} {droppedEvents:@DroppedEvents} {writeSequence:@WriteSequence}");
    }

    public void LogWatchWarning(int inconsistentRecords, uint writeSequence)
    {
        _logger.ZLogWarning(
            $"{WatchWarningType:@Type} {inconsistentRecords:@InconsistentRecords} {writeSequence:@WriteSequence}"
        );
    }

    public void LogWatchEnd(
        uint lastSequence,
        int emittedEvents,
        int suppressedEvents,
        int droppedEvents,
        int inconsistentRecords
    )
    {
        _logger.ZLogInformation(
            $"{WatchEndType:@Type} {lastSequence:@LastSequence} {emittedEvents:@EmittedEvents} {suppressedEvents:@SuppressedEvents} {droppedEvents:@DroppedEvents} {inconsistentRecords:@InconsistentRecords}"
        );
    }

    public void LogEvent(RuntimeWatchSession.RuntimeWatchCapturedEvent capturedEvent, DateTime timestampUtc)
    {
        var stack = capturedEvent.StackDwords;
        var definition = capturedEvent.Definition;
        var hookArea = RuntimeWatchEventInterpreter.HookArea(capturedEvent);
        var hookDescription = RuntimeWatchEventInterpreter.HookDescription(capturedEvent);
        var semanticEvent = RuntimeWatchEventInterpreter.SemanticEvent(capturedEvent);
        var category = RuntimeWatchEventInterpreter.Category(capturedEvent);
        var eventClass = RuntimeWatchEventInterpreter.EventClass(capturedEvent);
        var primaryRole = RuntimeWatchEventInterpreter.PrimaryRole(capturedEvent);
        var secondaryRole = RuntimeWatchEventInterpreter.SecondaryRole(capturedEvent);
        var extraRole = RuntimeWatchEventInterpreter.ExtraRole(capturedEvent);
        var signature = RuntimeWatchEventInterpreter.Signature(capturedEvent, _resolver);
        var summary = RuntimeWatchEventInterpreter.Summary(capturedEvent, _resolver);

        switch (definition.Id)
        {
            case RuntimeWatchHookId.LevelRecalc:
            {
                var pc = Resolve(in stack, 0);
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {pc.HandleHex:@PcHandle} {pc.Name:@PcName} {pc.ObjectTypeName:@PcObjectType} {pc.ProtoNumber:@PcProtoNumber} {pc.ProtoAssetPath:@PcProtoAssetPath} {pc.ArtAssetPath:@PcArtAssetPath} {pc.ResolutionSource:@PcResolutionSource}"
                );
                return;
            }
            case RuntimeWatchHookId.UpdateFollowerLevel:
            {
                var follower = Resolve(in stack, 0);
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {follower.HandleHex:@FollowerHandle} {follower.Name:@FollowerName} {follower.ObjectTypeName:@FollowerObjectType} {follower.ProtoNumber:@FollowerProtoNumber} {follower.ProtoAssetPath:@FollowerProtoAssetPath} {follower.ArtAssetPath:@FollowerArtAssetPath} {follower.ResolutionSource:@FollowerResolutionSource} {RuntimeWatchEventInterpreter.IntValue(capturedEvent, 2):@OldPcLevel} {RuntimeWatchEventInterpreter.IntValue(capturedEvent, 3):@NewPcLevel} {RuntimeWatchEventInterpreter.IntValue(capturedEvent, 3) - RuntimeWatchEventInterpreter.IntValue(capturedEvent, 2):@LevelDelta}"
                );
                return;
            }
            case RuntimeWatchHookId.StatBaseSet:
            {
                var obj = Resolve(in stack, 0);
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {obj.HandleHex:@ObjectHandle} {obj.Name:@ObjectName} {obj.ObjectTypeName:@ObjectType} {obj.ProtoNumber:@ObjectProtoNumber} {obj.ProtoAssetPath:@ObjectProtoAssetPath} {obj.ArtAssetPath:@ObjectArtAssetPath} {obj.ResolutionSource:@ObjectResolutionSource} {RuntimeWatchEventInterpreter.IntValue(capturedEvent, 2):@Stat} {RuntimeWatchEventInterpreter.StatName(RuntimeWatchEventInterpreter.IntValue(capturedEvent, 2)):@StatName} {RuntimeWatchEventInterpreter.StatGroup(RuntimeWatchEventInterpreter.IntValue(capturedEvent, 2)):@StatGroup} {RuntimeWatchEventInterpreter.IntValue(capturedEvent, 3):@Value}"
                );
                return;
            }
            case RuntimeWatchHookId.BackgroundEducateFollowers:
            {
                var pc = Resolve(in stack, 0);
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {pc.HandleHex:@PcHandle} {pc.Name:@PcName} {pc.ObjectTypeName:@PcObjectType} {pc.ProtoNumber:@PcProtoNumber} {pc.ProtoAssetPath:@PcProtoAssetPath} {pc.ArtAssetPath:@PcArtAssetPath} {pc.ResolutionSource:@PcResolutionSource}"
                );
                return;
            }
            case RuntimeWatchHookId.UiShowInvenLoot:
            {
                var pc = Resolve(in stack, 0);
                var target = Resolve(in stack, 2);
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {pc.HandleHex:@PcHandle} {pc.Name:@PcName} {pc.ObjectTypeName:@PcObjectType} {pc.ProtoNumber:@PcProtoNumber} {pc.ProtoAssetPath:@PcProtoAssetPath} {pc.ArtAssetPath:@PcArtAssetPath} {pc.ResolutionSource:@PcResolutionSource} {target.HandleHex:@TargetHandle} {target.Name:@TargetName} {target.ObjectTypeName:@TargetObjectType} {target.ProtoNumber:@TargetProtoNumber} {target.ProtoAssetPath:@TargetProtoAssetPath} {target.ArtAssetPath:@TargetArtAssetPath} {target.ResolutionSource:@TargetResolutionSource}"
                );
                return;
            }
            case RuntimeWatchHookId.ItemInsert:
            {
                var item = Resolve(in stack, 0);
                var parent = Resolve(in stack, 2);
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {item.HandleHex:@ItemHandle} {item.Name:@ItemName} {item.ObjectTypeName:@ItemObjectType} {item.ProtoNumber:@ItemProtoNumber} {item.ProtoAssetPath:@ItemProtoAssetPath} {item.ArtAssetPath:@ItemArtAssetPath} {item.ResolutionSource:@ItemResolutionSource} {parent.HandleHex:@ParentHandle} {parent.Name:@ParentName} {parent.ObjectTypeName:@ParentObjectType} {parent.ProtoNumber:@ParentProtoNumber} {parent.ProtoAssetPath:@ParentProtoAssetPath} {parent.ArtAssetPath:@ParentArtAssetPath} {parent.ResolutionSource:@ParentResolutionSource} {RuntimeWatchEventInterpreter.IntValue(capturedEvent, 4):@InventoryLocation} {RuntimeWatchEventInterpreter.InventoryLocationName(RuntimeWatchEventInterpreter.IntValue(capturedEvent, 4)):@InventoryLocationName} {RuntimeWatchEventInterpreter.InventoryLocationContext(RuntimeWatchEventInterpreter.IntValue(capturedEvent, 4)):@InventoryContext}"
                );
                return;
            }
            case RuntimeWatchHookId.ItemEquipped:
            {
                var item = Resolve(in stack, 0);
                var parent = Resolve(in stack, 2);
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {item.HandleHex:@ItemHandle} {item.Name:@ItemName} {item.ObjectTypeName:@ItemObjectType} {item.ProtoNumber:@ItemProtoNumber} {item.ProtoAssetPath:@ItemProtoAssetPath} {item.ArtAssetPath:@ItemArtAssetPath} {item.ResolutionSource:@ItemResolutionSource} {parent.HandleHex:@ParentHandle} {parent.Name:@ParentName} {parent.ObjectTypeName:@ParentObjectType} {parent.ProtoNumber:@ParentProtoNumber} {parent.ProtoAssetPath:@ParentProtoAssetPath} {parent.ArtAssetPath:@ParentArtAssetPath} {parent.ResolutionSource:@ParentResolutionSource} {RuntimeWatchEventInterpreter.IntValue(capturedEvent, 4):@InventoryLocation} {RuntimeWatchEventInterpreter.InventoryLocationName(RuntimeWatchEventInterpreter.IntValue(capturedEvent, 4)):@InventoryLocationName} {RuntimeWatchEventInterpreter.InventoryLocationContext(RuntimeWatchEventInterpreter.IntValue(capturedEvent, 4)):@InventoryContext}"
                );
                return;
            }
            case RuntimeWatchHookId.ItemForceRemove:
            {
                var item = Resolve(in stack, 0);
                var parent = Resolve(in stack, 2);
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {item.HandleHex:@ItemHandle} {item.Name:@ItemName} {item.ObjectTypeName:@ItemObjectType} {item.ProtoNumber:@ItemProtoNumber} {item.ProtoAssetPath:@ItemProtoAssetPath} {item.ArtAssetPath:@ItemArtAssetPath} {item.ResolutionSource:@ItemResolutionSource} {parent.HandleHex:@ParentHandle} {parent.Name:@ParentName} {parent.ObjectTypeName:@ParentObjectType} {parent.ProtoNumber:@ParentProtoNumber} {parent.ProtoAssetPath:@ParentProtoAssetPath} {parent.ArtAssetPath:@ParentArtAssetPath} {parent.ResolutionSource:@ParentResolutionSource}"
                );
                return;
            }
            case RuntimeWatchHookId.ItemUnequipped:
            {
                var item = Resolve(in stack, 0);
                var parent = Resolve(in stack, 2);
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {item.HandleHex:@ItemHandle} {item.Name:@ItemName} {item.ObjectTypeName:@ItemObjectType} {item.ProtoNumber:@ItemProtoNumber} {item.ProtoAssetPath:@ItemProtoAssetPath} {item.ArtAssetPath:@ItemArtAssetPath} {item.ResolutionSource:@ItemResolutionSource} {parent.HandleHex:@ParentHandle} {parent.Name:@ParentName} {parent.ObjectTypeName:@ParentObjectType} {parent.ProtoNumber:@ParentProtoNumber} {parent.ProtoAssetPath:@ParentProtoAssetPath} {parent.ArtAssetPath:@ParentArtAssetPath} {parent.ResolutionSource:@ParentResolutionSource} {RuntimeWatchEventInterpreter.IntValue(capturedEvent, 4):@InventoryLocation} {RuntimeWatchEventInterpreter.InventoryLocationName(RuntimeWatchEventInterpreter.IntValue(capturedEvent, 4)):@InventoryLocationName} {RuntimeWatchEventInterpreter.InventoryLocationContext(RuntimeWatchEventInterpreter.IntValue(capturedEvent, 4)):@InventoryContext}"
                );
                return;
            }
            case RuntimeWatchHookId.ObjectDestroy:
            {
                var obj = Resolve(in stack, 0);
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {obj.HandleHex:@ObjectHandle} {obj.Name:@ObjectName} {obj.ObjectTypeName:@ObjectType} {obj.ProtoNumber:@ObjectProtoNumber} {obj.ProtoAssetPath:@ObjectProtoAssetPath} {obj.ArtAssetPath:@ObjectArtAssetPath} {obj.ResolutionSource:@ObjectResolutionSource}"
                );
                return;
            }
            case RuntimeWatchHookId.ObjectScriptExecute:
            {
                var triggerer = Resolve(in stack, 0);
                var attachee = Resolve(in stack, 2);
                var extra = Resolve(in stack, 4);
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {triggerer.HandleHex:@TriggererHandle} {triggerer.Name:@TriggererName} {triggerer.ObjectTypeName:@TriggererObjectType} {triggerer.ProtoNumber:@TriggererProtoNumber} {triggerer.ProtoAssetPath:@TriggererProtoAssetPath} {triggerer.ArtAssetPath:@TriggererArtAssetPath} {triggerer.ResolutionSource:@TriggererResolutionSource} {attachee.HandleHex:@AttacheeHandle} {attachee.Name:@AttacheeName} {attachee.ObjectTypeName:@AttacheeObjectType} {attachee.ProtoNumber:@AttacheeProtoNumber} {attachee.ProtoAssetPath:@AttacheeProtoAssetPath} {attachee.ArtAssetPath:@AttacheeArtAssetPath} {attachee.ResolutionSource:@AttacheeResolutionSource} {extra.HandleHex:@ExtraHandle} {extra.Name:@ExtraName} {extra.ObjectTypeName:@ExtraObjectType} {extra.ProtoNumber:@ExtraProtoNumber} {extra.ProtoAssetPath:@ExtraProtoAssetPath} {extra.ArtAssetPath:@ExtraArtAssetPath} {extra.ResolutionSource:@ExtraResolutionSource} {RuntimeWatchEventInterpreter.AttachmentPoint(capturedEvent):@AttachmentPoint} {RuntimeWatchEventInterpreter.AttachmentPointName(RuntimeWatchEventInterpreter.AttachmentPoint(capturedEvent)):@AttachmentPointName} {RuntimeWatchEventInterpreter.IntValue(capturedEvent, 7):@Line} {RuntimeWatchEventInterpreter.HandleValue(in stack, 4) != 0:@HasExtraObject} {RuntimeWatchEventInterpreter.HandleValue(in stack, 0) == RuntimeWatchEventInterpreter.HandleValue(in stack, 2):@IsSelfTarget}"
                );
                return;
            }
            case RuntimeWatchHookId.UiStartDialog:
            {
                var pc = Resolve(in stack, 0);
                var npc = Resolve(in stack, 2);
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {pc.HandleHex:@PcHandle} {pc.Name:@PcName} {pc.ObjectTypeName:@PcObjectType} {pc.ProtoNumber:@PcProtoNumber} {pc.ProtoAssetPath:@PcProtoAssetPath} {pc.ArtAssetPath:@PcArtAssetPath} {pc.ResolutionSource:@PcResolutionSource} {npc.HandleHex:@NpcHandle} {npc.Name:@NpcName} {npc.ObjectTypeName:@NpcObjectType} {npc.ProtoNumber:@NpcProtoNumber} {npc.ProtoAssetPath:@NpcProtoAssetPath} {npc.ArtAssetPath:@NpcArtAssetPath} {npc.ResolutionSource:@NpcResolutionSource} {RuntimeWatchEventInterpreter.IntValue(capturedEvent, 4):@ScriptNum} {RuntimeWatchEventInterpreter.IntValue(capturedEvent, 5):@ScriptLine} {RuntimeWatchEventInterpreter.IntValue(capturedEvent, 6):@DialogIndex}"
                );
                return;
            }
            case RuntimeWatchHookId.ReactionAdj:
            {
                var npc = Resolve(in stack, 0);
                var pc = Resolve(in stack, 2);
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {npc.HandleHex:@NpcHandle} {npc.Name:@NpcName} {npc.ObjectTypeName:@NpcObjectType} {npc.ProtoNumber:@NpcProtoNumber} {npc.ProtoAssetPath:@NpcProtoAssetPath} {npc.ArtAssetPath:@NpcArtAssetPath} {npc.ResolutionSource:@NpcResolutionSource} {pc.HandleHex:@PcHandle} {pc.Name:@PcName} {pc.ObjectTypeName:@PcObjectType} {pc.ProtoNumber:@PcProtoNumber} {pc.ProtoAssetPath:@PcProtoAssetPath} {pc.ArtAssetPath:@PcArtAssetPath} {pc.ResolutionSource:@PcResolutionSource} {RuntimeWatchEventInterpreter.IntValue(capturedEvent, 4):@Delta} {RuntimeWatchEventInterpreter.ReactionDirection(RuntimeWatchEventInterpreter.IntValue(capturedEvent, 4)):@ReactionDirection} {RuntimeWatchEventInterpreter.ReactionDirectionText(RuntimeWatchEventInterpreter.IntValue(capturedEvent, 4)):@ReactionDirectionText}"
                );
                return;
            }
            case RuntimeWatchHookId.CritterKill:
            {
                var obj = Resolve(in stack, 0);
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {obj.HandleHex:@ObjectHandle} {obj.Name:@ObjectName} {obj.ObjectTypeName:@ObjectType} {obj.ProtoNumber:@ObjectProtoNumber} {obj.ProtoAssetPath:@ObjectProtoAssetPath} {obj.ArtAssetPath:@ObjectArtAssetPath} {obj.ResolutionSource:@ObjectResolutionSource}"
                );
                return;
            }
            default:
                _logger.ZLogInformation(
                    $"{EventType:@Type} {capturedEvent.Sequence:@Sequence} {definition.Key:@Hook} {definition.EventName:@EventName} {hookArea:@HookArea} {hookDescription:@HookDescription} {semanticEvent:@SemanticEvent} {category:@Category} {eventClass:@EventClass} {primaryRole:@PrimaryRole} {secondaryRole:@SecondaryRole} {extraRole:@ExtraRole} {signature:@Signature} {summary:@Summary} {definition.Site:@Site} {timestampUtc:@TimestampUtc} {stack.D0:@RawStack0} {stack.D1:@RawStack1} {stack.D2:@RawStack2} {stack.D3:@RawStack3} {stack.D4:@RawStack4} {stack.D5:@RawStack5} {stack.D6:@RawStack6} {stack.D7:@RawStack7}"
                );
                return;
        }
    }

    public void Dispose() => _loggerFactory.Dispose();

    private RuntimeWatchObjectResolver.ResolvedObject Resolve(
        in RuntimeWatchSession.RuntimeWatchStackCapture stack,
        int lowIndex
    ) => _resolver.ResolveHandle(RuntimeWatchEventInterpreter.HandleValue(in stack, lowIndex));

    private static void ConfigureStructuredJson(ZLoggerOptions options)
    {
        options.InternalErrorLogger = static ex => Console.Error.WriteLine(ex);
        options.UseJsonFormatter(static formatter =>
            formatter.IncludeProperties = IncludeProperties.ParameterKeyValues
        );
    }

    private const string EventType = "event";
    private const string WatchEndType = "watch-end";
    private const string WatchOverflowType = "watch-overflow";
    private const string WatchStartType = "watch-start";
    private const string WatchWarningType = "watch-warning";
}
