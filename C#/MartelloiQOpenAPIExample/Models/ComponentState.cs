using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace iQOpenApiExample.Models
{

    [DisplayName("esentity")]
    public class esentityState
    {
        public object joinKey { get; set; } = "parent";
        public string componentKey { get; set; }
        public string state { get; set; }
        [JsonIgnore]
        public HealthState StateEnum { get; set; }
        public int stateIndex
        {
            get { return (int)StateEnum; }
            set { StateEnum = (HealthState)value; }
        }

        public DateTime? timestamp { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Include)]
        public DateTime? lastSyncTime { get; set; }

        public bool isCurrent => lastSyncTime.HasValue;

        public Dictionary<string, object> source { get; set; }
        public string sourceName { get; set; }

        public string sourceType { get; set; }

        public Guid sourceId { get; set; }
    }

    public enum HealthState
    {
        [Description("Unknown")]
        Unknown = 0,

        [Description("Unreachable")]
        Unreachable = 1,

        [Description("Not Monitored")]
        NotMonitored = 2,

        [Description("In Maintenance Mode")]
        InMaintenanceMode = 3,

        [Description("Healthy")]
        Healthy = 4,

        [Description("Warning")]
        Warning = 5,

        [Description("Critical")]
        Critical = 6,
    }
}
