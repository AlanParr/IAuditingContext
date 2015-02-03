using System;
using System.Data.Entity;
using IAuditingContext.AuditStores;

namespace IAuditingContext
{
    public class AuditingContext : DbContext
    {
        public override int SaveChanges()
        {
            using (var auditer = new Auditer(new TextAuditStore(@"C:\auditing\"), "NOT SUPPLIED"))
            {
                var returnValue = -1;

                try
                {
                    auditer.AuditChanges(this, new EventArgs());
                    returnValue = base.SaveChanges();
                }
                catch (Exception ex)
                {
                    auditer.CancelAudit();
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }

                return returnValue;

            }
        }
    }
}
