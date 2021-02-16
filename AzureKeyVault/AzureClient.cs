using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Azure.Management.KeyVault.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;

namespace Keyfactor.AnyAgent.AzureKeyVault
{
    public class AzureClient
    {
        private protected virtual KeyVaultClient KeyVaultClient { get; set; }
        internal protected virtual AzureKeyVaultJobParameters JobParameters { get; set; }
        internal protected virtual HttpClient HttpClient { get; set; }

        public AzureClient() { }
        public AzureClient(AzureKeyVaultJobParameters jobParameters, HttpClient httpClient = null)
        {
            this.HttpClient = httpClient ?? new HttpClient(); // override for testing
            this.JobParameters = jobParameters;
            KeyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessTokenAsync), this.HttpClient);
        }

        public virtual async Task<DeletedCertificateBundle> DeleteCertificateAsync(string certName)
        {
            return await KeyVaultClient.DeleteCertificateAsync(JobParameters.VaultURL, certName);
        }

        /// <summary>
        /// Authentication call back method for the Azure Key Vault SDK
        /// </summary>
        /// <returns>Bearer access token</returns>
        private async Task<string> GetAccessTokenAsync(string authority, string resource, string scope)
        {
            var appCredentials = new ClientCredential(JobParameters.ApplicationId, JobParameters.ClientSecret);
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);

            var result = await context.AcquireTokenAsync(resource, appCredentials);

            return result.AccessToken;
        }

        public virtual async Task<Vault> CreateVault()
        {
            var token = await AcquireTokenBySPN(JobParameters.TenantId, JobParameters.ApplicationId, JobParameters.ClientSecret);

            var uri = $"https://management.azure.com/subscriptions/{JobParameters.SubscriptionId}/resourceGroups/{JobParameters.ResourceGroupName}/providers/Microsoft.KeyVault/vaults/{JobParameters.VaultName}?api-version=2018-02-14-preview";

            var req = new CreateVaultRequest()
            {
                Location = "eastus",  // TODO: confirm this will not change. 
                Properties = new VaultProperties()
                {
                    Sku = new Sku()
                    {
                        Name = SkuName.Standard
                    },

                    TenantId = new Guid(JobParameters.TenantId),
                    AccessPolicies = new List<AccessPolicyEntry>()
                    {
                        new AccessPolicyEntry()
                        {
                            ApplicationId = new Guid(JobParameters.ApplicationId),
                            ObjectId = JobParameters.APIObjectId,
                            Permissions = new Permissions()
                            {
                                Certificates = new List<string>
                                {
                                    "get",
                                    "list",
                                    "delete",
                                    "create",
                                    "import",
                                    "update",
                                    "managecontacts",
                                    "getissuers",
                                    "listissuers",
                                    "setissuers",
                                    "deleteissuers",
                                    "manageissuers",
                                    "recover",
                                    "purge"
                                },
                                Keys = new List<string>(),
                                Secrets = new List<string>(),
                                Storage = new List<string>()
                            },
                            TenantId = new Guid(JobParameters.TenantId)
                        }
                    }
                }
            };

            HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            HttpResponseMessage resp = await HttpClient.PutAsync(uri, content);
            return JsonConvert.DeserializeObject<Vault>(await resp.Content.ReadAsStringAsync());
        }

        public virtual async Task<CertificateOperation> CreateCertificateAsync(string certName)
        {
            return await KeyVaultClient.CreateCertificateAsync(JobParameters.VaultURL, certName);
        }

        public virtual async Task<CertificateBundle> ImportCertificateAsync(string name, X509Certificate2Collection uploadCollection)
        {
            return await KeyVaultClient.ImportCertificateAsync(JobParameters.VaultURL, name, uploadCollection, null);
        }

        public virtual async Task<IEnumerable<CertificateItem>> GetCertificatesAsync()
        {            
            var results = await KeyVaultClient.GetCertificatesAsync(JobParameters.VaultURL);
            var certs = new List<CertificateItem>();
            certs.AddRange(results);

            while (!string.IsNullOrWhiteSpace(results.NextPageLink))
            {                
                results = KeyVaultClient.GetCertificatesNextAsync(results.NextPageLink).GetAwaiter().GetResult();
                if (results != null) certs.AddRange(results);
            }

            return certs;
        }

        public virtual async Task<CertificateBundle> GetCertificateAsync(string id)
        {
            return await KeyVaultClient.GetCertificateAsync(id);
        }

        public virtual async Task<_DiscoveryResult> GetVaults()
        {
            var uri = $"https://management.azure.com/subscriptions/{JobParameters.SubscriptionId}/resources?%24filter=resourceType%20eq%20%27Microsoft.KeyVault%2Fvaults%27&api-version=2018-05-01";
            var token = await AcquireTokenBySPN(JobParameters.TenantId, JobParameters.ApplicationId, JobParameters.ClientSecret);
            HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            HttpResponseMessage resp = await HttpClient.GetAsync(uri);

            return JsonConvert.DeserializeObject<_DiscoveryResult>(resp.Content.ReadAsStringAsync().Result);
        }

        private async Task<string> AcquireTokenBySPN(string tenantId, string clientId, string clientSecret)
        {
            const string ARMResource = "https://management.azure.com/";
            const string TokenEndpoint = "https://login.windows.net/{0}/oauth2/token";
            const string SPNPayload = "resource={0}&client_id={1}&grant_type=client_credentials&client_secret={2}";

            var payload = string.Format(SPNPayload,
                                        WebUtility.UrlEncode(ARMResource),
                                        WebUtility.UrlEncode(clientId),
                                        WebUtility.UrlEncode(clientSecret));

            var address = string.Format(TokenEndpoint, tenantId);
            var content = new StringContent(payload, Encoding.UTF8, "application/x-www-form-urlencoded");
            dynamic body;
            using (var response = await HttpClient.PostAsync(address, content))
            {
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Status:  {0}", response.StatusCode); // To do : Change to NLog
                    Console.WriteLine("Content: {0}", await response.Content.ReadAsStringAsync()); // To do : Change to NLog
                }

                response.EnsureSuccessStatusCode();
                var stringContent = await response.Content.ReadAsStringAsync();
                body = JsonConvert.DeserializeObject(stringContent, typeof(ExpandoObject));
            }

            return body.access_token;
        }
    }

    public class CreateVaultRequest
    {
        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("properties")]
        public VaultProperties Properties { get; set; }
    }
}

