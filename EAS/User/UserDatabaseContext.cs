using Microsoft.EntityFrameworkCore;

namespace Eva.AuthorityServer.User;

public class UserDatabaseContext : DbContext {
    
    private string sqlConnection;
    
    public DbSet<UserEntity> Users => Set<UserEntity>();

    public UserDatabaseContext(string sqlConnection)
    {
        this.sqlConnection = sqlConnection;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => optionsBuilder.UseNpgsql(sqlConnection);
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>()
            .Property(u => u.Authorizations)
            .HasColumnType("text[]");
    }


}