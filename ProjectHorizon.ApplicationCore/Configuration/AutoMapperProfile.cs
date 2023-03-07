using AutoMapper;
using ProjectHorizon.ApplicationCore.Deployment;
using ProjectHorizon.ApplicationCore.DTOs;
using ProjectHorizon.ApplicationCore.Entities;

namespace ProjectHorizon.ApplicationCore.Configuration
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<PackageConfigurationContent, ApplicationDto>();
            CreateMap<ApplicationDto, PublicApplicationDto>();

            CreateMap<PublicApplication, PublicApplicationDto>();
            CreateMap<PublicApplicationDto, PublicApplication>()
                .ForMember(pa => pa.Id, config => config.Ignore())
                .ForMember(pa => pa.CreatedOn, config => config.Ignore())
                .ForMember(pa => pa.ModifiedOn, config => config.Ignore());
            CreateMap<PublicApplication, PublicApplication>()
                .ForMember(pa => pa.Id, config => config.Ignore())
                .ForMember(pa => pa.CreatedOn, config => config.Ignore())
                .ForMember(pa => pa.ModifiedOn, config => config.Ignore());

            CreateMap<PrivateApplication, PrivateApplicationDto>();
            CreateMap<PrivateApplicationDto, PrivateApplication>()
                .ForMember(pa => pa.Id, config => config.Ignore())
                .ForMember(pa => pa.CreatedOn, config => config.Ignore())
                .ForMember(pa => pa.ModifiedOn, config => config.Ignore())
                .ForMember(paDto => paDto.DeploymentStatus, config => config.Ignore())
                .ForMember(paDto => paDto.IntuneId, config => config.Ignore())
                .ForMember(paDto => paDto.DeployedVersion, config => config.Ignore())
                .ForMember(paDto => paDto.IsInShop, config => config.Ignore());
            CreateMap<PrivateApplication, PrivateApplication>();

            CreateMap<RegistrationDto, ApplicationUser>();
            CreateMap<RegistrationDto, Subscription>();
            CreateMap<ApplicationUser, UserDto>();
            CreateMap<UserInvitationDto, ApplicationUser>();
            CreateMap<UserInvitationDto, UserInvitation>();

            CreateMap<Subscription, SubscriptionDto>()
                .Include<Subscription, UserSubscriptionDto>()
                .ForMember(dto => dto.AzureIntegrationDone, config => config.MapFrom(entity => entity.GraphConfigId != null));
            CreateMap<Subscription, UserSubscriptionDto>();
            CreateMap<SubscriptionUser, UserSubscriptionDto>()
                .IncludeMembers(subscriptionUser => subscriptionUser.Subscription);

            CreateMap<Approval, ApprovalDto>();
            CreateMap<AuditLog, AuditLogDto>()
                .ForMember(a => a.User, config =>
                    config.MapFrom(a => (a.ApplicationUser == null || a.ApplicationUser.IsSuperAdmin) ? "Endpoint Admin" : a.ApplicationUser.FullName));
            CreateMap<Notification, NotificationDto>();
            CreateMap<NotificationSettingDto, NotificationSetting>();

            CreateMap<NewAssignmentProfileDto, AssignmentProfile>()
                .ForMember(pa => pa.CreatedOn, config => config.Ignore())
                .ForMember(pa => pa.ModifiedOn, config => config.Ignore());
            CreateMap<AssignmentProfile, AssignmentProfileDto>();
            CreateMap<AssignmentProfileGroupDto, AssignmentProfileGroup>()
                .ForMember(pa => pa.CreatedOn, config => config.Ignore())
                .ForMember(pa => pa.ModifiedOn, config => config.Ignore());
            CreateMap<AssignmentProfileGroup, AssignmentProfileGroupDto>();
            CreateMap<AssignmentProfile, AssignmentProfileDetailsDto>();
            CreateMap<AssignmentProfileGroup, AssignmentProfileGroupDetailsDto>();
            CreateMap<AzureGroupDto, AssignmentProfileGroupDetailsDto>();

            CreateMap<Application, ShoppingApplicationDto>();
            CreateMap<Application, ShoppingApplicationDetailsDto>();

            CreateMap<ShoppingRequest, ShoppingRequestDto>()
                .ForMember(dto => dto.RequesterName, config => config.MapFrom(entity => entity.RequesterName))
                .ForMember(dto => dto.ResolverName,
                    config => config.MapFrom(entity => entity.AdminResolver == null ? entity.ManagerResolverName : entity.AdminResolver.FullName));

            CreateMap<PrivateShoppingRequest, ShoppingRequestDto>()
                .IncludeBase<ShoppingRequest, ShoppingRequestDto>()
                .ForMember(dto => dto.ApplicationName, config => config.MapFrom(entity => entity.PrivateApplication.Name));

            CreateMap<PublicShoppingRequest, ShoppingRequestDto>()
                .IncludeBase<ShoppingRequest, ShoppingRequestDto>()
                .ForMember(dto => dto.ApplicationName, config => config.MapFrom(entity => entity.SubscriptionPublicApplication.PublicApplication.Name));

            CreateMap<DeploymentSchedule, DeploymentScheduleDto>();
            CreateMap<DeploymentSchedule, DeploymentScheduleDetailsDto>();
            CreateMap<DeploymentScheduleDetailsDto, DeploymentSchedule>();

            CreateMap<DeploymentSchedulePhase, DeploymentSchedulePhaseDto>()
                  .ForMember(dto => dto.AssignmentProfileName, config => config.MapFrom(entity => entity.AssignmentProfile == null ? null : entity.AssignmentProfile.Name));
            CreateMap<DeploymentSchedulePhaseDto, DeploymentSchedulePhase>();

            CreateMap<SubscriptionConsent, SubscriptionConsentDto>();
            CreateMap<SubscriptionConsentDto, SubscriptionConsent>();
        }
    }
}