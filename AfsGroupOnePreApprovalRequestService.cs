using System.Collections.Generic;
using Dms.Core.EntityFramework.Data;
using Dms.Core.EntityFramework.Model.Shared;
using Dms.Core.Extensions;
using Dms.Services.Interface.Activity;
using Dms.Services.Interface.Lookup;
using Dms.Services.Interface.Message;
using Dms.Services.Interface.Security;
using Dms.Services.Interface.Shared;
using Dms.Services.Interface.Task;
using Dms.Services.ViewModel;
using Dms.Services.ViewModel.Activity;
using System.Linq;
using Dms.Core.Utils;
using Dms.Services.Assembler;

namespace Dms.Services.Implementation.Activity
{
    public class AfsGroupOnePreApprovalRequestService : AfsPreApprovalRequestService
    {
        public AfsGroupOnePreApprovalRequestService(DmsContext context, ITaskService taskService, IDocumentService documentService, IActivityService activityService, IMessageService messageService, ISharedService sharedService, ILookupService lookupService, IUserService userService) 
        : base(context, taskService, documentService, activityService, messageService, sharedService, lookupService, userService)
        {
        }
        public override void CreateTask(PreApprovalRequestViewModel model)
        {
            base.CreateTask(model);
        }
        public override int ReInitiate(int preApprovalRequestId)
        {
            return base.ReInitiate(preApprovalRequestId);
        }
        public override PreApprovalRequestViewModel Get(int preApprovalRequestId, bool loadPreapprovalModifiedData, int postActivityId, int? cloaId = null)
        {   
            base.Get(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId, cloaId);
            return _preApprovalRequestViewModel;
        }

        public override PreApprovalRequestViewModel GetNew(int applicationId)
        {
            return base.GetNew(applicationId);
        }
        public override PreApprovalRequestViewModel Save(PreApprovalRequestViewModel model)
        {
            return base.Save(model);
        }
        public override bool SaveGeoGraphical(PreApprovalRequestViewModel model)
        {
            return base.SaveGeoGraphical(model);
        }

        public override bool SaveMsDecision(PreApprovalRequestViewModel model)
        {
            return base.SaveMsDecision(model);
        }
        public override PreApprovalRequestViewModel SavePerformanceResults(PreApprovalRequestViewModel adminModel)
        {
            return base.SavePerformanceResults(adminModel);
        }
        protected PreApprovalRequestViewModel SavePostActivityEvaluationDMEDPRE(PreApprovalRequestViewModel DmeDpremodel)
        {
            var postActivity = _postActivity;
            var model = DmeDpremodel;
            postActivity.Comments = model.AfsPostActivity.AfsPostActivityDmeDpre.Comments;
            postActivity.AirManName = model.AfsPostActivity.AfsPostActivityDmeDpre.AirManName;
            postActivity.AirManCertificateNumber = model.AfsPostActivity.AfsPostActivityDmeDpre.AirManCertificateNumber;
            postActivity.ProficiencyCheckResultId = model.AfsPostActivity.AfsPostActivityDmeDpre.ResultTypePowerplantId != null ? model.AfsPostActivity.AfsPostActivityDmeDpre.ResultTypePowerplantId : null; //Save ResultTypePowerplantId in ProficiencyCheckResultId(group2).
            postActivity.PracticalTestResultId = model.AfsPostActivity.AfsPostActivityDmeDpre.ResultTypeAirFrameId != null ? model.AfsPostActivity.AfsPostActivityDmeDpre.ResultTypeAirFrameId : null;        //Save ResultTypeAirFrameId in PracticalTestResultId(group2).
            postActivity.ActualStartDate = model.AfsPostActivity.AfsPostActivityDmeDpre.ActualStartDate.ToNullableDate();
            postActivity.ActualEndDate = model.AfsPostActivity.AfsPostActivityDmeDpre.ActualEndDate.ToNullableDate();
            postActivity.OralPortionTestDuration = model.AfsPostActivity.AfsPostActivityDmeDpre.OralPortionTestDuration.ToNullableDate();
            postActivity.PracticalPortionTestDuration = model.AfsPostActivity.AfsPostActivityDmeDpre.PracticalPortionTestDuration.ToNullableDate();
            postActivity.ResultsSubmissionDate = model.AfsPostActivity.AfsPostActivityDmeDpre.ResultsSubmissionDate.ToNullableDate();
            postActivity.ApplicantEmail = model.AfsPostActivity.AfsPostActivityDmeDpre.Email;
            postActivity.ApplicantPhone = model.AfsPostActivity.AfsPostActivityDmeDpre.Phone;
            //Address 
            if (model.AfsPostActivity.AfsPostActivityDmeDpre.Address != null && model.AfsPostActivity.AfsPostActivityDmeDpre.Address.Id == 0)
            {
                //Insert new address
                postActivity.Address = new Address
                {
                    AddressLine1 = model.AfsPostActivity.AfsPostActivityDmeDpre.Address.Address1,
                    AddressLine2 = model.AfsPostActivity.AfsPostActivityDmeDpre.Address.Address2,
                    City = model.AfsPostActivity.AfsPostActivityDmeDpre.Address.City,
                    CountryId = model.AfsPostActivity.AfsPostActivityDmeDpre.Address.Country?.Id,
                    StateId = model.AfsPostActivity.AfsPostActivityDmeDpre.Address.Country?.Id == 184 ? model.AfsPostActivity.AfsPostActivityDmeDpre.Address.State?.Id : null,
                    ZipCode = model.AfsPostActivity.AfsPostActivityDmeDpre.Address.ZipCode,
                };
            }
            else
            {
                postActivity.Address.AddressLine1 = model.AfsPostActivity.AfsPostActivityDmeDpre.Address.Address1;
                postActivity.Address.AddressLine2 = model.AfsPostActivity.AfsPostActivityDmeDpre.Address.Address2;
                postActivity.Address.City = model.AfsPostActivity.AfsPostActivityDmeDpre.Address.City;
                postActivity.Address.CountryId = model.AfsPostActivity.AfsPostActivityDmeDpre.Address.Country?.Id;
                postActivity.Address.StateId = model.AfsPostActivity.AfsPostActivityDmeDpre.Address.Country?.Id == 184 ? model.AfsPostActivity.AfsPostActivityDmeDpre.Address.State?.Id : null;
                postActivity.Address.ZipCode = model.AfsPostActivity.AfsPostActivityDmeDpre.Address.ZipCode;
            }
            var finalDmeModel = SavePostActivityEvaluationHelper(model);
            return finalDmeModel;
        }
        protected override IEnumerable<PreApprovalDateWiseCountViewModel> GetPreApprovalDateWiseCount(int applicationId)
        {
            return base.GetPreApprovalDateWiseCount(applicationId);
        }

        // new code
        public override AfsGroupsPostActivityViewModel Get(int postActivityId, bool createDocumentVersion = false)
        {
            _postActivity = GetPostActivityById(postActivityId);

            if (_postActivity != null)
            {
                _afsGroupsPostActivityViewModel.Id = _postActivity.Id;
                _afsGroupsPostActivityViewModel.ActualEndDate = _postActivity.ActualEndDate.DateToString();
                _afsGroupsPostActivityViewModel.ActualStartDate = _postActivity.ActualStartDate.DateToString();
                _afsGroupsPostActivityViewModel.ActualStartTime = _postActivity.ActualStartDate.ToTimeString();
                _afsGroupsPostActivityViewModel.ActualEndTime = _postActivity.ActualEndDate.ToTimeString();
                _afsGroupsPostActivityViewModel.TimeZoneId = _postActivity.TimeZoneId;
                _afsGroupsPostActivityViewModel.PreApprovalRequestId = _postActivity.PreApprovalRequestId;
                _afsGroupsPostActivityViewModel.UserId = _postActivity.CreatedBy;
                _afsGroupsPostActivityViewModel.ApplicantAddressId = _postActivity.ApplicantAddressId;
                _afsGroupsPostActivityViewModel.StatusId = _postActivity.StatusId;
                _afsGroupsPostActivityViewModel.Comments = _postActivity.Comments;
                _afsGroupsPostActivityViewModel.TrackingNumber = _postActivity.TrackingNumber;
                if (_postActivity.FormData != null)
                    _preApprovalRequestViewModel = CloaPreApprovalRequestViewModelMapper.DeSerializePreApprovalFormData(_context, _postActivity.FormData, _postActivity.PreApprovalRequest.CloaId);
                else
                {
                    var formData = Get(_afsGroupsPostActivityViewModel.PreApprovalRequestId, false, 0);
                    PreProcessData(formData);
                    _preApprovalRequestViewModel = formData;
                }

                _afsGroupsPostActivityViewModel.PreApprovalRequest = _preApprovalRequestViewModel;
                _afsGroupsPostActivityViewModel.CompletedDate = _postActivity?.CompletedDate;
                _afsGroupsPostActivityViewModel.IsLatestVersion = _postActivity?.PreApprovalRequest.PostActivities.OrderByDescending(pa => pa.Id).First().Id == postActivityId;                         
            }
          
            if (createDocumentVersion)
            {
                var documents = _context.DocumentReferences.Where(d => d.DocumentTypeId == (int)DocumentReferenceEnum.PostActivity && ((d.ReferenceId == postActivityId && d.SecondaryReferenceId == null) ||
                                    (d.ReferenceId == _afsGroupsPostActivityViewModel.PreApprovalRequestId && d.SecondaryReferenceId == _afsGroupsPostActivityViewModel.PreApprovalRequestId))).Select(d => d).ToArray();
                var futureVersionDocuments = documents.Where(d => d.ReferenceId == _afsGroupsPostActivityViewModel.PreApprovalRequestId && d.SecondaryReferenceId == _afsGroupsPostActivityViewModel.PreApprovalRequestId).Select(d => d.DocumentId);
                var currentVersionDocuments = documents.Where(d => d.ReferenceId == postActivityId && d.SecondaryReferenceId == null).Select(d => d.DocumentId);
                currentVersionDocuments.Where(cd => !futureVersionDocuments.Any(fd => fd == cd)).ToList().ForEach(d =>
                {
                    _context.DocumentReferences.Add(new DocumentReference
                    {
                        DocumentId = d,
                        DocumentTypeId = (int)DocumentReferenceEnum.PostActivity,
                        ReferenceId = _afsGroupsPostActivityViewModel.PreApprovalRequestId,
                        SecondaryReferenceId = _afsGroupsPostActivityViewModel.PreApprovalRequestId
                    });
                });

                _context.SaveChanges();
            }

            _afsGroupsPostActivityViewModel.DocumentReference = _documentService.GetDocumentsByRef((int)DocumentReferenceEnum.PostActivity,
                createDocumentVersion ? _afsGroupsPostActivityViewModel.PreApprovalRequestId : postActivityId,
                createDocumentVersion ? _afsGroupsPostActivityViewModel.PreApprovalRequestId : (int?)null);

            return _afsGroupsPostActivityViewModel;
        }      
       
    }
}
