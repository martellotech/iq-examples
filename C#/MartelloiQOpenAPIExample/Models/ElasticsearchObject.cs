using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace iQOpenApiExample.Models
{
    public partial class ElasticsearchObject
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("cluster_name")]
        public string ClusterName { get; set; }

        [JsonProperty("cluster_uuid")]
        public string ClusterUuid { get; set; }

        [JsonProperty("version")]
        public Version Version { get; set; }

        [JsonProperty("tagline")]
        public string Tagline { get; set; }
    }

    public partial class Version
    {
        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("build_flavor")]
        public string BuildFlavor { get; set; }

        [JsonProperty("build_type")]
        public string BuildType { get; set; }

        [JsonProperty("build_hash")]
        public string BuildHash { get; set; }

        [JsonProperty("build_date")]
        public DateTimeOffset BuildDate { get; set; }

        [JsonProperty("build_snapshot")]
        public bool BuildSnapshot { get; set; }

        [JsonProperty("lucene_version")]
        public string LuceneVersion { get; set; }

        [JsonProperty("minimum_wire_compatibility_version")]
        public string MinimumWireCompatibilityVersion { get; set; }

        [JsonProperty("minimum_index_compatibility_version")]
        public string MinimumIndexCompatibilityVersion { get; set; }
    }

}
