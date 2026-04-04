namespace Eva.AuthorityServer.Nodes;

public interface INodeRegistry : IDisposable
{
    /// <summary>
    /// Directory where node contract files are stored.
    /// </summary>
    string NodeDir { get; }

    /// <summary>
    /// Loaded node contracts.
    /// </summary>
    List<InternalNodeContract> NodeContracts { get; }

    /// <summary>
    /// Loads or reloads all node contracts from the directory.
    /// </summary>
    void LoadNodes();

    /// <summary>
    /// Creates a new node contract and persists it to disk.
    /// </summary>
    /// <param name="name">Node name.</param>
    /// <param name="authorization">List of authorizations.</param>
    /// <param name="host">Node host.</param>
    /// <param name="port">Node port.</param>
    void CreateContract(string name, string[] authorization, string host, int port);

    /// <summary>
    /// Retrieves a node contract by its name.
    /// </summary>
    /// <param name="name">Node name.</param>
    /// <returns>The matching NodeContract or null if not found.</returns>
    InternalNodeContract? GetContractByName(string name);

    /// <summary>
    /// Retrieves and validates a node contract using its name and token.
    /// </summary>
    /// <param name="name">Node name.</param>
    /// <param name="token">Node authentication token.</param>
    /// <returns>The validated NodeContract.</returns>
    /// <exception cref="Exception">Thrown if the contract is not found or invalid.</exception>
    InternalNodeContract GetContractByNameAndValidate(string name, string token);
}