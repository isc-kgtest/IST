using IST.Core.Entities.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IST.Infrastructure.Data.EntityConfigurations.OrganizationEntityConfigurations;

public class OrganizationNodeEntityConfiguration : IEntityTypeConfiguration<OrganizationNodeEntity>
{
    public void Configure(EntityTypeBuilder<OrganizationNodeEntity> b)
    {
        b.ToTable("nodes", "org");
        b.HasKey(n => n.Id);

        b.Property(n => n.Name).HasMaxLength(256).IsRequired();
        b.Property(n => n.Code).HasMaxLength(64);
        b.Property(n => n.Description).HasMaxLength(1000);
        b.Property(n => n.Path).HasMaxLength(2048).IsRequired();

        // Самореферентная связь parent → children.
        b.HasOne(n => n.ParentNode)
            .WithMany(n => n.Children)
            .HasForeignKey(n => n.ParentNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Связь с типом узла.
        b.HasOne(n => n.NodeType)
            .WithMany(t => t.Nodes)
            .HasForeignKey(n => n.NodeTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(n => n.Path);
        b.HasIndex(n => n.ParentNodeId);
        b.HasIndex(n => n.NodeTypeId);
    }
}
