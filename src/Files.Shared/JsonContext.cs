using System.Collections.Generic;
using System.Text.Json.Serialization;
using static Common.FileTagsDb;

namespace Common
{
    [JsonSerializable(typeof(IEnumerable<TaggedFile>))]
    [JsonSerializable(typeof(TaggedFile[]))]
    internal partial class JsonContext : JsonSerializerContext
    {

    }
}