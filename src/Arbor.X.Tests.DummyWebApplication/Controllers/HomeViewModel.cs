namespace Arbor.X.Tests.DummyWebApplication.Controllers
{
    public class HomeViewModel
    {
        public string UtcNow { get; }

        public HomeViewModel(string utcNow)
        {
            UtcNow = utcNow;
        }
    }
}