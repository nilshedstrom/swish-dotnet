using Swish;

namespace SwishTestWebAppCore
{
    public class SwishSettings
    {
        /// <summary>
        /// The url to the deploy of SwishTestWebAppCore. Must be available from the internet and be https://
        /// </summary>
        public string CallbackBaseUrl { get; set; }

        /// <summary>
        /// If getting the certificate from a file this property must contain the path to the file
        /// </summary>
        public string CertificateFile { get; set; }

        /// <summary>
        /// The certificate password. Only needed if the certificate is loaded from a file.
        /// </summary>
        public string CertificatePassword { get; set; }

        /// <summary>
        /// The merchant id (Swish number)
        /// </summary>
        public string MerchantId { get; set; }

        /// <summary>
        /// The environment (test or production)
        /// </summary>
        public SwishEnvironment Environment { get; set; }

        /// <summary>
        /// If the certificate is loaded from a KeyVault then this property must contain the name of the certificate in the KeyVault
        /// </summary>
        public string CertificateName { get; set; }
    }
}