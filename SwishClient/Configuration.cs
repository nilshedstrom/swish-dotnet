using System;
using System.Security.Cryptography.X509Certificates;

namespace SwishClient
{
    public interface IConfiguration
    {
        Uri BaseUri();

        X509Certificate2 GetCACertificate();
    }

    public class TestConfig : IConfiguration
    {
        private readonly X509Certificate2 _caCert;
        public X509Certificate2 GetCACertificate() => _caCert;

        public Uri BaseUri() => new Uri("https://mss.cpc.getswish.net");

        /// <summary>
        /// Creates new test config to test swish integration with Merchant Simulator
        /// Docs: https://developer.getswish.se/merchants-test-con/1-introduction/
        /// </summary>
        /// <param name="caCert">Optional CA root certificate used to verify server certificate, if not provided, no server certificate validation will be done</param>
        public TestConfig(X509Certificate2 caCert)
        {
            _caCert = caCert;
        }
    }
}
