using IST.Core.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IST.Infrastructure.Data.EntityConfigurations.AuthEntityConfigurations;

public class SecurityAuditLogEntityConfiguration : IEntityTypeConfiguration<SecurityAuditLogEntity>
{
    public void Configure(EntityTypeBuilder<SecurityAuditLogEntity> builder)
    {
        builder.ToTable("security_audit_logs", "access_control");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnType("uuid");

        builder.Property(x => x.Timestamp).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(64).IsRequired();

        builder.Property(x => x.ActorUserId).HasColumnType("uuid");
        builder.Property(x => x.ActorLogin).HasMaxLength(128);
        builder.Property(x => x.TargetUserId).HasColumnType("uuid");
        builder.Property(x => x.TargetLogin).HasMaxLength(128);

        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.Message).HasMaxLength(512);

        builder.Property(x => x.DetailsJson).HasColumnType("jsonb");

        builder.HasIndex(x => new { x.EventType, x.Timestamp })
            .HasDatabaseName("ix_security_audit_logs_event_type_timestamp");
        builder.HasIndex(x => x.ActorUserId)
            .HasDatabaseName("ix_security_audit_logs_actor_user");
        builder.HasIndex(x => x.TargetUserId)
            .HasDatabaseName("ix_security_audit_logs_target_user");
        builder.HasIndex(x => x.Timestamp)
            .HasDatabaseName("ix_security_audit_logs_timestamp");
    }
}
