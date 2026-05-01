var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.RagNet_Samples_WebApi>("api");

builder.Build().Run();
