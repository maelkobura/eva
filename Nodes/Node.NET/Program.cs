using Eva.Commons.Util;
using Microsoft.Extensions.Logging;
using Mono.Options;

string nodeConfigPath = "node.yml";

var options = new OptionSet {
    { "nc|nodeconfig=", "Node config path", n => nodeConfigPath = n }
};

EvaLogger.Init("Node");
var log = EvaLogger.CreateLogger<Program>();
log.LogInformation("Initializing Eva Node (.NET)...");