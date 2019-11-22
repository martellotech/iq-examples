using CommandLine;
using iQOpenApiExample.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace iQOpenApiExample
{
    class Options
    {

        [Option('e', "elasticsearch", Default = "localhost", HelpText = "elasticsearch server.")]
        public string Server { get; set; }

        [Option('p', "port", Default = "9200", HelpText = "elasticsearch server port (default 9200).")]
        public string Port { get; set; }


        [Option('s', "sourceGuid", Default = null, HelpText = "To get the Source Guid you need to generate it by to add Open API connector in iQ.")]
        public string sourceGuid { get; set; }
    }

    static class Program
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        public static void Main(string[] args)
        {
            Helper.ColoredConsoleWriteLine("Welcome to a demo Open API connector implementation", ConsoleColor.White);
            Helper.ColoredConsoleWriteLine("For the demonstration, we will store last 10 events in the event log into iQ as alerts", ConsoleColor.White);
            Helper.ColoredConsoleWriteLine("NOTE:To store that data you need a source guid.\nTo get the Source Guid you need to generate it by to add Open API connector in iQ.", ConsoleColor.DarkYellow);

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opts => InitializeDemo(opts.Server, opts.Port, opts.sourceGuid));
            Helper.ColoredConsoleWriteLine("The pulling example by Open API ended iQ, open your iQ environment to view the data in iQ", ConsoleColor.White);

            Console.ReadLine();
        }

        private static FlurlService _eService;
        private static readonly object LockObj = new object();
        private static string _elasticsearch;
        private static string _port;
        private static FlurlService EService
        {
            get
            {
                lock (LockObj)
                {
                    return _eService ?? (_eService = new FlurlService($"http://{_elasticsearch}:{_port}"));
                }
            }
        }

        private static string _logName;


        private static IEnumerable<EventLogEntry> _eventLogEntries;
        private static IEnumerable<EventLogEntry> _lastEvents;
        private static string _sourceGuid;


        private static void InitializeDemo(string elasticsearch, string port, string sourceGuid)
        {
            _elasticsearch = elasticsearch;
            _port = port;
            _sourceGuid = sourceGuid;
            int evntNum = 500;

            //Verify the elastisearch connectivity
            VerifyElastic();
            Helper.ColoredConsoleWriteLine("Elastic server verified", ConsoleColor.Yellow);

            //for sample we will pull event entry from System logs.
            _logName = "System";
            EventLog eventLogEntries = new EventLog(_logName);
            _eventLogEntries = eventLogEntries.Entries.Cast<EventLogEntry>();


            //latest n events.
            _lastEvents = _eventLogEntries.OrderByDescending(x => x.TimeGenerated).Take(evntNum);


            var periodicTask = PeriodicTaskFactory.Start(() =>
                {
                    GetComponents();
                    Helper.ColoredConsoleWriteLine("Components pushed into iQ", ConsoleColor.Yellow);
                }, intervalInMilliseconds: 2000, // fire every two seconds...
                maxIterations: 10).ContinueWith(_ =>
            {

                GetComponentsRelationship();
                Helper.ColoredConsoleWriteLine("Components Relationships pushed into iQ", ConsoleColor.Yellow);
            }).ContinueWith(_ =>
            {

                GetComponentsState();
                Helper.ColoredConsoleWriteLine("Components health states pushed into iQ", ConsoleColor.Yellow);
            }).ContinueWith(_ =>
            {

                GetAlerts();
                Helper.ColoredConsoleWriteLine("Alerts pushed into iQ", ConsoleColor.Yellow);
            });




            periodicTask.ContinueWith(_ => { Helper.ColoredConsoleWriteLine("Finished to push!", ConsoleColor.White); }).ContinueWith(_ =>
            {
                Helper.ColoredConsoleWriteLine("Close incident process!", ConsoleColor.White);
                CloseIncidents();
            }).ContinueWith(_ =>
            {
                Helper.ColoredConsoleWriteLine("Finish!", ConsoleColor.White);
            }).Wait();
        }
                
        private static void GetAlerts()
        {

            foreach (var ev in _lastEvents)
            {
                var evObj = new
                {
                    Category = ev.Category,
                    EntryType = ev.EntryType,
                    InstanceId = ev.InstanceId,
                    MachineName = ev.MachineName,
                    Message = ev.Message,
                    EventSource = ev.Source,
                    TimeGenerated = ev.TimeGenerated,
                    UserName = ev.UserName
                };

                var severity = GetSeverity(ev.EntryType);

                StoreAlert(new Alert
                {
                    componentKey = new List<string>(2)
                    {
                        $"{_sourceGuid}|{ev.MachineName}|{ev.Source}",
                        $"{_sourceGuid}|{ev.MachineName}"
                    },
                    linkedComponents = new HashSet<string>(),
                    assignee = "",
                    url = "",

                    message = ev.Message,
                    created = ev.TimeGenerated.ToUniversalTime(),
                    key = $"{_sourceGuid}|{ev.InstanceId.ToString()}|{ev.Index}",
                    isActive = (DateTime.UtcNow - ev.TimeGenerated.ToUniversalTime()).Days <= 4,
                    name = $"{ev.Source}",
                    source = new Dictionary<string, object>
                    {
                        {
                            "virtualConnector", JObject.FromObject(evObj)
                        }
                    },
                    severityEnum = severity,
                    severityIndex = (int)severity,
                    target = $"{ev.MachineName}|{ev.Source}",
                    lastUpdated = DateTime.UtcNow,
                    resolutionState = (DateTime.UtcNow - ev.TimeGenerated.ToUniversalTime()).Days >= 4 ? "Closed" : "Open", // for the example the events will stay open, but if you know the key alert you could update the resolutionState every operation cycle.
                    sourceId = Guid.Parse(_sourceGuid),
                    sourceName = "OpenAPIDemo",
                    sourceType = "VirtualConnector"
                });
            }
        }

        private static void GetComponentsState()
        {

            var logSources = Helper.GetSourceNamesFromLog(_logName);

            var worstState = HealthState.Healthy;
            foreach (var logSource in logSources.Select(x => x.Replace(' ', '_').Replace('/', '_')))
            {
                var key = $"{_sourceGuid}|{Environment.MachineName}|{logSource}";
                var comp = new esentityState
                {
                    joinKey = new { name = "esentity", parent = key },
                    componentKey = key,
                    sourceId = Guid.Parse(_sourceGuid),
                    sourceName = "OpenAPIDemo",
                    sourceType = "VirtualConnector",
                    StateEnum = HealthState.Healthy
                };
                comp.state = comp.StateEnum.GetDescription();
                comp.timestamp = DateTime.UtcNow;

                if (_lastEvents.Any(x => x.Source == logSource))
                {
                    var ev = _lastEvents.FirstOrDefault(x => x.Source == logSource);

                    comp.StateEnum = (DateTime.UtcNow - ev.TimeGenerated.ToUniversalTime()).Days > 1 ? HealthState.Healthy : GetHealthState(ev.EntryType);
                    comp.timestamp = ev.TimeGenerated.ToUniversalTime();
                    if ((int)worstState < (int)comp.StateEnum)
                    {
                        worstState = comp.StateEnum;
                    }
                }


                StoreComponentState(comp);
            }

            var server = new esentityState
            {
                joinKey = new
                {
                    name = "esentity",
                    parent = $"{_sourceGuid}|{Environment.MachineName}"
                },
                componentKey = $"{_sourceGuid}|{Environment.MachineName}",
                sourceId = Guid.Parse(_sourceGuid),
                sourceName = "OpenAPIDemo",
                sourceType = "VirtualConnector",                
                StateEnum = worstState,
                state = worstState.GetDescription(),
                timestamp = DateTime.UtcNow,
            };
            StoreComponentState(server);

        }

        private static void GetComponentsRelationship()
        {
            var logSources = Helper.GetSourceNamesFromLog(_logName);
            foreach (var logSource in logSources.Select(x => x.Replace(' ', '_').Replace('/', '_')))
            {
                StoreComponentRelationship(new ComponentRelationship
                {
                    key = $"{_sourceGuid}|{Environment.MachineName}_{_sourceGuid}|{Environment.MachineName}|{logSource}",
                    typeEnum = ComponentRelationshipType.Hosting,
                    sourceComponent = $"{_sourceGuid}|{Environment.MachineName}",
                    destinationComponent = $"{_sourceGuid}|{Environment.MachineName}|{logSource}",
                    name = $"{Environment.MachineName}|{logSource}_relationship",
                    sourceId = Guid.Parse(_sourceGuid),
                    sourceName = "OpenAPIDemo",
                    sourceType = "VirtualConnector",
                    source = new Dictionary<string, object>
                    {
                        {
                            "virtualConnector", new
                            {
                                Parent = JObject.FromObject(new {Machine=$"{_sourceGuid}|{Environment.MachineName}"}),
                                Child = JObject.FromObject(new {MachineComponent=$"{_sourceGuid}|{Environment.MachineName}|{logSource}"})
                            }
                        }
                    }
                });
            }

        }

        public class EventLogComponent
        {
            public string Machine { get; set; }
            public string IPAddress { get; set; }
            public string Log { get; set; }
        }
        private static void GetComponents()
        {
            
            var localFqdn = Helper.GetFqdn();
            var ip = Helper.GetLocalIpAddress();
            Dictionary<string, object> source1 = new Dictionary<string, object>
                {
                    { "virtualConnector", JObject.FromObject(new EventLogComponent()
                        {
                            Machine = Environment.MachineName,
                            IPAddress = ip
                        }) }
                };
            //You must provide a valid object of a specific type to correctly create and update a document in Elasticsearch using the iQ Open API
            //You can read more about it in the iQ Open API documentation.
            StoreComponent(new esentity
            {
                key = $"{_sourceGuid}|{Environment.MachineName}",
                fqdn = localFqdn,
                host = Environment.MachineName,
                typeEnum = ComponentType.Computer,
                iPAddress = ip,
                name = Environment.MachineName,
                sourceId = Guid.Parse(_sourceGuid),
                sourceName = "OpenAPIDemo",
                sourceType = "VirtualConnector",
                source = source1
            });

            var logSources = Helper.GetSourceNamesFromLog(_logName);
            foreach (var logSource in logSources.Select(x => x.Replace(' ', '_').Replace('/', '_')))
            {
                Dictionary<string, object> source2 = new Dictionary<string, object>
                {
                    { "virtualConnector", JObject.FromObject(new EventLogComponent()
                        {
                            Machine = Environment.MachineName,
                            Log = logSource
                        }) }
                };
                StoreComponent(new esentity
                {
                    key = $"{_sourceGuid}|{Environment.MachineName}|{logSource}",
                    fqdn = localFqdn,
                    host = Environment.MachineName,
                    typeEnum = ComponentType.Object,
                    iPAddress = ip,
                    name = logSource,
                    sourceId = Guid.Parse(_sourceGuid),
                    sourceName = "OpenAPIDemo",
                    sourceType = "VirtualConnector",
                    source = source2
                });
            }

        }



        private static void CloseIncidents()
        {
            //The logic strategy for close an incident, is based on zero open alerts assigned to incident.
            var isActive = true;
            foreach (var incident in GetAllActiveIncidents())
            {
                foreach (var alertKey in incident.Alerts)
                {
                    if (!IsAlertOpen(alertKey))
                    {
                        isActive = false;
                    }
                    else
                    {
                        isActive = true;
                        break;
                        
                    }
                }

                if (!isActive)
                    CloseIncident(incident);

            }
        }

        private static void CloseIncident(Source incident)
        {
            var body = incident;
            body.IsActive = false;
            body.State = "Closed";


            EService.SetEndPoint($"savisioniq_incidents_{_sourceGuid}/incident/{incident.Key}");
            EService.PutJsonAsync<Response>(new { routing = $"{incident.Key}" }, body).GetAwaiter().GetResult();
        }

        private static bool IsAlertOpen(string alertKey)
        {
            var alertSourceGuid = alertKey.Split('|')[0];
            var body = new
            {
                query = new
                {
                    match = new
                    {
                        key = alertKey
                    }
                }
            };

            EService.SetEndPoint($"savisioniq_alerts_{alertSourceGuid}/_search");
            var res = EService.PostJsonAsync<JObject>(null, body).GetAwaiter().GetResult();
            return res.SelectToken("hits.hits[0]._source.isActive").ToObject<bool>();
      
        }



        private static IEnumerable<Source> GetAllActiveIncidents()
        {

            var body = new
            {
                query = new
                {
                    @bool = new
                    {
                        filter = new[]
                        {
                                new {
                                    term = new
                                    {
                                        isActive = true
                                    }
                                }
                            }
                    }
                }
            };
            EService.SetEndPoint($"savisioniq_incidents_{_sourceGuid}/_search");
            var res = EService.PostJsonAsync<SearchRespose>(null, body).GetAwaiter().GetResult();
            return res.Hits.HitsHits.Select(x => x.Source).Where(x => x.IsActive);
        }

        private static void VerifyElastic()
        {

            try
            {
                EService?.GetJsonAsync<ElasticsearchObject>(null);
            }
            catch (Exception e)
            {
                Helper.ColoredConsoleWriteLine("Make sure the elasticsearch server correct and if need the port open", ConsoleColor.Red);
                Helper.ColoredConsoleWriteLine(e.Message, ConsoleColor.Red);
                Logger.Error(e);
            }
        }


        private static void StoreAlert(Alert alert)
        {
            EService.SetEndPoint($"savisioniq_alerts_{alert.sourceId}/alert/{alert.key}");
            EService.PostJsonAsync<Response>(null, alert).GetAwaiter().GetResult();
        }
        private static void StoreComponent(esentity component)
        {
            EService.SetEndPoint($"savisioniq_components_{component.sourceId}/esentity/{component.key}");
            EService.PostJsonAsync<Response>(new { routing = component.key }, component).GetAwaiter().GetResult();
        }
        private static void StoreComponentRelationship(ComponentRelationship componentRelationship)
        {
            EService.SetEndPoint($"savisioniq_component_relationships_{componentRelationship.sourceId}/componentrelationship/{componentRelationship.key}");
            EService.PutJsonAsync<Response>(null, componentRelationship).GetAwaiter().GetResult();
        }
        private static void StoreComponentState(esentityState component)
        {
            //For verify the current state of the component, we store the data twice; once without an elasticsearch id (https://www.elastic.co/guide/en/elasticsearch/reference/6.8/docs-index_.html#_automatic_id_generation)  
            //Second time we store the data with lastSyncTime field and elasticsearch id, that way iQ could know the latest state and continue to store historical data.
            EService.SetEndPoint($"savisioniq_components_{component.sourceId}/esentity");

            try
            {
                EService.PostJsonAsync<Response>(new { routing = component.componentKey }, component).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Helper.ColoredConsoleWriteLine(ex.Message, ConsoleColor.Red);
                return;
            } 

            component.lastSyncTime = DateTime.UtcNow;

            EService.SetEndPoint($"savisioniq_components_{component.sourceId}/esentity/{component.componentKey}|STATE");
            EService.PutJsonAsync<Response>(new { routing = $"{component.componentKey}" }, component).GetAwaiter().GetResult();
        }





        private static AlertSeverity GetSeverity(EventLogEntryType evLevel)
        {
            switch (evLevel)
            {

                case EventLogEntryType.FailureAudit:
                case EventLogEntryType.Error:
                    return AlertSeverity.Error;
                case EventLogEntryType.Warning:
                    return AlertSeverity.Warning;
                case EventLogEntryType.Information:
                case EventLogEntryType.SuccessAudit:
                    return AlertSeverity.Information;
                default:
                    return AlertSeverity.Information;
            }
        }

        private static HealthState GetHealthState(EventLogEntryType evLevel)
        {
            switch (evLevel)
            {

                case EventLogEntryType.FailureAudit:
                case EventLogEntryType.Error:
                    return HealthState.Critical;
                case EventLogEntryType.Warning:
                    return HealthState.Warning;
                case EventLogEntryType.Information:
                case EventLogEntryType.SuccessAudit:
                    return HealthState.Healthy;
                default:
                    return HealthState.Unknown;
            }
        }


    }

}
