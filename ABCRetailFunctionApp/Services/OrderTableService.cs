using Azure;
using Azure.Data.Tables;
using System.Threading.Tasks;
using ABC_Retail.Models;


public class OrderTableService
{
    private readonly TableClient _table;

    public OrderTableService(string connectionString, string tableName)
    {
        var svc = new TableServiceClient(connectionString?.Trim());
        _table = svc.GetTableClient(tableName.Trim());
        _table.CreateIfNotExists();
    }

    
    public Task UpsertOrderAsync(ABC_Retail.Models.Order order) =>
        _table.UpsertEntityAsync(order, TableUpdateMode.Replace);

    
    public Task UpdateStatusAsync(string partitionKey, string rowKey, string status)
    {
        var patch = new TableEntity(partitionKey, rowKey)
        {
            ["Status"] = status
        };
        
        return _table.UpdateEntityAsync(patch, ETag.All, TableUpdateMode.Merge);
    }
}
