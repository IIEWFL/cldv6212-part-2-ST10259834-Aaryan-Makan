using ABC_Retail.Models;



// Attribution: Microsoft. (2025). Azure Tables client library for .NET. Available at: https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme?view=azure-dotnet (Accessed: 28 August 2025).
// Attribution: Microsoft. (2025). Get started with Azure Blob Storage and .NET. Available at: https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blob-dotnet-get-started (Accessed: 28 August 2025).
// Attribution: Microsoft. (2024). Tutorial: Work with Azure Queue Storage queues in .NET. Available at: https://learn.microsoft.com/en-us/azure/storage/queues/storage-tutorial-queues (Accessed: 28 August 2025).
// Attribution: Microsoft. (2025). Develop for Azure Files with .NET. Available at: https://learn.microsoft.com/en-us/azure/storage/files/storage-dotnet-how-to-use-files (Accessed: 28 August 2025).
// Attribution: Microsoft. (2025). Deploy ASP.NET Core apps to Azure App Service. Available at: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/azure-apps/?view=aspnetcore-9.0 (Accessed: 28 August 2025).
// Attribution: W3Schools. (n.d.). C# Tutorial. Available at: https://www.w3schools.com/cs/index.php (Accessed: 28 August 2025).
// Attribution: W3Schools. (n.d.). ASP.NET Razor C# Syntax. Available at: https://www.w3schools.com/asp/razor_syntax.asp (Accessed: 28 August 2025).



/* 

1) ASP.NET Core MVC overview – https://learn.microsoft.com/aspnet/core/mvc/overview?view=aspnetcore-8.0
2) Dependency Injection in ASP.NET Core – https://learn.microsoft.com/aspnet/core/fundamentals/dependency-injection
3) Configuration in ASP.NET Core (appsettings, options) – https://learn.microsoft.com/aspnet/core/fundamentals/configuration
4) Azure.Data.Tables client library for .NET – https://learn.microsoft.com/azure/developer/net/sdk/azure/data-tables
5) Table storage design guidelines (partition/row keys) – https://learn.microsoft.com/azure/storage/tables/table-storage-design-guidelines
6) Azure.Storage.Blobs client library for .NET – https://learn.microsoft.com/dotnet/api/overview/azure/storage.blobs-readme
7) Azure.Storage.Queues client library for .NET – https://learn.microsoft.com/dotnet/api/overview/azure/storage.queues-readme
8) Azure.Storage.Files.Shares client library for .NET – https://learn.microsoft.com/dotnet/api/overview/azure/storage.files.shares-readme
9) Manage data with Azure Storage Explorer – https://learn.microsoft.com/azure/vs-azure-tools-storage-manage-with-storage-explorer
10) Bootstrap 5 documentation (styling) – https://getbootstrap.com/docs/5.3/getting-started/introduction/
*/


namespace ABC_Retail
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllersWithViews();

            var storageConnectionString = builder.Configuration.GetConnectionString("storageConnectionString")
                ?? throw new InvalidOperationException("Storage connection string is missing");

            // Storage resource names (create these in Azure)
            const string customerTable = "Customers";
            const string productTable = "Products";
            const string productImages = "productimages";   // blob container
            const string orderQueue = "inventory-events";
            const string contractShare = "contracts";

            // DI registrations (Table/Blob/Queue/File shares)
            builder.Services.AddSingleton(new TableStorageService<Customer>(storageConnectionString, customerTable));
            builder.Services.AddSingleton(new TableStorageService<Product>(storageConnectionString, productTable));
            builder.Services.AddSingleton(new BlobStorageService(storageConnectionString, productImages));
            builder.Services.AddSingleton(new QueueStorageService(storageConnectionString, orderQueue));
            builder.Services.AddSingleton(new FileShareStorageService(storageConnectionString, contractShare));
            builder.Services.AddSingleton(new TableStorageService<Order>(storageConnectionString, "Orders"));
            builder.Services.AddHttpClient<FunctionClient>();


            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
