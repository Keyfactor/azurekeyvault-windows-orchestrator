// Copyright 2021 Keyfactor
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FluentAssertions;
using Keyfactor.AnyAgent.AzureKeyVault;
using Keyfactor.Platform.Extensions.Agents.Enums;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace AzureKeyVaultTests
{
    [TestClass]
    public class ManagementTests
    {
        /// <summary>
        /// Job level tests : 
        ///     verify the correct job class and store type are being returned.
        ///     verify the correct delegates are called and others are not
        ///     verify the job handles client errors gracefully
        ///     ? verify appropriate event logging
        /// </summary>


        [TestMethod]
        public void ReturnsTheCorrectJobClassAndStoreType()
        {
            var manage = new Management();
            manage.GetJobClass().Should().Be("Management");
            manage.GetStoreType().Should().Be("AKV");
        }

        #region Create

        [TestMethod]
        public void JobForCreateOnlyCallsPerformCreate()
        {
            var managementMock = new Mock<Management>() { CallBase = true };
            managementMock.Protected().Setup("PerformCreateVault").Verifiable();
            managementMock.Protected().Setup("PerformRemoval", ItExpr.IsAny<string>()).Verifiable();
            managementMock.Protected().Setup("PerformAddition", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>()).Verifiable();

            var config = Mocks.GetMockConfig();
            config.Job.OperationType = AnyJobOperationType.Create;

            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);
            managementMock.Protected().Verify("PerformCreateVault", Times.Once());
            managementMock.Protected().Verify("PerformRemoval", Times.Never(), ItExpr.IsAny<string>());
            managementMock.Protected().Verify("PerformAddition", Times.Never(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>());
        }

        [TestMethod]
        public void JobForCreateCallsCreateVault()
        {
            var managementMock = new Mock<Management>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();
            managementMock.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);

            var config = Mocks.GetMockConfig();
            config.Job.OperationType = AnyJobOperationType.Create;

            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);

            mockAzClient.Verify(az => az.CreateVault(), Times.Once());
            result.Status.Should().Equals(2);
            result.Message.Should().Be("Create Vault Complete");
        }

        [TestMethod]
        public void JobForCreateThrowsErrorForMismatchedVaultId()
        {
            var managementMock = new Mock<Management>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();
            managementMock.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);

            var config = Mocks.GetMockConfig();
            config.Job.OperationType = AnyJobOperationType.Create;
            mockAzClient.Setup(m => m.CreateVault()).ReturnsAsync(new Vault(null, "wrongVault", "wrongVault"));
            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);

            mockAzClient.Verify(az => az.CreateVault(), Times.Once());
            result.Status.Should().Equals(4);
            result.Message.Should().Be("The creation of the Azure Key Vault failed for an unknown reason. Check your job parameters and ensure permissions are correct.");
        }

        [TestMethod]
        public void JobForCreateVaultHandlesClientError()
        {
            var managementMock = new Mock<Management>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();
            managementMock.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);

            var config = Mocks.GetMockConfig();
            config.Job.OperationType = AnyJobOperationType.Create;
            mockAzClient.Setup(m => m.CreateVault()).Throws(new Exception("FAILURE"));
            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);

            mockAzClient.Verify(az => az.CreateVault(), Times.Once());
            result.Status.Should().Equals(4);
            result.Message.Should().Be("FAILURE");
        }

        #endregion

        #region Add

        [TestMethod]
        public void JobForAddOnlyCallsPerformAddition()
        {
            var managementMock = new Mock<Management>() { CallBase = true };
            managementMock.Protected().Setup("PerformCreateVault").Verifiable();
            managementMock.Protected().Setup("PerformRemoval", ItExpr.IsAny<string>()).Verifiable();
            managementMock.Protected().Setup("PerformAddition", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>()).Verifiable();

            var config = Mocks.GetMockConfig();
            config.Job.OperationType = AnyJobOperationType.Add;

            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);
            managementMock.Protected().Verify("PerformCreateVault", Times.Never());
            managementMock.Protected().Verify("PerformRemoval", Times.Never(), ItExpr.IsAny<string>());
            managementMock.Protected().Verify("PerformAddition", Times.Once(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>());
        }

        [TestMethod]
        public void JobForAddCallsImportCertificateAsync()
        {
            var managementMock = new Mock<Management>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();
            var thumbprint = new byte[] { 1, 1, 1, 1, 1, 0, 1, 0, 0, 0, 0, 1, 0, 1, 0, 1, 0, 1, 1, 0, 0, 1, 0, 0, 1, 1, 0, 1, 1, 0, 1, 1 };
            var certBundle = new CertificateBundle("https://testcert.com/certificates/cert/1.0", "https://testcert.com/certificates/cert/1.0", "https://testcert.com/certificates/cert/1.0", thumbprint);

            mockAzClient.Setup(az => az.ImportCertificateAsync(It.IsAny<string>(), It.IsAny<X509Certificate2Collection>())).Returns(Task.FromResult(certBundle));
            managementMock.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);

            managementMock.Protected().Setup<X509Certificate2Collection>("GenerateCertificate", ItExpr.IsAny<string>(), ItExpr.IsAny<string>()).Returns(new X509Certificate2Collection());
            var config = Mocks.GetMockConfig();
            config.Job.OperationType = AnyJobOperationType.Add;
            config.Job.Alias = "MyTestCert";
            config.Job.PfxPassword = "Password1";
            config.Job.EntryContents = certData;
            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);

            mockAzClient.Verify(az => az.ImportCertificateAsync(It.IsAny<string>(), It.IsAny<X509Certificate2Collection>()), Times.Once());
            result.Status.Should().Equals(2);
            result.Message.Should().Be($"Successfully Added {config.Job.Alias}");
        }

        [TestMethod]
        public void JobForAddThrowsIfAliasOrPasswordMissing()
        {
            var managementMock = new Mock<Management>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();
            managementMock.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);

            var config = Mocks.GetMockConfig();
            config.Job.OperationType = AnyJobOperationType.Create;
            mockAzClient.Setup(m => m.CreateVault()).ReturnsAsync(new Vault(null, "wrongVault", "wrongVault"));
            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);

            mockAzClient.Verify(az => az.CreateVault(), Times.Once());
            result.Status.Should().Equals(4);
            result.Message.Should().Be("The creation of the Azure Key Vault failed for an unknown reason. Check your job parameters and ensure permissions are correct.");
        }

        [TestMethod]
        public void JobForAddHandlesClientError()
        {
        }

        #endregion

        #region Remove

        [TestMethod]
        public void JobForRemoveOnlyCallsPerformRemove()
        {
            var managementMock = new Mock<Management>() { CallBase = true };
            managementMock.Protected().Setup("PerformCreateVault").Verifiable();
            managementMock.Protected().Setup("PerformRemoval", ItExpr.IsAny<string>()).Verifiable();
            managementMock.Protected().Setup("PerformAddition", ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>()).Verifiable();

            var config = Mocks.GetMockConfig();
            config.Job.OperationType = AnyJobOperationType.Remove;

            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);
            managementMock.Protected().Verify("PerformCreateVault", Times.Never());
            managementMock.Protected().Verify("PerformRemoval", Times.Once(), ItExpr.IsAny<string>());
            managementMock.Protected().Verify("PerformAddition", Times.Never(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>(), ItExpr.IsAny<string>());
        }

        [TestMethod]
        public void JobForRemoveCallsDeleteCertificateAsync()
        {
            var managementMock = new Mock<Management>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();
            mockAzClient.Setup(m => m.DeleteCertificateAsync(It.IsAny<string>())).Returns(Task.FromResult(new DeletedCertificateBundle("https://testcert.com/certificates/TestCert/1.0")));
            managementMock.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);

            var config = Mocks.GetMockConfig();
            config.Job.OperationType = AnyJobOperationType.Remove;
            config.Job.Alias = "TestCert";

            var result = managementMock.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);

            mockAzClient.Verify(az => az.DeleteCertificateAsync("TestCert"), Times.Once());
            result.Status.Should().Equals(2);
            result.Message.Should().Be("Successfully removed TestCert");
        }

        [TestMethod]
        public void JobForRemoveThrowsIfAliasMissing() { }

        [TestMethod]
        public void JobForRemoveHandlesClientError() { }

        #endregion


        private const string certData = @"MIIDWzCCAkOgAwIBAgIUCUODGuVj/ieU3IDwVtseMx6nKyswDQYJKoZIhvcNAQEL
                                        BQAwPTELMAkGA1UEBhMCVVMxCzAJBgNVBAgMAk1JMSEwHwYDVQQKDBhJbnRlcm5l
                                        dCBXaWRnaXRzIFB0eSBMdGQwHhcNMjAxMTIzMjI0MzI1WhcNMjExMTIzMjI0MzI1
                                        WjA9MQswCQYDVQQGEwJVUzELMAkGA1UECAwCTUkxITAfBgNVBAoMGEludGVybmV0
                                        IFdpZGdpdHMgUHR5IEx0ZDCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEB
                                        ANumvutiv90H3i7GrPZgceJE4GQXRM5nzpsT+Ekfh0+8MSWaOCfV91WOtMEVFQcg
                                        sDbJz4hbFlKtPs44IIv0A5MmXnYVz9YYVi2VR/kQi+KSELCnxU6/K6pIiQJ6ptKC
                                        G1o+o2hjTu4QfxVHWi0QccaMt8ElOyfDKkj8HHJmxiXBqNM1BlMvz1PVKIHOLmpc
                                        uIDT4OxrLNE2lr8v3C5xmyVLI4v7b98LtQ/hfRixb7/pRvl/vaz68Ei7ePNfVBBO
                                        kNCkmpKdnI+HbDqpxxhXNgWD49a/tH96pACQeOT9/Lh7FbOjhAEcszBQN4u3PliX
                                        2jRLisvRLVWlQfSiov2ngFcCAwEAAaNTMFEwHQYDVR0OBBYEFBps0i5BGxwlDn4T
                                        Ljn9AT8PnbbhMB8GA1UdIwQYMBaAFBps0i5BGxwlDn4TLjn9AT8PnbbhMA8GA1Ud
                                        EwEB/wQFMAMBAf8wDQYJKoZIhvcNAQELBQADggEBAM8F3Y3xZ0Xbfuk+jo4Wp5Y+
                                        0/jAh+991NMbW342xG9IHpaSw8R7etxWRdUAG+Q3Had1mHVRSR/jnLSn7WctdU1S
                                        bkEPW5lANDZZHf0y9OzREcsrFYFrNZ6X6tmi5TQ/PCiS8jy7XAyTuEgm+FLnEoRq
                                        12nYX037SFsrUyUUHt/RdLiail47MtooFs6HVAJjkpkSWcA0f+Asazz52YEOBvBz
                                        3zoA+SolvuhDJkZQu9Fq2Ok7vWpb4GeuNmbwTA49Evol3nG/DntXFb4Cu5ZJ1hwO
                                        UsvAROy7dM+PS/OfVQ24HLaSScgF3rOCATTL2rQgm8y2Is5fX2TdhXu7fxL5cwk=";

    }
}
