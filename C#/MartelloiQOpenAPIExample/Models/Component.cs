using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;


namespace iQOpenApiExample.Models
{
    public partial class esentity
    {
        public string key;

        public object joinKey { get; set; } = "parent";
        public string name { get; set; }

        public ComponentType typeEnum { get; set; }
        public string Type => typeEnum.GetDescription();

        public string host { get; set; }

        public string iPAddress { get; set; }

        public string fqdn { get; set; }

        public  object source { get; set; }
        public string sourceName { get; set; }

        public string sourceType { get; set; }

        public Guid sourceId { get; set; }

    }
    public enum ComponentType
    {
        [Description("Object")]
        Object = 1,

        [Description("Group")]
        Group = 2,

        [Description("Service")]
        Service = 3,

        [Description("Computer")]
        Computer = 4,

        [Description("Database")]
        Database = 5,

        [Description("Website")]
        Website = 6,

        [Description("Virtual Machine")]
        [EnumMember(Value = "Virtual Machine")]
        VirtualMachine = 7,
    }
}
