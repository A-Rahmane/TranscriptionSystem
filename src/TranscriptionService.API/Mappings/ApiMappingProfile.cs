using AutoMapper;
using TranscriptionService.Application.DTOs;
using TranscriptionService.Contracts.Http.Responses;

namespace TranscriptionService.API.Mappings;

public class ApiMappingProfile : Profile
{
    public ApiMappingProfile()
    {
        CreateMap<JobStatusDto, JobStatusResponse>();
        CreateMap<TranscriptionResultDto, TranscriptionResultResponse>();
        CreateMap<TranscriptionSegmentDto, TranscriptionSegmentResponse>();
    }
}
