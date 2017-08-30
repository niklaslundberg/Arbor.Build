namespace Arbor.X.Tests.DummyWebApplication.Controllers
{
    public class HomeViewModel
    {
        public HomeViewModel(string utcNow)
        {
            UtcNow = utcNow;
        }

        public string UtcNow { get; }
    }
}
