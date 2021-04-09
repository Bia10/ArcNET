using System.IO;
using System.Text.RegularExpressions;

namespace ArcNET.DataTypes
{
    public class MessageEntryReader
    {
        private readonly string _input;
        private const string Pattern = @"{(.*?)}";
        private const string Substitution = @"$1";

        public MessageEntryReader(TextReader textReader)
        {
            _input = textReader.ReadLine();
        }

        public MessageEntryReader(string input)
        {
            _input = input;
        }

        private static string[] Parse(string input)
        {
            var regex = new Regex(Pattern);
            var matches = Regex.Matches(input, Pattern);

            var output = new string[matches.Count];
            for (var i = 0; i < matches.Count; i++)
            {
                output[i] = regex.Replace(matches[i].Value, Substitution);
            }

            return output;
        }

        public string[] Parse()
        {
            return Parse(_input);
        }
    }
}