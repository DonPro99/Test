using System;
using System.Collections.Generic;
using Dms.Core.EntityFramework.Data;
using Dms.Core.EntityFramework.Model.Activity;
using Dms.Services.ViewModel;
using Dms.Services.ViewModel.Activity;
using Microsoft.EntityFrameworkCore;
using Dms.Services.ViewModel.Security;
using Dms.Core.Utils;
using Dms.Services.ViewModel.Shared;
using Dms.Core.EntityFramework.Model.Lookup;
using Dms.Services.ViewModel.Lookup;
using Dms.Core.Extensions;
using Dms.Services.ViewModel.Cloa;
using System.Linq;
using Dms.Services.Extensions;
using System.Dynamic;
using Newtonsoft.Json;
namespace Dms.Services.Assembler
{
    public static class CloaPreApprovalRequestViewModelMapper
    {
        public static IList<TypeRatingViewModel> TypeRatingTypes { get; set; } = new List<TypeRatingViewModel>();
        public static PreApprovalRequest GetPreApprovalRequestAfS(DmsContext context, int preApprovalRequestId)
        {  
            var preApprvoalRequest = context.PreApprovalRequests.Where(p => p.Id == preApprovalRequestId)
                                                                  .Include(p => p.PreApprovalRequestFunctionCodes).ThenInclude(p => p.FunctionCode)
                                                                  .Include(p => p.PreApprovalRequestFunctionCodes).ThenInclude(p => p.PreApprovalRequestFunctionCodeTypeRatings)
                                                                  .Include(p => p.PreApprovalRequestFunctionCodes).ThenInclude(p => p.PreApprovalRequestFunctionCodeMakeModels)
                                                                  .Include(p => p.PreApprovalRequestExperimentals)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.AircraftMakeMode)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.School)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.CfrSectionSchool)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.AirCarrier)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.Address)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.Address).ThenInclude(p => p.StateProvince)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.Address).ThenInclude(p => p.Country)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.AircraftOwnerAddress)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.AircraftOwnerAddress).ThenInclude(p => p.StateProvince)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.AircraftOwnerAddress).ThenInclude(p => p.Country)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.AircraftOperatorAddress)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.AircraftOperatorAddress).ThenInclude(p => p.StateProvince)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.AircraftOperatorAddress).ThenInclude(p => p.Country)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.Airport)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.PilotLicenseIssuedCountry)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.PreApprovalRequestCancellationType)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.PreApprovalObservationType)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.Simulator)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.PreApprovalRequestTypeOfCheck).ThenInclude(p => p.PracticalOralTestType)
                                                                  .Include(p => p.PreApprovalRequestCertificateRatings)
                                                                  .Include(p => p.PreApprovalRequestProducts)
                                                                  .Include(p => p.PreApprovalRequestStatus)
                                                                  .Include(p => p.ApproverOfficeRole).ThenInclude(p => p.User).ThenInclude(p => p.Profile)
                                                                  .Include(p => p.GeoExpansionSelectedApprover).ThenInclude(p => p.User).ThenInclude(p => p.Profile)                                                                  
                                                                  .Include(p => p.GeoExpansionSelectedOffice)
                                                                  .FirstOrDefault();

            return preApprvoalRequest;
        }

        public static PreApprovalRequest GetPreApprovalRequestAov(DmsContext context, int preApprovalRequestId)
        {  
             
            var preApprvoalRequest = context.PreApprovalRequests.Where(p => p.Id == preApprovalRequestId)
                                                                  .Include(p => p.PreApprovalRequestFunctionCodes).ThenInclude(p => p.FunctionCode) 
                                                                  .Include(p => p.AovPreApprovalRequest).ThenInclude(p => p.Address).ThenInclude(p => p.StateProvince)
                                                                  .Include(p => p.AovPreApprovalRequest).ThenInclude(p => p.Address).ThenInclude(p => p.Country)
                                                                  .Include(p => p.AovPreApprovalRequest).ThenInclude(p => p.Company)
                                                                  .Include(p => p.AovPreApprovalRequest).ThenInclude(p => p.PracticalOralTestType)                                                                                                                
                                                                  .Include(p => p.PreApprovalRequestStatus)
                                                                  .Include(p => p.ApproverOfficeRole).ThenInclude(p => p.User).ThenInclude(p => p.Profile)
                                                                  .FirstOrDefault();

            return preApprvoalRequest;
        }
        public static PreApprovalRequest GetPreApprovalRequestManufacturing(DmsContext context, int preApprovalRequestId)
        {
            var preApprvoalRequest = context.PreApprovalRequests.Where(p => p.Id == preApprovalRequestId)
                                                                  .Include(p => p.PreApprovalRequestFunctionCodes).ThenInclude(p => p.FunctionCode)
                                                                  .Include(p => p.PreApprovalRequestFunctionCodes).ThenInclude(p => p.PreApprovalRequestFunctionCodeTypeRatings)
                                                                  .Include(p => p.PreApprovalRequestExperimentals)
                                                                  .Include(p => p.OtherPreApprovalRequest).ThenInclude(p => p.Address)
                                                                  .Include(p => p.OtherPreApprovalRequest).ThenInclude(p => p.Address).ThenInclude(p => p.StateProvince)
                                                                  .Include(p => p.OtherPreApprovalRequest).ThenInclude(p => p.Office)
                                                                  .Include(p => p.OtherPreApprovalRequest).ThenInclude(p => p.Airport)
                                                                  .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.AircraftMakeMode)
                                                                  .Include(p => p.PreApprovalRequestCertificateRatings)
                                                                  .Include(p => p.PreApprovalRequestProducts)
                                                                  .Include(p => p.PreApprovalRequestStatus)
                                                                  .Include(p => p.ApproverOfficeRole).ThenInclude(p => p.User).ThenInclude(p => p.Profile)
                                                                  .Include(p => p.GeoExpansionSelectedApprover).ThenInclude(p => p.User).ThenInclude(p => p.Profile)
                                                                  .Include(p => p.PostActivities).ThenInclude(p => p.PostActivityPerformanceReview)
                                                                  .Include(p => p.PostActivities).ThenInclude(p => p.PostActivityProducts)
                                                                  .Include(p => p.PostActivities).ThenInclude(p => p.PostActivityCertificates)
                                                                  .Include(p => p.PostActivities).ThenInclude(p => p.PostActivityResultType)
                                                                  .Include(p => p.PostActivities).ThenInclude(p => p.AircraftMakeMode)
                                                                  .Include(p => p.PostActivities).ThenInclude(p => p.Address)
                                                                  .Include(p => p.PostActivities).ThenInclude(p => p.PostActivityFunctionCodes)
                                                                  .Include(p => p.PostActivities).ThenInclude(p => p.Address).ThenInclude(p => p.StateProvince)
                                                                  .Include(p => p.PostActivities).ThenInclude(p => p.Address).ThenInclude(p => p.Country)
                                                                  .Include(p => p.PostActivities).ThenInclude(p => p.Airport)
                                                                  .Include(p => p.PostActivities).ThenInclude(p => p.PostActivityModifiedDatas)
                                                                  .Include(p => p.PostActivities).ThenInclude(p => p.PostActivityApplicants)
                                                                  .Include(p => p.GeoExpansionSelectedOffice)
                                                                  .FirstOrDefault();
            return preApprvoalRequest;
        }
        public static CloaPreApprovalRequestViewModel GetManufacturingEntitytoViewModel(DmsContext context, PreApprovalRequest preApprvoalRequest)
        {
            List<FunctionCodeViewModel> selectedOtherFunctionCodes = preApprvoalRequest.PreApprovalRequestFunctionCodes.Any()
                                                     ? preApprvoalRequest.PreApprovalRequestFunctionCodes.Select(d => new FunctionCodeViewModel
                                                       {
                                                           Id = d.FunctionCode.Id,
                                                           FunctionCode = d.FunctionCode.Name,
                                                           HasTypeRating = false,
                                                           CategoryId = d.FunctionCode.CategoryId ?? 0,
                                                           Category = d.FunctionCode.Category != null ? new CategoryViewModel
                                                           {
                                                               Id = d.FunctionCode.Category.Id,
                                                               Name = d.FunctionCode.Category.Name
                                                           } : null,
                                                           TypeRatings = null,
                                                           FunctionCodeTypeId = d.FunctionCode.FunctionCodeTypeId,
                                                           TypeId = d.TypeId,
                                                           SortOrder = d.FunctionCode.SortOrder
                                                       }).OrderBy(x => x.SortOrder).ToList() : new List<FunctionCodeViewModel>();
            var _cloaPreApprovalRequestModel = context.Cloas.Where(c => c.Id == preApprvoalRequest.CloaId)
                                                   .Select(c => new
                                                   {
                                                       CloaFunctionCodes = c.CloaFunctionCodes,
                                                       Id = preApprvoalRequest.Id,
                                                       ApplicationId = preApprvoalRequest.ApplicationId,
                                                       ManagingSpecialist = c.ManagingSpecialistId.Value,
                                                       ManagingSpecialistName = c.MsUserOfficeRole.User.Profile.ToFullName(),
                                                       ManagingOffice = new BaseLookup() { Id = c.Application.Office.Id, Name = c.Application.Office.Name },
                                                       UserId = c.Application.User.Id,
                                                       DesigneeTypeId = c.Application.DesigneeTypeId,
                                                       OfficeId = c.Application.OfficeId,
                                                       IsAutoPostActivity = c.IsAutoPostActivity,
                                                       TimeZoneId = c.CloaAdjunct != null ? c.CloaAdjunct.TimeZoneId : null,
                                                       DesigneeInfo = new DesigneeViewModel
                                                       {
                                                           Name = c.ProfileVersion.ToFullName(),
                                                           Number = c.Application.User.UserSecurityInfo.DesigneeNumber,
                                                           Type = c.Application.DesigneeType.Code,
                                                           TypeId = c.Application.DesigneeTypeId.Value,
                                                           ExpirationDate = c.ExpirationDate.ToShortDateString(),
                                                           Company = c.Application.DesigneeTypeId == (int)DesigneeTypeEnum.DMIR && c.CloaAddresses.Any(a => a.Address.AddressTypeId == (int)AddressTypeEnum.ProductionApprovalHolder)
                                                                            ? c.CloaAddresses.First(a => a.Address.AddressTypeId == (int)AddressTypeEnum.ProductionApprovalHolder).Address.Name
                                                                            : null,
                                                           CloaId = c.Id,
                                                           ManagingSpecialistId = c.ManagingSpecialistId,
                                                           ApplicationId = c.ApplicationId,
                                                           Id = c.Application.User.Id
                                                       },
                                                       RequestInfo = new PreApprovalRequestInfoViewModel()
                                                       {
                                                           ControlNumber = preApprvoalRequest.TrackingNumber,
                                                           ActivityStatus = preApprvoalRequest.PreApprovalRequestStatus != null
                                                            && preApprvoalRequest.PreApprovalRequestStatus.Id == (int)PreApprovalRequestStatusEnum.Pending ?
                                                                                                       new BaseLookup() { Id = preApprvoalRequest.PreApprovalRequestStatus.Id, Name = "Submitted" } : preApprvoalRequest.PreApprovalRequestStatus,
                                                           IsApproved = (preApprvoalRequest.IsApproved.HasValue && preApprvoalRequest.IsApproved.Value) ? true : false
                                                       },
                                                       DesigneeFunctionCodes = c.CloaFunctionCodes.Where(f => f.FunctionCode.IsActive ||  
                                                                                                        (preApprvoalRequest.PreApprovalRequestStatus.Id == (int)PreApprovalRequestStatusEnum.Approved
                                                                                                        || preApprvoalRequest.PreApprovalRequestStatus.Id == (int)PreApprovalRequestStatusEnum.Pending)).Select(d => new FunctionCodeViewModel
                                                       {
                                                           Id = d.FunctionCode.Id,
                                                           FunctionCode = d.FunctionCode.Name,
                                                           HasTypeRating = d.FunctionCode.HasTypeRating,
                                                           CategoryId = d.FunctionCode.CategoryId ?? 0,
                                                           IsAutomaticPreapproval = d.IsAutoPreApproval,
                                                           IsAutoPostActivity = (c.Application.DesigneeTypeId == (int)DesigneeTypeEnum.DMIR || c.Application.DesigneeTypeId == (int)DesigneeTypeEnum.DARF) ? d.IsAutoPostActivity : false,
                                                           Category = d.FunctionCode.Category != null ? new CategoryViewModel
                                                           {
                                                               Id = d.FunctionCode.Category.Id,
                                                               Name = d.FunctionCode.Category.Name
                                                           } : null,
                                                           TypeRatings = d.CloaFunctionCodeTypeRatings != null && d.CloaFunctionCodeTypeRatings.Any() ? d.CloaFunctionCodeTypeRatings.Select(cf => new LookupItem
                                                           {
                                                               label = cf.TypeRating.Name,
                                                               value = cf.TypeRating.Id
                                                           }).ToArray() : null,
                                                           FunctionCodeTypeId = d.FunctionCode.FunctionCodeTypeId,
                                                           SortOrder = d.FunctionCode.SortOrder
                                                       }).OrderBy(x => x.SortOrder).ToList(),
                                                       FacilityAddress = c.CloaAddresses
                                                            .Where(ar => ar.Address.AddressTypeId == (int)AddressTypeEnum.WorkAddress)
                                                            .Select(add => new AddressViewModel
                                                            {
                                                                Id = add.Address.Id,
                                                                Name = add.Address.Name,
                                                                Address1 = add.Address.AddressLine1,
                                                                Address2 = add.Address.AddressLine2,
                                                                City = add.Address.City,
                                                                County = add.Address.County,
                                                                State = add.Address.StateId.HasValue ? new StateViewModel
                                                                {
                                                                    Id = add.Address.StateProvince.Id,
                                                                    Name = add.Address.StateProvince.Name
                                                                } : null,
                                                                Country = new CountryViewModel
                                                                {
                                                                    Id = add.Address.Country.Id,
                                                                    Name = add.Address.Country.Name
                                                                },
                                                                ZipCode = add.Address.ZipCode,
                                                                PhoneNumber = add.Address.Phone
                                                            }).FirstOrDefault(),
                                                       SelectedOtherFunctionCodes = selectedOtherFunctionCodes,
                                                       PreApprovalExperimentalPurposes = preApprvoalRequest.PreApprovalRequestExperimentals.Any()
                                                        ? preApprvoalRequest.PreApprovalRequestExperimentals.Select(e => new PreApprovalRequestExperimentalViewModel
                                                        {
                                                            PreapprovalRequestExperimentalCategoryTypeId = e.PreapprovalRequestExperimentalCategoryTypeId,
                                                            PreapprovalRequestExperimentalTypeId = e.PreApprovalRequestExperimentalTypeId,
                                                            OtherText = e.OtherText
                                                        }).ToList()
                                                        : new List<PreApprovalRequestExperimentalViewModel>()
                                                   }).FirstOrDefault();
            if (_cloaPreApprovalRequestModel == null)
            {
                return new CloaPreApprovalRequestViewModel();
            }

            _cloaPreApprovalRequestModel.SelectedOtherFunctionCodes
                                        .ForEach(f => f.IsAutomaticPreapproval =
                                              _cloaPreApprovalRequestModel.CloaFunctionCodes.Any(c => c.FunctionCodeId == f.Id)
                                                ? _cloaPreApprovalRequestModel.CloaFunctionCodes.First(c => c.FunctionCodeId == f.Id).IsAutoPreApproval
                                                : false
                                             );
             _cloaPreApprovalRequestModel.SelectedOtherFunctionCodes
                                         .ForEach(f => f.IsAutoPostActivity = 
                                                 _cloaPreApprovalRequestModel.CloaFunctionCodes.Any(c => c.FunctionCodeId == f.Id)
                                                ? _cloaPreApprovalRequestModel.CloaFunctionCodes.First(c => c.FunctionCodeId == f.Id).IsAutoPostActivity
                                                : false
                                            );                               
              var _cloaPreApprovalRequestViewModel = new CloaPreApprovalRequestViewModel {
                        Id = _cloaPreApprovalRequestModel.Id,
                        ApplicationId = _cloaPreApprovalRequestModel.ApplicationId,
                        ManagingSpecialist = _cloaPreApprovalRequestModel.ManagingSpecialist,
                        ManagingSpecialistName = _cloaPreApprovalRequestModel.ManagingSpecialistName,
                        ManagingOffice = _cloaPreApprovalRequestModel.ManagingOffice,
                        UserId = _cloaPreApprovalRequestModel.UserId,
                        DesigneeTypeId = _cloaPreApprovalRequestModel.DesigneeTypeId,
                        OfficeId = _cloaPreApprovalRequestModel.OfficeId,
                        IsAutoPostActivity = _cloaPreApprovalRequestModel.IsAutoPostActivity,
                        DesigneeInfo = _cloaPreApprovalRequestModel.DesigneeInfo,
                        RequestInfo = _cloaPreApprovalRequestModel.RequestInfo,
                        DesigneeFunctionCodes = _cloaPreApprovalRequestModel.DesigneeFunctionCodes,
                        FacilityAddress = _cloaPreApprovalRequestModel.FacilityAddress,
                        SelectedOtherFunctionCodes = _cloaPreApprovalRequestModel.SelectedOtherFunctionCodes,
                        PreApprovalExperimentalPurposes = _cloaPreApprovalRequestModel.PreApprovalExperimentalPurposes,
                        TimeZoneId = _cloaPreApprovalRequestModel.TimeZoneId.GetValueOrDefault((int)GlobalEnum.CentralTimeZoneId)
                };                                                                      

            return _cloaPreApprovalRequestViewModel;
        }
        public static CloaPreApprovalRequestViewModel GetAfsCloaEntitytoViewModel(DmsContext context, PreApprovalRequest preApprvoalRequest, int? cloaId = null)
        {
            cloaId = cloaId ?? preApprvoalRequest.CloaId;
           var latestCloaId = context.Cloas.Where(c => c.ApplicationId == preApprvoalRequest.ApplicationId).OrderByDescending(c1 => c1.Id)
                                                                                                           .Select(c1 => c1.Id).First();

           List<FunctionCodeViewModel> selectedOtherFunctionCodes = preApprvoalRequest.CloaId == cloaId && preApprvoalRequest.PreApprovalRequestFunctionCodes.Any()
                                                                        ? preApprvoalRequest.PreApprovalRequestFunctionCodes.Select(d => new FunctionCodeViewModel
                                                        {
                                                            Id = d.FunctionCode.Id,
                                                            FunctionCode = d.FunctionCode.Name,
                                                            HasTypeRating = false,
                                                            CategoryId = d.FunctionCode.CategoryId ?? 0,
                                                            Category = d.FunctionCode.Category != null ? new CategoryViewModel
                                                            {
                                                                Id = d.FunctionCode.Category.Id,
                                                                Name = d.FunctionCode.Category.Name
                                                            } : null,
                                                            TypeRatings = null,
                                                            FunctionCodeTypeId = d.FunctionCode.FunctionCodeTypeId,
                                                            TypeId = d.TypeId,
                                                            SortOrder = d.FunctionCode.SortOrder
                                                        }).OrderBy(x => x.SortOrder).ToList() : new List<FunctionCodeViewModel>();

            var _cloaPreApprovalRequestModel = context.Cloas.Where(c => c.Id == latestCloaId)
                                                    .Include(c => c.Designator)
                                                    .Include(c => c.Application).ThenInclude(c => c.User).ThenInclude(c => c.Profile)
                                                    .Include(c => c.Application).ThenInclude(c => c.User).ThenInclude(c => c.UserSecurityInfo)
                                                    .Include(c => c.Application).ThenInclude(c => c.DesigneeType)
                                                    .Include(c => c.Application).ThenInclude(c => c.Office)
                                                    .Include(c => c.CloaStatus)
                                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.FunctionCode)
                                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.FunctionCode).ThenInclude(c => c.Category)
                                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.CloaFunctionCodeTypeRatings).ThenInclude(c => c.TypeRating)
                                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.CloaFunctionCodeMakeModels).ThenInclude(c => c.MakeModel)
                                                    .Include(c => c.MsUserOfficeRole).ThenInclude(c => c.User).ThenInclude(c => c.Profile)
                                                    .Select(c => new
                                                    {
                                                        CloaFunctionCodes = c.CloaFunctionCodes,
                                                        Id = preApprvoalRequest.Id,
                                                        ApplicationId = preApprvoalRequest.ApplicationId,
                                                        ManagingSpecialist = c.ManagingSpecialistId.Value,
                                                        ManagingSpecialistName = c.MsUserOfficeRole.User.Profile.ToFullName(),
                                                        ManagingOffice = new BaseLookup() { Id = c.Application.Office.Id, Name = c.Application.Office.Name },
                                                        UserId = c.Application.User.Id,
                                                        DesigneeTypeId = c.Application.DesigneeTypeId,
                                                        OfficeId = c.Application.OfficeId,
                                                        IsAutoPostActivity = c.IsAutoPostActivity,
                                                        DesingatorName = c.DesignatorId.HasValue ? c.Designator.Name : string.Empty,
                                                        TimeZoneId = c.CloaAdjunct != null ? c.CloaAdjunct.TimeZoneId : null,
                                                        DesigneeInfo = new DesigneeViewModel
                                                        {
                                                            Name = c.Application.Cloas.First(a => a.Id == cloaId).ProfileVersion.ToFullName(),
                                                            Number = c.Application.User.UserSecurityInfo.DesigneeNumber,
                                                            Type = c.Application.DesigneeType.Code,
                                                            Code = c.Designator.Code,
                                                            TypeId = c.Application.DesigneeTypeId.Value,
                                                            ExpirationDate = c.ExpirationDate.ToShortDateString(),
                                                            Company = c.Application.DesigneeTypeId == (int)DesigneeTypeEnum.DMIR && c.CloaAddresses.Any(a => a.Address.AddressTypeId == (int)AddressTypeEnum.ProductionApprovalHolder)
                                                                             ? c.CloaAddresses.First(a => a.Address.AddressTypeId == (int)AddressTypeEnum.ProductionApprovalHolder).Address.Name
                                                                             : null,
                                                            CloaId = c.Id,
                                                            ManagingSpecialistId = c.ManagingSpecialistId,
                                                            ApplicationId = c.ApplicationId,
                                                            Id = c.Application.User.Id
                                                        },
                                                        RequestInfo = new PreApprovalRequestInfoViewModel()
                                                        {
                                                            ControlNumber = preApprvoalRequest.TrackingNumber,
                                                            ActivityStatus = preApprvoalRequest.PreApprovalRequestStatus != null
                                                             && preApprvoalRequest.PreApprovalRequestStatus.Id == (int)PreApprovalRequestStatusEnum.Pending ?
                                                                                                        new BaseLookup() { Id = preApprvoalRequest.PreApprovalRequestStatus.Id, Name = "Submitted" } : preApprvoalRequest.PreApprovalRequestStatus,
                                                            IsApproved = (preApprvoalRequest.IsApproved.HasValue && preApprvoalRequest.IsApproved.Value) ? true : false
                                                        },
                                                        DesigneeFunctionCodes = c.CloaFunctionCodes.Select(d => new FunctionCodeViewModel
                                                        {
                                                            Id = d.FunctionCode.Id,
                                                            FunctionCode = d.FunctionCode.Name,
                                                            HasTypeRating = d.FunctionCode.HasTypeRating,
                                                            CategoryId = d.FunctionCode.CategoryId ?? 0,
                                                            IsAutomaticPreapproval = d.IsAutoPreApproval,
                                                            Category = d.FunctionCode.Category != null ? new CategoryViewModel
                                                            {
                                                                Id = d.FunctionCode.Category.Id,
                                                                Name = d.FunctionCode.Category.Name
                                                            } : null,
                                                            TypeRatings = d.CloaFunctionCodeTypeRatings != null && d.CloaFunctionCodeTypeRatings.Any() 
                                                                        ? d.CloaFunctionCodeTypeRatings.Select(cf => new LookupItem
                                                                            {
                                                                                label = cf.TypeRating.Name,
                                                                                value = cf.TypeRating.Id
                                                                            }).ToArray() 
                                                                        : d.CloaFunctionCodeMakeModels != null && d.CloaFunctionCodeMakeModels.Any() 
                                                                            ? d.CloaFunctionCodeMakeModels.Select(cf => new LookupItem
                                                                                {
                                                                                    label = cf.MakeModel.Code,
                                                                                    value = cf.MakeModel.Id
                                                                                }).ToArray() 
                                                                            : null,
                                                            FunctionCodeTypeId = d.FunctionCode.FunctionCodeTypeId,
                                                            SortOrder = d.FunctionCode.SortOrder
                                                        }).OrderBy(x => x.SortOrder).ToList(),
                                                        FacilityAddress = c.CloaAddresses
                                                             .Where(ar => ar.Address.AddressTypeId == (int)AddressTypeEnum.WorkAddress)
                                                             .Select(add => new AddressViewModel
                                                             {
                                                                 Id = add.Address.Id,
                                                                 Name = add.Address.Name,
                                                                 Address1 = add.Address.AddressLine1,
                                                                 Address2 = add.Address.AddressLine2,
                                                                 City = add.Address.City,
                                                                 County = add.Address.County,
                                                                 State = add.Address.StateId.HasValue ? new StateViewModel
                                                                 {
                                                                     Id = add.Address.StateProvince.Id,
                                                                     Name = add.Address.StateProvince.Name
                                                                 } : null,
                                                                 Country = new CountryViewModel
                                                                 {
                                                                     Id = add.Address.Country.Id,
                                                                     Name = add.Address.Country.Name
                                                                 },
                                                                 ZipCode = add.Address.ZipCode,
                                                                 PhoneNumber = add.Address.Phone
                                                             }).FirstOrDefault(),
                                                        FacilityAddresses = c.CloaAddresses
                                                             .Where(ar => ar.Address.AddressTypeId == (int)AddressTypeEnum.WorkAddress)
                                                             .OrderBy(a => a.Id)
                                                             .Select(add => new AddressViewModel
                                                             {
                                                                 Id = add.Address.Id,
                                                                 Name = add.Address.Name,
                                                                 Address1 = add.Address.AddressLine1,
                                                                 Address2 = add.Address.AddressLine2,
                                                                 City = add.Address.City,   
                                                                 County = add.Address.County,
                                                                 State = add.Address.StateId.HasValue ? new StateViewModel
                                                                 {
                                                                     Id = add.Address.StateProvince.Id,
                                                                     Name = add.Address.StateProvince.Name
                                                                 } : null,
                                                                 Country = new CountryViewModel
                                                                 {
                                                                     Id = add.Address.Country.Id,
                                                                     Name = add.Address.Country.Name
                                                                 },
                                                                 ZipCode = add.Address.ZipCode,
                                                                 PhoneNumber = add.Address.Phone
                                                             }).OrderBy(a => a.Id).ToList(),
                                                       SelectedOtherFunctionCodes = selectedOtherFunctionCodes
                                                    }).FirstOrDefault();
                
            List<FunctionCodeViewModel>  initialFunctionCodes = null;
            if (preApprvoalRequest.StatusId == (int)ActivityStatusEnum.Completed || preApprvoalRequest.StatusId == (int)ActivityStatusEnum.Cancelled)
            {
                var initiatedDate = preApprvoalRequest.SubmittedDate == null ? preApprvoalRequest.CreatedDate : preApprvoalRequest.SubmittedDate;
                initialFunctionCodes = context.Cloas.Where(c => c.ApplicationId == _cloaPreApprovalRequestModel.ApplicationId && c.CreatedDate < initiatedDate)
                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.FunctionCode)
                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.FunctionCode).ThenInclude(c => c.Category)
                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.CloaFunctionCodeTypeRatings).ThenInclude(c => c.TypeRating)
                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.CloaFunctionCodeMakeModels).ThenInclude(c => c.MakeModel)
                                    .OrderBy(c => c.CreatedDate)
                                    .Select(c => c.CloaFunctionCodes.Select(d => new FunctionCodeViewModel
                                    {
                                        Id = d.FunctionCode.Id,
                                        FunctionCode = d.FunctionCode.Name,
                                        HasTypeRating = d.FunctionCode.HasTypeRating,
                                        CategoryId = d.FunctionCode.CategoryId ?? 0,
                                        IsAutomaticPreapproval = d.IsAutoPreApproval,
                                        Category = d.FunctionCode.Category != null ? new CategoryViewModel
                                        {
                                            Id = d.FunctionCode.Category.Id,
                                            Name = d.FunctionCode.Category.Name
                                        } : null,
                                        TypeRatings = d.CloaFunctionCodeTypeRatings != null && d.CloaFunctionCodeTypeRatings.Any() 
                                                    ? d.CloaFunctionCodeTypeRatings.Select(cf => new LookupItem
                                                        {
                                                            label = cf.TypeRating.Name,
                                                            value = cf.TypeRating.Id
                                                        }).ToArray() 
                                                    : d.CloaFunctionCodeMakeModels != null && d.CloaFunctionCodeMakeModels.Any() 
                                                        ? d.CloaFunctionCodeMakeModels.Select(cf => new LookupItem
                                                            {
                                                                label = cf.MakeModel.Code,
                                                                value = cf.MakeModel.Id
                                                            }).ToArray() 
                                                        : null,
                                        FunctionCodeTypeId = d.FunctionCode.FunctionCodeTypeId,
                                        SortOrder = d.FunctionCode.SortOrder
                                    }).OrderBy(x => x.SortOrder).ToList()
                                    ).LastOrDefault();
            }

            _cloaPreApprovalRequestModel.SelectedOtherFunctionCodes
                                        .ForEach(f => f.IsAutomaticPreapproval =
                                              _cloaPreApprovalRequestModel.CloaFunctionCodes.Any(c => c.FunctionCodeId == f.Id)
                                                ? _cloaPreApprovalRequestModel.CloaFunctionCodes.First(c => c.FunctionCodeId == f.Id).IsAutoPreApproval
                                                : false
                                             );
          
                var _cloaPreApprovalRequestViewModel = new CloaPreApprovalRequestViewModel {
                        Id = _cloaPreApprovalRequestModel.Id,
                        ApplicationId = _cloaPreApprovalRequestModel.ApplicationId,
                        ManagingSpecialist = _cloaPreApprovalRequestModel.ManagingSpecialist,
                        ManagingSpecialistName = _cloaPreApprovalRequestModel.ManagingSpecialistName,
                        ManagingOffice = _cloaPreApprovalRequestModel.ManagingOffice,
                        UserId = _cloaPreApprovalRequestModel.UserId,
                        DesigneeTypeId = _cloaPreApprovalRequestModel.DesigneeTypeId,
                        OfficeId = _cloaPreApprovalRequestModel.OfficeId,
                        IsAutoPostActivity = _cloaPreApprovalRequestModel.IsAutoPostActivity,
                        DesingatorName = _cloaPreApprovalRequestModel.DesingatorName,
                        DesigneeInfo = _cloaPreApprovalRequestModel.DesigneeInfo,
                        RequestInfo = _cloaPreApprovalRequestModel.RequestInfo,
                        DesigneeFunctionCodes = _cloaPreApprovalRequestModel.DesigneeFunctionCodes,
                        FacilityAddress = _cloaPreApprovalRequestModel.FacilityAddress,
                        FacilityAddresses = _cloaPreApprovalRequestModel.FacilityAddresses,
                        SelectedOtherFunctionCodes = _cloaPreApprovalRequestModel.SelectedOtherFunctionCodes,
                        InitialFunctionCodes = initialFunctionCodes,
                        TimeZoneId = _cloaPreApprovalRequestModel.TimeZoneId.GetValueOrDefault((int)GlobalEnum.CentralTimeZoneId)
                };                       
            
            return _cloaPreApprovalRequestViewModel;
        }
         public static CloaPreApprovalRequestViewModel GetAovCloaEntitytoViewModel(DmsContext context, PreApprovalRequest preApprvoalRequest, int? cloaId = null)
        {
           cloaId = cloaId ?? preApprvoalRequest.CloaId;
           var latestCloaId = context.Cloas.Where(c => c.ApplicationId == preApprvoalRequest.ApplicationId).OrderByDescending(c1 => c1.Id)
                                                                                                           .Select(c1 => c1.Id).First();

           List<FunctionCodeViewModel> selectedOtherFunctionCodes = preApprvoalRequest.CloaId == cloaId && preApprvoalRequest.PreApprovalRequestFunctionCodes.Any()
                                                                        ? preApprvoalRequest.PreApprovalRequestFunctionCodes.Select(d => new FunctionCodeViewModel
                                                        {
                                                            Id = d.FunctionCode.Id,
                                                            FunctionCode = d.FunctionCode.Name,
                                                            HasTypeRating = false,
                                                            CategoryId = d.FunctionCode.CategoryId ?? 0,
                                                            Category = d.FunctionCode.Category != null ? new CategoryViewModel
                                                            {
                                                                Id = d.FunctionCode.Category.Id,
                                                                Name = d.FunctionCode.Category.Name
                                                            } : null,
                                                            TypeRatings = null,
                                                            FunctionCodeTypeId = d.FunctionCode.FunctionCodeTypeId,
                                                            TypeId = d.TypeId,
                                                            SortOrder = d.FunctionCode.SortOrder
                                                        }).OrderBy(x => x.SortOrder).ToList() : new List<FunctionCodeViewModel>();

            var _cloaPreApprovalRequestModel = context.Cloas.Where(c => c.Id == latestCloaId)                                                   
                                                    .Include(c => c.Application).ThenInclude(c => c.User).ThenInclude(c => c.Profile)
                                                    .Include(c => c.Application).ThenInclude(c => c.User).ThenInclude(c => c.UserSecurityInfo)
                                                    .Include(c => c.Application).ThenInclude(c => c.DesigneeType)
                                                    .Include(c => c.AovCompanyType)
                                                    .Include(c => c.CloaStatus)
                                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.FunctionCode)
                                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.FunctionCode).ThenInclude(c => c.Category)
                                                    .Include(c => c.MsUserOfficeRole).ThenInclude(c => c.User).ThenInclude(c => c.Profile)
                                                    .Select(c => new
                                                    {
                                                        CloaFunctionCodes = c.CloaFunctionCodes,
                                                        Id = preApprvoalRequest.Id,
                                                        ApplicationId = preApprvoalRequest.ApplicationId,
                                                        ManagingSpecialist = c.ManagingSpecialistId.Value,
                                                        ManagingSpecialistName = c.MsUserOfficeRole.User.Profile.ToFullName(),
                                                        ManagingOffice = new BaseLookup() { Id = c.Application.Office.Id, Name = c.Application.Office.Name },
                                                        UserId = c.Application.User.Id,
                                                        DesigneeTypeId = c.Application.DesigneeTypeId,
                                                        OfficeId = c.Application.OfficeId,
                                                        IsAutoPostActivity = c.IsAutoPostActivity,
                                                        DesingatorName = c.DesignatorId.HasValue ? c.Designator.Name : string.Empty,
                                                        TimeZoneId = c.CloaAdjunct != null ? c.CloaAdjunct.TimeZoneId : null,
                                                        DesigneeInfo = new DesigneeViewModel
                                                        {
                                                            Name = c.Application.Cloas.First(a => a.Id == cloaId).ProfileVersion.ToFullName(),
                                                            Number = c.Application.User.UserSecurityInfo.DesigneeNumber,
                                                            Type = c.Application.DesigneeType.Code,                                                          
                                                            TypeId = c.Application.DesigneeTypeId.Value,
                                                            ExpirationDate = c.ExpirationDate.ToShortDateString(),                                                            
                                                            CloaId = c.Id,
                                                            ManagingSpecialistId = c.ManagingSpecialistId,
                                                            ApplicationId = c.ApplicationId,
                                                            Id = c.Application.User.Id,
                                                            AppointmentDate = c.AppointmentDate
                                                        },
                                                        RequestInfo = new PreApprovalRequestInfoViewModel()
                                                        {
                                                            ControlNumber = preApprvoalRequest.TrackingNumber,
                                                            ActivityStatus = preApprvoalRequest.PreApprovalRequestStatus != null
                                                             && preApprvoalRequest.PreApprovalRequestStatus.Id == (int)PreApprovalRequestStatusEnum.Pending ?
                                                                                                        new BaseLookup() { Id = preApprvoalRequest.PreApprovalRequestStatus.Id, Name = "Submitted" } : preApprvoalRequest.PreApprovalRequestStatus,
                                                            IsApproved = (preApprvoalRequest.IsApproved.HasValue && preApprvoalRequest.IsApproved.Value) ? true : false
                                                        },
                                                        DesigneeFunctionCodes = c.CloaFunctionCodes.Select(d => new FunctionCodeViewModel
                                                        {
                                                            Id = d.FunctionCode.Id,
                                                            FunctionCode = d.FunctionCode.Name,
                                                            HasTypeRating = d.FunctionCode.HasTypeRating,
                                                            CategoryId = d.FunctionCode.CategoryId ?? 0,
                                                            IsAutomaticPreapproval = d.IsAutoPreApproval,
                                                            Category = d.FunctionCode.Category != null ? new CategoryViewModel
                                                            {
                                                                Id = d.FunctionCode.Category.Id,
                                                                Name = d.FunctionCode.Category.Name
                                                            } : null,                                                            
                                                            FunctionCodeTypeId = d.FunctionCode.FunctionCodeTypeId,
                                                            SortOrder = d.FunctionCode.SortOrder
                                                        }).OrderBy(x => x.SortOrder).ToList(),
                                                        FacilityAddress = c.CloaAddresses
                                                             .Where(ar => ar.Address.AddressTypeId == (int)AddressTypeEnum.WorkAddress)
                                                             .Select(add => new AddressViewModel
                                                             {
                                                                 Id = add.Address.Id,
                                                                 Name = add.Address.Name,
                                                                 Address1 = add.Address.AddressLine1,
                                                                 Address2 = add.Address.AddressLine2,
                                                                 City = add.Address.City,
                                                                 County = add.Address.County,
                                                                 State = add.Address.StateId.HasValue ? new StateViewModel
                                                                 {
                                                                     Id = add.Address.StateProvince.Id,
                                                                     Name = add.Address.StateProvince.Name
                                                                 } : null,
                                                                 Country = new CountryViewModel
                                                                 {
                                                                     Id = add.Address.Country.Id,
                                                                     Name = add.Address.Country.Name
                                                                 },
                                                                 ZipCode = add.Address.ZipCode,
                                                                 PhoneNumber = add.Address.Phone,
                                                                 AirTrafficControlTower = add.Address.AirTrafficControlTower,
                                                                 Airport = add.Address.Airport
                                                             }).FirstOrDefault(),
                                                        FacilityAddresses = c.CloaAddresses
                                                             .Where(ar => ar.Address.AddressTypeId == (int)AddressTypeEnum.WorkAddress)
                                                             .OrderBy(a => a.Id)
                                                             .Select(add => new AddressViewModel
                                                             {
                                                                 Id = add.Address.Id,
                                                                 Name = add.Address.Name,
                                                                 Address1 = add.Address.AddressLine1,
                                                                 Address2 = add.Address.AddressLine2,
                                                                 City = add.Address.City,   
                                                                 County = add.Address.County,
                                                                 State = add.Address.StateId.HasValue ? new StateViewModel
                                                                 {
                                                                     Id = add.Address.StateProvince.Id,
                                                                     Name = add.Address.StateProvince.Name
                                                                 } : null,
                                                                 Country = new CountryViewModel
                                                                 {
                                                                     Id = add.Address.Country.Id,
                                                                     Name = add.Address.Country.Name
                                                                 },
                                                                 ZipCode = add.Address.ZipCode,
                                                                 PhoneNumber = add.Address.Phone,
                                                                 AirTrafficControlTower = add.Address.AirTrafficControlTower,
                                                                 Airport = add.Address.Airport
                                                             }).OrderBy(a => a.Id).ToList(),
                                                       SelectedOtherFunctionCodes = selectedOtherFunctionCodes,
                                                    }).FirstOrDefault();
                
            List<FunctionCodeViewModel>  initialFunctionCodes = null;
            if (preApprvoalRequest.StatusId == (int)ActivityStatusEnum.Completed || preApprvoalRequest.StatusId == (int)ActivityStatusEnum.Cancelled)
            {
                var initiatedDate = preApprvoalRequest.SubmittedDate == null ? preApprvoalRequest.CreatedDate : preApprvoalRequest.SubmittedDate;
                initialFunctionCodes = context.Cloas.Where(c => c.ApplicationId == _cloaPreApprovalRequestModel.ApplicationId && c.CreatedDate < initiatedDate)
                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.FunctionCode)
                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.FunctionCode).ThenInclude(c => c.Category)                                    
                                    .OrderBy(c => c.CreatedDate)
                                    .Select(c => c.CloaFunctionCodes.Select(d => new FunctionCodeViewModel
                                    {
                                        Id = d.FunctionCode.Id,
                                        FunctionCode = d.FunctionCode.Name,
                                        HasTypeRating = d.FunctionCode.HasTypeRating,
                                        CategoryId = d.FunctionCode.CategoryId ?? 0,
                                        IsAutomaticPreapproval = d.IsAutoPreApproval,
                                        Category = d.FunctionCode.Category != null ? new CategoryViewModel
                                        {
                                            Id = d.FunctionCode.Category.Id,
                                            Name = d.FunctionCode.Category.Name
                                        } : null,                                        
                                        FunctionCodeTypeId = d.FunctionCode.FunctionCodeTypeId,
                                        SortOrder = d.FunctionCode.SortOrder
                                    }).OrderBy(x => x.SortOrder).ToList()
                                    ).LastOrDefault();
            }

            _cloaPreApprovalRequestModel.SelectedOtherFunctionCodes
                                        .ForEach(f => f.IsAutomaticPreapproval =
                                              _cloaPreApprovalRequestModel.CloaFunctionCodes.Any(c => c.FunctionCodeId == f.Id)
                                                ? _cloaPreApprovalRequestModel.CloaFunctionCodes.FirstOrDefault(c => c.FunctionCodeId == f.Id)?.IsAutoPreApproval
                                                : false
                                             );
          
                var _cloaPreApprovalRequestViewModel = new CloaPreApprovalRequestViewModel {
                        Id = _cloaPreApprovalRequestModel.Id,
                        ApplicationId = _cloaPreApprovalRequestModel.ApplicationId,
                        ManagingSpecialist = _cloaPreApprovalRequestModel.ManagingSpecialist,
                        ManagingSpecialistName = _cloaPreApprovalRequestModel.ManagingSpecialistName,
                        ManagingOffice = _cloaPreApprovalRequestModel.ManagingOffice,
                        UserId = _cloaPreApprovalRequestModel.UserId,
                        DesigneeTypeId = _cloaPreApprovalRequestModel.DesigneeTypeId,
                        OfficeId = _cloaPreApprovalRequestModel.OfficeId,
                        IsAutoPostActivity = _cloaPreApprovalRequestModel.IsAutoPostActivity,
                        DesingatorName = _cloaPreApprovalRequestModel.DesingatorName,
                        DesigneeInfo = _cloaPreApprovalRequestModel.DesigneeInfo,
                        RequestInfo = _cloaPreApprovalRequestModel.RequestInfo,
                        DesigneeFunctionCodes = _cloaPreApprovalRequestModel.DesigneeFunctionCodes,
                        FacilityAddress = _cloaPreApprovalRequestModel.FacilityAddress,
                        FacilityAddresses = _cloaPreApprovalRequestModel.FacilityAddresses,
                        SelectedOtherFunctionCodes = _cloaPreApprovalRequestModel.SelectedOtherFunctionCodes,
                        InitialFunctionCodes = initialFunctionCodes,
                        TimeZoneId = _cloaPreApprovalRequestModel.TimeZoneId.GetValueOrDefault((int)GlobalEnum.CentralTimeZoneId)
                };                       
            
            return _cloaPreApprovalRequestViewModel;
        }
        public static PostActivityViewModel GetPostActivityToPostActivityViewModel(PreApprovalRequest preApprvoalRequest, int postActivityId = 0)
        {
            var postActivityViewModel = preApprvoalRequest.PostActivities.Where(po => (postActivityId == 0 || po.Id == postActivityId)).OrderByDescending(po => po.Id).Select(po => new PostActivityViewModel
            {
                Id = po.Id,
                AddiotionalComment = po.Comments,
                DenialComment = po.DenialReason,
                QuantityOfCertificate = po.NumberOfCertificates,
                ResultId = po.PostActivityResultTypeId,
                DatePaperWork = po.PostActivityPaperWorkDate == null ? DateTime.Now.ToShortDateString() : po.PostActivityPaperWorkDate.ToString(),
                DateActivityCompleted = po.CompletedDate.ToString(),
                ApprovalDate = po.ApprovalDate != null ? po.ApprovalDate.ToString() : ""
            }).First();

            return postActivityViewModel;
        }
        public static OtherPreApprovalRequestViewModel GetOtherPreApprovalRequestViewModel(PreApprovalRequest preApprvoalRequest)
        {
            var otherPreApprovalRequest = new OtherPreApprovalRequestViewModel
            {
                AirportName = preApprvoalRequest.OtherPreApprovalRequest.AirportName,
                Airport = preApprvoalRequest.OtherPreApprovalRequest.AirportId.HasValue ?
                                new BaseLookup { Id = preApprvoalRequest.OtherPreApprovalRequest.Airport.Id, Name = $"{preApprvoalRequest.OtherPreApprovalRequest.Airport.Name.Trim()}/ {preApprvoalRequest.OtherPreApprovalRequest.Airport.Code.Trim()}" } : null,
                ApplicantName = preApprvoalRequest.OtherPreApprovalRequest.ApplicantName,
                ApplicantNumber = preApprvoalRequest.OtherPreApprovalRequest.ApplicantPhone,
                AwcApplicationNumber = preApprvoalRequest.OtherPreApprovalRequest.AwcApplicationNumber,
                AirCraftMake = preApprvoalRequest.OtherPreApprovalRequest.AircraftMake,
                AirCraftModel = preApprvoalRequest.OtherPreApprovalRequest.AircraftModel,
                AirCraftRegNumber = preApprvoalRequest.OtherPreApprovalRequest.AircraftRegistrationNumber,

                ComponentName = preApprvoalRequest.OtherPreApprovalRequest.ComponentName,
                ComponentNumber = preApprvoalRequest.OtherPreApprovalRequest.ComponentNumber,

                RegisteredOwner = preApprvoalRequest.OtherPreApprovalRequest.RegisteredOwner,
                RegisteredOwnerPhone = preApprvoalRequest.OtherPreApprovalRequest.RegisteredOwnerPhone,
                IsOtherAirport = preApprvoalRequest.OtherPreApprovalRequest.IsOtherAirport,
                OtherAirportName = preApprvoalRequest.OtherPreApprovalRequest.OtherAirportName,
                IsUsedRemoteVideo = preApprvoalRequest.OtherPreApprovalRequest.IsUsedRemoteVideo,
                RemoteVideoComments = preApprvoalRequest.OtherPreApprovalRequest.RemoteVideoComments,
                // PreApprovalRequestExperimental = data.PreApprovalExperimentalPurposes,
                Address = preApprvoalRequest.OtherPreApprovalRequest.Address != null ? new AddressViewModel()
                {


                    Name = preApprvoalRequest.OtherPreApprovalRequest.Address != null ? preApprvoalRequest.OtherPreApprovalRequest.Address.Name : null,
                    Address1 = preApprvoalRequest.OtherPreApprovalRequest.Address.AddressLine1,
                    Address2 = preApprvoalRequest.OtherPreApprovalRequest.Address.AddressLine2,
                    City = preApprvoalRequest.OtherPreApprovalRequest.Address.City,
                    County = preApprvoalRequest.OtherPreApprovalRequest.Address.County,
                    State = preApprvoalRequest.OtherPreApprovalRequest.Address.StateProvince != null ? new StateViewModel
                    {
                        Id = preApprvoalRequest.OtherPreApprovalRequest.Address.StateProvince.Id,
                        Name = preApprvoalRequest.OtherPreApprovalRequest.Address.StateProvince.Name
                    } : null,
                    ZipCode = preApprvoalRequest.OtherPreApprovalRequest.Address.ZipCode

                } : null,
                Comments = preApprvoalRequest.OtherPreApprovalRequest.Comments,
                IsOutSideArea = preApprvoalRequest.OtherPreApprovalRequest.IsOutsideOfficeDistrict.HasValue && preApprvoalRequest.OtherPreApprovalRequest.IsOutsideOfficeDistrict.Value,
                SelectedOfficeId = preApprvoalRequest.OtherPreApprovalRequest.OfficeId,

                SelectedPreApprovalRequestTypeId = preApprvoalRequest.OtherPreApprovalRequest.CategoryId,
                PreApprovalSelectTypeId = preApprvoalRequest.OtherPreApprovalRequest.PreApprovalRequestSelectTypeId,
                PreApprovalSelectSubTypeId = preApprvoalRequest.OtherPreApprovalRequest.PreApprovalRequestSelectSubTypeId,

                ActivityFromDate = preApprvoalRequest.ProposeStartDate.ToString(),
                ActivityToDate = preApprvoalRequest.ProposeEndDate.ToString(),
                //IsAutoPostActivity = data.IsAutoPostActivity,
                NacipFormLoginNumber = preApprvoalRequest.OtherPreApprovalRequest.NacipFormLoginNumber,


                IsQuantityPanel = (preApprvoalRequest.OtherPreApprovalRequest.CategoryId == 18 && preApprvoalRequest.OtherPreApprovalRequest.PreApprovalRequestSelectTypeId != 7) //Conformity && !Installation
                                      || preApprvoalRequest.OtherPreApprovalRequest.CategoryId == 19  // Enable editable for Conformatity
                                      || preApprvoalRequest.OtherPreApprovalRequest.PreApprovalRequestSelectTypeId == 1       //Domestic
                                      || preApprvoalRequest.OtherPreApprovalRequest.PreApprovalRequestSelectSubTypeId == 16,  //Engine, Propeller or Article

                IsActivityPanel = (preApprvoalRequest.OtherPreApprovalRequest.CategoryId != 18 || (preApprvoalRequest.OtherPreApprovalRequest.CategoryId == 18 && preApprvoalRequest.OtherPreApprovalRequest.PreApprovalRequestSelectTypeId == 7)) // !Conformity || ( Conformity && Installation)
                                     && preApprvoalRequest.OtherPreApprovalRequest.PreApprovalRequestSelectTypeId != 1        //!Domestic
                                     && preApprvoalRequest.OtherPreApprovalRequest.PreApprovalRequestSelectSubTypeId != 16,   //!Engine, Propeller or Article

                SubmissionDate = preApprvoalRequest.SubmittedDate.HasValue ? preApprvoalRequest.SubmittedDate.Value.ToString("MM/dd/yyyy  HH:mm tt") : String.Empty
            };
            return otherPreApprovalRequest;
        }

        public static AddressViewModel GetAddressViewModel(PreApprovalRequest preApprvoalRequest)
        {
            var addressViewModel = preApprvoalRequest.AfsPreApprovalRequest.Address != null ? new AddressViewModel
            {
                Id = preApprvoalRequest.AfsPreApprovalRequest.Address.Id,
                Name = preApprvoalRequest.AfsPreApprovalRequest.Address.Name,
                Address1 = preApprvoalRequest.AfsPreApprovalRequest.Address.AddressLine1,
                Address2 = preApprvoalRequest.AfsPreApprovalRequest.Address.AddressLine2,
                City = preApprvoalRequest.AfsPreApprovalRequest.Address.City,
                County = preApprvoalRequest.AfsPreApprovalRequest.Address.County,
                State = preApprvoalRequest.AfsPreApprovalRequest.Address.StateProvince != null ? new StateViewModel
                {
                    Id = preApprvoalRequest.AfsPreApprovalRequest.Address.StateProvince.Id,
                    Name = preApprvoalRequest.AfsPreApprovalRequest.Address.StateProvince.Name
                } : null,
                Country = preApprvoalRequest.AfsPreApprovalRequest.Address.Country != null ? new CountryViewModel
                {
                    Id = preApprvoalRequest.AfsPreApprovalRequest.Address.Country.Id,
                    Name = preApprvoalRequest.AfsPreApprovalRequest.Address.Country.Name
                } : null,
                ZipCode = preApprvoalRequest.AfsPreApprovalRequest.Address.ZipCode,
                PhoneNumber = preApprvoalRequest.AfsPreApprovalRequest.Address.Phone
            } : new AddressViewModel();
            
            return addressViewModel;
        }
        public static PostActivityViewModel GetPostActivityViewModel(PreApprovalRequest preApprvoalRequest)
        {
            return null;
        }
        public static IEnumerable<PreApprovalDateWiseCountViewModel> GetPreApprovalDateWiseCountViewModel(DmsContext context, int applicationId)
        {
            var ignoreStatusIds = new List<int>() { (int)PreApprovalRequestStatusEnum.Canceled, (int)PreApprovalRequestStatusEnum.Saved };
            var dateWiseCounter = context.PreApprovalRequests
                                .Where(it => it.ApplicationId == applicationId
                                        && it.ProposeStartDate.HasValue
                                        && !ignoreStatusIds.Contains(it.StatusId.GetValueOrDefault())
                                        && (it.IsApproved == null || (it.IsApproved.HasValue && it.IsApproved.Value))
                                        )
                                .GroupBy(it => it.ProposeStartDate)
                                .Select(p => new PreApprovalDateWiseCountViewModel
                                {
                                    PreApprovalDate = p.Key.Value.ToString("MM/dd/yyyy"),
                                    Count = p.Count()
                                }).ToList();
            return dateWiseCounter;
        }

        public static CloaPreApprovalRequestViewModel GetNewCloaEntitytoViewModel(DmsContext context, int applicationId)
        {
            var cloa = context.Cloas.Include(c => c.CloaAddresses).ThenInclude(ar => ar.Address.StateProvince) 
            .Include(c => c.CloaAddresses).ThenInclude(ar => ar.Address.Country)   
            .Where(c => c.ApplicationId == applicationId) 
            .Where(c => c.CloaStatusId == (int) CloaStatusEnum.Active) 
            .Select(a => new 
                {
                    a.CloaAddresses, 
                    Select = new CloaPreApprovalRequestViewModel 
                    { 
                        Id = a.Id, 
                        ManagingSpecialist = a.ManagingSpecialistId.Value, 
                        UserId = a.Application.UserId, 
                        DesingatorName = a.DesignatorId.HasValue ? a.Designator.Name : string.Empty, 
                        TimeZoneId = (a.CloaAdjunct != null ? a.CloaAdjunct.TimeZoneId : null).GetValueOrDefault((int)GlobalEnum.CentralTimeZoneId),
                        DesigneeInformation = new DesigneeViewModel 
                        { 
                            Name = a.ProfileVersion.ToFullName(), 
                            Number = a.Application.User.UserSecurityInfo.DesigneeNumber, 
                            Type = a.Application.DesigneeType.Code, 
                            Code = a.Designator.Code, 
                            TypeId = a.Application.DesigneeType.Id, 
                            ExpirationDate = a.ExpirationDate.ToShortDateString(), 
                            AppointmentDate = a.AppointmentDate, 
                            CloaId = a.Id, 
                            ManagingSpecialistId = a.ManagingSpecialistId, 
                            ApplicationId = a.ApplicationId, 
                            Id = a.Application.User.Id, 
                            Company = a.Application.DesigneeTypeId == (int) DesigneeTypeEnum.DMIR && 
                                    a.CloaAddresses.Any(c => 
                                        c.Address.AddressTypeId == (int) AddressTypeEnum.ProductionApprovalHolder) 
                                ? a.CloaAddresses.First(c => 
                                        c.Address.AddressTypeId == (int) AddressTypeEnum.ProductionApprovalHolder) 
                                    .Address.Name 
                                : a.Application.DesigneeTypeId == (int) DesigneeTypeEnum.DMIR &&
                                  a.CloaAddresses.Any(c => 
                                        c.Address.AddressTypeId == (int) AddressTypeEnum.Supplier) 
                                ? a.CloaAddresses.First(c => 
                                        c.Address.AddressTypeId == (int) AddressTypeEnum.Supplier) 
                                    .Address.Name                                
                                : null                                
                                , 
                        }, 
                        RequestInfo = new PreApprovalRequestInfoViewModel() 
                        { 
                            SubmittedDate = string.Empty, 
                            ActivityStatus = new BaseLookup() {Id = 0, Name = "Open"} 
                        }, 
                        DesigneeFunctionCodes = a.CloaFunctionCodes.Where(f => f.FunctionCode.IsActive).Select(c => new FunctionCodeViewModel 
                        { 
                            Id = c.FunctionCode.Id, 
                            FunctionCode = c.FunctionCode.Name, 
                            CategoryId = c.FunctionCode.CategoryId ?? 0, 
                            Category = 
                                QueryExtensions.BuildLookupViewModel<CategoryViewModel>(c.FunctionCode.Category), 
                            HasTypeRating = c.FunctionCode.HasTypeRating, 
                            TypeRatings = c.CloaFunctionCodeTypeRatings != null && 
                                        c.CloaFunctionCodeTypeRatings.Any() 
                                ? c.CloaFunctionCodeTypeRatings.Select(m => new LookupItem 
                                { 
                                    label = m.TypeRating.Name, 
                                    value = m.TypeRating.Id 
                                }).ToArray() 
                                : c.CloaFunctionCodeMakeModels != null && c.CloaFunctionCodeMakeModels.Any() 
                                    ? c.CloaFunctionCodeMakeModels.Select(m => new LookupItem 
                                    { 
                                        label = m.MakeModel.Code, 
                                        value = m.MakeModel.Id 
                                    }).ToArray() 
                                    : null, 
                            FunctionCodeTypeId = c.FunctionCode.FunctionCodeTypeId, 
                            IsAutomaticPreapproval = c.IsAutoPreApproval, 
                            SortOrder = c.FunctionCode.SortOrder
                        }).OrderBy(x => x.SortOrder).ToList(),                      
                       AovCompanyTypeId = a.AovCompanyTypeId,
                    } 
                }).AsEnumerable()
                .Select(s => {
                                s.Select.FacilityAddress = s.CloaAddresses
                                .Where(ar => ar.Address.AddressTypeId == (int) AddressTypeEnum.WorkAddress) 
                                .Select(add => QueryExtensions.BuildAddressViewModel(add.Address, add.Address.StateProvince, add.Address.Country, null))
                                .FirstOrDefault();
                                s.Select.FacilityAddresses = s.CloaAddresses 
                                .Where(ar => ar.Address.AddressTypeId == (int) AddressTypeEnum.WorkAddress) 
                                .Select(add => QueryExtensions.BuildAddressViewModel(add.Address, add.Address.StateProvince, add.Address.Country, null)) 
                                .ToList(); 
                                return s.Select;
                }).FirstOrDefault();
            return cloa;
        }
        
        public static PerformanceResultViewModel GetPerformanceResultViewModel(PreApprovalRequest preApprvoalRequest)
        {
            var PerformanceResultViewModel = preApprvoalRequest.PostActivities.OrderByDescending(po => po.Id).Select(po => new PerformanceResultViewModel
            {
                Id = po.PostActivityPerformanceReview.Id,
                OverSightResultId = po.PostActivityPerformanceReview.PerformanceResultTypeId,
                Technical = po.PostActivityPerformanceReview.TechnicalComments,
                Professional = po.PostActivityPerformanceReview.ProfessionalComments,
                Procedural = po.PostActivityPerformanceReview.ProceduralComments,
                FollowUpActions = po.PostActivityPerformanceReview.RequiredFollowupComments,
                CompletedReview = po.PostActivityPerformanceReview.IsReviewCompleted,
                ReviewDate = po.PostActivityPerformanceReview.ReviewDate.ToString()
            }).First();

            return PerformanceResultViewModel;
        }
        public static PreAprovalPlannedActivityViewModel GetPreAprovalPlannedActivityViewModel(PreApprovalRequest preApprvoalRequest)
        {

            var preAprovalPlannedActivityViewModel = new PreAprovalPlannedActivityViewModel()
            {
                Products = preApprvoalRequest.PreApprovalRequestProducts.Select(prod =>
                        new PreApprovalRequestProductViewModel
                        {
                            Id = prod.Id,
                            PreApprovalRequestId = prod.PreApprovalRequestId,
                            Make = prod.Make,
                            Model = prod.Model,
                            SerialNumber = prod.SerialNumber,
                            ResultTypeId = prod.ResultTypeId
                        }).ToList()
            };
            return preAprovalPlannedActivityViewModel;
        }
        public static PreApprovalActivityLocationViewModel GetPreApprovalActivityLocationViewModel(PreApprovalRequest preApprvoalRequest)
        {
            var preApprovalActivityLocationViewModel = new PreApprovalActivityLocationViewModel()
            {

                IsActivityOutsideUsa = preApprvoalRequest.AfsPreApprovalRequest.IsActivityOutsideUsa,
                IsOutsideOfficeDistrict = preApprvoalRequest.AfsPreApprovalRequest.IsOutsideOfficeDistrict,
                OfficeId = preApprvoalRequest.AfsPreApprovalRequest.OfficeId,
                LocationAddress = preApprvoalRequest.AfsPreApprovalRequest.Address != null ? new AddressViewModel
                {
                    Id = preApprvoalRequest.AfsPreApprovalRequest.Address.Id,
                    Name = preApprvoalRequest.AfsPreApprovalRequest.Address.Name,
                    Address1 = preApprvoalRequest.AfsPreApprovalRequest.Address.AddressLine1,
                    Address2 = preApprvoalRequest.AfsPreApprovalRequest.Address.AddressLine2,
                    City = preApprvoalRequest.AfsPreApprovalRequest.Address.City,
                    County = preApprvoalRequest.AfsPreApprovalRequest.Address.County,
                    State = preApprvoalRequest.AfsPreApprovalRequest.Address.StateProvince != null ? new StateViewModel
                    {
                        Id = preApprvoalRequest.AfsPreApprovalRequest.Address.StateProvince.Id,
                        Name = preApprvoalRequest.AfsPreApprovalRequest.Address.StateProvince.Name
                    } : null,
                    Country = preApprvoalRequest.AfsPreApprovalRequest.Address.Country != null ? new CountryViewModel
                    {
                        Id = preApprvoalRequest.AfsPreApprovalRequest.Address.Country.Id,
                        Name = preApprvoalRequest.AfsPreApprovalRequest.Address.Country.Name
                    } : null,
                    ZipCode = preApprvoalRequest.AfsPreApprovalRequest.Address.ZipCode,
                    PhoneNumber = preApprvoalRequest.AfsPreApprovalRequest.Address.Phone
                } : new AddressViewModel(),
                OtherAddress = new AddressViewModel(),
                Airport = preApprvoalRequest.AfsPreApprovalRequest.AirportId.HasValue ?
                           new BaseLookup { Id = preApprvoalRequest.AfsPreApprovalRequest.Airport.Id, Name = $"{preApprvoalRequest.AfsPreApprovalRequest.Airport.Name.Trim()}/ {preApprvoalRequest.AfsPreApprovalRequest.Airport.Code.Trim()}"} : null,
            };
            return preApprovalActivityLocationViewModel;

        }
        public static PreApprovalTestInformationViewModel GetPreApprovalTestInformationViewModel(PreApprovalRequest preApprvoalRequest)
        {
            var preApprovalTestInformationViewModel = new PreApprovalTestInformationViewModel
            {
                PracticalOralTestId = preApprvoalRequest.AfsPreApprovalRequest.PracticalOralTestId,
                ProposeStartDate = preApprvoalRequest.ProposeStartDate.DateToString(),
                ProposeEndDate = preApprvoalRequest.ProposeEndDate.DateToString(),
                ProposedStartTime = preApprvoalRequest.ProposedStartTime.HasValue ? preApprvoalRequest.ProposedStartTime.Value.ToString("HH:mm") : string.Empty,
                TimeZoneId = preApprvoalRequest.TimeZoneId,
            };
            return preApprovalTestInformationViewModel;
        }
        public static PreApprovalApplicationInformationViewModel GetPreApprovalApplicationInformationViewModel(PreApprovalRequest preApprvoalRequest)
        {
            var preApprovalApplicationInformationViewModel = new PreApprovalApplicationInformationViewModel()
            {
                CertificateRatingTypeId = preApprvoalRequest.AfsPreApprovalRequest.CertificateRatingTypeId,
                SchoolId = preApprvoalRequest.AfsPreApprovalRequest.SchoolId.HasValue ?
                  new BaseLookup { Id = preApprvoalRequest.AfsPreApprovalRequest.School.Id, 
                  Name = $"{preApprvoalRequest.AfsPreApprovalRequest.School.Name.Trim()}/ {preApprvoalRequest.AfsPreApprovalRequest.School.Designator.Trim()}" }
                    : null,
                IsCfrSectionTest = preApprvoalRequest.AfsPreApprovalRequest.IsCfrSectionTest,
                CfrSectionSchoolId = preApprvoalRequest.AfsPreApprovalRequest.CfrSectionSchoolId.HasValue ?
                  new BaseLookup { Id = preApprvoalRequest.AfsPreApprovalRequest.CfrSectionSchool.Id, 
                    Name = $"{preApprvoalRequest.AfsPreApprovalRequest.CfrSectionSchool.Name.Trim()}/ {preApprvoalRequest.AfsPreApprovalRequest.CfrSectionSchool.Designator.Trim()}" }
                    : null,
                IsCivilExperience = preApprvoalRequest.AfsPreApprovalRequest.IsCivilExperience,
                IsMilitaryExperience = preApprvoalRequest.AfsPreApprovalRequest.IsMilitaryExperience,
                AuthorizedTestOfficeId = preApprvoalRequest.AfsPreApprovalRequest.AuthorizedTestOfficeId,
                PilotLicenseCountry = preApprvoalRequest.AfsPreApprovalRequest.PilotLicenseIssuedCountry != null ? new CountryViewModel
                {
                    Id = preApprvoalRequest.AfsPreApprovalRequest.PilotLicenseIssuedCountry.Id,
                    Name = preApprvoalRequest.AfsPreApprovalRequest.PilotLicenseIssuedCountry.Name
                } : null,
                AirCarrierId = preApprvoalRequest.AfsPreApprovalRequest.AirCarrierId.HasValue ?
                  new BaseLookup { Id = preApprvoalRequest.AfsPreApprovalRequest.AirCarrier.Id, Name = $"{preApprvoalRequest.AfsPreApprovalRequest.AirCarrier.Name.Trim()}/ {preApprvoalRequest.AfsPreApprovalRequest.AirCarrier.Code}" }
                  : null,
            };
            return preApprovalApplicationInformationViewModel;
        }

        public static PreApprovalApplicantInformationViewModel GetPreApprovalApplicantInformationViewModel(PreApprovalRequest preApprvoalRequest)
        {
            var preApprovalApplicantInformationViewModel = new PreApprovalApplicantInformationViewModel()
            {
                Name = preApprvoalRequest.AfsPreApprovalRequest.ApplicantName,
                Phone = preApprvoalRequest.AfsPreApprovalRequest.ApplicantPhone,
                Email = preApprvoalRequest.AfsPreApprovalRequest.ApplicantEmailAddress,
                CertificateNumber = preApprvoalRequest.AfsPreApprovalRequest.ApplicantCertificateNumber
            };
            return preApprovalApplicantInformationViewModel;
        }
        public static PreApprovalTestCheckInformationViewModel GetPreApprovalTestCheckInformationViewModel(PreApprovalRequest preApprvoalRequest)
        {
            var preApprovalTestCheckInformationViewModel = new PreApprovalTestCheckInformationViewModel
            {
                ReasonforAuthorization = preApprvoalRequest.AfsPreApprovalRequest.TemporaryAuthorizationReason,
                GradeCertificateId = preApprvoalRequest.AfsPreApprovalRequest.PreApprovalRequestGradeCertificateTypeId,
                AircraftCategoryId = preApprvoalRequest.AfsPreApprovalRequest.PreApprovalRequestAircraftCategoryTypeId,
                AircraftClassId = preApprvoalRequest.AfsPreApprovalRequest.PreApprovalRequestAircraftClassTypeId,
                IsAircraftNotRequired = preApprvoalRequest.AfsPreApprovalRequest.IsAircraftNotRequired,
                IsFlightPortionOnly = preApprvoalRequest.AfsPreApprovalRequest.IsFlightPortionOnly,
                AircraftMakeModelId = preApprvoalRequest.AfsPreApprovalRequest.AircraftMakeModelId.HasValue ?
                  new BaseLookup { Id = preApprvoalRequest.AfsPreApprovalRequest.AircraftMakeMode.Id, Name = preApprvoalRequest.AfsPreApprovalRequest.AircraftMakeMode.ToString() } : null,
                IsRecommendingInstructorNotAvailable = preApprvoalRequest.AfsPreApprovalRequest.IsRecommendingInstructorNotAvailable,
                RecommendingInstructor = preApprvoalRequest.AfsPreApprovalRequest.RecommendingInstructor,
                RecommendingInstructorCertificateNumber = preApprvoalRequest.AfsPreApprovalRequest.RecommendingInstructorCertificateNumber,
                IsTemporaryFunctionCode = preApprvoalRequest.PreApprovalRequestFunctionCodes.Count > 0 ? preApprvoalRequest.PreApprovalRequestFunctionCodes.Any(p => p.IsCloaFunctionCode.HasValue && p.IsCloaFunctionCode.Value) : (bool?)null,
                IsMultipleApplicants = preApprvoalRequest.AfsPreApprovalRequest.IsMultipleApplicants == null ? false : (bool)preApprvoalRequest.AfsPreApprovalRequest.IsMultipleApplicants,
                IsOtherAdminActivity = preApprvoalRequest.AfsPreApprovalRequest.IsOtherAdminActivity == null ? false : (bool)preApprvoalRequest.AfsPreApprovalRequest.IsOtherAdminActivity,
                IsTrainingDeviceTestCheck =  preApprvoalRequest.AfsPreApprovalRequest.IsTrainingDeviceTestCheck,
                PracticalOralTestId = preApprvoalRequest.AfsPreApprovalRequest.PracticalOralTestId,
                ObservationId = preApprvoalRequest.AfsPreApprovalRequest.ObservationTypeId,
                SimulatorId = preApprvoalRequest.AfsPreApprovalRequest.SimulatorId.HasValue ? 
                 new BaseLookup { Id = preApprvoalRequest.AfsPreApprovalRequest.Simulator.Id, Name =  $"({preApprvoalRequest.AfsPreApprovalRequest.Simulator.FaaId}) {preApprvoalRequest.AfsPreApprovalRequest.Simulator.Code}" } : null,
                IsAircraftTestCheck = preApprvoalRequest.AfsPreApprovalRequest.IsAircraftTestCheck,
                AircraftRegistrationNumber = preApprvoalRequest.AfsPreApprovalRequest.AircraftRegistrationNumber,
                AirlineFlightNumber = preApprvoalRequest.AfsPreApprovalRequest.AirlineFlightNumber,
                TypeOfCheck = preApprvoalRequest.AfsPreApprovalRequest.TypeOfCheckId.HasValue ? 
                 new BaseLookup { Id = preApprvoalRequest.AfsPreApprovalRequest.PreApprovalRequestTypeOfCheck.Id, Name =  preApprvoalRequest.AfsPreApprovalRequest.PreApprovalRequestTypeOfCheck.Name, IsActive = preApprvoalRequest.AfsPreApprovalRequest.PreApprovalRequestTypeOfCheck.IsActive} : null,
            };
            return preApprovalTestCheckInformationViewModel;
        }

        public static void GetPoTestCheckInformationViewModel(PreApprovalRequest preApprovalRequest, int postActivityId, PreApprovalTestCheckInformationViewModel testCheckInformation)
        {
            var postActivity = postActivityId > 0
                ? preApprovalRequest.PostActivities.FirstOrDefault(p => p.Id == postActivityId)
                : preApprovalRequest.PostActivities.OrderByDescending(p => p.Id).FirstOrDefault();

            if (postActivity?.StatusId == (int) ActivityStatusEnum.Initiated) return;
            //do not add or overwrite postactivitymodified form fields here 
            testCheckInformation.GradeCertificateId = postActivity?.GradeCertificateTypeId;
            testCheckInformation.AircraftCategoryId = postActivity?.AircraftCategoryTypeId;
            testCheckInformation.AircraftClassId = postActivity?.AircraftClassTypeId;
            testCheckInformation.IsRecommendingInstructorNotAvailable = postActivity?.IsRecommendingInstructorNotAvailable;
            testCheckInformation.RecommendingInstructor = postActivity?.RecommendingInstructor;
            testCheckInformation.RecommendingInstructorCertificateNumber = postActivity?.RecommendingInstructorCertificateNumber;

            testCheckInformation.AircraftMakeModelId = postActivity != null && postActivity.AircraftMakeMode != null
                ? new BaseLookup { Id = postActivity.AircraftMakeMode.Id, Name = $"{postActivity.AircraftMakeMode.Code}/{postActivity.AircraftMakeMode.Make}/{postActivity.AircraftMakeMode.Model}"} 
                : testCheckInformation.AircraftMakeModelId;
        }

        public static PreApprovalApplicationInformationViewModel GetPoApplicationInformationViewModel(PreApprovalRequest preApprovalRequest, List<PreApprovalCertificateRatingTypeViewModel> postActivityCertificateRatingTypes, int postActivityId)
        {
            if (preApprovalRequest == null) return null;
            var postActivity = postActivityId > 0 
                ? preApprovalRequest.PostActivities.FirstOrDefault(p => p.Id == postActivityId) 
                : preApprovalRequest.PostActivities.OrderByDescending(p => p.Id).FirstOrDefault();
            if (postActivity == null) return new PreApprovalApplicationInformationViewModel();
            var poApplicationInformationViewModel = new PreApprovalApplicationInformationViewModel()
            {

                CertificateRatingTypeId = postActivity.StatusId != (int)ActivityStatusEnum.Initiated ? postActivity.CertificateRatingTypeId
                                                                                                      : preApprovalRequest.AfsPreApprovalRequest.CertificateRatingTypeId,

                SchoolId = postActivity.StatusId != (int)ActivityStatusEnum.Initiated ? postActivity.School != null 
                        ? new BaseLookup { Id = postActivity.School.Id, 
                                           Name = $"{postActivity.School.Name.Trim()}/ {postActivity.School.Designator.Trim()}" } : null
                        : preApprovalRequest.AfsPreApprovalRequest.SchoolId.HasValue ? 
                        new BaseLookup { Id = preApprovalRequest.AfsPreApprovalRequest.School.Id, 
                                         Name = $"{preApprovalRequest.AfsPreApprovalRequest.School.Name.Trim()}/ {preApprovalRequest.AfsPreApprovalRequest.School.Designator.Trim()}" } : null,

                PilotLicenseCountry = postActivity.StatusId != (int)ActivityStatusEnum.Initiated ? postActivity.PilotLicenseIssuedCountry != null ? new CountryViewModel
                {
                    Id = postActivity.PilotLicenseIssuedCountry.Id,
                    Name = postActivity.PilotLicenseIssuedCountry.Name
                } : null :
                preApprovalRequest.AfsPreApprovalRequest.PilotLicenseIssuedCountry != null ? new CountryViewModel
                {
                    Id = preApprovalRequest.AfsPreApprovalRequest.PilotLicenseIssuedCountry.Id,
                    Name = preApprovalRequest.AfsPreApprovalRequest.PilotLicenseIssuedCountry.Name
                } : null,
                AirCarrierId = postActivity.StatusId != (int)ActivityStatusEnum.Initiated ? postActivity.AirCarrier != null ? new BaseLookup { Id = postActivity.AirCarrier.Id, Name = $"{postActivity.AirCarrier.Name.Trim()}/ {postActivity.AirCarrier.Code}" } : null
                             :
                             preApprovalRequest.AfsPreApprovalRequest.AirCarrierId.HasValue ?
                              new BaseLookup { Id = preApprovalRequest.AfsPreApprovalRequest.AirCarrier.Id, Name = $"{preApprovalRequest.AfsPreApprovalRequest.AirCarrier.Name.Trim()}/ {preApprovalRequest.AfsPreApprovalRequest.AirCarrier.Code}" }
                             : null,


                PreApprovalCertificateRatingType = postActivityCertificateRatingTypes
            };

            var postActivitycertificateRatingIds = postActivity.PostActivityCertificateRatings?.Select(x => x.PostActivityCertificateRatingTypeId).ToList();
            if (postActivity.StatusId != (int)ActivityStatusEnum.Initiated && postActivitycertificateRatingIds?.Count > 0)
            {
                poApplicationInformationViewModel.SelectedCertificateRatingTypeIds = postActivity.PostActivityCertificateRatings.Select(x => x.PostActivityCertificateRatingTypeId).ToList();
            }
            else
            {
                poApplicationInformationViewModel.SelectedCertificateRatingTypeIds = preApprovalRequest.PreApprovalRequestCertificateRatings.Select(x => x.PreApprovalRequestCertificateRatingTypeId).ToList();
            }


            return poApplicationInformationViewModel;
        }

        public static PreApprovalFacilityInformationViewModel GetPreApprovalFacilityInformationViewModel(PreApprovalRequest preApprvoalRequest)
        {
            var preApprovalFacilityInformationViewModel = new PreApprovalFacilityInformationViewModel
            {
                Airport = preApprvoalRequest.AfsPreApprovalRequest.AirportId.HasValue ?
                               new BaseLookup { Id = preApprvoalRequest.AfsPreApprovalRequest.Airport.Id, Name = $"{preApprvoalRequest.AfsPreApprovalRequest.Airport.Name.Trim()}/ {preApprvoalRequest.AfsPreApprovalRequest.Airport.Code.Trim()}" } : null,
                PointOfContactName = preApprvoalRequest.AfsPreApprovalRequest.PointOfContactName,
                pointOfContactPhone = preApprvoalRequest.AfsPreApprovalRequest.PointOfContactPhone,
                LocationDirections = preApprvoalRequest.AfsPreApprovalRequest.LocationDirections,
                IsDirectionToLocationNeeded = String.IsNullOrEmpty(preApprvoalRequest.AfsPreApprovalRequest.LocationDirections) ? false : true,
            };
            return preApprovalFacilityInformationViewModel;
        }
        public static PreApprovalAirCraftInformationViewModel GetPreApprovalAirCraftInformationViewModel(PreApprovalRequest preApprvoalRequest)
        {
            var preApprovalAirCraftInformationViewModel = new PreApprovalAirCraftInformationViewModel()
            {
                ProductTypeId = preApprvoalRequest.AfsPreApprovalRequest.ProductTypeId,
                EngineMake = preApprvoalRequest.AfsPreApprovalRequest.EngineMake,
                EngineModel = preApprvoalRequest.AfsPreApprovalRequest.EngineModel,
                EngineSerialNumber = preApprvoalRequest.AfsPreApprovalRequest.EngineSerialNumber,
                AircraftRegistrationNumber = preApprvoalRequest.AfsPreApprovalRequest.AircraftRegistrationNumber,
                AircraftRegistrationDate = preApprvoalRequest.AfsPreApprovalRequest.AircraftRegistrationDate.HasValue ?
                                                                 preApprvoalRequest.AfsPreApprovalRequest.AircraftRegistrationDate.Value.ToShortDateString() : String.Empty,
                AircraftMakeModelId = preApprvoalRequest.AfsPreApprovalRequest.AircraftMakeModelId.HasValue ?
                           new BaseLookup { Id = preApprvoalRequest.AfsPreApprovalRequest.AircraftMakeMode.Id, Name = preApprvoalRequest.AfsPreApprovalRequest.AircraftMakeMode.ToString() } : null,
                AircraftSerialNumber = preApprvoalRequest.AfsPreApprovalRequest.AircraftSerialNumber,
                IsAmBuiltLightSport = preApprvoalRequest.AfsPreApprovalRequest.IsAmBuiltLightSport,
                AircraftMakeModel = preApprvoalRequest.AfsPreApprovalRequest.AircraftMakeModel
            };

            return preApprovalAirCraftInformationViewModel;
        }

        public static string SerializePreApproval(PreApprovalRequestViewModel model)
        {
            dynamic formData = null;
            if(model != null) 
            {
                formData = CloaPreApprovalRequestViewModelMapper.ArrangePostActivityFormData<PreApprovalRequestViewModel>(model);
            }

            return JsonConvert.SerializeObject(formData, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        }

        public static dynamic ArrangePostActivityFormData<T>(this T model) 
        where T : class 
        {
            var type = model.GetType();
            var arrangedClass = new ExpandoObject() as IDictionary<string, object>;
            foreach (var property in type.GetProperties())
            {
                dynamic tempValue = null;
                dynamic val = null;
                switch (property.Name)
                {
                    case "PreApprovalCertificateRatingType":
                    case "PreApprovalDateWiseCount":
                    case "AfsPostActivity":
                    case "DesigneeInfo":
                        val = null;
                        break;
                    case "AuthFunctionCodes":
                    case "DesigneeFunctionCodes":
                    case "SelectedOtherFunctionCodes":
                    case "InitialFunctionCodes":
                    case "RequestedAuthorizations":
                    case "TemporaryAuthorizations":
                    case "SelectedRequestAuthorizations":
                    case "SelectedTemporaryAuthorizations":
                    case "AdminDpeSaeFunctionCodes":
                        tempValue = property.GetValue(model);
                        if (tempValue != null)
                        {
                            val = ((List<FunctionCodeViewModel>)tempValue).Select(f => new { f.Id, f.IsAutomaticPreapproval, f.SelectedTypeRatings });
                        }
                        break;
                    case "TestCheckInformation":
                        tempValue = property.GetValue(model);
                        if (tempValue != null)
                        {
                            val = ArrangePostActivityFormData<PreApprovalTestCheckInformationViewModel>(tempValue);
                        }
                        break;
                    case "ApplicationInformation":
                    case "PoApplicationInformation":
                        tempValue = property.GetValue(model);
                        if (tempValue != null)
                        {
                            val = ArrangePostActivityFormData<PreApprovalApplicationInformationViewModel>(tempValue);
                        }
                        break;
                    case "ManagingOffice":
                        tempValue = property.GetValue(model);
                        if (tempValue != null)
                        {
                            val = new { tempValue.Id };
                        }
                        break;
                    default:
                        val = property.GetValue(model);
                        break;
                }

                if(val != null && (!(val is string)  || !string.IsNullOrWhiteSpace(val.ToString()))) 
                {
                    arrangedClass.Add(property.Name, val);
                }
            }

            return arrangedClass;
        }

        public static PreApprovalRequestViewModel DeSerializePreApprovalFormData(DmsContext context, string formData, int cloaId)
        {
            if (string.IsNullOrWhiteSpace(formData))
            {
                return null;
            }
            
            var model = JsonConvert.DeserializeObject<PreApprovalRequestViewModel>(formData);
        
            var designeeFunctionCodes = context.FunctionCodes.Where(x => x.DesigneeTypeId == model.DesigneeTypeId)
                                                     .Select(fc => new DesigneeTypeFunctionCodeViewModel()
                                                     {
                                                         DesigneeTypeId = fc.DesigneeType.Id,
                                                         FunctionCodeId = fc.Id,
                                                         FunctionCode = fc.Name,
                                                         FunctionCodeDescription = fc.Description,
                                                         HasTypeRating = fc.HasTypeRating,
                                                         Category = fc.Category != null ? new CategoryViewModel
                                                         {
                                                             Id = fc.Category.Id,
                                                             Name = fc.Category.Name
                                                         }
                                                         : null,
                                                         CategoryId = fc.CategoryId ?? 0,
                                                         FunctionCodeTypeId = fc.FunctionCodeTypeId,
                                                         SortOrder = fc.SortOrder
                                                     }).ToArray();
            // "AuthFunctionCodes"
            model.AuthFunctionCodes = FillFunctionCodes(model.AuthFunctionCodes, designeeFunctionCodes);
            // "DesigneeFunctionCodes"
            model.DesigneeFunctionCodes = FillFunctionCodes(model.DesigneeFunctionCodes, designeeFunctionCodes);
            // "SelectedOtherFunctionCodes"
            model.SelectedOtherFunctionCodes = FillFunctionCodes(model.SelectedOtherFunctionCodes, designeeFunctionCodes);
            // "InitialFunctionCodes"
            model.InitialFunctionCodes = FillFunctionCodes(model.InitialFunctionCodes, designeeFunctionCodes);
            if (model.TestCheckInformation != null)
            {
                // "RequestedAuthorizations"
                var cloaTypeRatings = GetCloaTypeRatings(context, cloaId);
                model.TestCheckInformation.RequestedAuthorizations = FillFunctionCodes(model.TestCheckInformation.RequestedAuthorizations, designeeFunctionCodes, cloaTypeRatings);
                // "TemporaryAuthorizations"
                model.TestCheckInformation.TemporaryAuthorizations = FillFunctionCodes(model.TestCheckInformation.TemporaryAuthorizations, designeeFunctionCodes);
                if (model.DesigneeTypeId == (int)DesigneeTypeEnum.DPE || model.DesigneeTypeId == (int)DesigneeTypeEnum.SAE)
                {
                    model.TestCheckInformation.TemporaryAuthorizations = FillTemporaryAuthTypeRatings(model.TestCheckInformation.TemporaryAuthorizations, (int)model.DesigneeTypeId, cloaTypeRatings);
                }
                // "SelectedRequestAuthorizations"
                model.TestCheckInformation.SelectedRequestAuthorizations = FillFunctionCodes(model.TestCheckInformation.SelectedRequestAuthorizations, designeeFunctionCodes);
                // "SelectedTemporaryAuthorizations"
                model.TestCheckInformation.SelectedTemporaryAuthorizations = FillFunctionCodes(model.TestCheckInformation.SelectedTemporaryAuthorizations, designeeFunctionCodes);
                // "AdminDpeSaeFunctionCodes"
                model.TestCheckInformation.AdminDpeSaeFunctionCodes = FillFunctionCodes(model.TestCheckInformation.AdminDpeSaeFunctionCodes, designeeFunctionCodes);
            }

            return model;
        }

        public static IList<FunctionCodeViewModel> FillFunctionCodes(IEnumerable<FunctionCodeViewModel> functionCodes, IEnumerable<DesigneeTypeFunctionCodeViewModel> lookupCollection, IList<FunctionCodeViewModel> cloaTypeRatings = null)
        {
            if (functionCodes != null) 
            {
                functionCodes.ToList().ForEach(a => {
                    var lookup = lookupCollection.First(d => d.FunctionCodeId == a.Id);
                    a.FunctionCode = lookup.FunctionCode;
                    a.HasTypeRating = lookup.HasTypeRating;
                    a.CategoryId = lookup.CategoryId;
                    a.Category = lookup.Category;
                    a.FunctionCodeTypeId = lookup.FunctionCodeTypeId;
                    a.SortOrder = lookup.SortOrder;
                    a.TypeRatings = a.HasTypeRating && cloaTypeRatings != null && cloaTypeRatings.Any() ? cloaTypeRatings.FirstOrDefault(t => t.Id == a.Id)?.TypeRatings : null;
                });
                functionCodes = functionCodes.DistinctBy(d => d.Id).ToList();
            }

            return (functionCodes ?? new FunctionCodeViewModel[0]).ToArray();
        }

        private static IList<FunctionCodeViewModel> GetCloaTypeRatings(DmsContext context, int cloaId) {

            return context.Cloas.Where(c => c.Id == cloaId)
                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.FunctionCode)
                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.CloaFunctionCodeTypeRatings).ThenInclude(c => c.TypeRating)
                                    .Include(c => c.CloaFunctionCodes).ThenInclude(c => c.CloaFunctionCodeMakeModels).ThenInclude(c => c.MakeModel)
                                    .AsEnumerable()
                                    .Select(c => c.CloaFunctionCodes.Where(f => f.FunctionCode.HasTypeRating).Select(d => new FunctionCodeViewModel
                                    {
                                        Id = d.FunctionCode.Id,
                                        CategoryId = d.FunctionCode.CategoryId,
                                        TypeRatings = d.CloaFunctionCodeTypeRatings != null && d.CloaFunctionCodeTypeRatings.Any() 
                                                    ? d.CloaFunctionCodeTypeRatings.Select(cf => new LookupItem
                                                        {
                                                            label = cf.TypeRating.Name,
                                                            value = cf.TypeRating.Id
                                                        }).ToArray() 
                                                    : d.CloaFunctionCodeMakeModels != null && d.CloaFunctionCodeMakeModels.Any() 
                                                        ? d.CloaFunctionCodeMakeModels.Select(cf => new LookupItem
                                                            {
                                                                label = cf.MakeModel.Code,
                                                                value = cf.MakeModel.Id
                                                            }).ToArray() 
                                                        : null
                                    }).OrderBy(x => x.SortOrder).ToList()
                                    ).LastOrDefault();
        }

        private static IList<FunctionCodeViewModel> FillTemporaryAuthTypeRatings(IEnumerable<FunctionCodeViewModel> functionCodes, int designeeTypeId, IList<FunctionCodeViewModel> cloaTypeRatings)
        {
            foreach (var item in functionCodes.Where(f => f.HasTypeRating))
            {
                var fnTypeRatings = TypeRatingTypes.Any(t => t.DesigneeTypeId == designeeTypeId && t.CategoryId == item.CategoryId)
                                                    ? TypeRatingTypes.Where(t => t.DesigneeTypeId == designeeTypeId && t.CategoryId == item.CategoryId).Select(t => new LookupItem
                                                    {
                                                        label = t.Name,
                                                        value = t.Id,
                                                    }).ToList()
                                                    : TypeRatingTypes.Where(t => t.CategoryId == null).Select(t => new LookupItem
                                                    {
                                                        label = t.Name,
                                                        value = t.Id,
                                                    }).ToList();

                var functionCode = cloaTypeRatings.Any(x => x.Id == item.Id) ? cloaTypeRatings.First(x => x.Id == item.Id) : null;

                item.TypeRatings = functionCode != null ? fnTypeRatings.Where(t => !functionCode.TypeRatings.Any(ft => ft.value == t.value)).ToList() : fnTypeRatings;
            }
            return functionCodes.ToList();
        }
    }
}
