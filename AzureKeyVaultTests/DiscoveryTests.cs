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
using System.Collections.Generic;
using FluentAssertions;
using Keyfactor.AnyAgent.AzureKeyVault;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;

namespace AzureKeyVaultTests
{
    [TestClass]
    public class DiscoveryTests
    {
        [TestMethod]
        public void ReturnsTheCorrectJobClassAndStoreType()
        {
            var discovery = new Discovery();
            discovery.GetJobClass().Should().Be("Discovery");
            discovery.GetStoreType().Should().Be("AKV");
        }

        [TestMethod]
        public void JobCallsGetVaults()
        {
            var discovery = new Mock<Discovery>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();
            mockAzClient.Setup(az => az.GetVaults()).ReturnsAsync(new _DiscoveryResult() { Vaults = new List<_AKV_Location>() });
            discovery.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);
            
            var result = discovery.Object.processJob(Mocks.GetMockConfig(), Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, Mocks.GetSubmitDiscoveryDelegateMock().Object);

            mockAzClient.Verify(az => az.GetVaults(), Times.Once());
            result.Status.Should().Equals(2);
            result.Message.Should().Be("Discovery Complete");
        }

        [TestMethod]
        public void JobInvokesCorrectDelegate() 
        {
            var discovery = new Mock<Discovery>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();
            mockAzClient.Setup(az => az.GetVaults()).ReturnsAsync(new _DiscoveryResult() { Vaults = new List<_AKV_Location>() });
            discovery.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);
            var mockSdr = Mocks.GetSubmitDiscoveryDelegateMock();

            var result = discovery.Object.processJob(Mocks.GetMockConfig(), Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, mockSdr.Object);

            mockSdr.Verify(m => m(It.IsAny<List<string>>()));            
        }

        [TestMethod]
        public void JobReturnsCorrectVaultNames()
        {
            var discovery = new Mock<Discovery>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();

            var v1 = "TestVault1";
            var v2 = "TestVault2";
            var v3 = "TestVAult3";

            mockAzClient.Setup(c => c.GetVaults()).ReturnsAsync(() => new _DiscoveryResult()
            {
                Vaults = new List<_AKV_Location>() {
                    new _AKV_Location(){Name = v1},
                    new _AKV_Location(){Name = v2 },
                    new _AKV_Location(){Name = v3 } }
            });
            discovery.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);
            var mockSdr = Mocks.GetSubmitDiscoveryDelegateMock();

            var result = discovery.Object.processJob(Mocks.GetMockConfig(), Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, mockSdr.Object);

            mockSdr.Verify(m => m(It.Is<List<string>>(l => l.Contains(v1) && l.Contains(v2) && l.Contains(v3))));
        }

        [TestMethod]
        public void JobReturnsFailureResponseAfterError()
        {
            var discovery = new Mock<Discovery>() { CallBase = true };
            var mockAzClient = Mocks.GetMockAzureClient();

            mockAzClient.Setup(m => m.GetVaults()).ThrowsAsync(new Exception("FAIL"));
            discovery.Protected().Setup<AzureClient>("AzClient").Returns(mockAzClient.Object);
            var mockSdr = Mocks.GetSubmitDiscoveryDelegateMock();
            var result = discovery.Object.processJob(Mocks.GetMockConfig(), Mocks.GetSubmitInventoryDelegateMock().Object, Mocks.GetSubmitEnrollmentDelegateMock().Object, mockSdr.Object);

            mockSdr.Verify(m => m(It.IsAny<List<string>>()), Times.Never()); // sdr should not be invoked

            result.Status.Should().Be(4);
            result.Message.Should().Contain("FAIL");
            mockAzClient.Verify(az => az.GetVaults(), Times.Once());
        }
    }
}
