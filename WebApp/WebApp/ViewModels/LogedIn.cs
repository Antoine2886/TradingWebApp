namespace WebApp.ViewModels
{
    public class LogedIn
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}