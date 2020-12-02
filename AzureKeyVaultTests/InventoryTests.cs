using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Keyfactor.AnyAgent.AzureKeyVault;
using Keyfactor.Platform.Extensions.Agents;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Rest.Azure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

namespace AzureKeyVaultTests
{
    [TestClass]
    public class InventoryTests
    {
        [TestMethod]
        public void ReturnsTheCorrectJobClassAndStoreType()
        {
            var create = new Create();
            create.GetJobClass().Should().Equals("Inventory");
            create.GetStoreType().Should().Equals("AKV");
        }

        [TestMethod]
        public void JobCallsGetCertificates()
        {
            var inventory = new Mock<Inventory>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();
            mockAzClient.Setup(az => az.GetCertificatesAsync()).ReturnsAsync(new Page<CertificateItem>());
            inventory.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);

            var result = inventory.Object.processJob(Mocks.GetMockConfig(), Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);

            mockAzClient.Verify(az => az.GetCertificatesAsync(), Times.Once());
            result.Status.Should().Equals(2);
            result.Message.Should().Be("Inventory Complete");
        }

        [TestMethod]
        public void JobInvokesCorrectDelegate()
        {
            var inventory = new Mock<Inventory>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();
            mockAzClient.Setup(az => az.GetCertificatesAsync()).ReturnsAsync(new Page<CertificateItem>());
            inventory.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);
            var mockInventoryDelegate = Mocks.GetSubmitInventoryDelegateMock();

            var result = inventory.Object.processJob(Mocks.GetMockConfig(), mockInventoryDelegate.Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);

            mockInventoryDelegate.Verify(m => m(It.IsAny<List<AgentCertStoreInventoryItem>>()));
        }

        [TestMethod]
        public void JobReturnsCorrectCertificates()
        {
            var inventory = new Mock<Inventory>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();

            var c1 = new CertificateItem("https://myvault.vault.azure.net/certificates/TestCert1/pending");
            var c2 = new CertificateItem("https://myvault.vault.azure.net/certificates/TestCert2/pending");
            var c3 = new CertificateItem("https://myvault.vault.azure.net/certificates/TestCert3/pending");

            var c1b = new CertificateBundle(c1.Id);
            c1b.Cer = new byte[] { 0, 1, 0, 1, 0, 1, 1, 0 };           

            var c2b = new CertificateBundle(c2.Id);
            c2b.Cer = new byte[] { 1, 1, 0, 1, 0, 1, 1, 0 };

            var c3b = new CertificateBundle(c3.Id);
            c3b.Cer = new byte[] { 1, 0, 0, 1, 0, 1, 1, 0 };

            var certList = new List<CertificateItem>() { c1, c2, c3 };
            var pageObj = new KFPage<CertificateItem>(certList);
            var json = JsonConvert.SerializeObject(pageObj);

            var paged = JsonConvert.DeserializeObject<KFPage<CertificateItem>>(json);

            mockAzClient.Setup(az => az.GetCertificatesAsync()).ReturnsAsync(() => paged);

            mockAzClient.Setup(az => az.GetCertificateAsync(It.Is<string>(a => a == c1.Id))).ReturnsAsync(() => c1b);
            mockAzClient.Setup(az => az.GetCertificateAsync(It.Is<string>(a => a == c2.Id))).ReturnsAsync(() => c2b);
            mockAzClient.Setup(az => az.GetCertificateAsync(It.Is<string>(a => a == c3.Id))).ReturnsAsync(() => c3b);

            inventory.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);
            var mockInventoryDelegate = Mocks.GetSubmitInventoryDelegateMock();

            var result = inventory.Object.processJob(Mocks.GetMockConfig(), mockInventoryDelegate.Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);

            mockInventoryDelegate.Verify(m => m(It.Is<List<AgentCertStoreInventoryItem>>(l =>
                    l.Count == 3 &&
                    l.Any(i => i.Alias == "TestCert1") &&
                    l.Any(i => i.Alias == "TestCert2") &&
                    l.Any(i => i.Alias == "TestCert3")
                )));
        }

        [TestMethod]
        public void JobReturnsFailureResponseAfterError()
        {
            var inventory = new Mock<Inventory>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();

            var c1 = new CertificateItem("https://myvault.vault.azure.net/certificates/TestCert1/pending");
            var c2 = new CertificateItem("https://myvault.vault.azure.net/certificates/TestCert2/pending");
            var c3 = new CertificateItem("https://myvault.vault.azure.net/certificates/TestCert3/pending");

            var c1b = new CertificateBundle(c1.Id);
            c1b.Cer = new byte[] { 0, 1, 0, 1, 0, 1, 1, 0 };

            var c2b = new CertificateBundle(c2.Id);
            c2b.Cer = new byte[] { 1, 1, 0, 1, 0, 1, 1, 0 };

            var c3b = new CertificateBundle(c3.Id);
            c3b.Cer = new byte[] { 1, 0, 0, 1, 0, 1, 1, 0 };

            var certList = new List<CertificateItem>() { c1, c2, c3 };
            var pageObj = new KFPage<CertificateItem>(certList);
            var json = JsonConvert.SerializeObject(pageObj);

            mockAzClient.Setup(m => m.GetCertificatesAsync()).ThrowsAsync(new Exception("FAIL"));
            inventory.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);
            var mockInventoryDelegate = Mocks.GetSubmitInventoryDelegateMock();
            var result = inventory.Object.processJob(Mocks.GetMockConfig(), mockInventoryDelegate.Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);

            mockInventoryDelegate.Verify(m => m(It.IsAny<List<AgentCertStoreInventoryItem>>()), Times.Never()); // should not be invoked

            result.Status.Should().Be(4);
            result.Message.Should().Contain("FAIL");
            mockAzClient.Verify(az => az.GetCertificatesAsync(), Times.Once()); // calls getcertificates fine, then calls getcertificates which fails.
        }
    }
    public class KFPage<T> : IPage<T>
    {
        public string NextPageLink => "none";

        public KFPage(IEnumerable<T> items)
        {
            Items = items;
        }

        public IEnumerable<T> Items { get; set; }

        public IEnumerator<T> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
