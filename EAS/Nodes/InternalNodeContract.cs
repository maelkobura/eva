namespace Eva.AuthorityServer.Nodes;

public record InternalNodeContract(string Name, string[] Authorization, string Token, string Host, int Port);