using AutoMapper;
using EvCoOwnership.Repositories.DTOs.VehicleDTOs;
using EvCoOwnership.Repositories.Models;
using Newtonsoft.Json;

namespace EvCoOwnership.Services.Mapping
{
    public class VehicleMappingProfile : Profile
    {
        public VehicleMappingProfile()
        {
            CreateMap<VehicleCreateRequest, Vehicle>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.StatusEnum, opt => opt.MapFrom(src => EvCoOwnership.Repositories.Enums.EVehicleStatus.Available))
                .ForMember(dest => dest.VerificationStatusEnum, opt => opt.MapFrom(src => EvCoOwnership.Repositories.Enums.EVehicleVerificationStatus.Pending))
                .ForMember(dest => dest.DistanceTravelled, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.Now));

            CreateMap<Vehicle, VehicleDetailResponse>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.StatusEnum))
                .ForMember(dest => dest.VerificationStatus, opt => opt.MapFrom(src => src.VerificationStatusEnum))
                .ForMember(dest => dest.VerificationHistory, opt => opt.MapFrom(src => src.VehicleVerificationHistories));

            CreateMap<VehicleVerificationHistory, VehicleVerificationResponse>()
                .ForMember(dest => dest.VehicleName, opt => opt.MapFrom(src => src.Vehicle != null ? src.Vehicle.Name : ""))
                .ForMember(dest => dest.LicensePlate, opt => opt.MapFrom(src => src.Vehicle != null ? src.Vehicle.LicensePlate : ""))
                .ForMember(dest => dest.StaffName, opt => opt.MapFrom(src => src.Staff != null ? $"{src.Staff.FirstName} {src.Staff.LastName}" : ""))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.StatusEnum))
                .ForMember(dest => dest.ImageUrls, opt => opt.MapFrom(src =>
                    !string.IsNullOrEmpty(src.Images) ? JsonConvert.DeserializeObject<List<string>>(src.Images) : new List<string>()));

            CreateMap<VehicleVerificationRequest, VehicleVerificationHistory>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.StatusEnum, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Images, opt => opt.MapFrom(src =>
                    src.ImageUrls != null && src.ImageUrls.Any() ? JsonConvert.SerializeObject(src.ImageUrls) : null))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.Now));
        }
    }
}