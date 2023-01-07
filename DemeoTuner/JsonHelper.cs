using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DemeoTuner
{
    public static class JsonHelper
    {
        public static List<string> SkipTypes = new List<string>();

        public static string Serialize(object obj, int maxDepth)
        {
            SkipTypes.Add("System.Object");
            while (true)
            {
                try
                {
                    using (var strWriter = new StringWriter())
                    {
                        using (var jsonWriter = new CustomJsonTextWriter(strWriter))
                        {
                            Func<bool> include = () => jsonWriter.CurrentDepth <= maxDepth;
                            var resolver = new CustomContractResolver(include);
                            var serializer = new JsonSerializer { ContractResolver = resolver };
                            serializer.Serialize(jsonWriter, obj);
                        }
                        var json = strWriter.ToString();

                        return json;
                    }
                }
                catch (Exception ex)
                {
                    var regex = new Regex(@".+(?:' on '|' with type '| at ([\w\.]+?)\.Equals)([\w\.]*).*", RegexOptions.Singleline);
                    var match = regex.Match(ex.ToString());
                    //File.WriteAllLines("D:\\_matches.txt", match.Groups.Cast<Group>().Select(g => g.Value));
                    if (match.Success)
                    {
                        var typeName = string.IsNullOrEmpty(match.Groups[2].Value)
                            ? match.Groups[1].Value
                            : match.Groups[2].Value;
                        if (!string.IsNullOrEmpty(typeName) && !SkipTypes.Contains(typeName))
                        {
                            SkipTypes.Add(typeName);
                            continue;
                        }
                    }
                    throw new InvalidOperationException(string.Join(", ", SkipTypes), ex);
                }
            }
        }
    }

    public class CustomContractResolver : DefaultContractResolver
    {
        private readonly Func<bool> _includeProperty;

        public CustomContractResolver(Func<bool> includeProperty)
        {
            _includeProperty = includeProperty;
        }

        protected override JsonProperty CreateProperty(
            MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            var shouldSerialize = property.ShouldSerialize;

            property.ShouldSerialize = obj => !JsonHelper.SkipTypes.Any(t => t.EndsWith(property.DeclaringType.Name))
                && _includeProperty()
                && (shouldSerialize == null || shouldSerialize(obj));
            return property;
        }
    }

    public class CustomJsonTextWriter : JsonTextWriter
    {
        public CustomJsonTextWriter(TextWriter textWriter) : base(textWriter) { }

        public int CurrentDepth { get; private set; }

        public override void WriteStartObject()
        {
            CurrentDepth++;
            base.WriteStartObject();
        }

        public override void WriteEndObject()
        {
            CurrentDepth--;
            base.WriteEndObject();
        }
    }
}
