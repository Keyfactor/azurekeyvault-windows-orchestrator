using System;
using System.Linq;
using AzureKeyVault;
using CSS.Common.Logging;
using Keyfactor.Platform.Extensions.Agents;
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Keyfactor.Platform.Extensions.Agents.Interfaces;

namespace Keyfactor.AnyAgent.AzureKeyVault
{
    public abstract class AzureKeyVaultJob : LoggingClientBase, IAgentJobExtension
    {
        internal protected virtual AzureClient AzClient { get; set; }
        internal protected AzureKeyVaultJobParameters JobParameters { get; set; }

        public string GetJobClass()
        {
            var attr = GetType().GetCustomAttributes(true).First(a => a.GetType() == typeof(JobAttribute)) as JobAttribute;
            return attr?.JobClass ?? string.Empty;
        }

        public string GetStoreType() => AzureKeyVaultConstants.STORE_TYPE_NAME;

        public abstract AnyJobCompleteInfo processJob(AnyJobConfigInfo config, SubmitInventoryUpdate submitInventory, SubmitEnrollmentRequest submitEnrollmentRequest, SubmitDiscoveryResults sdr);

        protected virtual void Initialize(AnyJobConfigInfo config)
        {
            try
            {
                JobParameters = new AzureKeyVaultJobParameters((dynamic)config.Store.Properties);
                JobParameters.VaultURL = config.Store.StorePath;
                AzClient = AzClient ?? new AzureClient(JobParameters);
            }
            catch (Exception ex)
            {
                ThrowError(ex, "Initialization");
            }
            Logger.Trace($"Configuration complete for {GetStoreType()} - {GetJobClass()}.");
        }

        protected AnyJobCompleteInfo Success(string message = null)
        {

            return new AnyJobCompleteInfo()
            {
                Status = 2,
                Message = message ?? $"{GetJobClass()} Complete"
            };
        }

        protected AnyJobCompleteInfo ThrowError(Exception exception, string jobSection)
        {
            string message = FlattenException(exception);
            Logger.Error($"Error performing {jobSection} in {GetJobClass()} {GetStoreType()} - {message}");
            return new AnyJobCompleteInfo()
            {
                Status = 4,
                Message = message
            };
        }

        private string FlattenException(Exception ex)
        {
            string returnMessage = ex.Message;
            if (ex.InnerException != null)
                returnMessage += (" - " + FlattenException(ex.InnerException));

            return returnMessage;
        }
    }

    public class AzureKeyVaultJobParameters
    {
        public string VaultURL { get; set; }
        public string TenantId { get; set; }
        public string ClientSecret { get; set; }
        public string ApplicationId { get; set; }
        public string SubscriptionId { get; set; }
        public string VaultName { get; set; }
        public string ResourceGroupName { get; set; }
        public string APIObjectId { get; set; }

        public AzureKeyVaultJobParameters(dynamic source)
        {
            VaultURL = source.GetType().GetProperty("VaultUrl").GetValue(source, null);
            TenantId = source.GetType().GetProperty("TenantId").GetValue(source, null);
            ClientSecret = source.GetType().GetProperty("ClientSecret").GetValue(source, null);
            ApplicationId = source.GetType().GetProperty("ApplicationId").GetValue(source, null);
            SubscriptionId = source.GetType().GetProperty("SubscriptionId").GetValue(source, null);
            VaultName = source.GetType().GetProperty("VaultName").GetValue(source, null);;
            ResourceGroupName = source.GetType().GetProperty("ResourceGroupName").GetValue(source, null); ;
            APIObjectId = source.GetType().GetProperty("APIObjectId").GetValue(source, null); ;
        }

        public interface IStoreProperties
        {
            string VaultUrl { get; set; }
            string TenantId { get; set; }
            string ClientSecret { get; set; }
            string SubscriptionId { get; set; }
            string VaultName { get; set; }
            string ResourceGroupName { get; set; }
            string ApplicationId { get; set; }
            string APIObjectId { get; set; }
        }
    }
}
