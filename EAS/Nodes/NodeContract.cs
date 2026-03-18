namespace Eva.AuthorityServer.Nodes;

public record NodeContract(string Name, string[] Authorization, string token, string host, int port);