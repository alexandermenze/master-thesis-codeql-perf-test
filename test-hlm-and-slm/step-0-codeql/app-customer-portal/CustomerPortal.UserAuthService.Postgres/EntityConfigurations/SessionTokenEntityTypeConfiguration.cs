using CustomerPortal.UserAuthService.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CustomerPortal.UserAuthService.Postgres.EntityConfigurations;

public class SessionTokenEntityTypeConfiguration : IEntityTypeConfiguration<SessionToken>
{
    public void Configure(EntityTypeBuilder<SessionToken> builder)
    {
        builder.Property(s => s.Id).ValueGeneratedNever();
    }
}
