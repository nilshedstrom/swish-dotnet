using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SwishClient.UnitTests
{
    public class SwishCsrGeneratorTests
    {
        [Fact]
        public void GenerateCsrTest()
        {
            var certificateSubjectText = "CN=Magnet, C=NL";

            var generator = new SwishCsrGenerator();

            var keypair = generator.GenerateKeyPair();

            var csr = generator.GenerateCsr(keypair, certificateSubjectText);

            Assert.NotNull(csr.csrAsPem);
            Assert.NotNull(csr.privateKeyAsPem);
        }
    }
}
