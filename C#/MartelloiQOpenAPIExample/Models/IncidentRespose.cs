using System;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace iQOpenApiExample.Models
{
    public partial class SearchRespose
    {
        [JsonProperty("took")]
        public long Took { get; set; }

        [JsonProperty("timed_out")]
        public bool TimedOut { get; set; }

        [JsonProperty("_shards")]
        public Shards Shards { get; set; }

        [JsonProperty("hits")]
        public Hits Hits { get; set; }
    }

    public partial class Hits
    {
        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("max_score")]
        public double MaxScore { get; set; }

        [JsonProperty("hits")]
        public List<Hit> HitsHits { get; set; }
    }

    public partial class Hit
    {
        [JsonProperty("_index")]
        public string Index { get; set; }

        [JsonProperty("_type")]
        public string Type { get; set; }

        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("_score")]
        public double Score { get; set; }

        [JsonProperty("_source")]
        public Source Source { get; set; }
    }

    public partial class Source
    {
        [JsonProperty("url")]
        public Uri Url { get; set; }

        [JsonProperty("assignedTo")]
        public string AssignedTo { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("impact")]
        public object Impact { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("priority")]
        public object Priority { get; set; }

        [JsonProperty("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonProperty("sourceType")]
        public string SourceType { get; set; }

        [JsonProperty("sourceId")]
        public Guid SourceId { get; set; }

        [JsonProperty("alerts")]
        public List<string> Alerts { get; set; }

        [JsonProperty("linkedComponents")]
        public object LinkedComponents { get; set; }

        [JsonProperty("sourceName")]
        public string SourceName { get; set; }

        [JsonProperty("source")]
        public SourceClass SourceSource { get; set; }

        [JsonProperty("created")]
        public DateTimeOffset Created { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("target")]
        public string Target { get; set; }

        [JsonProperty("urgency")]
        public object Urgency { get; set; }

        [JsonProperty("resolveAlerts")]
        public bool ResolveAlerts { get; set; }

        [JsonProperty("componentKey")]
        public List<object> ComponentKey { get; set; }

        [JsonProperty("joinKey")]
        public object JoinKey { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }
    }

    public partial class SourceClass
    {
        [JsonProperty("virtualConnector")]
        public Incident VirtualConnector { get; set; }
    }

    public partial class Incident
    {
        [JsonProperty("severity")]
        public string Severity { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("userroleid")]
        public long Userroleid { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("details")]
        public string Details { get; set; }

        [JsonProperty("destinationemails")]
        public string Destinationemails { get; set; }

        [JsonProperty("destinationphone")]
        public string Destinationphone { get; set; }

        [JsonProperty("affecteditemkey")]
        public long Affecteditemkey { get; set; }

        [JsonProperty("destinationaccount")]
        public string Destinationaccount { get; set; }

        [JsonProperty("userrole")]
        public string Userrole { get; set; }

        [JsonProperty("affecteditemname")]
        public string Affecteditemname { get; set; }

        [JsonProperty("affecteditemtype")]
        public string Affecteditemtype { get; set; }

        [JsonProperty("notificationtrigger")]
        public string Notificationtrigger { get; set; }

        [JsonProperty("url")]
        public Uri Url { get; set; }
    }


}
