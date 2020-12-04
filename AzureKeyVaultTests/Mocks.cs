﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Keyfactor.AnyAgent.AzureKeyVault;
using Keyfactor.Platform.Extensions.Agents;
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo("AzureKeyVault")]
namespace AzureKeyVaultTests
{
    public static class Mocks
    {
        public static Mock<AzureClient> GetMockAzureClient()
        {
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
           .ReturnsAsync(new HttpResponseMessage()
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent(JsonConvert.SerializeObject(new { id = "testVaultName"})),
           })
           .Verifiable();

            mockHttpMessageHandler.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(a => a.RequestUri.ToString() == $"https://login.windows.net/8b74a908-b153-41dc-bfe5-3ea7b22b9678/oauth2/token"), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(new { access_token = Guid.NewGuid().ToString() })),
            })
             .Verifiable();

            var mockAzClient = new Mock<AzureClient>() { CallBase = true };
            mockAzClient.Protected().Setup<HttpClient>("HttpClient").Returns(new HttpClient(mockHttpMessageHandler.Object));
            var props = new AzureKeyVaultJobParameters(GetMockConfig().Store.Properties);
            mockAzClient.Protected().Setup<AzureKeyVaultJobParameters>("JobParameters").Returns(props);
            return mockAzClient;
        }
        public static AnyJobConfigInfo GetMockConfig()
        {
            var ajStore = new AnyJobStoreInfo()
            {
                Properties = new
                {
                    VaultUrl = "https://test.vault",
                    TenantId = "8b74a908-b153-41dc-bfe5-3ea7b22b9678",
                    ClientSecret = "testClientSecret",
                    ApplicationId = Guid.NewGuid().ToString(),
                    SubscriptionId = Guid.NewGuid().ToString(),
                    VaultName = "testVaultName",
                    ResourceGroupName = "testResourceGroupName",
                    APIObjectId = Guid.NewGuid().ToString(),
                },
                Inventory = new List<AnyJobInventoryItem>(),
                StorePath = "http://test.vault",
                Storetype = 1,
            };
            var ajJob = new AnyJobJobInfo { OperationType = Keyfactor.Platform.Extensions.Agents.Enums.AnyJobOperationType.Create, Alias = "testJob", JobId = Guid.NewGuid(), JobTypeId = Guid.NewGuid(), };
            var ajServer = new AnyJobServerInfo { Username = "testUsername", Password = "testPassword", UseSSL = true };
            var ajc = new AnyJobConfigInfo()
            {
                Store = ajStore,
                Job = ajJob,
                Server = ajServer
            };

            return ajc;
        }

        public static Mock<SubmitInventoryUpdate> GetSubmitInventoryDelegateMock() => new Mock<SubmitInventoryUpdate>();

        public static Mock<SubmitEnrollmentRequest> GetSubmitEnrollmentDelegateMock() => new Mock<SubmitEnrollmentRequest>();

        public static Mock<SubmitDiscoveryResults> GetSubmitDiscoveryDelegateMock() => new Mock<SubmitDiscoveryResults>();
    }
}