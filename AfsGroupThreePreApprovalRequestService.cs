using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dms.Core.EntityFramework.Data;
using Dms.Core.EntityFramework.Model.Activity;
using Dms.Core.EntityFramework.Model.Apply;
using Dms.Core.EntityFramework.Model.Shared;
using Dms.Core.Extensions;
using Dms.Core.Utils;
using Dms.Services.Assembler;
using Dms.Services.Interface.Activity;
using Dms.Services.Interface.Lookup;
using Dms.Services.Interface.Message;
using Dms.Services.Interface.Security;
using Dms.Services.Interface.Shared;
using Dms.Services.Interface.Task;
using Dms.Services.ViewModel.Activity;
using Dms.Services.ViewModel.Lookup;
using Dms.Services.ViewModel.Shared;
using Dms.Services.ViewModel.Task;
using Dms.Services.ViewModel.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Dms.Services.Implementation.Activity
{
    public class AfsGroupThreePreApprovalRequestService : AfsPreApprovalRequestService
    {
        public AfsGroupThreePreApprovalRequestService(DmsContext context, ITaskService taskService, IDocumentService documentService, IActivityService activityService, IMessageService messageService, ISharedService sharedService, ILookupService lookupService, IUserService userService)
            : base(context, taskService, documentService, activityService, messageService, sharedService, lookupService, userService)
        { }

        public override PreApprovalRequestViewModel Get(int preApprovalRequestId, bool loadPreapprovalModifiedData, int postActivityId, int? cloaId = null)
        {
            _preApprovalRequestViewModel = base.Get(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId, cloaId);
          
            return _preApprovalRequestViewModel;
        }

        public override AfsGroupsPostActivityViewModel Get(int postActivityId, bool createDocumentVersion = false)
        {
            var postActivity = GetPostActivityById(postActivityId);

            if (postActivity != null)
            {
                MapPostActivityToPostActivityViewModel(postActivity);
            }

            _afsGroupsPostActivityViewModel.IsLatestVersion = postActivity?.PreApprovalRequest.PostActivities.OrderByDescending(pa => pa.Id).First().Id == postActivityId;
            _afsGroupsPostActivityViewModel.CompletedDate = postActivity?.CompletedDate;
                      
            _afsGroupsPostActivityViewModel.DocumentReference = _documentService.GetDocumentsByRef((int)DocumentReferenceEnum.PostActivity, postActivityId,null);

            return _afsGroupsPostActivityViewModel;
        }

        private void MapPostActivityToPostActivityViewModel(PostActivity postActivity)
        {
            _afsGroupsPostActivityViewModel.ActualEndDate = postActivity.ActualEndDate.HasValue ? postActivity.ActualEndDate.DateToString() : postActivity.PreApprovalRequest?.ProposeEndDate.DateToString();
            _afsGroupsPostActivityViewModel.ActualStartDate = postActivity.ActualStartDate.DateToString();
            _afsGroupsPostActivityViewModel.ActualStartTime = postActivity.ActualStartDate.ToTimeString();
            _afsGroupsPostActivityViewModel.ActualEndTime = postActivity.ActualEndDate.ToTimeString();
            if (postActivity.StatusId == (int)PreApprovalRequestStatusEnum.Initiated)
            {
                _afsGroupsPostActivityViewModel.ActualEndTime = DateTime.Now.ToTimeString();
            }
            
            _afsGroupsPostActivityViewModel.TimeZoneId = postActivity.TimeZoneId;
            _afsGroupsPostActivityViewModel.ApplicantAddressId = postActivity.ApplicantAddressId;
            _afsGroupsPostActivityViewModel.ApplicantCountry = postActivity.ApplicantCountry != null ? new CountryViewModel
            {
                Id = postActivity.ApplicantCountry.Id,
                Name = postActivity.ApplicantCountry.Name
            } : null;
            _afsGroupsPostActivityViewModel.Id = postActivity.Id;
            _afsGroupsPostActivityViewModel.IsAdditionalInstructionsProvided = postActivity.IsAdditionalInstructionsProvided;
            _afsGroupsPostActivityViewModel.IsFaaBasedAdditionalInstructionsProvided = postActivity.IsFaaBasedAdditionalInstructionsProvided;
            _afsGroupsPostActivityViewModel.PostActivityPaperWorkDate = postActivity.PostActivityPaperWorkDate.DateToString();
            _afsGroupsPostActivityViewModel.PracticalTestResultId = postActivity.PracticalTestResultId;
            _afsGroupsPostActivityViewModel.ObservationResultTypeId = postActivity.PostActivityObservationResultTypeId;
            _afsGroupsPostActivityViewModel.AreasOfOperation = postActivity.AreasOfOperation;
            _afsGroupsPostActivityViewModel.ReasonsOfDiscontinuance = postActivity.ReasonsOfDiscontinuance;
            _afsGroupsPostActivityViewModel.GroundPortionDuration = postActivity.GroundPortionDuration;
            _afsGroupsPostActivityViewModel.FlightPortionDuration = postActivity.FlightPortionDuration;
            _afsGroupsPostActivityViewModel.ObservationDuration = postActivity.ObservationDuration;
            _afsGroupsPostActivityViewModel.IacraStatusTypeId = postActivity.IacraStatusTypeId;
            _afsGroupsPostActivityViewModel.IacraApplicationId = postActivity.IacraApplicationId;
            _afsGroupsPostActivityViewModel.IacraFtn = postActivity.IacraFtn;
            _afsGroupsPostActivityViewModel.GradeCertificateId = postActivity.GradeCertificateTypeId;
            _afsGroupsPostActivityViewModel.AircraftCategoryId = postActivity.AircraftCategoryTypeId;
            _afsGroupsPostActivityViewModel.AircraftClassId = postActivity.AircraftClassTypeId;
            _afsGroupsPostActivityViewModel.Comments = postActivity.Comments;
            _afsGroupsPostActivityViewModel.StatusId = postActivity.StatusId;
            _afsGroupsPostActivityViewModel.TrackingNumber = postActivity.TrackingNumber;
            _afsGroupsPostActivityViewModel.IsAirManCertificateNotIssue = postActivity.IsAirmanCertificateNotIssued;
            _afsGroupsPostActivityViewModel.isMsReviewed = false;
            _afsGroupsPostActivityViewModel.ReasonsForAirManCertificateNotIssue = postActivity.ReasionForAirManCertificateNotIssue;
            _afsGroupsPostActivityViewModel.ApplicantAddress = postActivity.Address != null ? new AddressViewModel()
            {
                Id = postActivity.Address.Id,
                Address1 = postActivity.Address.AddressLine1,
                Address2 = postActivity.Address.AddressLine2,
                City = postActivity.Address.City,
                State = postActivity.Address.StateProvince != null ? new StateViewModel
                {
                    Id = postActivity.Address.StateProvince.Id,
                    Name = postActivity.Address.StateProvince.Name
                } : null,
                Country = postActivity.Address.Country != null ? new CountryViewModel
                {
                    Id = postActivity.Address.Country.Id,
                    Name = postActivity.Address.Country.Name
                } : null,
                ZipCode = postActivity.Address.ZipCode,
            }
            : new AddressViewModel();

            _afsGroupsPostActivityViewModel.Applicants = postActivity.PostActivityApplicants != null
                ? postActivity.PostActivityApplicants.Select(prod =>
                    new AfsPostActivityApplicantViewModel
                    {
                        Id = prod.Id,
                        PostActivityId = prod.PostActivityId,
                        ApplicantName = prod.ApplicantName,
                        CertificateNumber = prod.CertificateNumber
                    }).ToList()
                : new List<AfsPostActivityApplicantViewModel>() { };

            if (postActivity.FormData != null) _preApprovalRequestViewModel = CloaPreApprovalRequestViewModelMapper.DeSerializePreApprovalFormData(_context, postActivity.FormData, postActivity.PreApprovalRequest.CloaId);

            _afsGroupsPostActivityViewModel.PreApprovalRequest = _preApprovalRequestViewModel;
            _afsGroupsPostActivityViewModel.PreApprovalRequestId = postActivity.PreApprovalRequestId;
            _afsGroupsPostActivityViewModel.UserId = postActivity.CreatedBy;
            _afsGroupsPostActivityViewModel.PostActivityRecommendingInstructor = postActivity.PostActivityRecommendingInstructor;
            _afsGroupsPostActivityViewModel.PostActivityRecommendingInstructorCertificateNumber = postActivity.PostActivityRecommendingInstructorCertificateNumber;
        }

        [SuppressMessage("SonarQube", "S3776:Cognitive Complexity of methods should not be too high",
            Justification = "Persists an AFS group-three post-activity: maps the post-activity fields, then dispatches applicant/address persistence (multiple-applicants rebuild vs new/updated applicant address) and completed-vs-submit task creation and re-serialization. The complexity is breadth across the applicant/address and submit branches; control-flow restructuring does not bring it to threshold.")]
        public override AfsGroupsPostActivityViewModel SaveAfsPostActivity(AfsGroupsPostActivityViewModel model)
        {
            _postActivity = _context.PostActivities
                .Include(pa => pa.PreApprovalRequest)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(pr => pr.Cloa).ThenInclude(c => c.CloaAddresses).ThenInclude(a => a.Address)
                .Include(a => a.Address)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(p => p.PreApprovalRequestFunctionCodes).ThenInclude(it => it.Type)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(p => p.PreApprovalRequestFunctionCodes).ThenInclude(p => p.PreApprovalRequestFunctionCodeTypeRatings)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(p => p.PreApprovalRequestFunctionCodes).ThenInclude(p => p.PreApprovalRequestFunctionCodeMakeModels)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(p => p.AfsPreApprovalRequest)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(p => p.AfsPreApprovalRequest).ThenInclude(p => p.Address)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(p => p.AfsPreApprovalRequest).ThenInclude(p => p.AircraftOwnerAddress)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(p => p.AfsPreApprovalRequest).ThenInclude(p => p.AircraftOperatorAddress)
                .Include(pa => pa.PostActivityApplicants)
                .AsNoTracking()
                .Single(r => r.Id == model.Id);

            var isCompletedPreApproval =
                _postActivity.PreApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Completed;

            if (isCompletedPreApproval)
            {
                //new version must be created
                _postActivity.Address = null;
                _postActivity.ApplicantAddressId = null;
                _postActivity.Id = 0;
                _postActivity.CompletedDate = null;
            }

            _postActivity.IsAdditionalInstructionsProvided = model.IsAdditionalInstructionsProvided;
            _postActivity.IsFaaBasedAdditionalInstructionsProvided = model.IsFaaBasedAdditionalInstructionsProvided;
            _postActivity.AreasOfOperation = model.AreasOfOperation;
            _postActivity.ReasonsOfDiscontinuance = model.ReasonsOfDiscontinuance;
            _postActivity.ActualStartDate = model.ActualStartDate.ToNullableDateTime(model.ActualStartTime);
            _postActivity.ActualEndDate = !string.IsNullOrEmpty(model.ActualEndDate) ? model.ActualEndDate.ToNullableDateTime(model.ActualEndTime) : null;
            _postActivity.TimeZoneId = model.TimeZoneId;
            _postActivity.GroundPortionDuration = model.GroundPortionDuration;
            _postActivity.FlightPortionDuration = model.FlightPortionDuration;
            _postActivity.ObservationDuration = model.ObservationDuration;
            _postActivity.IacraStatusTypeId = model.IacraStatusTypeId;
            _postActivity.PostActivityPaperWorkDate = model.PostActivityPaperWorkDate.ToNullableDate();
            _postActivity.ApplicantCountryId = model.ApplicantCountry?.Id;
            _postActivity.PracticalTestResultId = model.PracticalTestResultId;
            _postActivity.PostActivityObservationResultTypeId = model.ObservationResultTypeId;
            _postActivity.GradeCertificateTypeId = model.GradeCertificateId;
            _postActivity.AircraftCategoryTypeId = model.AircraftCategoryId;
            _postActivity.AircraftClassTypeId = model.AircraftClassId;
            _postActivity.IsAirmanCertificateNotIssued = model.IsAirManCertificateNotIssue;
            _postActivity.ReasionForAirManCertificateNotIssue = model.ReasonsForAirManCertificateNotIssue;
            _postActivity.Comments = model.Comments;
            _postActivity.PostActivityRecommendingInstructor = model.PostActivityRecommendingInstructor;
            _postActivity.PostActivityRecommendingInstructorCertificateNumber = model.PostActivityRecommendingInstructorCertificateNumber;
            _postActivity.IacraApplicationId = model.IacraApplicationId;
            _postActivity.IacraFtn = model.IacraFtn;

            if (_postActivity.PreApprovalRequest.AfsPreApprovalRequest.IsMultipleApplicants.HasValue && _postActivity.PreApprovalRequest.AfsPreApprovalRequest.IsMultipleApplicants.Value)
            {
                if (_postActivity.PostActivityApplicants != null && !isCompletedPreApproval) // isCompletedPreApproval flag check for not deleting if new version is being created
                {
                    foreach (var a in _postActivity.PostActivityApplicants)
                    {
                        _context.Entry(a).State = EntityState.Deleted;
                    }
                }
                _postActivity.PostActivityApplicants = new List<PostActivityApplicant>();
                foreach (var a in model.Applicants)
                {
                    var newApplicant = new PostActivityApplicant
                    {
                        ApplicantName = a.ApplicantName,
                        CertificateNumber = a.CertificateNumber
                    };
                    _postActivity.PostActivityApplicants.Add(newApplicant);
                    _context.Entry(newApplicant).State = EntityState.Added;
                }
            }
            else if (_postActivity.Address == null && (model.ApplicantAddress.Address1 != null || model.ApplicantAddress.Address2 != null 
            ||  model.ApplicantAddress.City != null || model.ApplicantAddress.State != null || model.ApplicantAddress.Country != null 
            ||  model.ApplicantAddress.ZipCode != null))
            {
                _postActivity.Address = new Address
                {
                    AddressLine1 = model.ApplicantAddress.Address1,
                    AddressLine2 = model.ApplicantAddress.Address2,
                    City = model.ApplicantAddress.City,
                    StateId = model.ApplicantAddress.State?.Id,
                    CountryId = model.ApplicantAddress.Country?.Id,
                    ZipCode = model.ApplicantAddress.ZipCode
                };
                _context.Entry(_postActivity.Address).State = EntityState.Added;
            }
            else if (_postActivity.Address?.Id > 0)
            {
                _postActivity.Address.AddressLine1 = model.ApplicantAddress.Address1;
                _postActivity.Address.AddressLine2 = model.ApplicantAddress.Address2;
                _postActivity.Address.City = model.ApplicantAddress.City;
                _postActivity.Address.StateId = model.ApplicantAddress.State?.Id;
                _postActivity.Address.CountryId = model.ApplicantAddress.Country?.Id;
                _postActivity.Address.ZipCode = model.ApplicantAddress.ZipCode;
                _context.Entry(_postActivity.Address).State = EntityState.Modified;
            }
            var formData = CloaPreApprovalRequestViewModelMapper.SerializePreApproval(model.PreApprovalRequest);
            _postActivity.FormData = formData;
            var finalModel = SavePostActivityEvaluationHelper(model);

            _context.Entry(_postActivity).State = _postActivity.Id > 0 ? EntityState.Modified : EntityState.Added;
            _context.Entry(_postActivity.PreApprovalRequest).State = EntityState.Modified;
            _context.SaveChanges();

            if (isCompletedPreApproval)
            {
                //Create Task for MS Review
                _taskService.CreateTask(new TaskViewModel
                {
                    TaskSubTypeId = (int)TaskSubTypeEnum.ReviewPostActivityChanges,
                    TaskStatusId = (int)TaskStatusEnum.Pending,
                    ActionId = _postActivity.Id,
                    UserOfficeRoleId = _postActivity.PreApprovalRequest.Cloa.ManagingSpecialistId.GetValueOrDefault(),
                    ApplicationId = _postActivity.PreApprovalRequest.ApplicationId,
                }, false);

                // Update document reference
                finalModel.DocumentReference.ReferenceId = _postActivity.Id;
                finalModel.DocumentReference.SecondaryReferenceId = null;
            
            
            }
            else if (model.IsSubmit)
            {
                CheckForPreApprovalModifiedData(model);
                // Modified controls is saved
                _postActivity.FormData = CloaPreApprovalRequestViewModelMapper.SerializePreApproval(model.PreApprovalRequest);
                _context.Entry(_postActivity).State = _postActivity.Id > 0 ? EntityState.Modified : EntityState.Added;
                _context.Entry(_postActivity.PreApprovalRequest).State = EntityState.Modified;
            }

            _context.SaveChanges();
            finalModel.Id = _postActivity.Id;
            finalModel.StatusId = _postActivity.StatusId;
            finalModel.Comments = _postActivity.Comments;           
            return finalModel;
        }

        protected AfsGroupsPostActivityViewModel SavePostActivityEvaluationHelper(AfsGroupsPostActivityViewModel model)
        {
            var postActivity = _postActivity;
            postActivity.StatusId = model.IsSubmit ? (int)PreApprovalRequestStatusEnum.Completed : (int)PreApprovalRequestStatusEnum.Saved;
            if (model.IsSubmit)
            {
                postActivity.CompletedDate = DateTime.Now;
                postActivity.PreApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Completed;
            }
            return model;
        }

        protected void CheckForPreApprovalModifiedData(AfsGroupsPostActivityViewModel model)
        {
            var preApprovalRequest = _postActivity.PreApprovalRequest;
            var triggerCorrectiveActionTrigger = false;
            var postActivity = _postActivity;
            if (model.PreApprovalRequest.ModifiedPreapprovalControls == null)
            {
                model.PreApprovalRequest.ModifiedPreapprovalControls = new  Dictionary<string, List<ModifiedControlViewModel>>();
            }
            switch (model.DesigneeInfo.TypeId)
            {
                case (int)DesigneeTypeEnum.APD:
                    triggerCorrectiveActionTrigger = ApdPreApprovalRequestService.FindModifiedDataForApd(model, preApprovalRequest, model.PreApprovalRequest.ModifiedPreapprovalControls, postActivity);
                    var changedFuncCodesApd = FindAnyChangeInTypeRatings(model, preApprovalRequest, model.PreApprovalRequest.ModifiedPreapprovalControls);
                    if (!triggerCorrectiveActionTrigger)
                        triggerCorrectiveActionTrigger = changedFuncCodesApd;
                    var changedMakeModelApd = FindAnyChangeInMakeOrModel(model, preApprovalRequest, model.PreApprovalRequest.ModifiedPreapprovalControls);
                    if (!triggerCorrectiveActionTrigger)
                        triggerCorrectiveActionTrigger = changedMakeModelApd;
                    break;
                case (int)DesigneeTypeEnum.TCE:
                    triggerCorrectiveActionTrigger = TcePreApprovalRequestService.FindModifiedDataForTce(model, preApprovalRequest, model.PreApprovalRequest.ModifiedPreapprovalControls, postActivity);
                    var changedFuncCodesTce = FindAnyChangeInMakeModels(model, preApprovalRequest, model.PreApprovalRequest.ModifiedPreapprovalControls);
                    if (!triggerCorrectiveActionTrigger)
                        triggerCorrectiveActionTrigger = changedFuncCodesTce;
                    var changedMakeModelTce = FindAnyChangeInMakeOrModel(model, preApprovalRequest, model.PreApprovalRequest.ModifiedPreapprovalControls);
                    if (!triggerCorrectiveActionTrigger)
                        triggerCorrectiveActionTrigger = changedMakeModelTce;
                    break;
                case (int)DesigneeTypeEnum.DADE:
                    triggerCorrectiveActionTrigger = DadePreApprovalRequestService.FindModifiedDataForDade(model, preApprovalRequest, model.PreApprovalRequest.ModifiedPreapprovalControls, postActivity);
                    var changedMakeModelDade = FindAnyChangeInMakeOrModel(model, preApprovalRequest, model.PreApprovalRequest.ModifiedPreapprovalControls);
                    if (!triggerCorrectiveActionTrigger)
                        triggerCorrectiveActionTrigger = changedMakeModelDade;
                    break;
                default:
                    break;
            }

            if (triggerCorrectiveActionTrigger && model.IsSubmit)
            {


                    var activity = new Dms.Core.EntityFramework.Model.Activity.Activity
                    {
                        ModuleId = (int)ProcessTypeEnum.CorrectiveAction,
                        StatusId = (int)ActivityStatusEnum.Saved,
                        ActivityTypeId = (int)ActivityTypeEnum.PostActivityCorrectiveAction,
                        FormData = JsonConvert.SerializeObject(new { PostActivityId = _postActivity.Id }),
                        RequestDate = DateTime.Today,
                        TrackingNumber = _activityService.GenerateTrackingNumber(model.DesigneeInfo.CloaId, (int)ProcessTypeEnum.CorrectiveAction),
                        DueDate = DateTime.MaxValue, // to not show in any pending due date sections
                        CloaId = model.DesigneeInfo.CloaId,
                        ApplicationDueDate = new ApplicationDueDate()
                        {
                            ApplicationId = model.DesigneeInfo.ApplicationId,
                            DueDate = DateTime.MaxValue,
                        }
                    };
                    _context.Activities.Add(activity);
                    _context.SaveChanges();

                    _taskService.CreateTask(new TaskViewModel
                    {
                        TaskSubTypeId = (int)TaskSubTypeEnum.PostActivityCorrectiveAction,
                        TaskStatusId = (int)TaskStatusEnum.Pending,
                        UserOfficeRoleId = _postActivity.PreApprovalRequest.Cloa.ManagingSpecialistId.GetValueOrDefault(),
                        ApplicationId = _postActivity.PreApprovalRequest.ApplicationId,
                        ActionId = activity.Id
                    }, true);
            }
        }

        private bool FindAnyChangeInMakeOrModel(AfsGroupsPostActivityViewModel model, PreApprovalRequest preApprovalRequest, Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls)
        {
            bool foundChange = false;
            if (model.PreApprovalRequest.TestCheckInformation.AircraftMakeModelId?.Id != preApprovalRequest.AfsPreApprovalRequest.AircraftMakeModelId)
            {
                var poMakeModel = model.PreApprovalRequest.TestCheckInformation.AircraftMakeModelId != null ? _context.MakeModel.Where(w => w.Id == (model.PreApprovalRequest.TestCheckInformation.AircraftMakeModelId.Id)).FirstOrDefault() : null;
                var paMakeModel = _context.MakeModel.Where(w => w.Id == (preApprovalRequest.AfsPreApprovalRequest.AircraftMakeModelId)).FirstOrDefault();

                foundChange = poMakeModel != null && paMakeModel != null && (poMakeModel.Make.Trim().ToLower() != paMakeModel.Make.Trim().ToLower() ||
                                poMakeModel.Model.Trim().ToLower() != paMakeModel.Model.Trim().ToLower()) ? true : false;
                if (foundChange) {
                    AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel {Control = "aircraftMakeModelId", TriggersCorrectiveAction = true}, modifiedPreapprovalControls);
                } else {
                    AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel {Control = "aircraftMakeModelId", TriggersCorrectiveAction = false}, modifiedPreapprovalControls);
                }

            }
            return foundChange;
        }

        public static bool FindModifiedDataForApdTce(AfsGroupsPostActivityViewModel model, PreApprovalRequest preApprovalRequest,
                                                        Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls, PostActivity postActivity)
        {
            bool triggerCorrectiveActiveTrigger = false;
            if (model.PreApprovalRequest.TestCheckInformation.IsOtherAdminActivity.GetValueOrDefault(false) != preApprovalRequest.AfsPreApprovalRequest.IsOtherAdminActivity && preApprovalRequest.AfsPreApprovalRequest.IsOtherAdminActivity != null)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("TestCheckInformation",  new ModifiedControlViewModel {Control = "isOtherAdminActivity"}, modifiedPreapprovalControls);
            }           

            if (model.IsAdmin && model.PreApprovalRequest.TestCheckInformation.PracticalOralTestId != preApprovalRequest.AfsPreApprovalRequest.PracticalOralTestId && preApprovalRequest.AfsPreApprovalRequest.PracticalOralTestId != null)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("TestCheckInformation",  new ModifiedControlViewModel {Control = "practicalOralTestId"}, modifiedPreapprovalControls);
            }

            if (model.PreApprovalRequest.TestCheckInformation.IsAircraftNotRequired.GetValueOrDefault() != preApprovalRequest.AfsPreApprovalRequest.IsAircraftNotRequired.GetValueOrDefault())
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("TestCheckInformation",  new ModifiedControlViewModel {Control = "isAircraftNotRequired"}, modifiedPreapprovalControls);
            }
            if (model.PreApprovalRequest.TestCheckInformation.IsFlightPortionOnly.GetValueOrDefault() != preApprovalRequest.AfsPreApprovalRequest.IsFlightPortionOnly.GetValueOrDefault())
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("TestCheckInformation",  new ModifiedControlViewModel {Control = "isFlightPortionOnly"}, modifiedPreapprovalControls);
            }
            if (model.PreApprovalRequest.TestCheckInformation.ReasonforAuthorization != preApprovalRequest.AfsPreApprovalRequest.TemporaryAuthorizationReason)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("TestCheckInformation",  new ModifiedControlViewModel {Control = "temporaryAuthorizationReason"}, modifiedPreapprovalControls);
            }
            if (!string.IsNullOrEmpty(model.ActualStartDate))
            {
                DateTime actualStartDate = Convert.ToDateTime(model.ActualStartDate);
                DateTime? actualEndDate = null;
                if (!string.IsNullOrEmpty(model.ActualEndDate))
                {
                    actualEndDate = Convert.ToDateTime(model.ActualEndDate);
                }

                var proposedStartDate = preApprovalRequest.AfsPreApprovalRequest.PreApprovalRequest.ProposeStartDate;
                var proposedEndDate = preApprovalRequest.AfsPreApprovalRequest.PreApprovalRequest.ProposeEndDate;

                var isProposedDateNotWithinRange = actualEndDate != null &&
                                                    !((actualStartDate >= proposedStartDate && actualStartDate <= proposedEndDate) &&
                                                    (actualEndDate >= proposedStartDate && actualEndDate <= proposedEndDate));
                if (isProposedDateNotWithinRange)
                {
                    triggerCorrectiveActiveTrigger = true;
                    AddItemToModifiedPreapprovals("TestInformation",  new ModifiedControlViewModel {Control = "proposeStartDate"}, modifiedPreapprovalControls);
                }
            }

            if (CheckForAddress(model, preApprovalRequest, modifiedPreapprovalControls, postActivity))
            {
                triggerCorrectiveActiveTrigger = true;
            }


            return triggerCorrectiveActiveTrigger;
        }
        
        [SuppressMessage("SonarQube", "S3776:Cognitive Complexity of methods should not be too high",
            Justification = "Detects activity-location address changes field by field, gated on address presence and non-line-check, plus line-check and facility-on-record change flags. The nesting is a single necessary null-guard around many flat field comparisons with no early-exit point; it is not reducible by the sanctioned transformations.")]
        protected static bool CheckForAddress(AfsGroupsPostActivityViewModel model, PreApprovalRequest preApprovalRequest,
                                                Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls, PostActivity postActivity)
        {
            bool triggerCorrectiveActiveTrigger = false;
            if (preApprovalRequest.AfsPreApprovalRequest.Address != null && model.PreApprovalRequest.ActivityLocation.IsLineCheck.GetValueOrDefault() == false)
            {
                    if ((model.PreApprovalRequest.ActivityLocation.LocationAddress.Name != null) && model.PreApprovalRequest.ActivityLocation.LocationAddress.Name != preApprovalRequest.AfsPreApprovalRequest.Address.Name)
                    {
                        triggerCorrectiveActiveTrigger = true;
                        AddItemToModifiedPreapprovals("LocationAddress", new ModifiedControlViewModel {Control = "name"}, modifiedPreapprovalControls);
                    }
                    if (model.PreApprovalRequest.ActivityLocation.LocationAddress.Address1 != preApprovalRequest.AfsPreApprovalRequest.Address.AddressLine1)
                    {
                        triggerCorrectiveActiveTrigger = true;
                        AddItemToModifiedPreapprovals("LocationAddress", new ModifiedControlViewModel {Control = "addressLine1"}, modifiedPreapprovalControls);
                    }
                    if (model.PreApprovalRequest.ActivityLocation.LocationAddress.Address2 != preApprovalRequest.AfsPreApprovalRequest.Address.AddressLine2)
                    {
                        triggerCorrectiveActiveTrigger = true;
                        AddItemToModifiedPreapprovals("LocationAddress", new ModifiedControlViewModel {Control = "addressLine2"}, modifiedPreapprovalControls);
                    }
                    if (model.PreApprovalRequest.ActivityLocation.LocationAddress.City != preApprovalRequest.AfsPreApprovalRequest.Address.City)
                    {
                        triggerCorrectiveActiveTrigger = true;
                        AddItemToModifiedPreapprovals("LocationAddress", new ModifiedControlViewModel {Control = "city"}, modifiedPreapprovalControls);
                    }
                    if (model.PreApprovalRequest.ActivityLocation.LocationAddress.State?.Id != preApprovalRequest.AfsPreApprovalRequest.Address.StateId)
                    {
                        triggerCorrectiveActiveTrigger = true;
                        AddItemToModifiedPreapprovals("LocationAddress", new ModifiedControlViewModel {Control = "stateId"}, modifiedPreapprovalControls);
                    }
                    if (model.PreApprovalRequest.ActivityLocation.LocationAddress.Country?.Id != preApprovalRequest.AfsPreApprovalRequest.Address.CountryId)
                    {
                        triggerCorrectiveActiveTrigger = true;
                        AddItemToModifiedPreapprovals("LocationAddress", new ModifiedControlViewModel {Control = "countryId"}, modifiedPreapprovalControls);
                    }
                    if (model.PreApprovalRequest.ActivityLocation.LocationAddress.ZipCode != preApprovalRequest.AfsPreApprovalRequest.Address.ZipCode)
                    {
                        triggerCorrectiveActiveTrigger = true;
                        AddItemToModifiedPreapprovals("LocationAddress", new ModifiedControlViewModel {Control = "zipCode"}, modifiedPreapprovalControls);
                    }
            }

            if (model.PreApprovalRequest.ActivityLocation.IsLineCheck.GetValueOrDefault() != preApprovalRequest.AfsPreApprovalRequest.IsLineCheck.GetValueOrDefault())
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("LocationAddress", new ModifiedControlViewModel {Control = "isLineCheck"}, modifiedPreapprovalControls);
            }

            if (model.PreApprovalRequest.ActivityLocation.FacilityonRecord.GetValueOrDefault() != IsFacilityOnRecord(preApprovalRequest, postActivity))
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("LocationAddress", new ModifiedControlViewModel {Control = "facilityonRecord"}, modifiedPreapprovalControls);
            }

            return triggerCorrectiveActiveTrigger;
        }

        public static bool IsFacilityOnRecord(PreApprovalRequest preApprovalRequest, PostActivity postActivity)
        {

            return postActivity.PreApprovalRequest.Cloa.CloaAddresses.Count(ar => ar.Address.AddressTypeId == (int)AddressTypeEnum.WorkAddress) > 1
                                                    ? postActivity.PreApprovalRequest.Cloa.CloaAddresses.Any(a => a.AddressId == preApprovalRequest.AfsPreApprovalRequest.ActivityLocationAddressId)
                                                      : postActivity.PreApprovalRequest.Cloa.CloaAddresses != null
                                                    ? preApprovalRequest.AfsPreApprovalRequest.ActivityLocationAddressId == postActivity.PreApprovalRequest.Cloa.CloaAddresses.FirstOrDefault(ar => ar.Address.AddressTypeId == (int)AddressTypeEnum.WorkAddress)?.AddressId
                                                      : false;
        }
        private static bool FindAnyChangeInTypeRatings(AfsGroupsPostActivityViewModel model, PreApprovalRequest preApprovalRequest, Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls)
        {
            bool foundChange = false;
            if (preApprovalRequest.PreApprovalRequestFunctionCodes != null &&
                model.PreApprovalRequest.TestCheckInformation != null)
            {
                var preapprovalFunctionCodes = preApprovalRequest.PreApprovalRequestFunctionCodes.Select(p => new FunctionCodeViewModel
                {
                    Id = p.FunctionCodeId,
                    SelectedTypeRatings = p.PreApprovalRequestFunctionCodeTypeRatings?.Select(pm => pm.TypeRatingId).ToArray()
                }).ToArray();
                foundChange = FindChangeInFuctionCodes(model, modifiedPreapprovalControls, preapprovalFunctionCodes);
            }

            return foundChange;
        }

        [SuppressMessage("SonarQube", "S3776:Cognitive Complexity of methods should not be too high",
            Justification = "Detects function-code changes by building the effective authorization set from the post-activity's temporary and request authorizations (presence-based merge) then set-differencing against the pre-approval function codes. The complexity is breadth across the authorization-presence cases; the one available guard inversion does not bring it to threshold.")]
        private static bool FindChangeInFuctionCodes(AfsGroupsPostActivityViewModel model, Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls, FunctionCodeViewModel[] preapprovalFunctionCodes)
        {
            bool foundChange = false;
            List<FunctionCodeViewModel> selectedTemporaryAuthorizations = null;
            if (model.PreApprovalRequest.TestCheckInformation.SelectedTemporaryAuthorizations != null && model.PreApprovalRequest.TestCheckInformation.SelectedTemporaryAuthorizations.Count > 0)
            {
                selectedTemporaryAuthorizations = model.PreApprovalRequest.TestCheckInformation.SelectedTemporaryAuthorizations.Select(f => new FunctionCodeViewModel
                {
                    Id = f.Id,
                    HasTypeRating = f.HasTypeRating,
                    SelectedTypeRatings = f.SelectedTypeRatings?.ToList()
                }).ToList();
            }
            List<FunctionCodeViewModel> selectedRequestAuthorizations = null;
            if (model.PreApprovalRequest.TestCheckInformation.SelectedRequestAuthorizations != null && model.PreApprovalRequest.TestCheckInformation.SelectedRequestAuthorizations.Count > 0)
            {
                selectedRequestAuthorizations = model.PreApprovalRequest.TestCheckInformation.SelectedRequestAuthorizations.Select(f => new FunctionCodeViewModel
                {
                    Id = f.Id,
                    HasTypeRating = f.HasTypeRating,
                    SelectedTypeRatings = f.SelectedTypeRatings?.ToList()
                }).ToList();
            }
            List<FunctionCodeViewModel> vmFunctionCodes = null;

            if (selectedTemporaryAuthorizations != null && selectedTemporaryAuthorizations.Count > 0)
            {
                if (selectedRequestAuthorizations != null && selectedRequestAuthorizations.Count > 0)
                {
                    vmFunctionCodes = selectedTemporaryAuthorizations.Union(selectedRequestAuthorizations).ToList();
                }
                else
                {
                    vmFunctionCodes = selectedTemporaryAuthorizations;
                }
            }
            else
            {
                if (selectedRequestAuthorizations != null && selectedRequestAuthorizations.Count > 0)
                {
                    vmFunctionCodes = selectedRequestAuthorizations;
                }
            }

            if (vmFunctionCodes != null && vmFunctionCodes.Any())
            {
                var differences = vmFunctionCodes.Except(preapprovalFunctionCodes, new FunctionCodeComparer()).ToList();
                var predifferences = preapprovalFunctionCodes.Except(vmFunctionCodes, new FunctionCodeComparer()).ToList();
                if (differences.Count > 0 || predifferences.Count > 0)
                {
                    foundChange = true;
                    AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel {Control = "functionCode"}, modifiedPreapprovalControls);
                }
            }

            return foundChange;
        }

        private static bool FindAnyChangeInMakeModels(AfsGroupsPostActivityViewModel model, PreApprovalRequest preApprovalRequest, Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls)
        {
            bool foundChange = false;
            if (preApprovalRequest.PreApprovalRequestFunctionCodes != null &&
                model.PreApprovalRequest.TestCheckInformation != null)
            {
                var preapprovalFunctionCodes = preApprovalRequest.PreApprovalRequestFunctionCodes.Select(p => new FunctionCodeViewModel
                {
                    Id = p.FunctionCodeId,
                    HasTypeRating = p.PreApprovalRequestFunctionCodeMakeModels != null ? p.PreApprovalRequestFunctionCodeMakeModels.Select(pm => pm.MakeModelId).Any() : false,
                    SelectedTypeRatings = p.PreApprovalRequestFunctionCodeMakeModels?.Select(pm => pm.MakeModelId).ToArray()
                }).ToArray();

                foundChange = FindChangeInFuctionCodes(model, modifiedPreapprovalControls, preapprovalFunctionCodes);
            }

            return foundChange;
        }
        public override IList<AfsGroupsPostActivityViewModel> GetGroupThreePostActivityVersions(int postActivityId)
        {
            var preApprovalRequest = _context.PostActivities.Where(p => p.Id == postActivityId).SelectMany(s => s.PreApprovalRequest.PostActivities.Select(p => new
            {
                PreApprovalRequestId = p.PreApprovalRequestId,
                PostActivityId = p.Id
            })).AsEnumerable()
            .GroupBy(p => p.PreApprovalRequestId).Select(p => new
            {
                PreApprovalRequestId = p.Key,
                CurrentVersionPostActivityId = postActivityId,
                PreviousVersionPostActivityId = p.OrderByDescending(po => po.PostActivityId).First(po => po.PostActivityId < postActivityId).PostActivityId
            }).First();
            // NOTE: For not over-writing the object from previous to current - deep copy
            var currentPostActivty = (AfsGroupsPostActivityViewModel)(Get(preApprovalRequest.CurrentVersionPostActivityId).GetClone());
            var model = new List<AfsGroupsPostActivityViewModel>()
            {
                currentPostActivty,
                Get(preApprovalRequest.PreviousVersionPostActivityId)
            };

            return model;
        }

        public override bool SaveMsPostActivityReview(AfsGroupsPostActivityViewModel model)
        {
            if (model != null)
            {
                //Complete the Task Status.
                var task = _context.Tasks.FirstOrDefault(it => it.ActionId == model.Id && it.StatusId == (int)TaskStatusEnum.Pending && it.SubTypeId == (int)TaskSubTypeEnum.ReviewPostActivityChanges);
                if (task != null)
                {
                    task.StatusId = (int)TaskStatusEnum.Completed;
                }
                _context.SaveChanges();
                return true;
            }
            else
                return false;
        }

        [SuppressMessage("SonarQube", "S3776:Cognitive Complexity of methods should not be too high",
            Justification = "Persists the managing-specialist pre-approval decision: short-circuits an already-approved request, sets decision fields, then branches approve (create initiated post-activity, notify) vs reject (reject pending post-activity, notify) and completes the pending review task. The complexity is breadth across the decision branches; the available guard inversion does not bring it to threshold.")]
        public override bool SaveMsDecision(PreApprovalRequestViewModel model)
        {
            var preApprovalRequest = _context.PreApprovalRequests.Include(p => p.AfsPreApprovalRequest)
            .Include(p => p.PreApprovalRequestFunctionCodes).Include(p => p.PostActivities).FirstOrDefault(p => p.Id == model.Id);

            if (preApprovalRequest != null)
            {
                if (preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Approved)
                {
                    if (model.IsPossibleDirectObservation.HasValue)
                    {
                        preApprovalRequest.IsPossibleDirectObservation = model.IsPossibleDirectObservation.Value;
                    }                    
                    _context.SaveChanges();
                    return true;
                }

                preApprovalRequest.IsPossibleDirectObservation = model.IsPossibleDirectObservation;
                preApprovalRequest.ApproverComments = model.ApproverComment;
                preApprovalRequest.IsApproved = model.IsApproved;
                preApprovalRequest.ApproverJustification = model.ApproverJusitification;
                preApprovalRequest.ApproverOfficeRoleId = model.ApprovedBy;

                if (model.IsApproved == true)
                {
                    preApprovalRequest.IsPreApprovalOnHold = false;
                    preApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Approved;
                    if (preApprovalRequest.PostActivities.Count == 0)
                    {
                        preApprovalRequest.PostActivities = new List<PostActivity>
                        {
                            new PostActivity
                            {
                                StatusId = (int)PreApprovalRequestStatusEnum.Initiated,
                                TrackingNumber = model.TrackingNumber.Replace("PR", "PO"),
                                ApprovalDate = DateTime.Now,
                                FormData = CloaPreApprovalRequestViewModelMapper.SerializePreApproval(model)
                            }
                        };
                    }
                    //Send approved notification.
                    SendPreapprovalDecisionNotification(model.TrackingNumber, model.DesigneeInfo.Name, model.DesigneeInfo.Id, "PREAPR");
                }
                else
                {
                    preApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Rejected;

                    if (model.IsPossibleDirectObservation.HasValue && model.IsPossibleDirectObservation.Value)
                    {
                        var pendingPostActivity = preApprovalRequest.PostActivities.FirstOrDefault(po => po.StatusId == (int)PreApprovalRequestStatusEnum.Initiated);
                        if (pendingPostActivity != null)
                        {
                            pendingPostActivity.StatusId = (int)PreApprovalRequestStatusEnum.Rejected;
                        }
                    }

                    //send denied notification.
                    SendPreapprovalDecisionNotification(model.TrackingNumber, model.DesigneeInfo.Name, model.DesigneeInfo.Id, "PRERJT");
                }


                //Complete the Task Status.
                var task = _context.Tasks.FirstOrDefault(it => it.ActionId == preApprovalRequest.Id && it.StatusId == (int)TaskStatusEnum.Pending && (it.SubTypeId == (int)TaskSubTypeEnum.PreApprovalRequest || it.SubTypeId == (int)TaskSubTypeEnum.GeographicExpansionRequest));
                if (task != null)
                {
                    task.StatusId = (int)TaskStatusEnum.Completed;
                    _context.SaveChanges();
                }
                _context.SaveChanges();
                return true;
            }
            else
            {
                return false;
            }
        }

        protected PreApprovalRequestViewModel GetAfsHelperTwo(int preApprovalRequestId)
        {
            var preApprvoalRequest = _preApprovalRequest;
            var preApprovalRequestViewModel = _preApprovalRequestViewModel;
            // Get GeographicExpansionAvailable
            preApprovalRequestViewModel.IsGeographicExpansionAvailable = preApprvoalRequest.AfsPreApprovalRequest.IsOutsideOfficeDistrict.HasValue && preApprvoalRequest.AfsPreApprovalRequest.IsOutsideOfficeDistrict.Value;
            if (preApprovalRequestViewModel.IsGeographicExpansionAvailable)
            {
                preApprovalRequestViewModel.GeographicExpansionUserOfficeRoleIds = _context.UserOfficeRoles.Include(o => o.OfficeRole)
                 .Where(uor => uor.OfficeRole.OfficeId == preApprvoalRequest.AfsPreApprovalRequest.OfficeId && uor.OfficeRole.RoleId == (int)RoleEnum.GeographicExpansionCoordinator && uor.IsActive)
                  .Select(it => it.Id).ToArray();
            }
            preApprovalRequestViewModel.DocumentReference = _documentService.GetDocumentsByRef((int)DocumentReferenceEnum.PreapprovalRequest, preApprovalRequestId, null);

            return preApprovalRequestViewModel;
        }

        protected List<FunctionCodeViewModel> GetTemporaryAuthorizations()
        {
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

            return tempauthorizations;
        }

        protected void GetAdminFunctionCodesByCloaFunctionCodes()
        {
            _preApprovalRequestViewModel.TestCheckInformation.AdminDpeSaeFunctionCodes = _cloa.DesigneeFunctionCodes.Where(x => x.FunctionCodeTypeId == (int)FunctionCodeTypeEnum.Admin).Select(c => new FunctionCodeViewModel
            {
                Id = c.Id,
                FunctionCode = c.FunctionCode,
                DesigneeTypeId = c.DesigneeTypeId,
                CategoryId = c.CategoryId ?? 0,
                IsAutomaticPreapproval = c.IsAutomaticPreapproval,
                Category = c.Category != null ? new CategoryViewModel
                {
                    Id = c.Category.Id,
                    Name = c.Category.Name
                } : null,
                HasTypeRating = c.HasTypeRating,
            }).ToList();
        }

        public override PreApprovalRequestViewModel Copy(int id)
        {
            var preApprovalRequest = base.Copy(id);
            preApprovalRequest.IsApproved = false;
            return preApprovalRequest;
        }
    }

}
