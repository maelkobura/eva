using Eva.Commons.Util;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Eva.AuthorityServer.Nodes;

public class NodeRegistry
{
    public static NodeRegistry? Instance { get; private set; }
    private static ILogger logger = EvaLogger.CreateLogger<NodeRegistry>();
    
    public string NodeDir { get; private set; } = "Contract";
    public List<NodeContract> NodeContracts { get; private set; } = new List<NodeContract>();
    
    public static void Init(string nodeDir)
    {
        if (Instance != null) return;
        Instance = new NodeRegistry(nodeDir);
    }
    
    private NodeRegistry(string nodeDir)
    {
        logger.LogInformation("Initializing NodeRegistry...");
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
                logger.LogError(e, "Failed to load node contract: {File}", file.Split('/').Last());
                failed++;
            }
        }
        
        logger.LogInformation("Loaded {Success} node contracts, {Failed} failed.", success, failed);
    }
    
    private void LoadNodeContract(string file)
    {
        logger.LogInformation("Loading node contract: {File}", file.Split('/').Last());
        string json = File.ReadAllText(file);
        if (string.IsNullOrEmpty(json))
        {
            throw new Exception("Node contract is empty");
        }
        NodeContract? contract = JsonConvert.DeserializeObject<NodeContract>(json);
        if (contract == null)
        {
            throw new Exception("Failed to parse node contract");
        }
        NodeContracts.Add(contract);
    }

    public void CreateContract(string Name, string[] Authorization, string host, int port)
    {
        string file = Path.Combine(NodeDir, $"{Name}.node");
        
        string token = Base64.GenerateToken();
        
        NodeContract contract = new NodeContract(Name, Authorization, token, host, port);
        if (NodeContracts.Select(c => c.Name).Contains(Name) ||
           File.Exists(file))
        {
            throw new Exception($"Node contract with '{Name}' already exists.");
        }
        
        string json = JsonConvert.SerializeObject(contract);
        
        File.WriteAllText(file, json);
        logger.LogInformation("Created node contract: {Name}", Name);
        LoadNodeContract(file);
    }

    public NodeContract? GetContractByName(string name)
    {
        return NodeContracts.FirstOrDefault(c => c.Name == name);
    }

    public NodeContract GetContractByNameAndValidate(string name, string token)
    {
        var contract = GetContractByName(name);
        if(contract == null) throw new Exception("Node contract not found");
        if(contract.Token != token) throw new Exception("Node contract doesn't match");
        return contract;
    }
}