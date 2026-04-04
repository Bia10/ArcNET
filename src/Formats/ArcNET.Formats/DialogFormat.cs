using System.Buffers;
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
public sealed class DialogFormat : IFormatReader<DlgFile>, IFormatWriter<DlgFile>
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
        var pos = 0;

        string? NextField()
        {
            var start = text.IndexOf('{', pos);
            if (start < 0)
                return null;

            var depth = 1;
            var i = start + 1;
            while (i < text.Length && depth > 0)
            {
                if (text[i] == '{')
                    depth++;
                else if (text[i] == '}')
                    depth--;
                i++;
            }

            if (depth != 0)
                return null;

            pos = i;
            return text[(start + 1)..(i - 1)];
        }

        while (true)
        {
            var numStr = NextField();
            if (numStr is null)
                break;

            var str = NextField();
            if (str is null)
                break;

            var gender = NextField();
            if (gender is null)
                break;

            var iqStr = NextField();
            if (iqStr is null)
                break;

            var cond = NextField();
            if (cond is null)
                break;

            var respStr = NextField();
            if (respStr is null)
                break;

            var acts = NextField();
            if (acts is null)
                break;

            if (
                !int.TryParse(numStr.AsSpan().Trim(), out var num)
                || !int.TryParse(iqStr.AsSpan().Trim(), out var iq)
                || !int.TryParse(respStr.AsSpan().Trim(), out var resp)
            )
                continue;

            entries.Add(
                new DialogEntry
                {
                    Num = num,
                    Text = str,
                    GenderField = gender,
                    Iq = iq,
                    Conditions = cond,
                    ResponseVal = resp,
                    Actions = acts,
                }
            );
        }

        entries.Sort(static (a, b) => a.Num.CompareTo(b.Num));
        return new DlgFile { Entries = entries };
    }

    /// <inheritdoc/>
    public static DlgFile ParseMemory(ReadOnlyMemory<byte> memory)
    {
        var reader = new SpanReader(memory.Span);
        return Parse(ref reader);
    }

    /// <inheritdoc/>
    public static DlgFile ParseFile(string path) => ParseMemory(File.ReadAllBytes(path));

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
    public static byte[] WriteToArray(in DlgFile value)
    {
        var buf = new ArrayBufferWriter<byte>();
        var writer = new SpanWriter(buf);
        Write(in value, ref writer);
        return buf.WrittenSpan.ToArray();
    }

    /// <inheritdoc/>
    public static void WriteToFile(in DlgFile value, string path) => File.WriteAllBytes(path, WriteToArray(in value));
}
