using Dms.Core.EntityFramework.Data;
using Dms.Core.EntityFramework.Model;
using Dms.Core.EntityFramework.Model.Activity;
using Dms.Core.EntityFramework.Model.Lookup;
using Dms.Core.EntityFramework.Model.Shared;
using Dms.Core.Utils;
using Dms.Services.Interface.Activity;
using Dms.Services.Interface.Lookup;
using Dms.Services.Interface.Message;
using Dms.Services.Interface.Security;
using Dms.Services.Interface.Shared;
using Dms.Services.Interface.Task;
using Dms.Services.ViewModel;
using Dms.Services.ViewModel.Activity;
using Dms.Services.ViewModel.Lookup;
using Microsoft.EntityFrameworkCore;
﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Dms.Services.Implementation.Activity
{
    public class TcePreApprovalRequestService : AfsGroupThreePreApprovalRequestService
    {
        public TcePreApprovalRequestService(DmsContext context, ITaskService taskService, IDocumentService documentService, IActivityService activityService, IMessageService messageService, ISharedService sharedService, ILookupService lookUpService, IUserService userService)
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
            SaveFunctionCodes(model, _preApprovalRequest);
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
            _preApprovalRequest.AfsPreApprovalRequest.GraduatedFromCurriculum = model.ApplicationInformation.GraduatedFromCurriculum;
            _preApprovalRequest.AfsPreApprovalRequest.NameOfApprovedTrainingProgram = model.ApplicationInformation.NameOfApprovedTrainingProgram;
            _preApprovalRequest.AfsPreApprovalRequest.AirCarrierId = model.ApplicationInformation.AirCarrierId?.Id;
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

        public static bool FindModifiedDataForTce(AfsGroupsPostActivityViewModel model, PreApprovalRequest preApprovalRequest,
                                                    Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls, PostActivity postActivity)
        {
            return FindModifiedDataForApdTce(model, preApprovalRequest, modifiedPreapprovalControls, postActivity);
        }

        protected override IEnumerable<PreApprovalDateWiseCountViewModel> GetPreApprovalDateWiseCount(int applicationId)
        {

            return base.GetPreApprovalDateWiseCount(applicationId);
        }

        public override PreApprovalRequestViewModel GetNew(int applicationId)
        {
            base.GetNew(applicationId);

            var makeModels = _lookupService.LookupValues("makeModelSeries").Result.MakeModelSeries;

            var authorizations = _context.FunctionCodes
                .Include(c => c.Category)
                .Where(x => x.DesigneeTypeId == _cloa.DesigneeInformation.TypeId
                            && x.IsActive
                            && x.FunctionCodeTypeId != (int)FunctionCodeTypeEnum.Admin
                            && x.Id != 1319)
                .ToArray();

            var tempAuthorizations = new List<FunctionCodeViewModel>();

            foreach (var item in authorizations)
            {
                if (ShouldSkipAuthorization(item))
                {
                    continue;
                }

                var viewModel = CreateFunctionCodeViewModel(item, makeModels);
                tempAuthorizations.Add(viewModel);
            }

            _preApprovalRequestViewModel.TestCheckInformation = new PreApprovalTestCheckInformationViewModel()
            {
                RequestedAuthorizations = _preApprovalRequestViewModel.AuthFunctionCodes
                    .Where(x => x.FunctionCodeTypeId != (int)FunctionCodeTypeEnum.Admin)
                    .ToArray(),
                TemporaryAuthorizations = tempAuthorizations
            };

            GetAdminFunctionCodesByCloaFunctionCodes();

            _preApprovalRequestViewModel.DesigneeFunctionCodes = _cloa.DesigneeFunctionCodes;
            _preApprovalRequestViewModel.IsAfsType = true;
            _preApprovalRequestViewModel.PreApprovalDateWiseCount = GetPreApprovalDateWiseCount(_cloa.DesigneeInformation.ApplicationId);

            return _preApprovalRequestViewModel;
        }

        private bool ShouldSkipAuthorization(FunctionCode item)
        {
            bool existsInCloa = _cloa.DesigneeFunctionCodes.Any(x => x.Id == item.Id);
            if (!existsInCloa)
            {
                return false;
            }

            bool hasTypeRating = _cloa.DesigneeFunctionCodes.Any(x => x.Id == item.Id && item.HasTypeRating);
            return !hasTypeRating;
        }

        private FunctionCodeViewModel CreateFunctionCodeViewModel(FunctionCode item, IList<MakeModelViewModel> makeModels)
        {
            var matchingAuth = _preApprovalRequestViewModel.AuthFunctionCodes
                .FirstOrDefault(x => x.Id == item.Id && x.HasTypeRating);

            var typeRatings = GetAvailableTypeRatings(item, matchingAuth, makeModels);

            return new FunctionCodeViewModel()
            {
                Id = item.Id,
                FunctionCode = item.Name,
                HasTypeRating = item.HasTypeRating,
                DesigneeTypeId = _cloa.DesigneeInformation.TypeId,
                CategoryId = item.CategoryId ?? 0,
                Category = item.Category != null ? new CategoryViewModel
                {
                    Id = item.Category.Id,
                    Name = item.Category.Name
                } : null,
                TypeRatings = typeRatings
            };
        }

        private List<LookupItem> GetAvailableTypeRatings(FunctionCode item, FunctionCodeViewModel matchingAuth, IList<MakeModelViewModel> makeModels)
        {
            if (matchingAuth?.TypeRatings == null || !item.HasTypeRating)
            {
                return new List<LookupItem>();
            }

            var baseModels = makeModels.Where(t => t.DesigneeTypeId == _cloa.DesigneeInformation.TypeId && t.CategoryId == item.CategoryId).ToList();
            if (!baseModels.Any())
            {
                baseModels = makeModels.Where(t => t.CategoryId == null).ToList();
            }

            return baseModels
                .Where(t => !matchingAuth.TypeRatings.Any(ft => ft.value == t.Id))
                .Select(t => new LookupItem
                {
                    label = t.Name,
                    value = t.Id,
                })
                .ToList();
        }

#pragma warning disable S3776 // Cognitive Complexity of methods should not be too high
        [SuppressMessage("Major Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Complexity is currently accepted for this mapping-heavy method")]
        public override PreApprovalRequestViewModel Get(int preApprovalRequestId, bool loadPreapprovalModifiedData, int postActivityId, int? cloaId = null)
        {
            var preApprovalRequest = base.Get(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId, cloaId);
            GetAfsHelper(preApprovalRequest, postActivityId, loadPreapprovalModifiedData);
            {
                var certificateRatingTypes = new List<PreApprovalCertificateRatingTypeViewModel>();
                foreach (var item in _lookupService.LookupValues().Result.PreApprovalRequestCertificateRatingTypes)
                {
                    certificateRatingTypes.Add(new PreApprovalCertificateRatingTypeViewModel()
                    {
                        Id = item.Id,
                        Name = item.Name,
                    });
                }
                _preApprovalRequestViewModel.ApplicationInformation.PreApprovalCertificateRatingType = certificateRatingTypes;
                _preApprovalRequestViewModel.ApplicationInformation.SelectedCertificateRatingTypeIds = _preApprovalRequest.PreApprovalRequestCertificateRatings
                                                                                                                        .Select(x => x.PreApprovalRequestCertificateRatingTypeId).ToList();
                if (_preApprovalRequest.AfsPreApprovalRequest != null)
                {
                    _preApprovalRequestViewModel.ApplicationInformation.GraduatedFromCurriculum = _preApprovalRequest.AfsPreApprovalRequest.GraduatedFromCurriculum;
                    _preApprovalRequestViewModel.ApplicationInformation.NameOfApprovedTrainingProgram = _preApprovalRequest.AfsPreApprovalRequest.NameOfApprovedTrainingProgram;
                    _preApprovalRequestViewModel.ApplicationInformation.AirCarrierId = _preApprovalRequest.AfsPreApprovalRequest.AirCarrierId.HasValue ?
                            new BaseLookup { Id = _preApprovalRequest.AfsPreApprovalRequest.AirCarrier.Id, Name = $"{_preApprovalRequest.AfsPreApprovalRequest.AirCarrier.Name.Trim()}/ {_preApprovalRequest.AfsPreApprovalRequest.AirCarrier.Code}" }
                            : null;
                }

                var tempauthorizations = new List<FunctionCodeViewModel>();
                var authorizations = _context.FunctionCodes.Include(x => x.Category).Where(x => x.DesigneeTypeId == _cloa.DesigneeTypeId && x.IsActive && x.FunctionCodeTypeId != (int)FunctionCodeTypeEnum.Admin && x.Id != 1319).ToArray();
                foreach (var item in authorizations)
                {
                    if (!_cloa.DesigneeFunctionCodes.Any(x => x.Id == item.Id))
                    {
                        tempauthorizations.Add(new FunctionCodeViewModel()
                        {
                            Id = item.Id,
                            FunctionCode = item.Name,
                            DesigneeTypeId = item.DesigneeTypeId,
                            HasTypeRating = item.HasTypeRating,
                            Category = authorizations.First(f => item.Id == f.Id).Category != null ? new CategoryViewModel
                            {
                                Id = authorizations.First(f => item.Id == f.Id).Category.Id,
                                Name = authorizations.First(f => item.Id == f.Id).Category.Name
                            } : null,
                            CategoryId = authorizations.First(f => item.Id == f.Id).CategoryId ?? 0
                        });
                    }
                }

                _preApprovalRequestViewModel.TestCheckInformation.SelectedRequestAuthorizations = _preApprovalRequest.PreApprovalRequestFunctionCodes.Where(x => x.IsCloaFunctionCode.HasValue && x.IsCloaFunctionCode.Value)
                                                                                                            ?.Select(p => new FunctionCodeViewModel
                                                                                                            {
                                                                                                                Id = p.FunctionCode.Id,
                                                                                                                DesigneeTypeId = p.FunctionCode.DesigneeTypeId,
                                                                                                                FunctionCode = p.FunctionCode.Name,
                                                                                                                HasTypeRating = p.FunctionCode.HasTypeRating,
                                                                                                                TypeRatings = p.FunctionCode.HasTypeRating ? _cloa.DesigneeFunctionCodes.First(df => df.Id == p.FunctionCodeId).TypeRatings?.ToList() : null,
                                                                                                                Category = p.FunctionCode.Category != null ? new CategoryViewModel
                                                                                                                {
                                                                                                                    Id = p.FunctionCode.Category.Id,
                                                                                                                    Name = p.FunctionCode.Category.Name
                                                                                                                } : null,
                                                                                                                CategoryId = p.FunctionCode.CategoryId ?? 0,
                                                                                                                SelectedTypeRatings = p.PreApprovalRequestFunctionCodeMakeModels?.Select(pm => pm.MakeModelId).ToArray()
                                                                                                            }).ToArray();
                _preApprovalRequestViewModel.TestCheckInformation.RequestedAuthorizations = _cloa.DesigneeFunctionCodes.Where(x => x.FunctionCodeTypeId != (int)FunctionCodeTypeEnum.Admin).ToArray();
                _preApprovalRequestViewModel.TestCheckInformation.TemporaryAuthorizations = tempauthorizations;
                GetAdminFunctionCodesByCloaFunctionCodes();
            }
            _preApprovalRequestViewModel.IsMsReviewedAutoPreApproval = !(_preApprovalRequest.CreatedBy == _preApprovalRequest.ModifiedBy && IsAutoPreApproval(_preApprovalRequestViewModel));
            GetAfsHelperTwo(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId);
            var poExist = _context.PostActivities.Where(e => e.PreApprovalRequestId == preApprovalRequestId);
            if (loadPreapprovalModifiedData && _preApprovalRequestViewModel.AfsPostActivity.AfsPostActivityModifiedPreApprovalViewModel != null && poExist.Any())
            {
                LoadModifiedDataForAfsGroup3(_preApprovalRequestViewModel, _preApprovalRequestViewModel.AfsPostActivity, _cloa.DesigneeInfo.TypeId, _preApprovalRequestViewModel.ModifiedPreapprovalControls);
            }

            if (loadPreapprovalModifiedData && _preApprovalRequestViewModel.AfsPostActivity.AfsPostActivityModifiedPreApprovalViewModel != null && poExist.Any())
            {
                LoadPreApprovalModifiedData(_preApprovalRequestViewModel, _preApprovalRequestViewModel.AfsPostActivity, _cloa.DesigneeInfo.TypeId, _preApprovalRequestViewModel.ModifiedPreapprovalControls);
            }
            GetHelper(_preApprovalRequestViewModel);

            var designeeType = string.IsNullOrEmpty(_cloa.DesigneeInfo?.Code) ?
                                                        _cloa.DesigneeInfo.Type :
                                                        _cloa.DesigneeInfo.Type + " (" + _cloa.DesigneeInfo?.Code + ")";
            _preApprovalRequestViewModel.DesigneeInfo.Type = designeeType;

            _preApprovalRequestViewModel.IsLessThan24Hours = base.CheckIfLessThan24Hours(_preApprovalRequestViewModel);
            return _preApprovalRequestViewModel;
        }
        public override void CreateTask(PreApprovalRequestViewModel model)
        {
            base.CreateTask(model);
        }

        public override PreApprovalRequestViewModel SavePerformanceResults(PreApprovalRequestViewModel adminModel)
        {
            return base.SavePerformanceResults(adminModel);
        }

        public override PreApprovalRequestViewModel SavePostActivityEvaluation(PreApprovalRequestViewModel adminModel)
        {
            var model = base.SavePostActivityEvaluation(adminModel);
            var postActivity = _postActivity;
            var group2 = model.AfsPostActivity.AfsPostActivityGroup2;

            // 1. Direct property mapping
            MapGroup2ToPostActivity(group2, postActivity);

            // 2. Address or Multiple Applicants Logic
            var isMultiple = postActivity.PreApprovalRequest?.AfsPreApprovalRequest?.IsMultipleApplicants ?? false;
            if (!isMultiple)
            {
                UpdateOrInitializeAddress(group2.Address, postActivity);
            }
            else
            {
                UpdatePostActivityApplicants(group2.Applicants, postActivity);
            }

            postActivity.ApplicantCountryId = group2.ApplicantCountryId;

            SavePostActivityApplicationAndTestCheckInfo(model, postActivity);
            return base.SavePostActivityEvaluationHelper(model);
        }

        #region Helper Methods for Complexity Reduction

        private void MapGroup2ToPostActivity(dynamic group2, dynamic postActivity)
        {
            postActivity.IsAdditionalInstructionsProvided = group2.IsAdditionalInstructionsProvided;
            postActivity.AirManName = group2.AirManName;
            postActivity.AirManCertificateNumber = group2.AirManCertificateNumber;
            postActivity.FlightInstructorName = group2.FlightInstructorName;
            postActivity.FlightInstructorCertificateNumber = group2.FlightInstructorCertificateNumber;
            postActivity.PostActivityResultTypeId = group2.PostActivityResultTypeId;
            postActivity.PreApprovalRequestGradeCertificateTypeId = group2.GradeOfCertificateId;
            postActivity.AreasOfOperation = group2.AreasOfOperationAndTaskFound;
            postActivity.ReasonsOfDiscontinuance = group2.ReasonForDiscontinue;
            postActivity.PreApprovalRequestAircraftCategoryTypeId = group2.AirCraftCatergoryId;
            postActivity.PreApprovalRequestAircraftClassTypeId = group2.AirCraftClassId;
            postActivity.IsAircraftNotRequired = group2.IsAircraftNotRequired;
            postActivity.AircraftMakeModelId = group2.AirCraftMakeModelUsedId?.Id;
            postActivity.AircraftResgistrationNumber = group2.AirCraftRegistrationNumber;
            postActivity.SimulatorId = group2.SimulatorFaaId;
            postActivity.GroundPortionDuration = group2.DurationOfGroundPortion;
            postActivity.FlightPortionDuration = group2.DurationOfFlightPortion;
            postActivity.ApplicantPhone = group2.PhoneNumber;
            postActivity.ApplicantName = group2.NameOfApplicant;
            postActivity.ApplicantEmail = group2.Email;
            postActivity.ApplicantCertificateNumber = group2.CertificateNumberOfApplicant;
            postActivity.AirportId = group2.AirportOfTraining?.Id;
            postActivity.IacraStatusTypeId = group2.IacraStatusTypeId;
            postActivity.IsAirmanCertificateNotIssued = group2.IsAirManCertificateNotIssue;
            postActivity.ReasionForAirManCertificateNotIssue = group2.ReasonsForAirManCertificateNotIssue;

            // Parsed fields
            postActivity.ActualStartDate = ParseNullableDateTime(group2.ActualStartDate);
            postActivity.ActualEndDate = ParseNullableDateTime(group2.ActualEndDate);
            postActivity.PostActivityPaperWorkDate = ParseNullableDateTime(group2.DatePaperWorkSent);
        }

        private void UpdateOrInitializeAddress(dynamic sourceAddress, dynamic postActivity)
        {
            if (sourceAddress == null) return;

            int? stateId = sourceAddress.Country?.Id == 184 ? sourceAddress.State?.Id : null;

            if (postActivity.Address == null)
            {
                postActivity.Address = new Address();
            }

            postActivity.Address.AddressLine1 = sourceAddress.Address1;
            postActivity.Address.AddressLine2 = sourceAddress.Address2;
            postActivity.Address.City = sourceAddress.City;
            postActivity.Address.StateId = stateId;
            postActivity.Address.CountryId = sourceAddress.Country.Id;
            postActivity.Address.ZipCode = sourceAddress.ZipCode;
        }

        private void UpdatePostActivityApplicants(IEnumerable<dynamic> sourceApplicants, dynamic postActivity)
        {
            if (postActivity.PostActivityApplicants != null)
            {
                foreach (var app in postActivity.PostActivityApplicants.ToList())
                {
                    _context.PostActivityApplicants.Remove(app);
                }
            }

            postActivity.PostActivityApplicants = sourceApplicants.Select(a => new PostActivityApplicant
            {
                PostActivityId = postActivity.Id,
                ApplicantName = a.ApplicantName,
                CertificateNumber = a.CertificateNumber
            }).ToList();
        }

        private DateTime? ParseNullableDateTime(string dateStr)
        {
            return !string.IsNullOrEmpty(dateStr) ? DateTime.Parse(dateStr) : null;
        }

        #endregion
        protected void LoadModifiedDataForAfsGroup3(PreApprovalRequestViewModel preApprovalRequestViewModel, AfsPostActivityViewModel afsPostActivity,
            int designeeTypeId,
            Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls)
        {
            var modifiedVm = afsPostActivity?.AfsPostActivityModifiedPreApprovalViewModel;
            if (modifiedVm == null) return;

            // 1. Core Property Mapping & UI Tracking
            UpdateTestInformation(preApprovalRequestViewModel, modifiedVm, modifiedPreapprovalControls);
            UpdateTestCheckInformation(preApprovalRequestViewModel, modifiedVm, modifiedPreapprovalControls);

            // 2. Complex Lookup Conversions
            UpdateAircraftMakeModel(preApprovalRequestViewModel, modifiedVm, modifiedPreapprovalControls);
            UpdateNearestAirport(preApprovalRequestViewModel, modifiedVm, modifiedPreapprovalControls);
        }

        private void UpdateTestInformation(
            PreApprovalRequestViewModel preApprovalRequest,
            AfsPostActivityModifiedPreApprovalViewModel modifiedVm,
            Dictionary<string, List<ModifiedControlViewModel>> modifiedControls)
        {
            if (modifiedVm.PracticalOralTestId != null)
            {
                preApprovalRequest.TestInformation.PracticalOralTestId = modifiedVm.PracticalOralTestId;
                AddItemToModifiedPreapprovals("TestInformation", new ModifiedControlViewModel { Control = "practicalOralTestId" }, modifiedControls);
            }

            if (!string.IsNullOrEmpty(modifiedVm.ProposeStartDate))
            {
                preApprovalRequest.TestInformation.ProposeStartDate = modifiedVm.ProposeStartDate;
                AddItemToModifiedPreapprovals("TestInformation", new ModifiedControlViewModel { Control = "proposeStartDate" }, modifiedControls);
            }
        }

        private void UpdateTestCheckInformation(
            PreApprovalRequestViewModel preApprovalRequest,
            AfsPostActivityModifiedPreApprovalViewModel modifiedVm,
            Dictionary<string, List<ModifiedControlViewModel>> modifiedControls)
        {
            if (modifiedVm.IsOtherAdminActivity != null)
            {
                preApprovalRequest.TestCheckInformation.IsOtherAdminActivity = modifiedVm.IsOtherAdminActivity;
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel { Control = "isOtherAdminActivity" }, modifiedControls);
            }

            if (modifiedVm.IsAircraftNotRequired != null)
            {
                preApprovalRequest.TestCheckInformation.IsAircraftNotRequired = modifiedVm.IsAircraftNotRequired;
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel { Control = "aircraft" }, modifiedControls);
            }

            if (!string.IsNullOrEmpty(modifiedVm.TemporaryAuthorizationReason))
            {
                preApprovalRequest.TestCheckInformation.ReasonforAuthorization = modifiedVm.TemporaryAuthorizationReason;
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel { Control = "reason" }, modifiedControls);
            }
        }

        private void UpdateAircraftMakeModel(
            PreApprovalRequestViewModel preApprovalRequest,
            AfsPostActivityModifiedPreApprovalViewModel modifiedVm,
            Dictionary<string, List<ModifiedControlViewModel>> modifiedControls)
        {
            int mId = modifiedVm.AircraftMakeModelId.GetValueOrDefault();
            if (mId <= 0 || preApprovalRequest.TestCheckInformation == null) return;

            // Map lookup service entry
            preApprovalRequest.TestCheckInformation.AircraftMakeModelId = _lookupService.LookupValues("makeModelSeries").Result.MakeModelSeries
                .Where(s => s.Id == mId)
                .Select(x => new BaseLookup { Id = x.Id, Name = $"{x.Code}/{x.Make}/{x.Model}" })
                .FirstOrDefault();

            // Check if change triggers a corrective action
            bool triggersAction = HasAircraftChanged(preApprovalRequest.Id, mId);

            AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel
            {
                Control = "aircraftMakeModelId",
                TriggersCorrectiveAction = triggersAction
            }, modifiedControls);
        }

        private bool HasAircraftChanged(int preApprovalRequestId, int newModelId)
        {
            var postModel = _context.MakeModel.FirstOrDefault(w => w.Id == newModelId);
            var preApproval = _context.AfsPreApprovalRequests.FirstOrDefault(w => w.PreApprovalRequestId == preApprovalRequestId);

            if (postModel == null || preApproval?.AircraftMakeModelId == null) return false;

            var preModel = _context.MakeModel.FirstOrDefault(w => w.Id == preApproval.AircraftMakeModelId);
            if (preModel == null) return false;

            bool makeChanged = !string.Equals(postModel.Make?.Trim(), preModel.Make?.Trim(), StringComparison.OrdinalIgnoreCase);
            bool modelChanged = !string.Equals(postModel.Model?.Trim(), preModel.Model?.Trim(), StringComparison.OrdinalIgnoreCase);

            return makeChanged || modelChanged;
        }

        private void UpdateNearestAirport(
            PreApprovalRequestViewModel preApprovalRequest,
            AfsPostActivityModifiedPreApprovalViewModel modifiedVm,
            Dictionary<string, List<ModifiedControlViewModel>> modifiedControls)
        {
            int airId = modifiedVm.NearestAirportId.GetValueOrDefault();
            if (airId <= 0 || preApprovalRequest.ActivityLocation == null) return;

            preApprovalRequest.ActivityLocation.Airport = _lookupService.LookupValues("airports").Result.Airports
                .Where(s => s.Id == airId)
                .Select(x => new BaseLookup { Id = x.Id, Name = x.Name })
                .FirstOrDefault();

            AddItemToModifiedPreapprovals("FacilityInformation", new ModifiedControlViewModel { Control = "airport" }, modifiedControls);
        }

        public override AfsGroupsPostActivityViewModel Get(int postActivityId, bool createDocumentVersion = false)
        {
            base.Get(postActivityId, createDocumentVersion);

            var designatorCode = _context.PostActivities.Where(pa => pa.Id == postActivityId).Select(pa => pa.PreApprovalRequest.Cloa.Designator.Code).FirstOrDefault();
            _afsGroupsPostActivityViewModel.DesigneeInfo.Code = designatorCode;
            return _afsGroupsPostActivityViewModel;
        }

        protected static void SaveFunctionCodes(PreApprovalRequestViewModel model, PreApprovalRequest preApprovalRequest)
        {
            if (model.TestCheckInformation.SelectedRequestAuthorizations != null && model.TestCheckInformation.SelectedRequestAuthorizations.Count > 0)
            {
                preApprovalRequest.PreApprovalRequestFunctionCodes = model.TestCheckInformation.SelectedRequestAuthorizations.Select(f => new PreApprovalRequestFunctionCode
                {
                    FunctionCodeId = f.Id,
                    IsCloaFunctionCode = true,
                    PreApprovalRequestFunctionCodeMakeModels = f.SelectedTypeRatings != null && f.SelectedTypeRatings.Any() ? f.SelectedTypeRatings.Select(sf => new PreApprovalRequestFunctionCodeMakeModel
                    {
                        MakeModelId = sf
                    }).ToArray() : null
                }).ToList();
            }
        }
    }
}
