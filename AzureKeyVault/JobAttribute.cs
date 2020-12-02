using System;

namespace AzureKeyVault
{
    [AttributeUsage(AttributeTargets.Class)]
    public class JobAttribute : Attribute
    {
        private string jobClass { get; set; }

        public JobAttribute(string jobClass)
        {            
            this.jobClass = jobClass;
        }

        public virtual string JobClass
        {
            get { return jobClass; }
        }
    }
}
