using Eva.AuthorityServer.System;
using Eva.Commons.Util;
using Microsoft.Extensions.Logging;

namespace Eva.AuthorityServer.User;

public class InternalUserAuthenticator : IUserAuthenticator {
    private static ILogger logger = EvaLogger.CreateLogger<InternalUserAuthenticator>();
    
    private UserDatabaseContext context;
    
    public InternalUserAuthenticator()
    {
        string host = Configuration.Content["database:userAuthentification:host"] ?? "localhost";
        string port = Configuration.Content["database:userAuthentification:port"] ?? "5432";
        string database = Configuration.Content["database:userAuthentification:database"] ?? "evauser";
        string user = Configuration.Content["database:userAuthentification:user"] ?? "eas";
        string password = Configuration.Content["database:userAuthentification:password"] ?? "eas123";
        
        string dbConnection = $"Host={host};Port={port};Database={database};Username={user};Password={password}"; //TODO: Change for config
        
        context = new UserDatabaseContext(dbConnection);
        context.Database.EnsureCreated();
        if(context.Users.Count()==0)
        {
            logger.LogInformation("No users found, creating default user...");
            
            var defUser = new UserEntity
            {
                Username = "eva.user",
                Code = "000000", //TODO PBKDF2
                Authorizations = new List<string>() {"*"}
            };
            context.Users.Add(defUser);
            context.SaveChanges();
            logger.LogInformation("Default user created.");
        }
    }
    
    public UserEntity Login(string username, string code)
    {
        context.Database.EnsureCreated();
        
        //TODO PBKDF2
        var user = context.Users.FirstOrDefault(u => u.Username == username);
        if (user == null) throw new Exception("User not found.");
        if (user.Code != code) throw new Exception("Invalid code.");
        return user;
    }

    public void Dispose()
    {
        context.Dispose();
    }
}