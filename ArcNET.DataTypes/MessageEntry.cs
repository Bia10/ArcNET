using Spectre.Console;
using System;
using System.IO;

namespace ArcNET.DataTypes;

public class MessageEntry
{
    private int _index;
    private int _dataCount;
    private string[] _data;

    public MessageEntry(string[] data)
    {
        _data = data;
        if (!ParseIndex())
            throw new Exception("Failure to parse index for MessageEntry");
    }

    public MessageEntry(string line)
    {
        _data = new MessageEntryReader(line).Parse();
        if (!ParseIndex())
            throw new Exception("Failure to parse index for MessageEntry");
    }

    public MessageEntry(TextReader textReader)
    {
        _data = new MessageEntryReader(textReader).Parse();
        if (!ParseIndex())
            throw new Exception("Failure to parse index for MessageEntry");
    }

    private bool ParseIndex()
    {
        var indexParsed = false;

        try
        {
            indexParsed = int.TryParse(_data[0], out _index);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
        }

        _dataCount = _data.Length;

        return indexParsed && (_dataCount is 2 or 7);
    }

    public int GetIndex()
        => _index;
}