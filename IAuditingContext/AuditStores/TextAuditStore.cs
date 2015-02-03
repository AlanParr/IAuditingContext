using System;
using System.IO;

namespace IAuditingContext.AuditStores
{
    public class TextAuditStore
    {
        private readonly string _storePath;

        public TextAuditStore(string storePath)
        {
            _storePath = storePath;
            Directory.CreateDirectory(_storePath);
        }

        public AuditEntry Get(Guid id)
        {
            var ais = File.ReadAllText(Path.Combine(_storePath, id + ".txt"));
            return Newtonsoft.Json.JsonConvert.DeserializeObject<AuditEntry>(ais);
        }

        public OperationResult Set(AuditEntry entry)
        {
            entry.Id = Guid.NewGuid();
            var ais = Newtonsoft.Json.JsonConvert.SerializeObject(entry);
            var filePath = Path.Combine(_storePath, entry.Id + ".txt");

            File.WriteAllText(filePath,ais);
            return new OperationResult();
        }
    }
}
