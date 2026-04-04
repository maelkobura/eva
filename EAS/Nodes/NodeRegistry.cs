using Eva.Commons.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Eva.AuthorityServer.Nodes;

internal class InternalNodeRegistry : INodeRegistry
{
    private static ILogger logger = EvaLogger.CreateLogger<InternalNodeRegistry>();

    public string NodeDir { get; private set; }
    public List<InternalNodeContract> NodeContracts { get; private set; } = new();

    public InternalNodeRegistry(string nodeDir)
    {
        NodeDir = nodeDir;
        Directory.CreateDirectory(NodeDir);
        LoadNodes();
    }

    public void LoadNodes()
    {
        if (NodeContracts.Count == 0)
        {
            logger.LogInformation("Loading nodes contract from directory: {NodeDir}", NodeDir);
        }
        else
        {
            logger.LogInformation("Reloading nodes contract from directory: {NodeDir}", NodeDir);
            NodeContracts.Clear();
        }

        var files = Directory.EnumerateFiles(NodeDir, "*.node");

        int success = 0;
        int failed = 0;

        foreach (var file in files)
        {
            try
            {
                LoadNodeContract(file);
                success++;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to load node contract: {File}", Path.GetFileName(file));
                failed++;
            }
        }

        logger.LogInformation("Loaded {Success} node contracts, {Failed} failed.", success, failed);
    }

    private void LoadNodeContract(string file)
    {
        logger.LogInformation("Loading node contract: {File}", Path.GetFileName(file));

        string json = File.ReadAllText(file);
        if (string.IsNullOrEmpty(json))
        {
            throw new Exception("Node contract is empty");
        }

        InternalNodeContract? contract = JsonConvert.DeserializeObject<InternalNodeContract>(json);
        if (contract == null)
        {
            throw new Exception("Failed to parse node contract");
        }

        NodeContracts.Add(contract);
    }

    public void CreateContract(string name, string[] authorization, string host, int port)
    {
        string file = Path.Combine(NodeDir, $"{name}.node");

        string token = Base64.GenerateToken();

        InternalNodeContract contract = new(name, authorization, token, host, port);

        if (NodeContracts.Any(c => c.Name == name) || File.Exists(file))
        {
            throw new Exception($"Node contract with '{name}' already exists.");
        }

        string json = JsonConvert.SerializeObject(contract);

        File.WriteAllText(file, json);
        logger.LogInformation("Created node contract: {Name}", name);

        LoadNodeContract(file);
    }

    public InternalNodeContract? GetContractByName(string name)
    {
        return NodeContracts.FirstOrDefault(c => c.Name == name);
    }

    public InternalNodeContract GetContractByNameAndValidate(string name, string token)
    {
        var contract = GetContractByName(name);

        if (contract == null)
            throw new Exception("Node contract not found");

        if (contract.Token != token)
            throw new Exception("Node contract doesn't match");

        return contract;
    }

    public void Dispose(){}
}