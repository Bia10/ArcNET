namespace ArcNET.Diagnostics;

public readonly record struct RuntimeWatchTimeEventTypeDescriptor(
    string Name,
    string TimeTypeName,
    bool Saveable,
    RuntimeWatchTimeEventParamKind Param0,
    RuntimeWatchTimeEventParamKind Param1,
    RuntimeWatchTimeEventParamKind Param2,
    RuntimeWatchTimeEventParamKind Param3
)
{
    public RuntimeWatchTimeEventParamKind ParamKind(int index) =>
        index switch
        {
            0 => Param0,
            1 => Param1,
            2 => Param2,
            3 => Param3,
            _ => RuntimeWatchTimeEventParamKind.None,
        };
}
