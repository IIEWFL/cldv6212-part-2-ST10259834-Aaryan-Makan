using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();

// Resource names (must exist in your storage account)
const string ORDERS_TABLE = "orders";
const string ORDERS_QUEUE = "order-commands";
const string BLOBS_CONTAINER = "orderdocs";
const string FILES_SHARE = "orderfiles";

var cs = builder.Configuration.GetValue<string>("storageConnectionString");
if (string.IsNullOrWhiteSpace(cs))
    throw new InvalidOperationException("Missing storageConnectionString");

// Register services
builder.Services.AddSingleton(sp => new OrderTableService(cs, ORDERS_TABLE));
builder.Services.AddSingleton(sp => new QueueService(cs, ORDERS_QUEUE));
builder.Services.AddSingleton(sp => new BlobService(cs, BLOBS_CONTAINER));
builder.Services.AddSingleton(sp => new FileShareService(cs, FILES_SHARE));

builder.Build().Run();
