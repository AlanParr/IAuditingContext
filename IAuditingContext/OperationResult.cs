namespace IAuditingContext
{
    public class OperationResult
    {
        public OperationResult()
        {
            Success = true;
        }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
