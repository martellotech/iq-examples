using System;
using System.Collections.Generic;
using System.ComponentModel;


namespace iQOpenApiExample.Models
{
    public class ComponentRelationship
    {
        public Dictionary<string, object> source { get; set; }
        public string sourceName { get; set; }

        public string sourceType { get; set; }

        public Guid sourceId { get; set; }
        public string key { get; set; }
        public string name { get; set; }

        public string sourceComponent { get; set; }

        public string destinationComponent { get; set; }

        public ComponentRelationshipType typeEnum { get; set; }
        public string type { get { return typeEnum.GetDescription(); } }
    }
    public enum ComponentRelationshipType
    {
        [Description("Hosting")]
        Hosting = 1,

        [Description("Containment")]
        Containment = 2,

        [Description("Reference")]
        Reference = 3
    }
}
