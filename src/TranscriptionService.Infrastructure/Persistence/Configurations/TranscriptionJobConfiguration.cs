using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TranscriptionService.Domain.Entities;
using TranscriptionService.Domain.Enums;
using TranscriptionService.Domain.ValueObjects;

namespace TranscriptionService.Infrastructure.Persistence.Configurations;

public class TranscriptionJobConfiguration : IEntityTypeConfiguration<TranscriptionJob>
{
    public void Configure(EntityTypeBuilder<TranscriptionJob> builder)
    {
        builder.ToTable("TranscriptionJobs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Model)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Language)
            .HasMaxLength(10);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.QueuePosition)
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(500);

        builder.Property(x => x.ErrorDetails)
            .HasColumnType("text");

        // AudioFile value object
        builder.OwnsOne(x => x.AudioFile, audioFile =>
        {
            audioFile.Property(a => a.FileName)
                .HasColumnName("FileName")
                .HasMaxLength(255)
                .IsRequired();

            audioFile.Property(a => a.FilePath)
                .HasColumnName("FilePath")
                .HasMaxLength(500)
                .IsRequired();

            audioFile.Property(a => a.FileSizeBytes)
                .HasColumnName("FileSizeBytes")
                .IsRequired();

            audioFile.Property(a => a.Format)
                .HasColumnName("Format")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            audioFile.Property(a => a.DurationSeconds)
                .HasColumnName("DurationSeconds");

            audioFile.Property(a => a.SampleRate)
                .HasColumnName("SampleRate");
        });

        // TranscriptionResult owned entity
        builder.OwnsOne(x => x.Result, result =>
        {
            result.Property(r => r.FullText)
                .HasColumnName("ResultFullText")
                .HasColumnType("text");

            result.Property(r => r.DetectedLanguage)
                .HasColumnName("DetectedLanguage")
                .HasMaxLength(10);

            result.Property(r => r.AverageConfidence)
                .HasColumnName("AverageConfidence");

            result.Property(r => r.ProcessingDuration)
                .HasColumnName("ProcessingDuration");

            result.Property(r => r.CompletedAt)
                .HasColumnName("ResultCompletedAt");

            // Segments as JSON
            result.OwnsMany(r => r.Segments, segment =>
            {
                segment.ToTable("TranscriptionSegments");
                
                segment.WithOwner().HasForeignKey("TranscriptionJobId");

                segment.Property<int>("Id")
                    .ValueGeneratedOnAdd();

                segment.HasKey("Id");

                segment.Property(s => s.Index)
                    .IsRequired();

                segment.Property(s => s.StartTime)
                    .IsRequired();

                segment.Property(s => s.EndTime)
                    .IsRequired();

                segment.Property(s => s.Text)
                    .HasMaxLength(2000)
                    .IsRequired();

                segment.Property(s => s.Confidence)
                    .IsRequired();
            });
        });

        // Indexes
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => x.QueuePosition);
    }
}
