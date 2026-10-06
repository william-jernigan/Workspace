using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OneExchangeOrderService.Domain.Entities.Exchange;

namespace OneExchangeOrderService.Infrastructure.Persistance.Configurations.Exchange
{
    public partial class ProgramConfiguration : IEntityTypeConfiguration<Program>
    {
        public void Configure(EntityTypeBuilder<Program> builder)
        {
            builder.HasKey(e => e.Id)
               .HasName("pk_program")
               .IsClustered(false);

            builder.ToTable("program", "exchange");

            builder.HasIndex(e => e.ClustRowId, "IX_program__clustRowId").IsUnique();

            builder.HasIndex(e => e.ClustRowId, "ix_program_clustRowId").IsClustered();

            builder.HasIndex(e => new { e.TenantId, e.ProgramCode }, "uk_program_code").IsUnique();

            builder.Property(e => e.Id)
                .HasDefaultValueSql("newId()")
                .HasColumnName("_id");
            builder.Property(e => e.ClustRowId)
                .ValueGeneratedOnAdd()
                .HasColumnName("_clustRowId");
            builder.Property(e => e.InsertedBy)
                .HasMaxLength(50)
                .HasColumnName("_insertedBy");
            builder.Property(e => e.InsertedDateTime)
                .HasDefaultValueSql("getDate()")
                .HasColumnName("_insertedDateTime");
            builder.Property(e => e.ProgramCode)
                .HasMaxLength(10)
                .HasColumnName("programCode");
            builder.Property(e => e.ProgramName)
                .HasMaxLength(50)
                .HasColumnName("programName");
            builder.Property(e => e.Status)
                .HasDefaultValue(true)
                .HasColumnName("_status");
            builder.Property(e => e.StatusReason)
                .HasMaxLength(100)
                .HasColumnName("_statusReason");
            builder.Property(e => e.TenantId).HasColumnName("tenantId");
            builder.Property(e => e.UpdatedBy)
                .HasMaxLength(50)
                .HasColumnName("_updatedBy");
            builder.Property(e => e.UpdatedDateTime).HasColumnName("_updatedDateTime");

            builder.HasOne(d => d.Tenant).WithMany(p => p.Programs)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_program_tenant");
        }
    }
}
