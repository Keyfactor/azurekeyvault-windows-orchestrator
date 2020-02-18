using System;
using Microsoft.Azure.KeyVault;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.KeyVault.Models;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Security.Cryptography.X509Certificates;

namespace Azure_KV_Driver
{
    class Program
    {
        const string subscription = "b4362654-9aa3-412b-b385-2084d7dc4d67";

        const string tenantID = "65a065eb-5e50-4ba2-8ef8-e70be774cd8a";
        const string applicationId = "eb25555f-3984-427d-911a-e74bab81cfe7";
        const string clientSecret = "wWZPEqcpofaUalaqG+DU04jnJdOPUC1HiK+4fkyHcGo=";
        const string objectId = "283346ae-b3bf-4802-94f8-cffb81e2dfdd";

        const string resource = "https://cmskvpoc.vault.azure.net/";

        public static void Main()
        {
            // Managerial OAuth
            string managementToken = ManagementAuthenticationHandler.AcquireTokenBySPN(tenantID, applicationId, clientSecret).Result;

            // ----- list vaults
            //Console.WriteLine("Getting list of vaults...");
            //var test = ListVaults(
            //    $"https://management.azure.com/subscriptions/{subscription}/resources?%24filter=resourceType%20eq%20%27Microsoft.KeyVault%2Fvaults%27&api-version=2018-05-01",
            //    managementToken
            //);

            //Console.WriteLine(JToken.Parse(test.Result.ToString()));

            // ----- create vault
            var resourceGroupName = "CSSDemo";
            var vaultName = "FortKnocks";

            Console.WriteLine("Creating new vault...");
            var test = CreateVault(
                $"https://management.azure.com/subscriptions/{subscription}/resourceGroups/{resourceGroupName}/providers/Microsoft.KeyVault/vaults/{vaultName}?api-version=2018-02-14",
                managementToken
            );

            Console.WriteLine(test.Result.Id);

            // ----- get key
            //Console.WriteLine("\nGetting key...");
            //string keyName = "Test";
            //string res = (new Program()).GetSecretAsync(resource, keyName).Result;

            //Console.WriteLine(JToken.Parse(res));

            // ----- get cert
            //Console.WriteLine("\nGetting Cert...");
            //var res = (new Program()).GetCertificateAsync("https://cmskvpoc.vault.azure.net/certificates/TestGenCert/").Result;

            //Console.WriteLine(res);

            // ----- get certs
            //Console.WriteLine("\nGetting Certs...");
            //var res = (new Program()).GetCertificatesAsync("https://cmskvpoc.vault.azure.net/").Result;

            //foreach(var item in res)
            //    Console.WriteLine(item.Attributes.);

            //Console.WriteLine(string.Concat(item.X509Thumbprint.Select(i => i.ToString("X2"))));

            // ----- create cert
            //Console.WriteLine("\nImporting Cert...");
            //var success = (new Program()).ImportCertificateAsync(resource, "TestAPICertUpload");

            //if (success.Result) Console.WriteLine("Certificate Imported!");
            //else Console.WriteLine("Error uploading certificate!");

            // ----- list raw certs
            // -- get all certs
            //Console.WriteLine("\nGetting Certs...");
            //var res = (new Program()).GetCertificatesAsync("https://cmskvpoc.vault.azure.net/").Result;

            //foreach (var item in res)
            //{
            //    Console.WriteLine("\nGetting a Cert...");
            //    Console.WriteLine((new Program()).GetCertificateAsync(item.Id).Result.CertificateIdentifier.Name);
            //}

            // -- get all certs on vms
            //Console.WriteLine("Looking for certs...");
            //var test = CertSearch(
            //    $"https://management.azure.com/subscriptions/{subscription}/providers/Microsoft.Compute/virtualMachines?api-version=2017-12-01",
            //    managementToken
            //);

            //File.WriteAllText("C:\\Users\\CMS-Service\\Desktop\\AzureVMs.txt", JToken.Parse(test.Result.ToString()).ToString());
            //Console.WriteLine("Result written to desktop.");

            // ----- hault console window
            Console.WriteLine("\nDone.");
            Console.Read();
        }

        // Management API
        private static async Task<object> ListVaults(string uri, string token)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            HttpResponseMessage resp = await client.GetAsync(uri);

            return resp.Content.ReadAsStringAsync().Result;
        }

        private static async Task<Vault> CreateVault(string uri, string token)
        {
            HttpClient client = new HttpClient();

            CreateVaultRequest req = new CreateVaultRequest()
            {
                Location = "eastus",
                Properties = new VaultProperties()
                {
                    Sku = new Sku()
                    {
                        Name = SkuName.Standard
                    },

                    TenantId = new Guid(tenantID),
                    AccessPolicies = new List<AccessPolicyEntry>()
                    {
                        new AccessPolicyEntry()
                        {
                            ApplicationId = new Guid(applicationId),
                            ObjectId = objectId,
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
                            TenantId = new Guid(tenantID)
                        }
                    }
                }
            };

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            HttpResponseMessage resp = await client.PutAsync(uri, content);
            return JsonConvert.DeserializeObject<Vault>(resp.Content.ReadAsStringAsync().Result);
        }

        internal class CreateVaultRequest
        {
            [JsonProperty("location")]
            public string Location { get; set; }

            [JsonProperty("properties")]
            public VaultProperties Properties { get; set; } 
        }

        private static async Task<object> CertSearch(string uri, string token)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            HttpResponseMessage resp = await client.GetAsync(uri);

            return resp.Content.ReadAsStringAsync().Result;
        }

        // Key Vault API
        public async Task<string> GetSecretAsync(string vaultUrl, string vaultKey)
        {
            var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessTokenAsync), new HttpClient());
            var secret = await client.GetKeyAsync(vaultUrl, vaultKey);
             
            return secret.Key.ToString();
        }

        public async Task<Microsoft.Azure.KeyVault.Models.CertificateBundle> GetCertificateAsync(string vaultUrl)
        {
            var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessTokenAsync), new HttpClient());
            var cert = await client.GetCertificateAsync(vaultUrl);

            return cert;
        }

        public async Task<Microsoft.Rest.Azure.IPage<Microsoft.Azure.KeyVault.Models.CertificateItem>> GetCertificatesAsync(string vaultUrl)
        {
            var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessTokenAsync), new HttpClient());
            var certs = await client.GetCertificatesAsync(vaultUrl);

            return certs;
        }

        public async Task<bool> ImportCertificateAsync(string vaultUrl, string certName_)
        {
            var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessTokenAsync), new HttpClient());

            // create the certificate collection
            X509Certificate2Collection uploadCollection = new X509Certificate2Collection();
            uploadCollection.Add(
                new X509Certificate2(@"C:\Users\CMS-Service\Documents\isp-amq-qa.st12003.pfx", "Cheese22", X509KeyStorageFlags.Exportable)
            );

            Console.WriteLine(uploadCollection.Count + " certs scheduled for upload.");
            Console.WriteLine("Collection contains private key: " + uploadCollection[0].HasPrivateKey);

            await client.ImportCertificateAsync(vaultUrl, certName_, uploadCollection, null);

            return true;
        }

        private async Task<string> GetAccessTokenAsync(string authority, string resource, string scope)
        {
            var appCredentials = new ClientCredential(applicationId, clientSecret);
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);
 
            var result = await context.AcquireTokenAsync(resource, appCredentials);
 
            return result.AccessToken;
        }
    }

    internal class _AKV_Location
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("location")]
        public string location { get; set; }

        [JsonProperty("tags")]
        public _Tags Tags { get; set; }
    }

    internal class _DiscoveryResult
    {
        [JsonProperty("value")]
        public List<_AKV_Location> Vaults { get; set; }
    }

    internal class _Tags
    {
        List<string> Values { get; set; }
    }
}
