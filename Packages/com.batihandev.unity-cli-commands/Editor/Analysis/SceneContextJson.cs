using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace BatihanDev.UnityCliCommands.Analysis
{
    internal static class SceneContextJson
    {
        internal static JToken Parse(string json)
        {
            if (json == null) throw new FormatException("JSON is required.");
            var outside = new StringBuilder(); var quoted = false;
            for (var index = 0; index < json.Length; index++)
            {
                var c = json[index];
                if (quoted)
                {
                    if (c == '\\') { if (++index >= json.Length) throw new FormatException(); }
                    else if (c == '"') { quoted = false; outside.Append('"'); }
                    else if (c < 32) throw new FormatException("Unescaped JSON control character.");
                }
                else if (c == '"') { quoted = true; outside.Append('"'); }
                else { if (c == '\'' || c == '/') throw new FormatException("JSON comments and single quotes are unsupported."); outside.Append(c); }
            }
            if (quoted || Regex.IsMatch(outside.ToString(), @",\s*[\]}]")) throw new FormatException("Trailing JSON comma or unterminated string.");
            using (var reader = new JsonTextReader(new StringReader(json)) { DateParseHandling = DateParseHandling.None })
            {
                var token = JToken.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new FormatException("Extra JSON content.");
                return token;
            }
        }
    }
}
