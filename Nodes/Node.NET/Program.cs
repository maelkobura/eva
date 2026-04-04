using System.Reflection;
using System.Runtime.InteropServices;
using Eva.AuthorityServer.System;
using Eva.Commons.System;
using Eva.Commons.Util;
using Eva.Node.Authority;
using Eva.Node.Authority.Certificate;
using Eva.Node.Loader;
using Eva.Node.Network;
using Eva.Node.Network.Discover;
using Eva.Node.Service;
using Eva.Node.Service.Functions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mono.Options;

string nodeConfigPath = "node.yml";
string serviceBinaryPath = "node.dll";
var configOverride = new Dictionary<string, string>();

var options = new OptionSet {
    { "nc|nodeconfig=", "Node config path", n => nodeConfigPath = n },
    { "b|bin=", "Eva Service binary path", n => serviceBinaryPath = n },
    { "p|prop=:", "Property key value", (key, value) =>
        {
            configOverride[key] = value;
        }
    }
};

options.Parse(args);

EvaLogger.Init("Node");
var log = EvaLogger.CreateLogger<Program>();
log.LogInformation("Initializing Eva Node (.NET)...");
log.LogInformation("Current Runtime: {}", RuntimeInformation.FrameworkDescription);
using var templateStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Eva.Node.node.default.yml");

Configuration.Init(nodeConfigPath, configOverride, templateStream);

EvaSystem.AddSingleton<IAssemblyLoader, InternalAssemblyLoader>(serviceBinaryPath);
var description = EvaSystem.Singleton<IAssemblyLoader>().LoadDescription();
log.LogInformation("Service Information: {DisplayName} ({Name}) - Ver {Version}", description.DisplayName, description.Name, description.Version);
log.LogInformation("Service Description: {Description}", description.Description);
log.LogInformation("Service Author: {Author}", description.Author);
log.LogInformation("Service License: {License}", description.License);
log.LogInformation("Service Authorization Count: {Count} authorizations", description.Authorization?.Length ?? 0);

EvaSystem.AddSingleton<IAuthorityClient, InternalAuthorityClient>(
    Configuration.Content.GetSection("eas:main").Get<AuthorityConnectionInfo>(), 
    Configuration.Content.GetSection("eas:backup").Get<AuthorityConnectionInfo>());

EvaSystem.AddSingleton<ICertificateManager, InternalCertificateManager>(Configuration.Content.GetSection("authentification:token").Get<string>());
EvaSystem.Singleton<ICertificateManager>().GenerateEvaCertificate(description.Name);
EvaSystem.Singleton<ICertificateManager>().GenerateTlsCertificate();

EvaSystem.AddSingleton<INodeDiscover, InternalNodeDiscover>(description);
EvaSystem.AddSingleton<INetworkNodeManager, InternalNetworkNodeManager>();

EvaSystem.AddSingleton<INetworkManager, InternalNetworkManager>();
EvaSystem.Singleton<INetworkManager>().Start();
EvaSystem.Singleton<INodeDiscover>().Discover(true);

FunctionRegistry.Init();

ServiceLoader.Init(description);
ServiceLoader.Instance!.LoadService();

Console.ReadKey(true);

