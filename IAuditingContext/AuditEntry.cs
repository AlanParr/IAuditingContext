using System;
using System.Collections.Generic;

namespace IAuditingContext
{
    public class AuditEntry
    {
        public AuditEntry()
        {
            Keys = new Dictionary<string, object>();
            ChangedProperties = new List<ChangedProperty>();
        }

        public Guid Id { get; set; }
        public string AuditCategory { get; set; }
        public string TypeName { get; set; }
        public string AuditString { get; set; }
        public string UserName { get; set; }
        public DateTime AuditDateTime { get; set; }
        public Dictionary<string, object> Keys { get; set; }
        public List<ChangedProperty> ChangedProperties { get; set; } 
    }
}
