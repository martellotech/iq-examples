using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace iQOpenApiExample.Models
{
    public class Alert
    {
        
        public string key { get; set; }
        public string name { get; set; }
        public List<string> componentKey { get; set; }
        public HashSet<string> linkedComponents { get; set; }
        public AlertSeverity severityEnum { get; set; }

        public int severityIndex
        {
            get { return (int)severityEnum; }
            set { severityEnum = (AlertSeverity)value; }
        }

        public string severity => severityEnum.GetDescription();
        public string target { get; set; }
        public string message { get; set; }
        public bool isActive { get; set; }
        public bool isAcknowledged { get; set; }
        public string resolutionState { get; set; }
        public DateTime created { get; set; }
        public DateTime lastUpdated { get; set; }
        public string assignee { get; set; }
        public string url { get; set; }

        public Dictionary<string, object> source { get; set; }
        public string sourceName { get; set; }

        public string sourceType { get; set; }

        public Guid sourceId { get; set; }
    }

    public enum AlertSeverity
    {
        [Description("Information")]
        Information = 1,

        [Description("Warning")]
        Warning = 2,

        [Description("Error")]
        Error = 3,
    }
}
