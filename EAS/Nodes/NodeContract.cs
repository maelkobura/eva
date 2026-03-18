namespace Eva.AuthorityServer.Nodes;

public record NodeContract(string Name, string[] Authorization, string Token, string Host, int Port);