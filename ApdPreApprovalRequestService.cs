using System;
using System.Collections.Generic;
using Dms.Core.EntityFramework.Data;
using Dms.Core.EntityFramework.Model.Activity;
using Dms.Core.Utils;
using Dms.Services.Interface.Activity;
using Dms.Services.Interface.Lookup;
using Dms.Services.Interface.Message;
using Dms.Services.Interface.Shared;
using Dms.Services.Interface.Task;
using Dms.Services.ViewModel;
using Dms.Services.ViewModel.Activity;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Dms.Services.ViewModel.Lookup;
using Dms.Core.EntityFramework.Model.Shared;
using Dms.Core.EntityFramework.Model.Lookup;
using Dms.Services.Assembler;
using Dms.Services.Interface.Security;

namespace Dms.Services.Implementation.Activity
{
    public class ApdPreApprovalRequestService : AfsGroupThreePreApprovalRequestService
    {
        public ApdPreApprovalRequestService(DmsContext context, ITaskService taskService, IDocumentService documentService, IActivityService activityService, IMessageService messageService, ISharedService sharedService, ILookupService lookUpService, IUserService userService)
            : base(context, taskService, documentService, activityService, messageService, sharedService, lookUpService, userService)
        {
        }
        public override bool Cancel(ActivityPaperWorkViewModel model)
        {
            return base.Cancel(model);
        }

        public override int ReInitiate(int preApprovalRequestId)
        {
            var newPreapprovalRequest = Get(preApprovalRequestId, false, 0);
            CancelHelper(preApprovalRequestId);
            newPreapprovalRequest.Id = 0;
            newPreapprovalRequest.isSubmit = false;
            newPreapprovalRequest.RequestInfo.ControlNumber = _activityService.GenerateTrackingNumber(newPreapprovalRequest.DesigneeInfo.CloaId, (int)ProcessTypeEnum.PreApproval);
            newPreapprovalRequest.IsAfsType = true;
            newPreapprovalRequest.IsCancel = true;
            newPreapprovalRequest = Save(newPreapprovalRequest);
            return newPreapprovalRequest.Id;
        }
        public override PreApprovalRequestViewModel Save(PreApprovalRequestViewModel model)
        {
            base.Save(model);
            model = _preApprovalRequestViewModel;
            SaveDpeSaeFunctionCodes(model, _preApprovalRequest);
            SaveCertificateRatingsGroup2(model, _preApprovalRequest);

            if (model.Id != 0)
            {
                if (model.PlannedActivity != null && model.PlannedActivity.Products != null && model.PlannedActivity.Products.Any())
                {
                    _context.PreApprovalRequestProducts.RemoveRange(_preApprovalRequest.PreApprovalRequestProducts);
                }

                UpdateAfsData(model, _preApprovalRequest);
            }
            else
            {
                InsertAfsData(model, _preApprovalRequest);
                InsertAfsDataHelper(model, _preApprovalRequest);
                _context.PreApprovalRequests.Add(_preApprovalRequest);
            }

            _preApprovalRequest.AfsPreApprovalRequest.PracticalOralTestId = model.TestCheckInformation.PracticalOralTestId != null ? model.TestCheckInformation.PracticalOralTestId : null;
            _preApprovalRequest.AfsPreApprovalRequest.NameOfApprovedTrainingProgram = model.ApplicationInformation.NameOfApprovedTrainingProgram;
            SaveHelper(model);

            CompleteOrCreateTask(model, IsAutoPreApproval(model));

            return _preApprovalRequestViewModel;
        }

        protected override bool IsAutoPreApproval(PreApprovalRequestViewModel model)
        {
            var cloaAutoPreApproval = _context.PreApprovalRequests
         .Where(par => par.Id == model.Id)
         .Select(par => par.Cloa.IsAutoPreApproval)
         .FirstOrDefault();

            if (cloaAutoPreApproval)
            {
                return true;
            }

            return !model.TestCheckInformation.IsOtherAdminActivity.GetValueOrDefault() && base.IsAutoPreApproval(model);
        }

        public override bool SaveMsDecision(PreApprovalRequestViewModel model)
        {
            return base.SaveMsDecision(model);
        }

        public static bool FindModifiedDataForApd(AfsGroupsPostActivityViewModel model, PreApprovalRequest preApprovalRequest,
                                                Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls, PostActivity postActivity)
        {
            var triggerCorrectiveActiveTrigger = FindModifiedDataForApdTce(model, preApprovalRequest, modifiedPreapprovalControls, postActivity);

            if ((model.PreApprovalRequest.ActivityLocation.IsLineCheck.GetValueOrDefault() == true || preApprovalRequest.AfsPreApprovalRequest.IsLineCheck.GetValueOrDefault() == true) && model.PreApprovalRequest.ActivityLocation.Airport?.Id != preApprovalRequest.AfsPreApprovalRequest.AirportId)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("ActivityLocation", new ModifiedControlViewModel { Control = "airportId" }, modifiedPreapprovalControls);
            }
            return triggerCorrectiveActiveTrigger;
        }
        protected override IEnumerable<PreApprovalDateWiseCountViewModel> GetPreApprovalDateWiseCount(int applicationId)
        {

            return base.GetPreApprovalDateWiseCount(applicationId);
        }
        private List<TypeRatingViewModel> GetTypeRatings(IList<TypeRatingViewModel> typeRatingTypes, FunctionCode item)
        {
            return _preApprovalRequestViewModel.AuthFunctionCodes.Any(x => x.Id == item.Id && x.HasTypeRating)
                            ? typeRatingTypes.Any(t => t.DesigneeTypeId == _cloa.DesigneeInformation.TypeId && t.CategoryId == item.CategoryId)
                                ? typeRatingTypes.Where(t => t.DesigneeTypeId == _cloa.DesigneeInformation.TypeId && t.CategoryId == item.CategoryId).ToList()
                                : typeRatingTypes.Where(t => t.CategoryId == null).ToList()
                            : new List<TypeRatingViewModel>();
        }

        private FunctionCodeViewModel GetFunctionCode(FunctionCode item)
        {
            return _preApprovalRequestViewModel.AuthFunctionCodes.Any(x => x.Id == item.Id && x.HasTypeRating)
                                        ? _preApprovalRequestViewModel.AuthFunctionCodes.First(x => x.Id == item.Id)
                                        : null;
        }

        public override PreApprovalRequestViewModel GetNew(int applicationId)
        {
            base.GetNew(applicationId);
            var tempauthorizations = new List<FunctionCodeViewModel>();
            var typeRatingTypes = _lookupService.LookupValues("typeRatings").Result.TypeRatings;
            var authorizations = _context.FunctionCodes.Include(c => c.Category)
                                .Where(x => x.DesigneeTypeId == _cloa.DesigneeInformation.TypeId && x.IsActive
                                        && x.FunctionCodeTypeId != (int)FunctionCodeTypeEnum.Admin && x.Id != 1319).ToArray();

            foreach (var item in authorizations)
            {
                if (!_cloa.DesigneeFunctionCodes.Any(x => x.Id == item.Id) || _cloa.DesigneeFunctionCodes.Any(x => x.Id == item.Id && item.HasTypeRating))
                {
                    var fnTypeRatings = GetTypeRatings(typeRatingTypes, item);
                    var funtionCode = GetFunctionCode(item);
                    tempauthorizations.Add(new FunctionCodeViewModel()
                    {
                        Id = item.Id,
                        FunctionCode = item.Name,
                        HasTypeRating = item.HasTypeRating,
                        DesigneeTypeId = _cloa.DesigneeInformation.TypeId,
                        Category = authorizations.First(f => item.Id == f.Id).Category != null ? new CategoryViewModel
                        {
                            Id = authorizations.First(f => item.Id == f.Id).Category.Id,
                            Name = authorizations.First(f => item.Id == f.Id).Category.Name
                        } : null,
                        CategoryId = authorizations.First(f => item.Id == f.Id).CategoryId ?? 0,
                        TypeRatings = funtionCode != null && item.HasTypeRating
                              ? fnTypeRatings.Where(t => !funtionCode.TypeRatings.Any(ft => ft.value == t.Id)).Select(t => new LookupItem
                              {
                                  label = t.Name,
                                  value = t.Id,
                              }).ToList()
                              : new List<LookupItem>()
                    });
                }
            }
            _preApprovalRequestViewModel.TestCheckInformation = new PreApprovalTestCheckInformationViewModel()
            {
                RequestedAuthorizations = _preApprovalRequestViewModel.AuthFunctionCodes.Where(x => x.FunctionCodeTypeId != (int)FunctionCodeTypeEnum.Admin).ToArray(),
                TemporaryAuthorizations = tempauthorizations
            };

            GetAdminFunctionCodesByCloaFunctionCodes();

            _preApprovalRequestViewModel.DesigneeFunctionCodes = _cloa.DesigneeFunctionCodes;
            _preApprovalRequestViewModel.IsAfsType = true;
            _preApprovalRequestViewModel.PreApprovalDateWiseCount = GetPreApprovalDateWiseCount(_cloa.DesigneeInformation.ApplicationId);
            return _preApprovalRequestViewModel;
        }

        public override PreApprovalRequestViewModel Get(int preApprovalRequestId, bool loadPreapprovalModifiedData, int postActivityId, int? cloaId = null)
        {
            var preApprovalRequest = base.Get(preApprovalRequestId, false, 0, cloaId);
            GetAfsHelper(preApprovalRequest);
            _preApprovalRequestViewModel.IsMsReviewedAutoPreApproval = !(_preApprovalRequest.CreatedBy == _preApprovalRequest.ModifiedBy && IsAutoPreApproval(_preApprovalRequestViewModel));
            _preApprovalRequestViewModel.ApplicationInformation.PreApprovalCertificateRatingType = GetCertificateRatings();
            _preApprovalRequestViewModel.ApplicationInformation.SelectedCertificateRatingTypeIds = _preApprovalRequest.PreApprovalRequestCertificateRatings
                                                                                                                    .Select(x => x.PreApprovalRequestCertificateRatingTypeId).ToList();
            if (_cloa.DesigneeTypeId == (int)DesigneeTypeEnum.APD && _preApprovalRequest.AfsPreApprovalRequest != null)
            {
                _preApprovalRequestViewModel.ApplicationInformation.NameOfApprovedTrainingProgram = _preApprovalRequest.AfsPreApprovalRequest.NameOfApprovedTrainingProgram;
            }
            _preApprovalRequestViewModel.TestCheckInformation.TemporaryAuthorizations = GetTemporaryAuthorizations();

            _preApprovalRequestViewModel.TestCheckInformation.SelectedRequestAuthorizations = _preApprovalRequest.PreApprovalRequestFunctionCodes.Where(x => x.IsCloaFunctionCode.HasValue && x.IsCloaFunctionCode.Value)
                                                                                                        .Select(p => new FunctionCodeViewModel
                                                                                                        {
                                                                                                            Id = p.FunctionCode.Id,
                                                                                                            FunctionCode = p.FunctionCode.Name,
                                                                                                            HasTypeRating = p.FunctionCode.HasTypeRating,
                                                                                                            TypeRatings = p.FunctionCode.HasTypeRating ? _cloa.DesigneeFunctionCodes.First(df => df.Id == p.FunctionCodeId).TypeRatings?.ToList() : null,
                                                                                                            Category = p.FunctionCode.Category != null ? new CategoryViewModel
                                                                                                            {
                                                                                                                Id = p.FunctionCode.Category.Id,
                                                                                                                Name = p.FunctionCode.Category.Name
                                                                                                            } : null,
                                                                                                            CategoryId = p.FunctionCode.CategoryId ?? 0,
                                                                                                            SelectedTypeRatings = p.PreApprovalRequestFunctionCodeTypeRatings?.Select(pm => pm.TypeRatingId).ToArray(),
                                                                                                            IsAutomaticPreapproval = _preApprovalRequestViewModel.AuthFunctionCodes.FirstOrDefault(a => a.Id == p.FunctionCode.Id)?.IsAutomaticPreapproval
                                                                                                        }).ToArray();
            _preApprovalRequestViewModel.TestCheckInformation.SelectedTemporaryAuthorizations = _preApprovalRequest.PreApprovalRequestFunctionCodes.Where(x => x.IsCloaFunctionCode.HasValue && !x.IsCloaFunctionCode.Value)
                                                                                                        .Select(p => new FunctionCodeViewModel
                                                                                                        {
                                                                                                            Id = p.FunctionCode.Id,
                                                                                                            FunctionCode = p.FunctionCode.Name,
                                                                                                            DesigneeTypeId = p.FunctionCode.DesigneeTypeId,
                                                                                                            HasTypeRating = p.FunctionCode.HasTypeRating,
                                                                                                            Category = p.FunctionCode.Category != null ? new CategoryViewModel
                                                                                                            {
                                                                                                                Id = p.FunctionCode.Category.Id,
                                                                                                                Name = p.FunctionCode.Category.Name
                                                                                                            } : null,
                                                                                                            CategoryId = p.FunctionCode.CategoryId ?? 0,
                                                                                                            SelectedTypeRatings = p.PreApprovalRequestFunctionCodeTypeRatings?.Select(pm => pm.TypeRatingId).ToArray()
                                                                                                        }).ToArray();
            _preApprovalRequestViewModel.TestCheckInformation.RequestedAuthorizations = _cloa.DesigneeFunctionCodes.Where(x => x.FunctionCodeTypeId != (int)FunctionCodeTypeEnum.Admin).ToArray();
            GetAdminFunctionCodesByCloaFunctionCodes();

            GetAfsHelperTwo(preApprovalRequestId);
            GetHelper(_preApprovalRequestViewModel);
            _preApprovalRequestViewModel.IsLessThan24Hours =  base.CheckIfLessThan24Hours( _preApprovalRequestViewModel) ;  
            return _preApprovalRequestViewModel;
        }

        public override AfsGroupsPostActivityViewModel Get(int postActivityId, bool createDocumentVersion = false)
        {
            var preApprovalRequest = base.Get(postActivityId, createDocumentVersion);
            return preApprovalRequest;
        }

        public override PreApprovalRequestViewModel SavePostActivityEvaluation(PreApprovalRequestViewModel adminModel)
        {
            var model = base.SavePostActivityEvaluation(adminModel);
            var postActivity = _postActivity;

            postActivity.IsAdditionalInstructionsProvided = model.AfsPostActivity.AfsPostActivityGroup2.IsAdditionalInstructionsProvided;
            postActivity.AirManName = model.AfsPostActivity.AfsPostActivityGroup2.AirManName;
            postActivity.AirManCertificateNumber = model.AfsPostActivity.AfsPostActivityGroup2.AirManCertificateNumber;
            postActivity.FlightInstructorName = model.AfsPostActivity.AfsPostActivityGroup2.FlightInstructorName;
            postActivity.FlightInstructorCertificateNumber = model.AfsPostActivity.AfsPostActivityGroup2.FlightInstructorCertificateNumber;
            postActivity.PostActivityResultTypeId = model.AfsPostActivity.AfsPostActivityGroup2.PostActivityResultTypeId;
            postActivity.PreApprovalRequestGradeCertificateTypeId = model.AfsPostActivity.AfsPostActivityGroup2.GradeOfCertificateId;
            postActivity.AreasOfOperation = model.AfsPostActivity.AfsPostActivityGroup2.AreasOfOperationAndTaskFound;
            postActivity.ReasonsOfDiscontinuance = model.AfsPostActivity.AfsPostActivityGroup2.ReasonForDiscontinue;
            postActivity.PreApprovalRequestAircraftCategoryTypeId = model.AfsPostActivity.AfsPostActivityGroup2.AirCraftCatergoryId;
            postActivity.PreApprovalRequestAircraftClassTypeId = model.AfsPostActivity.AfsPostActivityGroup2.AirCraftClassId;
            postActivity.IsAircraftNotRequired = model.AfsPostActivity.AfsPostActivityGroup2.IsAircraftNotRequired;
            postActivity.AircraftMakeModelId = model.AfsPostActivity.AfsPostActivityGroup2.AirCraftMakeModelUsedId?.Id;
            postActivity.AircraftResgistrationNumber = model.AfsPostActivity.AfsPostActivityGroup2.AirCraftRegistrationNumber;
            postActivity.SimulatorId = model.AfsPostActivity.AfsPostActivityGroup2.SimulatorFaaId;
            postActivity.ActualStartDate = model.AfsPostActivity.AfsPostActivityGroup2.ActualStartDate != string.Empty ? DateTime.Parse(model.AfsPostActivity.AfsPostActivityGroup2.ActualStartDate) : (DateTime?)null;
            postActivity.ActualEndDate = model.AfsPostActivity.AfsPostActivityGroup2.ActualEndDate != string.Empty ? DateTime.Parse(model.AfsPostActivity.AfsPostActivityGroup2.ActualEndDate) : (DateTime?)null;
            postActivity.GroundPortionDuration = model.AfsPostActivity.AfsPostActivityGroup2.DurationOfGroundPortion;
            postActivity.FlightPortionDuration = model.AfsPostActivity.AfsPostActivityGroup2.DurationOfFlightPortion;
            postActivity.ApplicantPhone = model.AfsPostActivity.AfsPostActivityGroup2.PhoneNumber;
            postActivity.ApplicantName = model.AfsPostActivity.AfsPostActivityGroup2.NameOfApplicant;
            postActivity.ApplicantEmail = model.AfsPostActivity.AfsPostActivityGroup2.Email;
            postActivity.ApplicantCertificateNumber = model.AfsPostActivity.AfsPostActivityGroup2.CertificateNumberOfApplicant;
            postActivity.AirportId = model.AfsPostActivity.AfsPostActivityGroup2.AirportOfTraining?.Id;
            postActivity.IacraStatusTypeId = model.AfsPostActivity.AfsPostActivityGroup2.IacraStatusTypeId;
            postActivity.PostActivityPaperWorkDate = model.AfsPostActivity.AfsPostActivityGroup2.DatePaperWorkSent != string.Empty ? DateTime.Parse(model.AfsPostActivity.AfsPostActivityGroup2.DatePaperWorkSent) : (DateTime?)null;
            postActivity.IsAirmanCertificateNotIssued = model.AfsPostActivity.AfsPostActivityGroup2.IsAirManCertificateNotIssue;
            postActivity.ReasionForAirManCertificateNotIssue = model.AfsPostActivity.AfsPostActivityGroup2.ReasonsForAirManCertificateNotIssue;

            if (!postActivity.PreApprovalRequest.AfsPreApprovalRequest.IsMultipleApplicants.HasValue || (postActivity.PreApprovalRequest.AfsPreApprovalRequest.IsMultipleApplicants.HasValue && !postActivity.PreApprovalRequest.AfsPreApprovalRequest.IsMultipleApplicants.Value))
            {
                if (postActivity.Address == null)
                {
                    postActivity.Address = new Address()
                    {
                        AddressLine1 = model.AfsPostActivity.AfsPostActivityGroup2.Address.Address1,
                        AddressLine2 = model.AfsPostActivity.AfsPostActivityGroup2.Address.Address2,
                        City = model.AfsPostActivity.AfsPostActivityGroup2.Address.City,
                        StateId = GetStateId(model),
                        CountryId = model.AfsPostActivity.AfsPostActivityGroup2.Address.Country.Id,
                        ZipCode = model.AfsPostActivity.AfsPostActivityGroup2.Address.ZipCode
                    };
                }
                else
                {
                    postActivity.Address.AddressLine1 = model.AfsPostActivity.AfsPostActivityGroup2.Address.Address1;
                    postActivity.Address.AddressLine2 = model.AfsPostActivity.AfsPostActivityGroup2.Address.Address2;
                    postActivity.Address.City = model.AfsPostActivity.AfsPostActivityGroup2.Address.City;
                    postActivity.Address.StateId = GetStateId(model);
                    postActivity.Address.CountryId = model.AfsPostActivity.AfsPostActivityGroup2.Address.Country.Id;
                    postActivity.Address.ZipCode = model.AfsPostActivity.AfsPostActivityGroup2.Address.ZipCode;
                }
            }
            else
            {
                foreach (var app in postActivity.PostActivityApplicants.ToList())
                {
                    _context.PostActivityApplicants.Remove(app);
                }
                postActivity.PostActivityApplicants = model.AfsPostActivity.AfsPostActivityGroup2.Applicants.Select(a => new PostActivityApplicant
                {
                    PostActivityId = postActivity.Id,
                    ApplicantName = a.ApplicantName,
                    CertificateNumber = a.CertificateNumber
                }).ToList();
            }
            postActivity.ApplicantCountryId = model.AfsPostActivity.AfsPostActivityGroup2.ApplicantCountryId != null ? model.AfsPostActivity.AfsPostActivityGroup2.ApplicantCountryId : null;

            SavePostActivityApplicationAndTestCheckInfo(model, postActivity);
            var finalModel = base.SavePostActivityEvaluationHelper(model);
            return finalModel;
        }

        private static int? GetStateId(PreApprovalRequestViewModel model) => model.AfsPostActivity.AfsPostActivityGroup2.Address.Country?.Id == 184 ? model.AfsPostActivity.AfsPostActivityGroup2.Address.State?.Id : null;

        protected PreApprovalRequestViewModel GetAfsHelper(PreApprovalRequestViewModel preApprovalRequestViewModel)
        {
            _preApprovalRequestViewModel = preApprovalRequestViewModel;
            var preApprovalActivityLocationViewModel = CloaPreApprovalRequestViewModelMapper.GetPreApprovalActivityLocationViewModel(_preApprovalRequest);
            preApprovalActivityLocationViewModel.IsLineCheck = _preApprovalRequest.AfsPreApprovalRequest.IsLineCheck;
            preApprovalActivityLocationViewModel.FacilityonRecord = _cloa.FacilityAddresses.Count > 1 ? _cloa.FacilityAddresses.Any(a => a.Id == _preApprovalRequest.AfsPreApprovalRequest.ActivityLocationAddressId)
                                                                                                    : _cloa.FacilityAddress != null ? _preApprovalRequest.AfsPreApprovalRequest.ActivityLocationAddressId == _cloa.FacilityAddress.Id : false;
            preApprovalActivityLocationViewModel.FacilityAddress = _cloa.FacilityAddress;
            preApprovalActivityLocationViewModel.FacilityAddresses = _cloa.FacilityAddresses;
            _preApprovalRequestViewModel.ActivityLocation = preApprovalActivityLocationViewModel;
            //Set Other address in viewmodel
            if (preApprovalActivityLocationViewModel.FacilityonRecord.HasValue && !preApprovalActivityLocationViewModel.FacilityonRecord.Value)
            {
                _preApprovalRequestViewModel.ActivityLocation.OtherAddress = CloaPreApprovalRequestViewModelMapper.GetAddressViewModel(_preApprovalRequest);

            }
            _preApprovalRequestViewModel.TestInformation = CloaPreApprovalRequestViewModelMapper.GetPreApprovalTestInformationViewModel(_preApprovalRequest);
            if (!_preApprovalRequestViewModel.TestInformation.TimeZoneId.HasValue)
            {
                _preApprovalRequestViewModel.TestInformation.TimeZoneId = _cloa.TimeZoneId;
            }

            _preApprovalRequestViewModel.ApplicationInformation = CloaPreApprovalRequestViewModelMapper.GetPreApprovalApplicationInformationViewModel(_preApprovalRequest);

            _preApprovalRequestViewModel.ApplicationInformation.DesignatorName = _cloa.DesingatorName;

            _preApprovalRequestViewModel.ApplicantInformation = CloaPreApprovalRequestViewModelMapper.GetPreApprovalApplicantInformationViewModel(_preApprovalRequest);

            _preApprovalRequestViewModel.TestCheckInformation = CloaPreApprovalRequestViewModelMapper.GetPreApprovalTestCheckInformationViewModel(_preApprovalRequest);

            return _preApprovalRequestViewModel;
        }
    }
}
