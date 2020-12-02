using System;
using FluentAssertions;
using Keyfactor.AnyAgent.AzureKeyVault;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace AzureKeyVaultTests
{
    [TestClass]
    public class ReenrollmentTests
    {        
        [TestMethod]
        public void ReturnsTheCorrectJobClassAndStoreType()
        {
            var create = new Create();
            create.GetJobClass().Should().Equals("Enrollment");
            create.GetStoreType().Should().Equals("AKV");
        }

        [TestMethod]
        public void JobCallsCreateVault()
        {
            var create = new Mock<Create>(){ CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();
            create.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);
            var result = create.Object.processJob(Mocks.GetMockConfig(), Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);

            mockAzClient.Verify(az => az.CreateVault(), Times.Once());

            result.Status.Should().Equals(2);
            result.Message.Should().Be("Create Complete");
        }

        [TestMethod]
        public void JobReturnsFailureResponseAfterError() 
        {
            var create = new Mock<Create>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();
            create.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);

            var config = Mocks.GetMockConfig();            
            config.Store.Properties = new
            {
                VaultUrl = "https://test.vault",
                TenantId = "8b74a908-b153-41dc-bfe5-3ea7b22b9678",
                ClientSecret = "testClientSecret",
                ApplicationId = Guid.NewGuid().ToString(),
                SubscriptionId = Guid.NewGuid().ToString(),
                VaultName = "wrongVaultName",
                ResourceGroupName = "testResourceGroupName",
                APIObjectId = Guid.NewGuid().ToString(),
            };

            var result = create.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);
            result.Status.Should().Be(4);
            result.Message.Should().Contain("The creation of the Azure Key Vault failed for an unknown reason. Check your job parameters and ensure permissions are correct.");
            mockAzClient.Verify(az => az.CreateVault(), Times.Once());            
        }
    }
}
