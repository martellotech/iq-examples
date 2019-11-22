using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace iQOpenApiExample.Models
{
    public partial class Response
{
    [JsonProperty("_index")]
    public string Index { get; set; }

    [JsonProperty("_type")]
    public string Type { get; set; }

    [JsonProperty("_id")]
    public string Id { get; set; }

    [JsonProperty("_version")]
    public long Version { get; set; }

    [JsonProperty("result")]
    public string Result { get; set; }

    [JsonProperty("_shards")]
    public Shards Shards { get; set; }

    [JsonProperty("created")]
    public bool Created { get; set; }
}

public partial class Shards
{
    [JsonProperty("total")]
    public long Total { get; set; }

    [JsonProperty("successful")]
    public long Successful { get; set; }

    [JsonProperty("failed")]
    public long Failed { get; set; }
}
}
