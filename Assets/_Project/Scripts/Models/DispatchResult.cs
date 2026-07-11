namespace NeighborhoodManager.Models
{
    public readonly struct DispatchResult
    {
        public bool Success { get; }
        public string Message { get; }

        public DispatchResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static DispatchResult Failed(string message) => new DispatchResult(false, message);
        public static DispatchResult Succeeded(string message) => new DispatchResult(true, message);
    }
}
