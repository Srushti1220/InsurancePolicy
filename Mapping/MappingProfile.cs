using AutoMapper;
using InsuranceAPI.Models;
using InsuranceAPI.DTOs;

namespace InsuranceAPI.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<CreateUserDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());
            CreateMap<UpdateUserDto, User>();

            CreateMap<PolicyType, PolicyTypeDto>();
            CreateMap<CreatePolicyTypeDto, PolicyType>();
            CreateMap<UpdatePolicyTypeDto, PolicyType>();

            CreateMap<Policy, PolicyDto>()
                .ForMember(dest => dest.PolicyTypeName, opt => opt.MapFrom(src => src.PolicyType != null ? src.PolicyType.Name : string.Empty))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.FullName : string.Empty))
                .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Email : string.Empty));
            CreateMap<CreatePolicyDto, Policy>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ParsePolicyStatus(src.Status)));

            CreateMap<Payment, PaymentDto>()
                .ForMember(dest => dest.PolicyNumber, opt => opt.MapFrom(src => src.Policy != null ? src.Policy.PolicyNumber : string.Empty))
                .ForMember(dest => dest.PolicyTypeName, opt => opt.MapFrom(src => src.Policy != null && src.Policy.PolicyType != null ? src.Policy.PolicyType.Name : string.Empty))
                .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.Policy != null ? src.Policy.CustomerId : 0))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Policy != null && src.Policy.Customer != null ? src.Policy.Customer.FullName : string.Empty));

            CreateMap<Claim, ClaimDto>()
                .ForMember(dest => dest.PolicyNumber, opt => opt.MapFrom(src => src.Policy != null ? src.Policy.PolicyNumber : string.Empty))
                .ForMember(dest => dest.PolicyTypeName, opt => opt.MapFrom(src => src.Policy != null && src.Policy.PolicyType != null ? src.Policy.PolicyType.Name : string.Empty))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.FullName : string.Empty));
        }

        private static InsuranceAPI.Models.Enums.PolicyStatus ParsePolicyStatus(string status)
        {
            return System.Enum.TryParse<InsuranceAPI.Models.Enums.PolicyStatus>(status, true, out var result) 
                ? result 
                : InsuranceAPI.Models.Enums.PolicyStatus.Pending;
        }
    }
}
