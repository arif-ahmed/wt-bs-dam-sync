using BrandshareDamSync.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace BrandshareDamSync.Infrastructure.Persistence.Data.EntityConfigurations;

public class FileEntityConfiguration : IEntityTypeConfiguration<FileEntity>
{
    public void Configure(EntityTypeBuilder<FileEntity> builder)
    {
        builder.ToTable("Files");

        builder.HasOne(f => f.Folder)
            .WithMany(d => d.Files)
            .HasForeignKey(f => f.DirectoryId); // rename to FolderId first if you can
    }
}
