using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dms.Core.EntityFramework.Data;
using Dms.Core.EntityFramework.Model.Activity;
using Dms.Core.EntityFramework.Model.Lookup;
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
using Dms.Services.ViewModel;
using Dms.Services.ViewModel.Activity;
using Dms.Services.ViewModel.Lookup;
using Dms.Services.ViewModel.Shared;
using Microsoft.EntityFrameworkCore;

namespace Dms.Services.Implementation.Activity
{
    public class DmePreApprovalRequestService : AfsGroupOnePreApprovalRequestService
    {
        public DmePreApprovalRequestService(DmsContext context, ITaskService taskService, IDocumentService documentService, IActivityService activityService, IMessageService messageService, ISharedService sharedService, ILookupService lookupService, IUserService userService)
        : base(context, taskService, documentService, activityService, messageService, sharedService, lookupService, userService)
        {
        }
        public override PreApprovalRequestViewModel GetNew(int applicationId)
        {
            base.GetNew(applicationId);
            _preApprovalRequestViewModel.DesigneeFunctionCodes = _cloa.DesigneeFunctionCodes;
            _preApprovalRequestViewModel.IsAfsType = true;
            _preApprovalRequestViewModel.PreApprovalDateWiseCount = GetPreApprovalDateWiseCount(_cloa.DesigneeInformation.ApplicationId);

            return _preApprovalRequestViewModel;
        }
        public override bool Cancel(ActivityPaperWorkViewModel model)
        {
            return base.Cancel(model);
        }
      
        public override PreApprovalRequestViewModel Save(PreApprovalRequestViewModel model)
        {
            base.Save(model);
            // the same for both adminModel.id==0 and adminModel.id!=0
            _preApprovalRequest.PreApprovalRequestFunctionCodes = model.SelectedFunctionCodes.Select(f => new PreApprovalRequestFunctionCode
            {
                FunctionCodeId = f,
                IsCloaFunctionCode = true,
            }).ToList();

            if (model.Id != 0)
            {

                if (model.PlannedActivity != null && model.PlannedActivity.Products != null && model.PlannedActivity.Products.Any())
                {
                    _context.PreApprovalRequestProducts.RemoveRange(_preApprovalRequest.PreApprovalRequestProducts);
                }
                _preApprovalRequest.PreApprovalRequestFunctionCodes = model.SelectedFunctionCodes.Select(f => new PreApprovalRequestFunctionCode
                {
                    FunctionCodeId = f,
                    IsCloaFunctionCode = true,
                }).ToList();
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
        [SuppressMessage("Major Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "cpmplexity involved")]
        public override PreApprovalRequestViewModel SavePostActivityEvaluation(PreApprovalRequestViewModel adminModel)
        {
            var admModel = base.SavePostActivityEvaluation(adminModel);
            var postActivity = _postActivity;
            postActivity.PracticalOralTestTypeId = admModel.AfsPostActivity.AfsPostActivityDmeDpre.PracticalOralTestTypeId;
            postActivity.ApplicantName = admModel.AfsPostActivity.AfsPostActivityDmeDpre.Name;
            postActivity.ApplicantCertificateNumber = admModel.AfsPostActivity.AfsPostActivityDmeDpre.CertificateNumber;

            //Remove functioncodes
            foreach (var fc in postActivity.PostActivityFunctionCodes.ToList())
            {
                _context.PostActivityFunctionCodes.Remove(fc);
            }
            //Insert Function Codes 
            postActivity.PostActivityFunctionCodes = admModel.AfsPostActivity.AfsPostActivityDmeDpre.SelectedFunctionCodes.Select(f => new PostActivityFunctionCode
            {
                FunctionCodeId = f
            }).ToList();
            var resultTypeAirFrameId = admModel.AfsPostActivity.AfsPostActivityDmeDpre.ResultTypeAirFrameId;
            var resultTypePowerplantId = admModel.AfsPostActivity.AfsPostActivityDmeDpre.ResultTypePowerplantId;

            if (resultTypeAirFrameId == null && resultTypePowerplantId == null)
                postActivity.PostActivityResultTypeId = null;

            if (resultTypeAirFrameId == null && resultTypePowerplantId != null)
                postActivity.PostActivityResultTypeId = resultTypePowerplantId;

            if (resultTypeAirFrameId != null && resultTypePowerplantId == null)
                postActivity.PostActivityResultTypeId = resultTypeAirFrameId;

            if (resultTypeAirFrameId == (int)PostActivityResultTypeDmeEnum.Satisfactory && resultTypePowerplantId == (int)PostActivityResultTypeDmeEnum.Satisfactory) //Both Satifactory, then overall result is Satisfactory
                postActivity.PostActivityResultTypeId = (int)PostActivityResultTypeDmeEnum.Satisfactory;

            if (resultTypeAirFrameId == (int)PostActivityResultTypeDmeEnum.Discontinued && resultTypePowerplantId == (int)PostActivityResultTypeDmeEnum.Discontinued) //Both Discontinued, then overall result is Discontinued/Cancelled
                postActivity.PostActivityResultTypeId = (int)PostActivityResultTypeDmeEnum.Discontinued;

            if (resultTypeAirFrameId == (int)PostActivityResultTypeDmeEnum.Unsatisfactory || resultTypePowerplantId == (int)PostActivityResultTypeDmeEnum.Unsatisfactory) //One is Unsatisfactory and one is satisfactory, then overall result is Unsatisfactory
                postActivity.PostActivityResultTypeId = (int)PostActivityResultTypeDmeEnum.Unsatisfactory;

            if (resultTypeAirFrameId == (int)PostActivityResultTypeDmeEnum.Satisfactory && resultTypePowerplantId == (int)PostActivityResultTypeDmeEnum.Discontinued) //One is Satisfactory and one is discontinued/cancelled, then overall result is Discontinued/Cancelled
                postActivity.PostActivityResultTypeId = (int)PostActivityResultTypeDmeEnum.Discontinued;

            if (resultTypePowerplantId == (int)PostActivityResultTypeDmeEnum.Satisfactory && resultTypeAirFrameId == (int)PostActivityResultTypeDmeEnum.Discontinued) //One is Satisfactory and one is discontinued/cancelled, then overall result is Discontinued/Cancelled 
                postActivity.PostActivityResultTypeId = (int)PostActivityResultTypeDmeEnum.Discontinued;
            var modelDme = SavePostActivityEvaluationDMEDPRE(admModel);
            return modelDme;

        }
        public override PreApprovalRequestViewModel Get(int preApprovalRequestId, bool loadPreapprovalModifiedData, int postActivityId, int? cloaId = null)
        {
            base.Get(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId, cloaId);
            _preApprovalRequestViewModel.SelectedFunctionCodes = _cloa.SelectedOtherFunctionCodes != null ? _cloa.SelectedOtherFunctionCodes.Select(it => it.Id).ToList() : new List<int>();
            //Planned Activity Products
            _preApprovalRequestViewModel.PlannedActivity = CloaPreApprovalRequestViewModelMapper.GetPreAprovalPlannedActivityViewModel(_preApprovalRequest);

            if (_preApprovalRequestViewModel.SelectedFunctionCodes.Contains((int)DartSpecialFunctionCodeEnums.FunctionCode180)
            || _preApprovalRequestViewModel.SelectedFunctionCodes.Contains((int)DartSpecialFunctionCodeEnums.FunctionCode191))
            {
                _preApprovalRequestViewModel.PlannedActivity.IssuedExportApprovalQuantity = _preApprovalRequest.AfsPreApprovalRequest.IssuedExportApprovalQuantity;
                _preApprovalRequestViewModel.PlannedActivity.IssuedDomesticApprovalQuantity = _preApprovalRequest.AfsPreApprovalRequest.IssuedDomesticApprovalQuantity;
            }
            GetAfsHelper(_preApprovalRequestViewModel, postActivityId, loadPreapprovalModifiedData);
            GetAfsHelperTwo(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId);
            if (loadPreapprovalModifiedData && _preApprovalRequestViewModel.AfsPostActivity.AfsPostActivityModifiedPreApprovalViewModel != null && _context.PostActivities.Where(e => e.PreApprovalRequestId == preApprovalRequestId).Any())
            {
                LoadModifiedDataForDme(_preApprovalRequestViewModel, _preApprovalRequestViewModel.AfsPostActivity, _preApprovalRequestViewModel.ModifiedPreapprovalControls);
                LoadPreApprovalModifiedData(_preApprovalRequestViewModel, _preApprovalRequestViewModel.AfsPostActivity, _cloa.DesigneeInfo.TypeId, _preApprovalRequestViewModel.ModifiedPreapprovalControls);
            }
            GetHelper(_preApprovalRequestViewModel);
            CheckIfActivityOutside(_preApprovalRequestViewModel);

            return _preApprovalRequestViewModel;
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
        private void LoadModifiedDataForDme(PreApprovalRequestViewModel preApprovalRequestViewModel, AfsPostActivityViewModel afsPostActivity, Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls)
        {
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.CertificateRatingTypeId != null)
            {
                preApprovalRequestViewModel.ApplicationInformation.CertificateRatingTypeId = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.CertificateRatingTypeId;
                AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel { Control = "certificateRatingTypeId" }, modifiedPreapprovalControls);
            }

            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AuthorizedTestOfficeId != null)
            {
                preApprovalRequestViewModel.ApplicationInformation.AuthorizedTestOfficeId = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AuthorizedTestOfficeId;
                AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel { Control = "authorizedTestOfficeId" }, modifiedPreapprovalControls);
            }

            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.SchoolId != null && afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.SchoolId > 0 && preApprovalRequestViewModel.ApplicationInformation != null
                && afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.SchoolId.HasValue)
            {
                    var sId = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.SchoolId.GetValueOrDefault();
                    preApprovalRequestViewModel.ApplicationInformation.SchoolId = _lookupService.LookupValues("schools").Result.Schools.Where(s => s.Id == sId).Select(x => new BaseLookup
                    {
                        Id = x.Id,
                        Name = x.Name
                    }).FirstOrDefault();
                    AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel { Control = "schoolId" }, modifiedPreapprovalControls);
            }
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsCivilExperience != null)
            {
                preApprovalRequestViewModel.ApplicationInformation.IsCivilExperience = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsCivilExperience;
                AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel { Control = "isCivilExperience" }, modifiedPreapprovalControls);
            }
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsMilitaryExperience != null)
            {
                preApprovalRequestViewModel.ApplicationInformation.IsMilitaryExperience = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsMilitaryExperience;
                AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel { Control = "isMilitaryExperience" }, modifiedPreapprovalControls);
            }
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsCfrSectionTest != null)
            {
                preApprovalRequestViewModel.ApplicationInformation.IsCfrSectionTest = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsCfrSectionTest;
                AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel { Control = "isCfrSectionTest" }, modifiedPreapprovalControls);
            }
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.CfrSectionSchoolId != null && afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.CfrSectionSchoolId > 0 && preApprovalRequestViewModel.ApplicationInformation != null
                && afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.CfrSectionSchoolId.HasValue)
            {
                    var sId = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.CfrSectionSchoolId.GetValueOrDefault();
                    preApprovalRequestViewModel.ApplicationInformation.CfrSectionSchoolId = _lookupService.LookupValues("schools").Result.Schools.Where(s => s.Id == sId).Select(x => new BaseLookup
                    {
                        Id = x.Id,
                        Name = x.Name
                    }).FirstOrDefault();
                    AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel { Control = "cfrSectionSchoolId" }, modifiedPreapprovalControls);
            }
        }
        public static bool FindModifiedDataForDme(PreApprovalRequestViewModel model, PreApprovalRequest preApprovalRequest, AfsPostActivityModifiedPreApprovalViewModel afsPostActivityModifiedPreApprovalViewModel, bool triggerCorrectiveActiveTrigger)
        {
            if (model.ApplicationInformation.CertificateRatingTypeId != preApprovalRequest.AfsPreApprovalRequest.CertificateRatingTypeId)
            {
                afsPostActivityModifiedPreApprovalViewModel.CertificateRatingTypeId = model.ApplicationInformation.CertificateRatingTypeId;
                triggerCorrectiveActiveTrigger = true;
            }

            if (model.ApplicationInformation.AuthorizedTestOfficeId != preApprovalRequest.AfsPreApprovalRequest.AuthorizedTestOfficeId)
            {
                afsPostActivityModifiedPreApprovalViewModel.AuthorizedTestOfficeId = model.ApplicationInformation.AuthorizedTestOfficeId;
                triggerCorrectiveActiveTrigger = true;
            }

            if (model.ApplicationInformation.CertificateRatingTypeId == (int)CertificateRatingTypeEnum.GraduateofApprovedCourse)
            {
                if (model.ApplicationInformation.SchoolId?.Id != preApprovalRequest.AfsPreApprovalRequest.SchoolId)
                {
                    afsPostActivityModifiedPreApprovalViewModel.SchoolId = model.ApplicationInformation.SchoolId?.Id;
                    triggerCorrectiveActiveTrigger = true;
                }

                if (model.ApplicationInformation.IsCfrSectionTest != preApprovalRequest.AfsPreApprovalRequest.IsCfrSectionTest)
                {
                    afsPostActivityModifiedPreApprovalViewModel.IsCfrSectionTest = model.ApplicationInformation.IsCfrSectionTest;
                    triggerCorrectiveActiveTrigger = true;
                }
                if (model.ApplicationInformation.IsCfrSectionTest.HasValue && model.ApplicationInformation.IsCfrSectionTest.Value
                    && model.ApplicationInformation.CfrSectionSchoolId?.Id != preApprovalRequest.AfsPreApprovalRequest.CfrSectionSchoolId)
                {
                        afsPostActivityModifiedPreApprovalViewModel.CfrSectionSchoolId = model.ApplicationInformation.CfrSectionSchoolId.Id;
                        triggerCorrectiveActiveTrigger = true;
                }
            }
            else if (model.ApplicationInformation.CertificateRatingTypeId == (int)CertificateRatingTypeEnum.Experience)
            {
                if (model.ApplicationInformation.IsCivilExperience != preApprovalRequest.AfsPreApprovalRequest.IsCivilExperience)
                {
                    afsPostActivityModifiedPreApprovalViewModel.IsCivilExperience = model.ApplicationInformation.IsCivilExperience;
                    triggerCorrectiveActiveTrigger = true;
                }
                if (model.ApplicationInformation.IsMilitaryExperience != preApprovalRequest.AfsPreApprovalRequest.IsMilitaryExperience)
                {
                    afsPostActivityModifiedPreApprovalViewModel.IsMilitaryExperience = model.ApplicationInformation.IsMilitaryExperience;
                    triggerCorrectiveActiveTrigger = true;
                }
            }

            triggerCorrectiveActiveTrigger |= CheckForAddress(model, preApprovalRequest, afsPostActivityModifiedPreApprovalViewModel, triggerCorrectiveActiveTrigger);
            return triggerCorrectiveActiveTrigger;
        }

        private static string GetOralPortionTestDuration(PostActivity _postActivity) => _postActivity.PracticalOralTestTypeId == (int)PracticalOralTestTypeEnum.IssueFormOnlyNoTest
                                                    ? _postActivity.OralPortionTestDuration.HasValue
                                                                ? _postActivity.OralPortionTestDuration.Value.ToString("HH:mm") : null
                                                    : _postActivity.OralPortionTestDuration.HasValue
                                                                ? _postActivity.OralPortionTestDuration.Value.ToString("HH:mm") : "01:00";

         private static string GetPracticalPortionTestDuration(PostActivity _postActivity) => _postActivity.PracticalOralTestTypeId == (int)PracticalOralTestTypeEnum.IssueFormOnlyNoTest
                                                        ? _postActivity.PracticalPortionTestDuration.HasValue
                                                                ? _postActivity.PracticalPortionTestDuration.Value.ToString("HH:mm") : null
                                                        : _postActivity.PracticalPortionTestDuration.HasValue
                                                                ? _postActivity.PracticalPortionTestDuration.Value.ToString("HH:mm") : "01:00";
        public override AfsGroupsPostActivityViewModel Get(int postActivityId, bool createDocumentVersion = false)
        {
            base.Get(postActivityId, createDocumentVersion);
            _afsGroupsPostActivityViewModel.GeneralComments = _postActivity.GeneralComments;

            _afsGroupsPostActivityViewModel.AfsPostActivityDmeDpre = new AfsPostActivityDmeDpreViewModel()
            {
                PracticalOralTestTypeId = _postActivity.PracticalOralTestTypeId,
                ResultTypeId = _postActivity.PostActivityResultTypeId,
                ResultTypePowerplantId = _postActivity.ProficiencyCheckResultId,
                ResultTypeAirFrameId = _postActivity.PracticalTestResultId,
                IsAdditionalInstructionsProvided = _postActivity.IsAdditionalInstructionsProvided,
                Comments = _postActivity.Comments,
                ActualStartDate = _postActivity.ActualStartDate.HasValue ? _postActivity.ActualStartDate.Value.ToString("MM/dd/yyyy HH:mm") : string.Empty,
                ActualEndDate = _postActivity.ActualEndDate.HasValue ? _postActivity.ActualEndDate.Value.ToString("MM/dd/yyyy HH:mm") : string.Empty,
                OralPortionTestDuration = GetOralPortionTestDuration(_postActivity),
                PracticalPortionTestDuration = GetPracticalPortionTestDuration(_postActivity),
                AirManName = _postActivity.AirManName,
                AirManCertificateNumber = _postActivity.AirManCertificateNumber,
                Name = _postActivity.ApplicantName,
                CertificateNumber = _postActivity.ApplicantCertificateNumber,
                Phone = _postActivity.ApplicantPhone,
                Email = _postActivity.ApplicantEmail,
                ResultsSubmissionDate = _postActivity.ResultsSubmissionDate.HasValue
                                                    ? _postActivity.ResultsSubmissionDate.Value.ToShortDateString() : string.Empty,
                Address = new AddressViewModel(),
                SelectedFunctionCodes = _postActivity.PostActivityFunctionCodes.Select(e => e.FunctionCodeId).ToList()

            };
            if (_postActivity.Address != null)
            {
                _afsGroupsPostActivityViewModel.AfsPostActivityDmeDpre.Address = new AddressViewModel()
                {
                    Id = _postActivity.Address.Id,
                    Address1 = _postActivity.Address.AddressLine1,
                    Address2 = _postActivity.Address.AddressLine2,
                    City = _postActivity.Address.City,
                    County = _postActivity.Address.County,
                    State = _postActivity.Address.StateProvince != null ? new StateViewModel
                    {
                        Id = _postActivity.Address.StateProvince.Id,
                        Name = _postActivity.Address.StateProvince.Name
                    } : null,
                    Country = _postActivity.Address.Country != null ? new CountryViewModel
                    {
                        Id = _postActivity.Address.Country.Id,
                        Name = _postActivity.Address.Country.Name
                    } : null,
                    ZipCode = _postActivity.Address.ZipCode,
                    PhoneNumber = _postActivity.Address.Phone
                };
            }
            return _afsGroupsPostActivityViewModel;
        }

        public override AfsGroupsPostActivityViewModel SaveAfsPostActivity(AfsGroupsPostActivityViewModel model)
        {
            base.SaveAfsPostActivityHelper(model);

            _postActivity.PostActivityResultTypeId = getPostActivityResultType(model);
            _postActivity.PracticalOralTestTypeId = model.AfsPostActivityDmeDpre.PracticalOralTestTypeId;
            _postActivity.ApplicantName = model.AfsPostActivityDmeDpre.Name;
            _postActivity.ApplicantCertificateNumber = model.AfsPostActivityDmeDpre.CertificateNumber;
            _postActivity.Comments = model.AfsPostActivityDmeDpre.Comments;
            _postActivity.AirManName = model.AfsPostActivityDmeDpre.AirManName;
            _postActivity.AirManCertificateNumber = model.AfsPostActivityDmeDpre.AirManCertificateNumber;
            _postActivity.ProficiencyCheckResultId = model.AfsPostActivityDmeDpre.ResultTypePowerplantId != null ? model.AfsPostActivityDmeDpre.ResultTypePowerplantId : null; //Save ResultTypePowerplantId in ProficiencyCheckResultId(group2).
            _postActivity.PracticalTestResultId = model.AfsPostActivityDmeDpre.ResultTypeAirFrameId != null ? model.AfsPostActivityDmeDpre.ResultTypeAirFrameId : null;        //Save ResultTypeAirFrameId in PracticalTestResultId(group2).
            _postActivity.ActualStartDate = model.AfsPostActivityDmeDpre.ActualStartDate.ToNullableDate();
            _postActivity.ActualEndDate = model.AfsPostActivityDmeDpre.ActualEndDate.ToNullableDate();
            _postActivity.OralPortionTestDuration = model.AfsPostActivityDmeDpre.OralPortionTestDuration.ToNullableDate();
            _postActivity.PracticalPortionTestDuration = model.AfsPostActivityDmeDpre.PracticalPortionTestDuration.ToNullableDate();
            _postActivity.ResultsSubmissionDate = model.AfsPostActivityDmeDpre.ResultsSubmissionDate.ToNullableDate();
            _postActivity.ApplicantEmail = model.AfsPostActivityDmeDpre.Email;
            _postActivity.ApplicantPhone = model.AfsPostActivityDmeDpre.Phone;
            //Address 
            if (model.AfsPostActivityDmeDpre.Address != null && (model.AfsPostActivityDmeDpre.Address.Id == 0 || _postActivity.Id == 0))
            {
                //Insert new address
                _postActivity.Address = new Address
                {
                    AddressLine1 = model.AfsPostActivityDmeDpre.Address.Address1,
                    AddressLine2 = model.AfsPostActivityDmeDpre.Address.Address2,
                    City = model.AfsPostActivityDmeDpre.Address.City,
                    CountryId = model.AfsPostActivityDmeDpre.Address.Country?.Id,
                    StateId = GetStateId(model),
                    ZipCode = model.AfsPostActivityDmeDpre.Address.ZipCode,
                };
                _context.Entry(_postActivity.Address).State = EntityState.Added;
            }
            else
            {
                _postActivity.Address.AddressLine1 = model.AfsPostActivityDmeDpre.Address.Address1;
                _postActivity.Address.AddressLine2 = model.AfsPostActivityDmeDpre.Address.Address2;
                _postActivity.Address.City = model.AfsPostActivityDmeDpre.Address.City;
                _postActivity.Address.CountryId = model.AfsPostActivityDmeDpre.Address.Country?.Id;
                _postActivity.Address.StateId = GetStateId(model);
                _postActivity.Address.ZipCode = model.AfsPostActivityDmeDpre.Address.ZipCode;
                _context.Entry(_postActivity.Address).State = EntityState.Modified;
            }

            if (_postActivity.PreApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Completed && _postActivity.Id != 0)
            {  
                foreach (var fc in _postActivity.PostActivityFunctionCodes.ToList())
                {
                    _context.Entry(fc).State = EntityState.Deleted;
                }
            }
            //Insert Function Codes 
            _postActivity.PostActivityFunctionCodes = new List<PostActivityFunctionCode>();
            foreach (var fc in model.AfsPostActivityDmeDpre.SelectedFunctionCodes)
            {
                var newfc = new PostActivityFunctionCode
                {
                    FunctionCodeId = fc
                };
                _postActivity.PostActivityFunctionCodes.Add(newfc);
                _context.Entry(newfc).State = EntityState.Added;
            }

            if (!model.IsSubmit)
            {
                _postActivity.FormData = CloaPreApprovalRequestViewModelMapper.SerializePreApproval(model.PreApprovalRequest);
            }
            _context.Entry(_postActivity).State = GetState(_postActivity);
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
                _context.Entry(_postActivity).State = GetState(_postActivity);
                _context.Entry(_postActivity.PreApprovalRequest).State = EntityState.Modified;
            }
            _context.SaveChanges();
            model.Id = _postActivity.Id;

            return model;
        }

        private static EntityState GetState(PostActivity _postActivity) => _postActivity.Id > 0 ? EntityState.Modified : EntityState.Added;

        private static int? GetStateId(AfsGroupsPostActivityViewModel model) => model.AfsPostActivityDmeDpre.Address.Country?.Id == 184 ? model.AfsPostActivityDmeDpre.Address.State?.Id : null;

        protected void CheckPreApprovalModifiedData(AfsGroupsPostActivityViewModel model)
        {
            model.PreApprovalRequest.ModifiedPreapprovalControls = new Dictionary<string, List<ModifiedControlViewModel>>();
            var preApprovalRequest =  base.GetOriginalPreApprovalData(model.PreApprovalRequestId);

            var triggerCorrectiveActionTrigger = false;
            triggerCorrectiveActionTrigger = FindModifiedData(model.PreApprovalRequest, preApprovalRequest, model.PreApprovalRequest.ModifiedPreapprovalControls);

            //Corrective action triggering for Post activity actual start date is difffrent than pre approval test date
            var dateTriggerCorrectiveActionTrigger = false;
            var proposedStartDate = preApprovalRequest.ProposeStartDate.Value;
            var proposedEndDate = preApprovalRequest.ProposeEndDate.Value;

            if (!string.IsNullOrEmpty(model.AfsPostActivityDmeDpre.ActualStartDate))
            {
                var actualStartDate = DateTime.Parse(model.AfsPostActivityDmeDpre.ActualStartDate);
                var actualEndDate = DateTime.Parse(model.AfsPostActivityDmeDpre.ActualEndDate);

                dateTriggerCorrectiveActionTrigger = !((actualStartDate.Date >= proposedStartDate.Date && actualStartDate.Date <= proposedEndDate) &&
                                                       (actualEndDate.Date >= proposedStartDate.Date && actualEndDate.Date <= proposedEndDate.Date));

            }

            triggerCorrectiveActionTrigger = dateTriggerCorrectiveActionTrigger || triggerCorrectiveActionTrigger;

            //Create activity with format and save
            if (triggerCorrectiveActionTrigger)
            {
                CreateCorrectiveAction(
                    preApprovalRequest.CloaId,
                    model.PreApprovalRequest.ApplicationId.GetValueOrDefault(),
                    model.PreApprovalRequest.ManagingSpecialist
                );
            }
        }

        private bool FindModifiedData(PreApprovalRequestViewModel model, PreApprovalRequest preApprovalRequest,
                                                       Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls)
        {
            bool triggerCorrectiveAction = false;

            if (model.ApplicationInformation.CertificateRatingTypeId != preApprovalRequest.AfsPreApprovalRequest.CertificateRatingTypeId)
            {
                AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel { Control = "certificateRatingTypeId" }, modifiedPreapprovalControls);
                triggerCorrectiveAction = true;
            }

            if (model.ApplicationInformation.AuthorizedTestOfficeId != preApprovalRequest.AfsPreApprovalRequest.AuthorizedTestOfficeId)
            {
                AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel { Control = "authorizedTestOfficeId" }, modifiedPreapprovalControls);
                triggerCorrectiveAction = true;
            }

            if (model.ApplicationInformation.CertificateRatingTypeId == (int)CertificateRatingTypeEnum.GraduateofApprovedCourse)
            {
                if (model.ApplicationInformation.SchoolId?.Id != preApprovalRequest.AfsPreApprovalRequest.SchoolId)
                {
                    AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel { Control = "schoolId" }, modifiedPreapprovalControls);
                    triggerCorrectiveAction = true;
                }

                if (model.ApplicationInformation.IsCfrSectionTest != preApprovalRequest.AfsPreApprovalRequest.IsCfrSectionTest)
                {
                    AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel { Control = "isCfrSectionTest" }, modifiedPreapprovalControls);
                    triggerCorrectiveAction = true;
                }
                if (model.ApplicationInformation.IsCfrSectionTest.HasValue && model.ApplicationInformation.IsCfrSectionTest.Value
                    && model.ApplicationInformation.CfrSectionSchoolId?.Id != preApprovalRequest.AfsPreApprovalRequest.CfrSectionSchoolId)
                {
                        AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel { Control = "cfrSectionSchoolId" }, modifiedPreapprovalControls);
                        triggerCorrectiveAction = true;
                }
            }
            else if (model.ApplicationInformation.CertificateRatingTypeId == (int)CertificateRatingTypeEnum.Experience)
            {
                if (model.ApplicationInformation.IsCivilExperience != preApprovalRequest.AfsPreApprovalRequest.IsCivilExperience)
                {
                    AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel { Control = "isCivilExperience" }, modifiedPreapprovalControls);
                    triggerCorrectiveAction = true;
                }
                if (model.ApplicationInformation.IsMilitaryExperience != preApprovalRequest.AfsPreApprovalRequest.IsMilitaryExperience)
                {
                    AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("ApplicationInformation", new ModifiedControlViewModel { Control = "isMilitaryExperience" }, modifiedPreapprovalControls);
                    triggerCorrectiveAction = true;
                }
            }

            triggerCorrectiveAction |= CheckForAddress(model.ActivityLocation.LocationAddress,
                            preApprovalRequest.AfsPreApprovalRequest.Address,
                            modifiedPreapprovalControls,
                            model.ActivityLocation.FacilityonRecord
                                .GetValueOrDefault(),
                            preApprovalRequest.AfsPreApprovalRequest.FacilityOnRecord.GetValueOrDefault());

            return triggerCorrectiveAction;
        }

        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity inherent to task navigation system")]
        private static int? getPostActivityResultType(AfsGroupsPostActivityViewModel viewModel)
        {
            var resultTypeAirFrameId = viewModel.AfsPostActivityDmeDpre.ResultTypeAirFrameId;
            var resultTypePowerplantId = viewModel.AfsPostActivityDmeDpre.ResultTypePowerplantId;

            if (resultTypeAirFrameId == null && resultTypePowerplantId == null)
                return null;

            else if (resultTypeAirFrameId == null && resultTypePowerplantId != null)
                return resultTypePowerplantId;

            else if (resultTypeAirFrameId != null && resultTypePowerplantId == null)
                return resultTypeAirFrameId;

            else if (resultTypeAirFrameId == (int)PostActivityResultTypeDmeEnum.Satisfactory && resultTypePowerplantId == (int)PostActivityResultTypeDmeEnum.Satisfactory) //Both Satifactory, then overall result is Satisfactory
                return (int)PostActivityResultTypeDmeEnum.Satisfactory;

            else if (resultTypeAirFrameId == (int)PostActivityResultTypeDmeEnum.Discontinued && resultTypePowerplantId == (int)PostActivityResultTypeDmeEnum.Discontinued) //Both Discontinued, then overall result is Discontinued/Cancelled
                return (int)PostActivityResultTypeDmeEnum.Discontinued;

            else if (resultTypeAirFrameId == (int)PostActivityResultTypeDmeEnum.Unsatisfactory || resultTypePowerplantId == (int)PostActivityResultTypeDmeEnum.Unsatisfactory) //One is Unsatisfactory and one is satisfactory, then overall result is Unsatisfactory
                return (int)PostActivityResultTypeDmeEnum.Unsatisfactory;

            else if (resultTypeAirFrameId == (int)PostActivityResultTypeDmeEnum.Satisfactory && resultTypePowerplantId == (int)PostActivityResultTypeDmeEnum.Discontinued) //One is Satisfactory and one is discontinued/cancelled, then overall result is Discontinued/Cancelled
                return (int)PostActivityResultTypeDmeEnum.Discontinued;

            else if (resultTypePowerplantId == (int)PostActivityResultTypeDmeEnum.Satisfactory && resultTypeAirFrameId == (int)PostActivityResultTypeDmeEnum.Discontinued) //One is Satisfactory and one is discontinued/cancelled, then overall result is Discontinued/Cancelled 
                return (int)PostActivityResultTypeDmeEnum.Discontinued;
            return null;
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
