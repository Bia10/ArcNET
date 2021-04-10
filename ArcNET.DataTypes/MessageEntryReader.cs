﻿using ArcNET.Utilities;
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
            //failures to conform to pattern:
            input = input switch
            {
                "2028 door light wood" => "{2028}{door-light-wood}",
                "2029 door heavy wood" => "{2029}{door-heavy-wood}",
                "2030 door metal gate" => "{2030}{door-metal-gate}",
                "2031 door metal" => "{2031}{door-metal}",
                "2032 door glass" => "{2032}{door-glass}",
                "2033 door stone" => "{2033}{door-stone}",
                "2034 door rock cave in" => "{2034}{door-rock-cave-in}",
                "*** Windows ***" => "{00000}{***-Windows-***}",
                "2035 window house standard" => "{2035}{window-house-standard}",
                "2036 window store front" => "{2036}{window-store-front}",
                "2037 window curtains only" => "{2037}{window-curtains-only}",
                "2038 window jail bars" => "{2038}{window-jail-bars}",
                "2039 window wooden shutters" => "{2039}{window-wooden-shutters}",
                "2040 window metal shutters" => "{2040}{window-metal-shutters}",
                "2041 window stained glass" => "{2041}{window-stained-glass}",
                "2042 window boarded up" => "{2042}{window-boarded-up}",
                "" => "{00000}{?????}",
                "INFO:    0-999           are wall types and parameters." => "{00000}{INFO: 0-999 are wall types and parameters.}", //bugged
                "1000-1999        defines thumbnail for world-ed." => "{00000}{1000-1999 defines thumbnail for world-ed.}", //bugged
                "Thumbnail must be named the same as indicated in the 1000+ section." => "{00000}{Thumbnail must be named the same as indicated in the 1000+ section.}",
                "Interior-Exterior-Floor-Roof" => "{00000}{Interior-Exterior-Floor-Roof}",
                _ => input
            };

            var regex = new Regex(Pattern);
            if (Regex.Matches(input, Pattern).Count == 0)
                AnsiConsoleExtensions.Log($"Input: |{input}| failed to match pattern: {Pattern}!", "error");
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