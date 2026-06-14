namespace ArcNET.Diagnostics.Tests;

public sealed class CrashDumpAnalysisParserTests
{
    [Test]
    public async Task Parse_ExtractsStructuredCrashSummary()
    {
        const string output = """
            PROCESS_NAME:  Arcanum.exe
            EXCEPTION_CODE: (NTSTATUS) 0xc0000005 - The instruction at 0x%p referenced memory at 0x%p.
            FAULTING_IP:
            Arcanum+1234
            00401234 8bff            mov     edi,edi

            STACK_TEXT:
            0019f7b8 00401234 Arcanum+0x1234
            0019f7d0 76f47ba9 kernel32!BaseThreadInitThunk+0x19
            0019f82c 77a6c2eb ntdll!__RtlUserThreadStart+0x2b
            """;

        var parsed = CrashDumpAnalysisParser.Parse(output);

        await Assert.That(parsed.ProcessName).IsEqualTo("Arcanum.exe");
        await Assert.That(parsed.ExceptionCode).Contains("0xc0000005");
        await Assert.That(parsed.FaultingInstruction).IsEqualTo("Arcanum+1234");
        await Assert.That(parsed.StackPreview.Count).IsEqualTo(3);
        await Assert.That(parsed.StackPreview[0]).Contains("Arcanum+0x1234");
        await Assert.That(parsed.Highlights[0]).IsEqualTo("PROCESS_NAME: Arcanum.exe");
    }

    [Test]
    public async Task Parse_FallsBackToLeadingLinesWhenStructuredMarkersAreMissing()
    {
        var parsed = CrashDumpAnalysisParser.Parse("line one\r\nline two\r\nline three");

        await Assert.That(parsed.ProcessName).IsNull();
        await Assert.That(parsed.ExceptionCode).IsNull();
        await Assert.That(parsed.FaultingInstruction).IsNull();
        await Assert.That(parsed.StackPreview).IsEmpty();
        await Assert.That(parsed.Highlights).IsEquivalentTo(["line one", "line two", "line three"]);
    }
}
