using System;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using NzbDrone.Common.Options;
using NzbDrone.Host;
using NzbDrone.Test.Common;

namespace NzbDrone.App.Test
{
    [TestFixture]
    public class BootstrapConfigurationFixture : TestBase
    {
        [TearDown]
        public void TearDown()
        {
            Environment.SetEnvironmentVariable("METAJAVARR__AUTH__METHOD", null);
            Environment.SetEnvironmentVariable("RADARR__AUTH__METHOD", null);
        }

        [Test]
        public void should_bind_auth_options_from_metajavarr_environment_section()
        {
            Environment.SetEnvironmentVariable("METAJAVARR__AUTH__METHOD", "External");

            GetAuthOptions()
                .Method
                .Should()
                .Be("External");
        }

        [Test]
        public void should_not_bind_auth_options_from_radarr_environment_section()
        {
            Environment.SetEnvironmentVariable("RADARR__AUTH__METHOD", "External");

            GetAuthOptions()
                .Method
                .Should()
                .BeNull();
        }

        private static AuthOptions GetAuthOptions()
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();
            ConfigureBootstrapOptions(services, config);

            return services.BuildServiceProvider()
                .GetRequiredService<IOptions<AuthOptions>>()
                .Value;
        }

        private static void ConfigureBootstrapOptions(IServiceCollection services, IConfiguration config)
        {
            var method = typeof(Bootstrap).GetMethod("ConfigureOptions", BindingFlags.NonPublic | BindingFlags.Static);
            method.Should().NotBeNull();

            method!.Invoke(null, new object[] { services, config });
        }
    }
}
