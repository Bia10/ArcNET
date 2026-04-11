using System.Text;
using ArcNET.Core;
using Bia.ValueBuffers;

namespace ArcNET.Formats;

/// <summary>
/// A single entry in an Arcanum dialogue (.dlg) file.
/// </summary>
public sealed class DialogEntry
{
    /// <summary>
    /// Unique entry identifier within the file.
    /// Entries are sorted ascending by this value at load time.
    /// </summary>
    public required int Num { get; init; }

    /// <summary>Main reply or option text shown to the player.</summary>
    public required string Text { get; init; }

    /// <summary>
    /// When <see cref="Iq"/> is 0 (NPC line): alternate female text; may be empty.
    /// When <see cref="Iq"/> is greater than 0 (PC option): gender integer as a string.
    /// </summary>
    public required string GenderField { get; init; }

    /// <summary>
    /// 0 means this is an NPC reply line.
    /// A positive value means this is a PC dialogue option requiring at least that much Intelligence.
    /// </summary>
    public required int Iq { get; init; }

    /// <summary>
    /// Condition expression evaluated at runtime; empty string means always available.
    /// The expression language is Arcanum's Python-based scripting DSL — treat as an opaque string.
    /// </summary>
    public required string Conditions { get; init; }

    /// <summary>
    /// Entry <see cref="Num"/> to transition to after this entry is selected.
    /// 0 ends the conversation.
    /// </summary>
    public required int ResponseVal { get; init; }

    /// <summary>
    /// Action expression executed when this entry is selected.
    /// Opaque DSL string — pass through verbatim.
    /// </summary>
    public required string Actions { get; init; }
}

/// <summary>Parsed contents of an Arcanum dialogue (.dlg) file.</summary>
public sealed class DlgFile
{
    /// <summary>All entries, ordered ascending by <see cref="DialogEntry.Num"/>.</summary>
    public required IReadOnlyList<DialogEntry> Entries { get; init; }
}

/// <summary>
/// Span-based parser and writer for Arcanum dialogue (.dlg) files.
/// DLG is a <b>plain-text</b> format: each entry consists of exactly 7 brace-delimited fields
/// in the order <c>{num}{str}{gender}{iq}{conditions}{response_val}{actions}</c>.
/// </summary>
public sealed class DialogFormat : IFormatFileReader<DlgFile>, IFormatFileWriter<DlgFile>
{
    // Arcanum .dlg files are encoded in Windows-1252 (cp1252).
    // Registering the provider once allows GetEncoding(1252) on all platforms.
    private static readonly Encoding s_encoding;

    static DialogFormat()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        s_encoding = Encoding.GetEncoding(1252);
    }

    /// <inheritdoc/>
    public static DlgFile Parse(scoped ref SpanReader reader)
    {
        var text = s_encoding.GetString(reader.ReadBytes(reader.Remaining));
        return ParseText(text);
    }

    private static DlgFile ParseText(string text)
    {
        var entries = new List<DialogEntry>();
        var tok = new ValueTokenizer(text.AsSpan());

        while (true)
        {
            if (!tok.TryReadNested(out var numSpan))
                break;

            if (!tok.TryReadNested(out var strSpan))
                break;

            if (!tok.TryReadNested(out var genderSpan))
                break;

            if (!tok.TryReadNested(out var iqSpan))
                break;

            if (!tok.TryReadNested(out var condSpan))
                break;

            if (!tok.TryReadNested(out var respSpan))
                break;

            if (!tok.TryReadNested(out var actsSpan))
                break;

            if (
                !int.TryParse(numSpan.Trim(), out var num)
                || !int.TryParse(iqSpan.Trim(), out var iq)
                || !int.TryParse(respSpan.Trim(), out var resp)
            )
                continue;

            entries.Add(
                new DialogEntry
                {
                    Num = num,
                    Text = strSpan.ToString(),
                    GenderField = genderSpan.ToString(),
                    Iq = iq,
                    Conditions = condSpan.ToString(),
                    ResponseVal = resp,
                    Actions = actsSpan.ToString(),
                }
            );
        }

        entries.Sort(static (a, b) => a.Num.CompareTo(b.Num));
        return new DlgFile { Entries = entries };
    }

    /// <inheritdoc/>
    public static DlgFile ParseMemory(ReadOnlyMemory<byte> memory) =>
        FormatIo.ParseMemory<DialogFormat, DlgFile>(memory);

    /// <inheritdoc/>
    public static DlgFile ParseFile(string path) => FormatIo.ParseFile<DialogFormat, DlgFile>(path);

    /// <inheritdoc/>
    public static void Write(in DlgFile value, ref SpanWriter writer)
    {
        Span<char> buf = stackalloc char[1024];
        var sb = new ValueStringBuilder(buf);
        foreach (var e in value.Entries)
        {
            sb.Append('{');
            sb.Append(e.Num);
            sb.Append('}');
            sb.Append('{');
            sb.Append(e.Text);
            sb.Append('}');
            sb.Append('{');
            sb.Append(e.GenderField);
            sb.Append('}');
            sb.Append('{');
            sb.Append(e.Iq);
            sb.Append('}');
            sb.Append('{');
            sb.Append(e.Conditions);
            sb.Append('}');
            sb.Append('{');
            sb.Append(e.ResponseVal);
            sb.Append('}');
            sb.Append('{');
            sb.Append(e.Actions);
            sb.Append('}');
            sb.AppendLine();
        }

        writer.WriteBytes(s_encoding.GetBytes(sb.ToString()));
    }

    /// <inheritdoc/>
    public static byte[] WriteToArray(in DlgFile value) => FormatIo.WriteToArray<DialogFormat, DlgFile>(in value);

    /// <inheritdoc/>
    public static void WriteToFile(in DlgFile value, string path) =>
        FormatIo.WriteToFile<DialogFormat, DlgFile>(in value, path);
}
