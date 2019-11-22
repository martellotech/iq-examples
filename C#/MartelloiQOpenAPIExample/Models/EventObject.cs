using System;
using System.Diagnostics.Eventing.Reader;



namespace iQOpenApiExample.Models
{

    public class EventObject
    {
      
        public string Message { get; set; }
   
        public DateTime Created { get; set; }
       
        public bool IsActive  { get; set; }
    
        public string Name { get; set; }
      
        public string Target { get; set; }
     
        public int Severity { get; set; }
        
        public string SeverityDisplayName { get; set; }

        public string EventId { get; set; }
  
    }
}
