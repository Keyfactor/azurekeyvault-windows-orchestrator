using System;
using FluentAssertions;
using Keyfactor.AnyAgent.AzureKeyVault;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

namespace AzureKeyVaultTests
{
    [TestClass]
    public class CreateTests
    {        
        [TestMethod]
        public void ReturnsTheCorrectJobClassAndStoreType()
        {
            var create = new Create();
            create.GetJobClass().Should().Be("Create");
            create.GetStoreType().Should().Be("AKV");
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
            var config = Mocks.GetMockConfig();            
            config.Store.Properties = JsonConvert.SerializeObject(new
            {
                VaultUrl = "https://test.vault",
                TenantId = "8b74a908-b153-41dc-bfe5-3ea7b22b9678",
                ClientSecret = "testClientSecret",
                ApplicationId = Guid.NewGuid().ToString(),
                SubscriptionId = Guid.NewGuid().ToString(),
                VaultName = "wrongVaultName",
                ResourceGroupName = "testResourceGroupName",
                APIObjectId = Guid.NewGuid().ToString(),
            });

            var create = new Mock<Create>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient(config);
            create.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);

            var result = create.Object.processJob(config, Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);
            result.Status.Should().Be(4);
            result.Message.Should().Contain("The creation of the Azure Key Vault failed for an unknown reason. Check your job parameters and ensure permissions are correct.");
            mockAzClient.Verify(az => az.CreateVault(), Times.Once());            
        }
    }
}
