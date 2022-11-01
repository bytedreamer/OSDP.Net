
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace OSDP.Net.Tests
{
    [SetUpFixture]
    class GlobalSetup
    {
        public static ILogger<T> CreateLogger<T>() => _loggerFactory.CreateLogger<T>();

        [OneTimeSetUp]
        public void BeforeAnyTests()
        {
            _loggerFactory.AddLog4Net();

            // Is next line needed? How does log4net know to write log out to console?? Yet it seems to
            //BasicConfigurator.Configure();
        }

        private static LoggerFactory _loggerFactory = new();
    }
}