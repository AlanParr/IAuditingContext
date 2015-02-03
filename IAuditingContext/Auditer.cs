using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using IAuditingContext.AuditStores;
using Newtonsoft.Json;

namespace IAuditingContext
{
    public class Auditer : IDisposable
    {
        private readonly TextAuditStore _auditStore;

        public Auditer(TextAuditStore auditStore, string username)
        {
            _auditStore = auditStore;
            UserName = username;
            _auditEntries = new List<AuditEntry>();
        }

        private IEnumerable<DbEntityEntry> _addedObjects;
        private DbContext _context;

        private readonly List<AuditEntry> _auditEntries;

        public string UserName { get; set; }

        public void AuditChanges(object sender, EventArgs e)
        {
            _context = sender as DbContext;

            if (_context == null) { throw new InvalidCastException("Sender cannot be cast to DbContext"); }

            _addedObjects = _context.ChangeTracker.Entries().Where(x => x.State == EntityState.Added);
            WriteAudit(_context.ChangeTracker.Entries().Where(x => x.State == EntityState.Modified));
            WriteAudit(_context.ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted));
        }

        private void WriteAudit(IEnumerable<DbEntityEntry> entries)
        {
            if (entries == null || !entries.Any())
            {
                return;
            }
            var auditableEntries = entries.Where(ose => ose.Entity is IAuditable);

            foreach (var entry in auditableEntries)
            {
                WriteEntryAudit(entry, UserName ?? "UNKNOWN");
            }
        }

        private void WriteEntryAudit(DbEntityEntry entry, string userName)
        {
            var typeName = ObjectContext.GetObjectType(entry.Entity.GetType()).Name;

            var ae = new AuditEntry
            {
                AuditCategory = entry.State.ToString(),
                TypeName = typeName,
                AuditDateTime = DateTime.Now.ToUniversalTime(),
                AuditString = "",
                UserName = userName
            };

            var childAuditableObjects = GetAuditableChildObjects(entry.Entity);
            var auditStrings = childAuditableObjects.Select(nestedObject => nestedObject.AuditString).ToList();
            
            foreach (var cp in from propertyName in entry.CurrentValues.PropertyNames 
                               let originalValue = entry.OriginalValues[propertyName] 
                               let newValue = entry.CurrentValues[propertyName] 
                               select new ChangedProperty
            {
                Name = propertyName,
                NewValue = ObjectToString(newValue),
                OldValue = ObjectToString(originalValue)
            } into cp where cp.NewValue != cp.OldValue select cp)
            {
                ae.ChangedProperties.Add(cp);
            }

            var objectStateEntry = ((IObjectContextAdapter)_context).ObjectContext.ObjectStateManager.GetObjectStateEntry(entry.Entity);

            foreach (var keyInfo in objectStateEntry.EntityKey.EntityKeyValues)
            {
                ae.Keys.Add(keyInfo.Key,keyInfo.Value);
            }

            ae.AuditString = JsonConvert.SerializeObject(auditStrings);

            _auditEntries.Add(ae);
        }

        private IEnumerable<IAuditable> GetAuditableChildObjects(object entity)
        {
            var t = entity.GetType();

            return t.GetProperties()
                    .Where(p => p.PropertyType.GetInterfaces().Any(interfaceType => interfaceType == typeof(IAuditable)))
                    .Select(pi => (IAuditable)pi.GetGetMethod().Invoke(entity, new object[] { }))
                    .Where(o => o != null);
        }

        private static string ObjectToString(object value)
        {
            const string defaultIfNull = "NULL";
            if (value == null) return defaultIfNull;
            return value.ToString();
        }

        /// <summary>
        /// On disponse, commit the audit
        /// </summary>
        public void Dispose()
        {
            WriteAudit(_addedObjects);

            if (!_cancelling)
            {
                _auditEntries.ForEach(x=>_auditStore.Set(x));
            }
        }


        /// <summary>
        ///  Flag that the commit in Dispose shouldn't go ahead
        /// </summary>
        private bool _cancelling;
        internal void CancelAudit()
        {
            _cancelling = true;
        }
    }
}
