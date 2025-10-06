using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;


/* 
1) .NET isolated worker guide – https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide
2) Create your first function in C# (.NET isolated) – https://learn.microsoft.com/azure/azure-functions/create-first-function-vs-code-csharp
3) HTTP trigger & bindings (isolated) – https://learn.microsoft.com/azure/azure-functions/functions-bindings-http-webhook-trigger?tabs=isolated-process
4) Azure Queue Storage trigger (isolated) – https://learn.microsoft.com/azure/azure-functions/functions-bindings-storage-queue-trigger?tabs=isolated-process%2Cin-process&pivots=programming-language-csharp
5) Poison messages for queue triggers – https://learn.microsoft.com/azure/azure-functions/functions-bindings-storage-queue-trigger?tabs=isolated-process#poison-messages
6) local.settings.json & local development – https://learn.microsoft.com/azure/azure-functions/functions-develop-local#local-settings-file
7) App settings & connection strings for Functions – https://learn.microsoft.com/azure/azure-functions/functions-app-settings
8) Azure Storage SDKs for .NET (overview) – https://learn.microsoft.com/azure/storage/common/storage-introduction#net-libraries
9) Monitor Functions with Application Insights – https://learn.microsoft.com/azure/azure-functions/functions-monitoring
10) Publish/deploy Functions from Visual Studio – https://learn.microsoft.com/azure/azure-functions/functions-deploy-vs
*/


var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();


const string ORDERS_TABLE = "orders";
const string ORDERS_QUEUE = "order-commands";
const string BLOBS_CONTAINER = "orderdocs";
const string FILES_SHARE = "orderfiles";

var cs = builder.Configuration.GetValue<string>("storageConnectionString");
if (string.IsNullOrWhiteSpace(cs))
    throw new InvalidOperationException("Missing storageConnectionString");


builder.Services.AddSingleton(sp => new OrderTableService(cs, ORDERS_TABLE));
builder.Services.AddSingleton(sp => new QueueService(cs, ORDERS_QUEUE));
builder.Services.AddSingleton(sp => new BlobService(cs, BLOBS_CONTAINER));
builder.Services.AddSingleton(sp => new FileShareService(cs, FILES_SHARE));



builder.Build().Run();
