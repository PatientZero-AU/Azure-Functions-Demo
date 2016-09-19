using Microsoft.VisualStudio.TestTools.UnitTesting;
using PzXero.Core;

namespace PzXero.Test
{
    [TestClass]
    public class XeroPayrunTests
    {
        [TestMethod]
        public void GetCertificateTest()
        {
            var cert = PatientXero.GetCertificate();
            Assert.IsNotNull(cert);
        }

        [TestMethod]
        public void CreateClient()
        {
            var cert = PatientXero.GetCertificate();
            var client = PatientXero.CreatePayrollClient(cert);

            Assert.IsNotNull(client);
        }
    }
}
