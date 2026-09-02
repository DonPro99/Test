﻿using System;
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
using Dms.Services.Interface.Security;

namespace Dms.Services.Implementation.Activity
{
    public class DadePreApprovalRequestService : AfsGroupThreePreApprovalRequestService
    {
        public DadePreApprovalRequestService(DmsContext context, ITaskService taskService, IDocumentService documentService, IActivityService activityService, IMessageService messageService, ISharedService sharedService, ILookupService lookUpService, IUserService userService)
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
            newPreapprovalRequest.IsMsReviewedAutoPreApproval = !(_preApprovalRequest.CreatedBy == _preApprovalRequest.ModifiedBy && IsAutoPreApproval(_preApprovalRequestViewModel));

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
            SaveFunctionCodes(model);
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
            _preApprovalRequest.AfsPreApprovalRequest.ApplicantName = model.ApplicantInformation.Name;
            _preApprovalRequest.AfsPreApprovalRequest.ApplicantCertificateNumber = model.ApplicantInformation.CertificateNumber;
            _preApprovalRequest.AfsPreApprovalRequest.DispatcherCertificationCourse = model.ApplicationInformation.DispatcherCertificationCourse;
            _preApprovalRequest.AfsPreApprovalRequest.DispatcherCertificationCourseLocation = model.ApplicationInformation.DispatcherCertificationCourseLocation;
            _preApprovalRequest.AfsPreApprovalRequest.ExperienceTypeId = model.ApplicationInformation.ExperienceTypeId;

            SaveHelper(model);
            CompleteOrCreateTask(model, IsAutoPreApproval(model));

            return _preApprovalRequestViewModel;
        }

        protected override bool IsAutoPreApproval(PreApprovalRequestViewModel model)
        {
            if (model?.ApplicationInformation?.CertificateRatingTypeId == (int)CertificateRatingTypeEnum.Experience)
            {
                return false;
            }
            return base.IsAutoPreApproval(model);
        }

        private void SaveFunctionCodes(PreApprovalRequestViewModel model)
        {
            if (model.SelectedFunctionCodes != null)
            {
                _preApprovalRequest.PreApprovalRequestFunctionCodes = model.SelectedFunctionCodes.Select(f => new PreApprovalRequestFunctionCode
                {
                    FunctionCodeId = f,
                    IsCloaFunctionCode = true
                }).ToList();
            }
        }

        public override bool SaveMsDecision(PreApprovalRequestViewModel model)
        {
            return base.SaveMsDecision(model);
        }

        public static bool FindModifiedDataForDade(AfsGroupsPostActivityViewModel model, PreApprovalRequest preApprovalRequest, 
                                                    Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls, 
                                                    PostActivity postActivity)
        {
            bool triggerCorrectiveActiveTrigger = false;
            if (model.PreApprovalRequest.TestCheckInformation.PracticalOralTestId != preApprovalRequest.AfsPreApprovalRequest.PracticalOralTestId)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel {Control = "practicalOralTestId"}, modifiedPreapprovalControls);
            }

            if (model.PreApprovalRequest.TestCheckInformation.IsAircraftNotRequired.GetValueOrDefault() != preApprovalRequest.AfsPreApprovalRequest.IsAircraftNotRequired.GetValueOrDefault())
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel {Control = "isAircraftNotRequired"}, modifiedPreapprovalControls);
            }
            if (model.PreApprovalRequest.TestCheckInformation.ReasonforAuthorization != preApprovalRequest.AfsPreApprovalRequest.TemporaryAuthorizationReason)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel {Control = "temporaryAuthorizationReason"}, modifiedPreapprovalControls);
            }

            if (model.PreApprovalRequest.ApplicationInformation.CertificateRatingTypeId != preApprovalRequest.AfsPreApprovalRequest.CertificateRatingTypeId)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel {Control = "certificateRatingTypeId"}, modifiedPreapprovalControls);
            }
            
            if (!string.IsNullOrEmpty(model.ActualStartDate))
            {
                DateTime actualStartDate = Convert.ToDateTime(model.ActualStartDate);
                DateTime actualEndDate = new DateTime();
                if (!string.IsNullOrEmpty(model.ActualEndDate))
                {
                    actualEndDate = Convert.ToDateTime(model.ActualEndDate);
                }
                if (preApprovalRequest.AfsPreApprovalRequest.PreApprovalRequest.ProposeStartDate != actualStartDate ||
                    preApprovalRequest.AfsPreApprovalRequest.PreApprovalRequest.ProposeStartDate != actualEndDate)
                {                    
                    var dateTriggerCorrectiveActionTrigger = !(actualStartDate.Date >= preApprovalRequest.AfsPreApprovalRequest.PreApprovalRequest.ProposeStartDate &&
                                                       actualStartDate.Date <= preApprovalRequest.AfsPreApprovalRequest.PreApprovalRequest.ProposeEndDate);
                    if (dateTriggerCorrectiveActionTrigger)
                    {
                        triggerCorrectiveActiveTrigger = true;
                        AddItemToModifiedPreapprovals("TestInformation", new ModifiedControlViewModel {Control = "proposeStartDate"}, modifiedPreapprovalControls);
                    }
                }
            }


            if (CheckForAddress(model, preApprovalRequest, modifiedPreapprovalControls, postActivity))
            {
                triggerCorrectiveActiveTrigger = true;
            }

            return triggerCorrectiveActiveTrigger;
        }

        protected override IEnumerable<PreApprovalDateWiseCountViewModel> GetPreApprovalDateWiseCount(int applicationId)
        {

            return base.GetPreApprovalDateWiseCount(applicationId);
        }

        public override PreApprovalRequestViewModel GetNew(int applicationId)
        {
            base.GetNew(applicationId);
            var tempauthorizations = new List<FunctionCodeViewModel>();
            var authorizations = _context.FunctionCodes.Include(c => c.Category).Where(x => x.DesigneeTypeId == _cloa.DesigneeInformation.TypeId
                                && x.IsActive
                                && x.FunctionCodeTypeId != (int)FunctionCodeTypeEnum.Admin).ToArray();

            _preApprovalRequestViewModel.TestCheckInformation = new PreApprovalTestCheckInformationViewModel()
            {
                RequestedAuthorizations = _preApprovalRequestViewModel.AuthFunctionCodes.Where(x => x.FunctionCodeTypeId != (int)FunctionCodeTypeEnum.Admin).ToArray(),
                TemporaryAuthorizations = tempauthorizations
            };

            GetAdminFunctionCodesByCloaFunctionCodes();

            _preApprovalRequestViewModel.DesigneeFunctionCodes = _cloa.DesigneeFunctionCodes;
            _preApprovalRequestViewModel.IsAfsType = true;
            _preApprovalRequestViewModel.PreApprovalDateWiseCount = GetPreApprovalDateWiseCount(_cloa.DesigneeInformation.ApplicationId);
            _preApprovalRequestViewModel.SelectedFunctionCodes = _preApprovalRequestViewModel.SelectedFunctionCodes != null && _preApprovalRequestViewModel.SelectedFunctionCodes.Any() ? _preApprovalRequestViewModel.SelectedFunctionCodes : _preApprovalRequestViewModel.AuthFunctionCodes.Select(f => f.Id).ToList();
            return _preApprovalRequestViewModel;
        }

        public override PreApprovalRequestViewModel Get(int preApprovalRequestId, bool loadPreapprovalModifiedData, int postActivityId, int? cloaId = null)
        {
            var preApprovalRequest = base.Get(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId, cloaId);
            preApprovalRequest.IsMsReviewedAutoPreApproval = !(_preApprovalRequest.CreatedBy == _preApprovalRequest.ModifiedBy && IsAutoPreApproval(_preApprovalRequestViewModel));

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
                _preApprovalRequestViewModel.ApplicationInformation.CertificateRatingTypeId = _preApprovalRequest.AfsPreApprovalRequest.CertificateRatingTypeId;
                _preApprovalRequestViewModel.ApplicationInformation.ExperienceTypeId = _preApprovalRequest.AfsPreApprovalRequest.ExperienceTypeId;
                _preApprovalRequestViewModel.ApplicationInformation.DispatcherCertificationCourse = _preApprovalRequest.AfsPreApprovalRequest.DispatcherCertificationCourse;
                _preApprovalRequestViewModel.ApplicationInformation.DispatcherCertificationCourseLocation = _preApprovalRequest.AfsPreApprovalRequest.DispatcherCertificationCourseLocation;
                _preApprovalRequestViewModel.SelectedFunctionCodes = _preApprovalRequest.PreApprovalRequestFunctionCodes.Select(p => p.FunctionCodeId).ToList();
            }
            GetAfsHelperTwo(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId);

            if (loadPreapprovalModifiedData && _context.PostActivities.Where(e => e.PreApprovalRequestId == preApprovalRequestId).Any() &&  _preApprovalRequestViewModel.AfsPostActivity.AfsPostActivityModifiedPreApprovalViewModel != null)
            {
                LoadModifiedDataForAfsGroup3(_preApprovalRequestViewModel, _preApprovalRequestViewModel.AfsPostActivity, _cloa.DesigneeInfo.TypeId, _preApprovalRequestViewModel.ModifiedPreapprovalControls);
            }

            if (loadPreapprovalModifiedData && _context.PostActivities.Where(e => e.PreApprovalRequestId == preApprovalRequestId).Any() && _preApprovalRequestViewModel.AfsPostActivity.AfsPostActivityModifiedPreApprovalViewModel != null)
            {
                LoadPreApprovalModifiedData(_preApprovalRequestViewModel, _preApprovalRequestViewModel.AfsPostActivity, _cloa.DesigneeInfo.TypeId, _preApprovalRequestViewModel.ModifiedPreapprovalControls);
            }
            GetHelper(_preApprovalRequestViewModel);
            
            _preApprovalRequestViewModel.IsLessThan24Hours =  base.CheckIfLessThan24Hours( _preApprovalRequestViewModel) ;  
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

        protected void LoadModifiedDataForAfsGroup3(PreApprovalRequestViewModel preApprovalRequestViewModel, AfsPostActivityViewModel afsPostActivity,
                                                    int designeeTypeId, Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls)
        {
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.PracticalOralTestId != null)
            {
                preApprovalRequestViewModel.TestInformation.PracticalOralTestId = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.PracticalOralTestId;
                AddItemToModifiedPreapprovals("TestInformation", new ModifiedControlViewModel {Control = "practicalOralTestId"}, modifiedPreapprovalControls);
            }
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsOtherAdminActivity != null)
            {
                preApprovalRequestViewModel.TestCheckInformation.IsOtherAdminActivity = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsOtherAdminActivity;
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel {Control = "isOtherAdminActivity"}, modifiedPreapprovalControls);
            }

            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.ProposeStartDate))
            {
                preApprovalRequestViewModel.TestInformation.ProposeStartDate = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.ProposeStartDate;
                AddItemToModifiedPreapprovals("TestInformation", new ModifiedControlViewModel {Control = "proposeStartDate"}, modifiedPreapprovalControls);
            }
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsAircraftNotRequired != null)
            {
                preApprovalRequestViewModel.TestCheckInformation.IsAircraftNotRequired = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsAircraftNotRequired;
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel {Control = "aircraft"}, modifiedPreapprovalControls);
            }
            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.TemporaryAuthorizationReason))
            {
                preApprovalRequestViewModel.TestCheckInformation.ReasonforAuthorization = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.TemporaryAuthorizationReason;
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel {Control = "reason"}, modifiedPreapprovalControls);
            }
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.CertificateRatingTypeId!=null)
            {
                preApprovalRequestViewModel.ApplicationInformation.CertificateRatingTypeId = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.CertificateRatingTypeId;
                AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel {Control = "certificateRatingTypeId"}, modifiedPreapprovalControls);
            }
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId != null &&
                afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId > 0 &&
                preApprovalRequestViewModel.TestCheckInformation != null &&
                afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId.HasValue)
            {
                    var mId = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId.GetValueOrDefault();
                    preApprovalRequestViewModel.TestCheckInformation.AircraftMakeModelId = _lookupService.LookupValues("makeModelSeries").Result.MakeModelSeries.Where(s => s.Id == mId).Select(x => new BaseLookup
                    {
                        Id = x.Id,
                        Name = $"{x.Code}/{x.Make}/{x.Model}"
                    }).FirstOrDefault();

                    var poMakeModel = _context.MakeModel.Where(w => w.Id == mId).FirstOrDefault();
                    var paAfsPre = _context.AfsPreApprovalRequests.Where(w => w.PreApprovalRequestId == (preApprovalRequestViewModel.Id)).FirstOrDefault();
                    var paMakeModel = GetPaMakeModel(paAfsPre);

                    var foundChange = IsFoundChange(poMakeModel, paMakeModel);

                    AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel 
                        { 
                            Control = "aircraftMakeModelId",
                            TriggersCorrectiveAction = foundChange
                        }, modifiedPreapprovalControls);
            }
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.NearestAirportId != null &&
                afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.NearestAirportId > 0 &&
                preApprovalRequestViewModel.ActivityLocation != null &&
                afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.NearestAirportId.HasValue)
            {
                    var airId = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.NearestAirportId.GetValueOrDefault();
                    preApprovalRequestViewModel.ActivityLocation.Airport = _lookupService.LookupValues("airports").Result.Airports.Where(s => s.Id == airId).Select(x => new BaseLookup
                    {
                        Id = x.Id,
                        Name = x.Name
                    }).FirstOrDefault();
                    AddItemToModifiedPreapprovals("FacilityInformation", new ModifiedControlViewModel {Control = "airport"}, modifiedPreapprovalControls);
            }
        }
        private static bool IsFoundChange(MakeModel poMakeModel, MakeModel paMakeModel) => poMakeModel != null && paMakeModel != null && (poMakeModel.Make.Trim().ToLower() != paMakeModel.Make.Trim().ToLower() ||
                                    poMakeModel.Model.Trim().ToLower() != paMakeModel.Model.Trim().ToLower()) ? true : false;
        private MakeModel GetPaMakeModel(AfsPreApprovalRequest paAfsPre) => paAfsPre != null && paAfsPre.AircraftMakeModelId != null ? 
                                        _context.MakeModel.Where(w => w.Id == paAfsPre.AircraftMakeModelId).FirstOrDefault() : null;
    }
}
