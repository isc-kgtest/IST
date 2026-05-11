using IST.Core.Entities.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IST.Infrastructure.Data.EntityConfigurations.OrganizationEntityConfigurations;

public class OrganizationNodeTypeEntityConfiguration : IEntityTypeConfiguration<OrganizationNodeTypeEntity>
{
    public void Configure(EntityTypeBuilder<OrganizationNodeTypeEntity> b)
    {
        b.ToTable("node_types", "org");
        b.HasKey(t => t.Id);

        b.Property(t => t.Code).HasMaxLength(64).IsRequired();
        b.Property(t => t.Name).HasMaxLength(128).IsRequired();
        b.Property(t => t.Description).HasMaxLength(500);
        b.Property(t => t.Icon).HasMaxLength(64);

        b.HasIndex(t => t.Code).IsUnique().HasFilter("\"IsDeleted\" = false");
    }
}
