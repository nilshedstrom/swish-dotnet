using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SwishClient.IntegrationTests
{
    public class IntegrationProdTests
    {
        private readonly byte[] _merchantCertificateDataInPEM;
        private readonly byte[] _merchantPrivateKey;
        private readonly string _merchantCertificatePassword;
        private readonly string _merchantId;

        public IntegrationProdTests()
        {
            _merchantCertificateDataInPEM = System.IO.File.ReadAllBytes("certificates/prod.pem");
            _merchantPrivateKey = System.IO.File.ReadAllBytes("certificates/private.key");
            _merchantCertificatePassword = "";
            _merchantId = "test";
        }

        [Fact]
        public async Task CertificateAcceptedTest()
        {
            var client = new SwishClient(
                environment: SwishEnvironment.Production,
                PEMCertificate: _merchantCertificateDataInPEM,
                clientPrivateKey: _merchantPrivateKey,
                clientPrivateKeyPassphrase: _merchantCertificatePassword,
                merchantId: _merchantId);

            // Just check the status of random ID, we just need to check that the connection is working
            var paymentStatus = await client.GetPaymentStatus("test");
            
        }

    }
}
