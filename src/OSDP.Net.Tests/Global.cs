
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace OSDP.Net.Tests
{
    [SetUpFixture]
    class GlobalSetup
    {
        public static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();

        [OneTimeSetUp]
        public void BeforeAnyTests()
        {
            LoggerFactory.AddLog4Net();

            // Is next line needed? How does log4net know to write log out to console?? Yet it seems to
            //BasicConfigurator.Configure();
        }

        private static readonly LoggerFactory LoggerFactory = new();
    }
}