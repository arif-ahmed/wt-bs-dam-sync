using BrandshareDamSync.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Globalization;
using System.Reflection.Emit;

namespace BrandshareDamSync.Infrastructure.Persistence.Data.EntityConfigurations;

public class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> builder)
    {
        builder.ToTable("Folders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(x => x.Path)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(x => x.ParentId)
            .HasColumnType("TEXT");

        builder.Property(x => x.Label)
            .HasColumnType("TEXT");

        // Store as ISO-8601 TEXT
        var modifiedAtConverter = new ValueConverter<DateTimeOffset?, string?>(
            v => v.HasValue ? v.Value.ToUniversalTime().ToString("O") : null,
            v => string.IsNullOrWhiteSpace(v) ? (DateTimeOffset?)null
                                              : DateTimeOffset.Parse(v!, null, DateTimeStyles.RoundtripKind)
        );

        builder.Property(x => x.ModifiedAt)
            .HasColumnType("TEXT")
            .HasConversion(modifiedAtConverter);

        builder.Property(x => x.LastSeenSyncId)
            .HasColumnType("TEXT");

        // Indexes
        builder.HasIndex(x => x.Path)
            .HasDatabaseName("IX_Folders_Path");

        builder.HasIndex(x => x.ParentId)
            .HasDatabaseName("IX_Folders_ParentId");

        // Optional self-referencing FK (comment this block out if you do not want a DB FK)
        //builder.HasOne(x => x.Parent)
        //    .WithMany(p => p.Children)
        //    .HasForeignKey(x => x.ParentId)
        //    .OnDelete(DeleteBehavior.Restrict);
    }
}
