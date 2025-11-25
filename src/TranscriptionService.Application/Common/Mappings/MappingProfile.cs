using AutoMapper;
using TranscriptionService.Application.DTOs;
using TranscriptionService.Domain.Entities;
using TranscriptionService.Domain.ValueObjects;

namespace TranscriptionService.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // TranscriptionJob to JobStatusDto
        CreateMap<TranscriptionJob, JobStatusDto>()
            .ForMember(dest => dest.JobId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.AudioFile.FileName))
            .ForMember(dest => dest.FileSize, opt => opt.MapFrom(src => src.AudioFile.GetFormattedSize()))
            .ForMember(dest => dest.Model, opt => opt.MapFrom(src => src.Model.ToString()))
            .ForMember(dest => dest.ProcessingDuration, opt => opt.MapFrom(src => 
                src.GetProcessingDuration().HasValue 
                    ? FormatTimeSpan(src.GetProcessingDuration()!.Value) 
                    : null));

        // TranscriptionResult to TranscriptionResultDto
        CreateMap<TranscriptionResult, TranscriptionResultDto>()
            .ForMember(dest => dest.ProcessingDuration, opt => opt.MapFrom(src => FormatTimeSpan(src.ProcessingDuration)))
            .ForMember(dest => dest.TotalAudioDuration, opt => opt.MapFrom(src => FormatTimeSpan(src.TotalAudioDuration)));

        // TranscriptionSegment to TranscriptionSegmentDto
        CreateMap<TranscriptionSegment, TranscriptionSegmentDto>()
            .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => FormatTimeSpan(src.StartTime)))
            .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => FormatTimeSpan(src.EndTime)));
    }

    private static string FormatTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
            return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        
        return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
    }
}
