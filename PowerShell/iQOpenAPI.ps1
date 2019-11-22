
#Model section
Enum AlertSeverity
{
    Information = 1
    Warning = 2
    Error = 3
}

enum ComponentType
{
        Object = 1
        Group = 2
        Service = 3
        Computer = 4
        Database = 5
        Website = 6
        VirtualMachine = 7 
}
enum HealthState
{
    Unknown = 0
    Unreachable = 1
    NotMonitored = 2
    InMaintenanceMode = 3
    Healthy = 4
    Warning = 5
    Critical = 6
}

class Alert {
    [string]$key
    [string]$name 
    [System.Collections.ArrayList]$componentKey 
    [string[]]$linkedComponents 
    [AlertSeverity] $severityIndex 
    [string]$severity;
    [string]$target 
    [string]$message 
    [bool]$isActive 
    [bool]$isAcknowledged 
    [string]$resolutionState 
    [string]$created 
    [string]$lastUpdated 
    [string]$assignee 
    [string]$url 
    [Object]$source 
    [string]$sourceName 
    [string]$sourceType 
    [guid]$sourceId 
}

class Component {
    [string]$key
    [string]$name 
    [ComponentType]$typeEnum
    [string]$type
    [string]$host
    [string]$path
    [string]$joinKey = "parent"
    [string]$iPAddress
    [string]$fqdn
    [Object]$source
    [string]$sourceName 
    [string]$sourceType 
    [guid]$sourceId 
}
class JoinKey {
    [string]$name
    [string]$parent
}
class ComponentState {
    [JoinKey]$joinKey
    [string]$componentKey
    [string]$state
    [HealthState]$stateIndex
    [string]$timestamp 
    [string]$lastSyncTime
    [bool]$isCurrent = $false 
    [Object]$source
    [string]$sourceName 
    [string]$sourceType 
    [guid]$sourceId 
}



class PingdomUtil {
    
    static [DateTime]ConvertFromUnixTimestamp($TimeStamp){
     $Origin = New-Object -Type DateTime -ArgumentList 1970, 1, 1, 0, 0, 0, 0
 
     return $Origin.AddSeconds($TimeStamp).ToLocalTime()
    }
 
    static [double]ConvertToUnixTimestamp([DateTime]$TimeStamp){ 
     $Origin = New-Object -Type DateTime -ArgumentList 1970, 1, 1, 0, 0, 0, 0
     $span = $TimeStamp.ToUniversalTime() - $Origin
     return $span.TotalSeconds
    }
 }
 
 enum CallMethod {
     GET
     PUT
     POST 
 }
 
 class RequestObjct {
      [string]$APIurl
      [CallMethod]$Method
      # Only include actions generated later than this timestamp.
      [DateTime]$From
      # Only include actions generated prior to this timestamp.
      [DateTime]$To
      # Limits the number of returned results to the specified quantity.
      [int]$Limit
      # Offset for listing
      [int]$Offset
      # Comma-separated list of check identifiers. Limit results to actions generated from these checks.
      [string[]]$CheckIds
      # Comma-separated list of contact identifiers. Limit results to actions sent to these contacts.
      [string[]]$ContactIds
      # Comma-separated list of statuses. Limit results to actions with these statuses. "sent", "delivered", "error", "not_delivered", "no_credits"
      [string[]]$Status
      # Comma-separated list of via mediums. Limit results to actions with these mediums. "email", "sms", "twitter", "iphone", "android"
      [string[]]$Via
 
     [string]GetFullURL(){
         $this.APIurl += "?" 
     
     if ($this.From -ne [DateTime]::MinValue) 
     {
         $this.APIurl += "from={0}" -f ([PingdomUtil]::ConvertFromUnixTimestamp($this.From))
     }
     if ($this.To -ne [DateTime]::MinValue)
     {
         $this.APIurl += "to={0}" -f ([PingdomUtil]::ConvertToUnixTimestamp($this.To))
     }
 
     if ($this.Limit)
     {
         $this.APIurl += "limit={0}" -f $this.Limit
     }
 
     if ($this.Offset)
     {
         $this.APIurl += "offset={0}" -f $this.Offset
     }
 
     if ($this.CheckIds)
     {
         $this.APIurl += "checkids={0}" -f ($this.CheckIds -join ',')
     }
 
     if ($this.ContactIds)
     {
         $this.APIurl += "contactids={0}" -f ($this.ContactIds -join ',')
     }
 
     return $this.APIurl 
     }
 }
 #====================================== Light Pingdom Client =======================================
 class PingdomClient{
     
 [string]$Current_api_version="3.1"
 [string]$APIUrl="https://api.pingdom.com/api/"
 [string]$APIKey
 
 PingdomClient([string]$APIversion, [string]$APIKey) {
 
      $this.Current_api_version=$APIversion
      $this.APIUrl += $this.Current_api_version
      $this.APIKey=$APIKey  
 }
 
 <#
 .Synopsis
    Returns a list overview of all checks or a detailed description of a specified check.
 #>
 [string]GetPingdomCheck(){
     $req=[RequestObjct]::new()
     $req.Method=[CallMethod]::GET
     $req.APIurl=$this.APIUrl +"/checks"     
     return $this.SendRequest($req)
 }
 
 <#
 .Synopsis
    Returns a list of actions (alerts) that have been generated for your account.
 #>
 [string]GetPingdomActions(){
     $req=[RequestObjct]::new()
     $req.Method=[CallMethod]::GET
     $req.APIurl=$this.APIUrl +"/actions"     
     return $this.SendRequest($req)
 }
 
 [string]SendRequest([RequestObjct]$requestObject){
     $resObj=  Invoke-RestMethod -Uri $requestObject.GetFullURL() -Method $requestObject.Method -Headers @{"Authorization"="Bearer " + $this.APIKey ; "Accept-Encoding"="gzip"}
     return ConvertTo-Json -InputObject $resObj -Compress
 }
 
 }
 
#=========================================== IQ OPEN API IMPLEMENTATION ============

$URL
function  Send-ToOpenAPIIQ  {
    param(
        [Alert]$Alert,
        [Component]$Component,
        [ComponentState]$ComponentState,

        [Parameter(Mandatory=$true)]
        [string]$ElasticSearchUrl,
        [bool]$CurrentState=$false
    )
    $URL=$ElasticSearchUrl
    
    if ($PSBoundParameters.ContainsKey('Alert')) {
        $json = ConvertTo-Json -InputObject $Alert
        $URL=$URL+"/savisioniq_alerts_$($alert.sourceId)/alert/$($alert.key)"
    }

    if ($PSBoundParameters.ContainsKey('Component')) {
        $json = ConvertTo-Json -InputObject $Component
        $URL=$URL+"/savisioniq_components_$($Component.sourceId)/esentity/$($Component.key)?routing=$($Component.key)"
    }

    if ($PSBoundParameters.ContainsKey('ComponentState')) {
        $json = ConvertTo-Json -InputObject $ComponentState

        if($CurrentState -ne $true){
            $URL=$URL+"/savisioniq_components_$($ComponentState.sourceId)/esentity?routing=$($ComponentState.componentKey)"
        }
        else{
            $URL=$URL+"/savisioniq_components_$($ComponentState.sourceId)/esentity/$($ComponentState.componentKey)|STATE?routing=$($ComponentState.componentKey)"
        }
       
    }

    Write-Debug -Message $URL
    $response = Invoke-RestMethod  $URL -Body $json -Method Post -ContentType 'application/json'
    Write-Debug -Message $response
}

function Get-Alerts {
    param(
      $IQSourceGuid,
      $PingdomVersionn,
      $PingdomKey
    )
  $json = [PingdomClient]::new($pingdomVersion,$pingdomKey).GetPingdomActions()
  $ListAlerts = [System.Collections.Generic.List[Alert]]::new()

    if ($null -eq $json) {
        return $ListAlerts;
    }
  $result = ConvertFrom-Json -InputObject $json
 
  
  foreach ($alert in $result.actions.alerts) {
        #in the case and the returned value is a string, we need to convert it to an object.
     if($alert.GetType().Name -eq "String"){
        $string = $alert.Substring(1,$alert.Length-1)
        $string = $string.replace("`n","").replace("`r","")
        $string = $string.Replace("=",'":"').Replace("; ",'","').Replace("{",'{"').Replace("}",'"}')
        $temp   = ConvertTo-Json -InputObject $string
        $alertObject = ConvertFrom-Json $temp
        $alert = ConvertFrom-Json $alertObject 
       }

      $iqAlert = New-Object -TypeName "Alert"
      $iqAlert.name= $alert.messageshort
      $iqAlert.message= $alert.messagefull
      $iqAlert.source = [System.Collections.Generic.Dictionary[string,System.Object]]::new()
      $iqAlert.source.Add("Pingdom",$alert)
      $iqAlert.sourceId = [guid]::Parse($iQSourceGuid)
      $iqAlert.componentKey = New-Object System.Collections.ArrayList
      $iqAlert.componentKey.Add($iQSourceGuid + "|" + $alert.checkid)
      $iqAlert.target = "Pingdom Alert"
      $iqAlert.sourceName = "Pingdom"
      $iqAlert.sourceType = "VirtualConnector"
      $iqAlert.resolutionState=$alert.status
      $iqAlert.severityIndex= [AlertSeverity]::Error
      $iqAlert.severity= [AlertSeverity]::Error.ToString()
      $iqAlert.key = $iQSourceGuid + "|" + $alert.time + "|" + $alert.checkid
      $iqAlert.isActive=$true
      $iqAlert.lastUpdated=[datetime]::Now.ToUniversalTime().ToString("o")
      $iqAlert.created = (([System.DateTimeOffset]::FromUnixTimeSeconds($alert.time)).DateTime).ToString("o")

      $ListAlerts.Add($iqAlert)
  }
  
 return $ListAlerts
}

function Get-Components {
    param(
      $IQSourceGuid,
      $PingdomVersionn,
      $PingdomKey
    )
    $json = [PingdomClient]::new($pingdomVersion,$pingdomKey).GetPingdomCheck()
    $ListComponents= [System.Collections.Generic.List[Component]]::new()

    if ($null -eq $json) {
        return $ListComponents;
    }
   $result = ConvertFrom-Json -InputObject $json

   foreach ($check in $result.checks) {
         #in the case and the returned value is a string, we need to convert it to an object.
       if($check.GetType().Name -eq "String"){
        $string = $check.Substring(1,$check.Length-1)
        $string = $string.replace("`n","").replace("`r","")
        $string = $string.Replace("=",'":"').Replace("; ",'","').Replace("{",'{"').Replace("}",'"}')
        $temp   = ConvertTo-Json -InputObject $string
        $checkObject = ConvertFrom-Json $temp
        $check = ConvertFrom-Json $checkObject 
       }
    

        $iqComponent = New-Object -TypeName "Component"
        $iqComponent.key = $iQSourceGuid + "|" + $check.id
        $iqComponent.name = $check.name
        $iqComponent.typeEnum=[ComponentType]::Object
        $iqComponent.type=[ComponentType]::Object
        $iqComponent.source = [System.Collections.Generic.Dictionary[string,System.Object]]::new()
        $iqComponent.source.Add("Pingdom",$check)
        $iqComponent.host = $check.hostname
        $iqComponent.sourceName = "Pingdom"
        $iqComponent.sourceType = "VirtualConnector"
        $iqComponent.sourceId = [guid]::Parse($iQSourceGuid)

        $ListComponents.Add($iqComponent)
   }
   return $ListComponents

}


function Get-ComponentsHealthState {
    param(
      $IQSourceGuid,
      $PingdomVersionn,
      $PingdomKey
    )
  
     $json = [PingdomClient]::new($pingdomVersion,$pingdomKey).GetPingdomCheck()
     $ListComponentState= [System.Collections.Generic.List[ComponentState]]::new()

  if ($null -eq $json) {
      return $ListComponentState;
  }
 $result = ConvertFrom-Json -InputObject $json

 foreach ($checkHealth in $result.checks) {
     #in the case and the returned value is a string, we need to convert it to an object.
     if($checkHealth.GetType().Name -eq "String"){
      $string = $checkHealth.Substring(1,$checkHealth.Length-1)
      $string = $string.replace("`n","").replace("`r","")
      $string = $string.Replace("=",'":"').Replace("; ",'","').Replace("{",'{"').Replace("}",'"}')
      $temp   = ConvertTo-Json -InputObject $string
      $checkHealthObject = ConvertFrom-Json $temp
      $checkHealth = ConvertFrom-Json $checkHealthObject 
     }
      $iqComponentHealth = New-Object -TypeName "ComponentState"
      $iqComponentHealth.componentKey = $iQSourceGuid + "|" + $checkHealth.id
      $iqComponentHealth.joinKey = New-Object -TypeName "JoinKey"
      $iqComponentHealth.joinKey.name = "esentity"
      $iqComponentHealth.joinKey.parent =  $iQSourceGuid + "|" + $checkHealth.id
      $iqComponentHealth.state = (Get-HealthState -State $checkHealth.status).ToString()
      $iqComponentHealth.stateIndex= Get-HealthState -State $checkHealth.status
      $iqComponentHealth.timestamp = (([System.DateTimeOffset]::FromUnixTimeSeconds($checkHealth.lasttesttime)).DateTime).ToString("o")
      $iqComponentHealth.source = [System.Collections.Generic.Dictionary[string,System.Object]]::new()
      $iqComponentHealth.source.Add("Pingdom",$checkHealth)
      $iqComponentHealth.sourceName = "Pingdom"
      $iqComponentHealth.sourceType = "VirtualConnector"
      $iqComponentHealth.sourceId = [guid]::Parse($iQSourceGuid)
      $ListComponentState.Add($iqComponentHealth)
 }
 return $ListComponentState
}


function Get-HealthState{
    param(
        [string]$State
    )
    switch ($State) {
        "up"  {[HealthState]::Healthy; break}
        "down"   {[HealthState]::Critical; break}
        "unconfirmed_down" {[HealthState]::Warning; break}
        "unknown"  {[HealthState]::Unknown; break}
        "paused" {[HealthState]::NotMonitored; break}
     }
    
}
function Start-CollectDataFromPingdom{
    param(
    # Elastichserach Server URL, default http://localhost:9200
    [Parameter(Mandatory=$true,Position=0)]
    [string]$ElasticSearchUrl,
    # OpenAPI IQ Source Guid
    [Parameter(Mandatory=$true)]
    [string]$iQSourceGuid,

    [Parameter(Mandatory=$true)]
    [string]$PingdomVersion,

    [Parameter(Mandatory=$true)]
    [string]$PingdomKey  
)
    #Extract and Transfer processes
    $alerts = Get-Alerts -IQSourceGuid $iQSourceGuid -PingdomVersionn  $pingdomVersion -PingdomKey $pingdomKey 
    $checks = Get-Components -IQSourceGuid $iQSourceGuid -PingdomVersionn  $pingdomVersion -PingdomKey $pingdomKey 
    $checksHealth = Get-ComponentsHealthState -IQSourceGuid $iQSourceGuid -PingdomVersionn  $pingdomVersion -PingdomKey $pingdomKey 
    
    #Load processes 
    foreach($alert in $alerts){
        if($alert.GetType().Name -eq "Alert"){
        Send-ToOpenAPIIQ -Alert $alert -ElasticSearchUrl $ElasticSearchUrl
        }
    }
    foreach($check in $checks){
        Send-ToOpenAPIIQ -Component $check -ElasticSearchUrl $ElasticSearchUrl
     }
     foreach($checkHealth in $checksHealth){
        Send-ToOpenAPIIQ -ComponentState $checkHealth -ElasticSearchUrl $ElasticSearchUrl

        $checkHealth.lastSyncTime = [datetime]::Now.ToUniversalTime().ToString("o")
        $checkHealth.isCurrent = $true

        Send-ToOpenAPIIQ -ComponentState $checkHealth -ElasticSearchUrl $ElasticSearchUrl -CurrentState $true

     }
}




