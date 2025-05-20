var builder = DistributedApplication.CreateBuilder(args);

var webapi = builder.AddProject<Projects.Telepathic_Web>("webapp");

builder.AddProject<Projects.Telepathic>("mauiapp")
    .WithReference(webapi);

builder.Build().Run();