namespace Eva.AuthorityServer.User;

public class UserEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Code { get; set; } = null!;
    public ICollection<string> Authorizations { get; set; } = new List<string>();
    
}