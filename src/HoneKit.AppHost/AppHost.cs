using System.Runtime.InteropServices;
using Aspire.Hosting.Publishing;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Aspire's compose publisher builds linux/amd64 by default. On an arm64 machine that puts the
// .NET services under emulation, which is slow and crashed the runtime here. Build for the host;
// for another architecture, build on a host of that architecture.
var localTargetPlatform = RuntimeInformation.ProcessArchitecture == Architecture.Arm64
    ? ContainerTargetPlatform.LinuxArm64
    : ContainerTargetPlatform.LinuxAmd64;

// The compose environment is the deployment target; the AppHost stays the single source of truth.
builder.AddDockerComposeEnvironment("compose");

// Aspire 13.6.4 exposes AddProject<T>() (project model V1); there is no AddDotnetProject() in this
// package. The generated Projects.* metadata classes drive the typed overload for all five services.
// The volume mounts on the app user's home directory: a fresh named volume inherits that
// directory's ownership from the image, so the non-root container can write its SQLite file.
// Mounting /data instead would hand the service a root-owned directory it cannot create in.
var content = builder.AddProject<Projects.HoneKit_Content>("content", launchProfileName: "http")
    .WithVolume("content-data", target: "/home/app", env: "DATA_DIR")
    .WithContainerBuildOptions(options => options.TargetPlatform = localTargetPlatform);

var progress = builder.AddProject<Projects.HoneKit_Progress>("progress", launchProfileName: "http")
    .WithVolume("progress-data", target: "/home/app", env: "DATA_DIR")
    .WithContainerBuildOptions(options => options.TargetPlatform = localTargetPlatform)
    .WithReference(content)
    .WaitFor(content);

var roster = builder.AddProject<Projects.HoneKit_Roster>("roster", launchProfileName: "http")
    .WithVolume("roster-data", target: "/home/app", env: "DATA_DIR")
    .WithContainerBuildOptions(options => options.TargetPlatform = localTargetPlatform)
    .WithReference(content)
    .WaitFor(content)
    .WithReference(progress)
    .WaitFor(progress);

var requirements = builder.AddProject<Projects.HoneKit_Requirements>("requirements", launchProfileName: "http")
    .WithContainerBuildOptions(options => options.TargetPlatform = localTargetPlatform);

var recommendations = builder.AddProject<Projects.HoneKit_Recommendations>("recommendations", launchProfileName: "http")
    .WithContainerBuildOptions(options => options.TargetPlatform = localTargetPlatform);

// Hand-written nginx image: PublishAsStaticWebsite only proxies one backend, so the frontend is a
// separate container. The Dockerfile is owned by the frontend agent; this only references its path.
builder.AddDockerfile("frontend", "../frontend")
    .WithContainerBuildOptions(options => options.TargetPlatform = localTargetPlatform)
    .WithHttpEndpoint(targetPort: 80, port: 8080, name: "http")
    .WithExternalHttpEndpoints()
    .WithReference(content)
    .WithReference(progress)
    .WithReference(roster)
    .WithReference(requirements)
    .WithReference(recommendations)
    .WaitFor(content)
    .WaitFor(progress)
    .WaitFor(roster)
    .WaitFor(requirements)
    .WaitFor(recommendations);

builder.Build().Run();
