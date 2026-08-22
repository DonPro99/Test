using System;
using System.Collections.Generic;
using System.Linq;
using Dms.Core.EntityFramework.Data;
using Dms.Core.EntityFramework.Model.Activity;
using Dms.Core.Extensions;
using Dms.Core.Utils;
using Dms.Services.Assembler;
using Dms.Services.Interface.Activity;
using Dms.Services.Interface.Lookup;
using Dms.Services.Interface.Message;
using Dms.Services.Interface.Security;
using Dms.Services.Interface.Shared;
using Dms.Services.Interface.Task;
using Dms.Services.ViewModel;
using Dms.Services.ViewModel.Activity;
using Microsoft.EntityFrameworkCore;

namespace Dms.Services.Implementation.Activity
{
    public class AdminPePreApprovalRequestService : AfsGroupTwoPreApprovalRequestService
    {
        public AdminPePreApprovalRequestService(DmsContext context, ITaskService taskService,
            IDocumentService documentService, IActivityService activityService, IMessageService messageService,
            ISharedService sharedService, ILookupService lookupService, IUserService userService)
            : base(context, taskService, documentService, activityService, messageService, sharedService, lookupService,
                userService)
        {
            _designeeType = (int)DesigneeTypeEnum.ADMINPE;
        }

        public override PreApprovalRequestViewModel GetNew(int applicationId)
        {
            base.GetNew(applicationId);
            _preApprovalRequestViewModel.DesigneeFunctionCodes = _cloa.DesigneeFunctionCodes;
            _preApprovalRequestViewModel.IsAfsType = true;
            _preApprovalRequestViewModel.PreApprovalDateWiseCount =
                GetPreApprovalDateWiseCount(_cloa.DesigneeInformation.ApplicationId);

            return _preApprovalRequestViewModel;
        }

        public override PreApprovalRequestViewModel Get(int preApprovalRequestId, bool loadPreapprovalModifiedData,
            int postActivityId, int? cloaId = null)
        {
            base.Get(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId, cloaId);

            if (loadPreapprovalModifiedData && _context.PostActivities.Where(e=>e.PreApprovalRequestId == _preApprovalRequest.Id).Any() &&
                
                _preApprovalRequestViewModel.AfsPostActivity.AfsPostActivityModifiedPreApprovalViewModel != null)
            {
                LoadPreApprovalModifiedData(_preApprovalRequestViewModel, _preApprovalRequestViewModel.AfsPostActivity,
                    _cloa.DesigneeInfo.TypeId, _preApprovalRequestViewModel.ModifiedPreapprovalControls);
            }

            GetHelper(_preApprovalRequestViewModel);
            return _preApprovalRequestViewModel;
        }

        public override bool Cancel(ActivityPaperWorkViewModel model)
        {
            return base.Cancel(model);
        }

        protected override IEnumerable<PreApprovalDateWiseCountViewModel> GetPreApprovalDateWiseCount(int applicationId)
        {
            return base.GetPreApprovalDateWiseCount(applicationId);
        }

        public override PreApprovalRequestViewModel Save(PreApprovalRequestViewModel model)
        {
            base.Save(model);
            //The same for both model.id==0 and model.id!=0
            SaveDpeSaeFunctionCodes(model, _preApprovalRequest);
            SaveCertificateRatingsGroup2(model, _preApprovalRequest);
            if (model.Id != 0)
            {
                //Delete Products if already
                if (model.PlannedActivity != null && model.PlannedActivity.Products != null &&
                    model.PlannedActivity.Products.Any())
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

            SaveHelper(model);
            CompleteOrCreateTask(model, IsAutoPreApproval(model));
            return _preApprovalRequestViewModel;
        }

        public override void CreateTask(PreApprovalRequestViewModel model)
        {
            base.CreateTask(model);
        }

        public override PreApprovalRequestViewModel SavePostActivityEvaluation(PreApprovalRequestViewModel adminModel)
        {
            return base.SavePostActivityEvaluation(adminModel);
        }

        public override bool SaveMsDecision(PreApprovalRequestViewModel model)
        {
            return base.SaveMsDecision(model);
        }

        public static bool FindModifiedDataForAdminPe(PreApprovalRequestViewModel model,
            PreApprovalRequest preApprovalRequest,
            AfsPostActivityModifiedPreApprovalViewModel afsPostActivityModifiedPreApprovalViewModel,
            bool triggerCorrectiveActiveTrigger)
        {
            //Type Of Activity
            if (model.TestInformation.PracticalOralTestId !=
                preApprovalRequest.AfsPreApprovalRequest.PracticalOralTestId &&
                preApprovalRequest.AfsPreApprovalRequest.PracticalOralTestId != null)
            {
                afsPostActivityModifiedPreApprovalViewModel.PracticalOralTestId =
                    model.TestInformation.PracticalOralTestId;
                triggerCorrectiveActiveTrigger = true;
            }

            if (model.TestCheckInformation.IsOtherAdminActivity.GetValueOrDefault() !=
                preApprovalRequest.AfsPreApprovalRequest.IsOtherAdminActivity)
            {
                afsPostActivityModifiedPreApprovalViewModel.IsOtherAdminActivity =
                    model.TestCheckInformation.IsOtherAdminActivity;
                triggerCorrectiveActiveTrigger = true;
            }

            if (model.TestInformation.ProposeStartDate != preApprovalRequest.ProposeStartDate.DateToString())
            {
                afsPostActivityModifiedPreApprovalViewModel.ProposeStartDate = model.TestInformation.ProposeStartDate;
                triggerCorrectiveActiveTrigger = true;
            }

            if (model.TestCheckInformation.AircraftMakeModelId?.Id !=
                preApprovalRequest.AfsPreApprovalRequest.AircraftMakeModelId)
            {
                afsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId =
                    model.TestCheckInformation.AircraftMakeModelId?.Id;
                afsPostActivityModifiedPreApprovalViewModel.AircraftMakeModel =
                    model.TestCheckInformation.AircraftMakeModelId?.Name;
            }

            triggerCorrectiveActiveTrigger =
                !CheckForAddress(model, preApprovalRequest, afsPostActivityModifiedPreApprovalViewModel, false) &&
                !triggerCorrectiveActiveTrigger
                    ? false
                    : true;
            return triggerCorrectiveActiveTrigger;
        }

        public override int ReInitiate(int preApprovalRequestId)
        {
            var newPreapprovalRequest = Get(preApprovalRequestId, false, 0);
            CancelHelper(preApprovalRequestId);
            newPreapprovalRequest.Id = 0;
            newPreapprovalRequest.isSubmit = false;
            newPreapprovalRequest.RequestInfo.ControlNumber =
                _activityService.GenerateTrackingNumber(newPreapprovalRequest.DesigneeInfo.CloaId,
                    (int)ProcessTypeEnum.PreApproval);
            newPreapprovalRequest.IsAfsType = true;
            newPreapprovalRequest.IsCancel = true;
            newPreapprovalRequest = Save(newPreapprovalRequest);
            return newPreapprovalRequest.Id;
        }

        public override AfsGroupsPostActivityViewModel Get(int postActivityId, bool createDocumentVersion = false)
        {
            base.Get(postActivityId, createDocumentVersion);

            _afsGroupsPostActivityViewModel.GeneralComments = _postActivity.GeneralComments;

            _afsGroupsPostActivityViewModel.LocationDirections = _postActivity.LocationDirections;
            _afsGroupsPostActivityViewModel.PointOfContactName = _postActivity.PointOfContactName;
            _afsGroupsPostActivityViewModel.PointOfContactPhone = _postActivity.PointOfContactPhone;

            return _afsGroupsPostActivityViewModel;
        }

        public override AfsGroupsPostActivityViewModel SaveAfsPostActivity(AfsGroupsPostActivityViewModel model)
        {
            SaveAfsPostActivityHelper(model);
            base.SaveAfsPostActivity(model);

             if (!model.IsSubmit || model.StatusId == (int)PreApprovalRequestStatusEnum.Completed){
                _postActivity.FormData = CloaPreApprovalRequestViewModelMapper.SerializePreApproval(model.PreApprovalRequest);
            }
            
            _context.Entry(_postActivity).State = _postActivity.Id > 0 ? EntityState.Modified : EntityState.Added;
            _context.Entry(_postActivity.PreApprovalRequest).State = EntityState.Modified;
            _context.SaveChanges();

            if (model.StatusId == (int)PreApprovalRequestStatusEnum.Completed)
            {                
                 CreateReviewPostActivityTask(model.PreApprovalRequest.ApplicationId, _postActivity.Id);
            }             
            else if (model.IsSubmit)
            {
                CheckPreApprovalModifiedData(model); 
                _postActivity.FormData = CloaPreApprovalRequestViewModelMapper.SerializePreApproval(model.PreApprovalRequest);         
                _context.Entry(_postActivity).State = _postActivity.Id > 0 ? EntityState.Modified : EntityState.Added;
                _context.Entry(_postActivity.PreApprovalRequest).State = EntityState.Modified;
            }
            
            _context.SaveChanges();
            model.Id = _postActivity.Id;

            return model;
        }

        private void CheckPreApprovalModifiedData(AfsGroupsPostActivityViewModel model)
        {
            model.PreApprovalRequest.ModifiedPreapprovalControls = new  Dictionary<string, List<ModifiedControlViewModel>>();

            var preApprovalRequest = base.GetOriginalPreApprovalData(model.PreApprovalRequestId);
            bool caTrigger = false;
            base.CheckForPreApprovalModifiedData(model, preApprovalRequest, ref caTrigger,
                model.PreApprovalRequest.ModifiedPreapprovalControls);

            if (model.PreApprovalRequest.TestCheckInformation.IsOtherAdminActivity.GetValueOrDefault() !=
                preApprovalRequest.AfsPreApprovalRequest.IsOtherAdminActivity)
            {
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel {Control = "isOtherAdminActivity"}, model.PreApprovalRequest.ModifiedPreapprovalControls);
                caTrigger = true;
            }

            if (model.PreApprovalRequest.TestInformation.ProposeStartDate !=
                preApprovalRequest.ProposeStartDate.DateToString())
            {
                AddItemToModifiedPreapprovals("TestInformation", new ModifiedControlViewModel { Control = "proposeStartDate" }, model.PreApprovalRequest.ModifiedPreapprovalControls);
                caTrigger = true;
            }

            var dateTriggerCorrectiveActionTrigger = false;
            var proposedStartDate = preApprovalRequest.ProposeStartDate.Value;
            var proposedEndDate = preApprovalRequest.ProposeEndDate.Value;
            if (model.AfsPostActivityGroup2 != null &&
                !string.IsNullOrEmpty(model.AfsPostActivityGroup2.ActualStartDate))
            {
                var actualStartDate = DateTime.Parse(model.AfsPostActivityGroup2.ActualStartDate);
                dateTriggerCorrectiveActionTrigger = !(actualStartDate.Date >= proposedStartDate.Date &&
                                                       actualStartDate.Date <= proposedEndDate);
                AddItemToModifiedPreapprovals("TestInformation", new ModifiedControlViewModel { Control = "proposeStartDate" }, model.PreApprovalRequest.ModifiedPreapprovalControls);
            }

            caTrigger = dateTriggerCorrectiveActionTrigger || caTrigger;

            if (caTrigger)
            {                      
                CreateCorrectiveAction(
                    preApprovalRequest.CloaId,
                    model.PreApprovalRequest.ApplicationId.GetValueOrDefault(),
                    model.PreApprovalRequest.ManagingSpecialist
                );
            }
        }

        public override IList<AfsGroupsPostActivityViewModel> GetPostActivityVersions(int postActivityId)
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
            var currentPostActivty = Get(preApprovalRequest.CurrentVersionPostActivityId).GetClone();
            var prevPostActivity = Get(preApprovalRequest.PreviousVersionPostActivityId);
            var model = new List<AfsGroupsPostActivityViewModel>()
            {
                currentPostActivty,
                prevPostActivity
            };

            return model;
        }
    }
}
