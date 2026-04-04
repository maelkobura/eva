namespace Eva.AuthorityServer.User;

public interface IUserAuthenticator : IDisposable{
    /// <summary>
    /// Attempts to log in a user by username and code.
    /// </summary>
    /// <param name="username">The user's username.</param>
    /// <param name="code">The user's password or access code.</param>
    /// <returns>The authenticated UserEntity.</returns>
    /// <exception cref="Exception">Thrown if the user is not found or the code is invalid.</exception>
    UserEntity Login(string username, string code);
}