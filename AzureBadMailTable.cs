
using Escc.Services.Azure;
using Microsoft.WindowsAzure.Storage.Table;

namespace Escc.AzureEmailForwarder
{
    /// <summary>
    /// An Azure table used to store bad mail for manual processing
    /// </summary>
    public class AzureBadMailTable
    {
        private readonly CloudTable _table;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBadMailTable"/> class.
        /// </summary>
        public AzureBadMailTable()
        {
            _table = CreateTable();
        }

        private static CloudTable CreateTable()
        {
            var config = new AzureServicesConfiguration();
            var storageAccount = config.StorageAccount;
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("bademail");
            table.CreateIfNotExists();
            return table;
        }

        /// <summary>
        /// Gets the table.
        /// </summary>
        /// <value>
        /// The table.
        /// </value>
        public CloudTable Table { get { return _table; } }
    }
}
