using System.Reflection;
using System.Runtime.InteropServices;
using Eva.AuthorityServer.System;
using Eva.Commons.Util;
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

EvaLogger.Init("Node");
var log = EvaLogger.CreateLogger<Program>();
log.LogInformation("Initializing Eva Node (.NET)...");
log.LogInformation("Current Runtime: {}", RuntimeInformation.FrameworkDescription);
using var templateStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Eva.Node.node.default.yml");

Configuration.Init(nodeConfigPath, configOverride, templateStream);
