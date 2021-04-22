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
