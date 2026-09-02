using System;
using System.Collections.Generic;
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
using Dms.Services.ViewModel.Utils;
using Microsoft.EntityFrameworkCore;


namespace Dms.Services.Implementation.Activity
{
    public class DprePreApprovalRequestService : AfsGroupOnePreApprovalRequestService
    {
        public DprePreApprovalRequestService(DmsContext context, ITaskService taskService, IDocumentService documentService, IActivityService activityService, IMessageService messageService, ISharedService sharedService, ILookupService lookupService, IUserService userService)
        : base(context, taskService, documentService, activityService, messageService, sharedService, lookupService, userService)
        {
        }
        public override PreApprovalRequestViewModel GetNew(int applicationId)
        {
            base.GetNew(applicationId);
            //starts 142
            var authorizationTypes = new List<FunctionCodeTypeViewModel>();
            var types = _context.PreApprovalRequestFunctionCodeTypes.ToArray();
            foreach (var item in types)
            {
                authorizationTypes.Add(new FunctionCodeTypeViewModel()
                {
                    FunctionCodeType = new BaseLookup() { Id = item.Id, Name = item.Name },
                    IsSelected = false,
                    authSelectedFunctionCodes = new List<int>()
                });
            }
            _preApprovalRequestViewModel.DpreFunctionCodes = authorizationTypes;
            //ends 155
            GetNewHelp(applicationId);
            _preApprovalRequestViewModel.DesigneeFunctionCodes = _cloa.DesigneeFunctionCodes;
            _preApprovalRequestViewModel.IsAfsType = true;
            _preApprovalRequestViewModel.PreApprovalDateWiseCount = GetPreApprovalDateWiseCount(_cloa.DesigneeInformation.ApplicationId);

            return _preApprovalRequestViewModel;

        }
        public override PreApprovalRequestViewModel Get(int preApprovalRequestId, bool loadPreapprovalModifiedData, int postActivityId, int? cloaId = null)
        {
            base.Get(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId, cloaId);
            //starts line 550 
            var authorizationTypes = new List<FunctionCodeTypeViewModel>();
            var types = _context.PreApprovalRequestFunctionCodeTypes.ToArray();
            foreach (var item in types)
            {
                authorizationTypes.Add(new FunctionCodeTypeViewModel()
                {
                    FunctionCodeType = new BaseLookup() { Id = item.Id, Name = item.Name },
                    IsSelected = _cloa.SelectedOtherFunctionCodes != null && _cloa.SelectedOtherFunctionCodes.Any(it => it.TypeId == item.Id),
                    authSelectedFunctionCodes = _cloa.SelectedOtherFunctionCodes != null ? _cloa.SelectedOtherFunctionCodes.Where(it => it.TypeId == item.Id).Select(fc => fc.Id).ToList() : new List<int>()
                });
            }

            _preApprovalRequestViewModel.DpreFunctionCodes = authorizationTypes;
            //ends 561
            GetAfsHelper(_preApprovalRequestViewModel, postActivityId, loadPreapprovalModifiedData);
            GetAfsHelperTwo(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId);//this includes GetAfsHelperPostActivity
            if (loadPreapprovalModifiedData && _preApprovalRequestViewModel.AfsPostActivity.AfsPostActivityModifiedPreApprovalViewModel != null && _context.PostActivities.Where(e => e.PreApprovalRequestId == preApprovalRequestId).OrderByDescending((po => po.Id)).Any())
            {
                LoadModifiedDataForDpre(_preApprovalRequestViewModel, _preApprovalRequestViewModel.AfsPostActivity, _preApprovalRequestViewModel.ModifiedPreapprovalControls);
                LoadPreApprovalModifiedData(_preApprovalRequestViewModel, _preApprovalRequestViewModel.AfsPostActivity, _cloa.DesigneeInfo.TypeId, _preApprovalRequestViewModel.ModifiedPreapprovalControls);
            }
            GetHelper(_preApprovalRequestViewModel);

            //Update Comments only in post Activity.
            if (!string.IsNullOrEmpty(_preApprovalRequestViewModel.AfsPostActivity?.AfsPostActivityModifiedPreApprovalViewModel?.Comments) && loadPreapprovalModifiedData)
            {
                _preApprovalRequestViewModel.Comments = _preApprovalRequestViewModel.AfsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.Comments;
            }
            CheckIfActivityOutside(_preApprovalRequestViewModel);
            return _preApprovalRequestViewModel;
        }
        public override PreApprovalRequestViewModel Save(PreApprovalRequestViewModel model)
        {

            base.Save(model);
            var dpreFuncCodes = new List<PreApprovalRequestFunctionCode>();
            // coz this is the same for both model.id=0 and model.id!=0
            foreach (var dpreFunctionCode in model.DpreFunctionCodes)
            {
                if (dpreFunctionCode.IsSelected && dpreFunctionCode.authSelectedFunctionCodes.Any())
                {
                        dpreFuncCodes.AddRange(dpreFunctionCode.authSelectedFunctionCodes.Select(f => new PreApprovalRequestFunctionCode
                        {
                            FunctionCodeId = f,
                            TypeId = dpreFunctionCode.FunctionCodeType.Id,
                            IsCloaFunctionCode = true,
                        }).ToList()
                        );
                }
            }
            _preApprovalRequest.PreApprovalRequestFunctionCodes = dpreFuncCodes;
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
            SaveHelper(model);
            CompleteOrCreateTask(model, IsAutoPreApproval(model));
            return _preApprovalRequestViewModel;
        }
        public override bool Cancel(ActivityPaperWorkViewModel model)
        {
            return base.Cancel(model);
        }
        public override void CreateTask(PreApprovalRequestViewModel model)
        {
            base.CreateTask(model);
        }
        private void LoadModifiedDataForDpre(PreApprovalRequestViewModel preApprovalRequestViewModel, AfsPostActivityViewModel afsPostActivity, Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls)
        {
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.PracticalOralTestId != null)
            {
                preApprovalRequestViewModel.TestInformation.PracticalOralTestId = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.PracticalOralTestId;
                AddItemToModifiedPreapprovals("TestInformation", new ModifiedControlViewModel { Control = "practicalOralTestId" }, modifiedPreapprovalControls);
            }
            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.ApplicantName))
            {
                preApprovalRequestViewModel.ApplicantInformation.Name = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.ApplicantName;
                AddItemToModifiedPreapprovals("ApplicantInformation", new ModifiedControlViewModel { Control = "name" }, modifiedPreapprovalControls);
            }

            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.ApplicantCertificateNumber))
            {
                preApprovalRequestViewModel.ApplicantInformation.CertificateNumber = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.ApplicantCertificateNumber;
            }

            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AuthorizedTestOfficeId.GetValueOrDefault() != 0)
            {
                preApprovalRequestViewModel.ApplicationInformation.AuthorizedTestOfficeId = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AuthorizedTestOfficeId;
            }

            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsCivilExperience != null)
            {
                preApprovalRequestViewModel.ApplicationInformation.IsCivilExperience = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsCivilExperience;
            }

            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsMilitaryExperience != null)
            {
                preApprovalRequestViewModel.ApplicationInformation.IsMilitaryExperience = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsMilitaryExperience;
            }
        }
        public override PreApprovalRequestViewModel SavePostActivityEvaluation(PreApprovalRequestViewModel adminModel)
        {
            var modelaAfs = base.SavePostActivityEvaluation(adminModel);
            _postActivity.IsAdditionalInstructionsProvided = modelaAfs.AfsPostActivity.AfsPostActivityDmeDpre.IsAdditionalInstructionsProvided.HasValue ? modelaAfs.AfsPostActivity.AfsPostActivityDmeDpre.IsAdditionalInstructionsProvided.Value : false;
            _postActivity.PostActivityResultTypeId = modelaAfs.AfsPostActivity.AfsPostActivityDmeDpre.ResultTypeId != null ? modelaAfs.AfsPostActivity.AfsPostActivityDmeDpre.ResultTypeId : null;
            var modelDpre = SavePostActivityEvaluationDMEDPRE(modelaAfs);
            return modelDpre;
        }
        public override bool SaveMsDecision(PreApprovalRequestViewModel model)
        {
            return base.SaveMsDecision(model);
        }
        protected override IEnumerable<PreApprovalDateWiseCountViewModel> GetPreApprovalDateWiseCount(int applicationId)
        {
            return base.GetPreApprovalDateWiseCount(applicationId);
        }
        public static bool FindModifiedDpreFunctionCodes(PreApprovalRequestViewModel model, PreApprovalRequest preApprovalRequest, AfsPostActivityModifiedPreApprovalViewModel afsPostActivityModifiedPreApprovalViewModel, bool triggerCorrectiveActionTrigger)
        {
            var preApprovalFuncCodes = new List<PreApprovalRequestFunctionCode>();
            bool differenceInFuncCodes = FindAnyChangeInDpreFuncCodes(model, preApprovalRequest);

            if (differenceInFuncCodes)
            {
                foreach (var type in model.DpreFunctionCodes.Where(it => it.IsSelected))
                {
                    foreach (var fun in type.authSelectedFunctionCodes)
                    {
                        preApprovalFuncCodes.Add(new PreApprovalRequestFunctionCode
                        {
                            FunctionCodeId = fun,
                            TypeId = type.FunctionCodeType.Id
                        });
                    }
                }
                afsPostActivityModifiedPreApprovalViewModel.DpreFunctionCodes = preApprovalFuncCodes;
                triggerCorrectiveActionTrigger = differenceInFuncCodes;
            }
            return triggerCorrectiveActionTrigger;
        }
        private static bool FindAnyChangeInDpreFuncCodes(PreApprovalRequestViewModel model, PreApprovalRequest preApprovalRequest)
        {
            var preapprovalFunctionCodes = preApprovalRequest.PreApprovalRequestFunctionCodes.Select(p => new
            {
                FunctionCodeId = p.FunctionCodeId,
                Type = p.Type,
                TypeId = p.TypeId,
            }).GroupBy(it => it.TypeId)
            .Select(ip => new FunctionCodeTypeViewModel
            {
                FunctionCodeType = ip.First().Type,
                IsSelected = true,
                authSelectedFunctionCodes = ip.Select(ig => ig.FunctionCodeId).Distinct().ToList()
            });
            var differences = model.DpreFunctionCodes.Where(it => it.IsSelected).Except(preapprovalFunctionCodes, new DpreFunctionCodeComparer()).ToList();
            var predifferences = preapprovalFunctionCodes.Except(model.DpreFunctionCodes, new DpreFunctionCodeComparer()).ToList();

            return (differences.Count > 0 || predifferences.Count > 0);
        }
        public static bool FindModifiedDataForDpre(PreApprovalRequestViewModel model, PreApprovalRequest preApprovalRequest, AfsPostActivityModifiedPreApprovalViewModel afsPostActivityModifiedPreApprovalViewModel, bool triggerCorrectiveActiveTrigger)
        {
            if (model.TestInformation.PracticalOralTestId != preApprovalRequest.AfsPreApprovalRequest.PracticalOralTestId)
            {
                afsPostActivityModifiedPreApprovalViewModel.PracticalOralTestId = model.TestInformation.PracticalOralTestId;
                triggerCorrectiveActiveTrigger = true;
            }

            if (model.ApplicantInformation.Name != preApprovalRequest.AfsPreApprovalRequest.ApplicantName)
            {
                afsPostActivityModifiedPreApprovalViewModel.ApplicantName = model.ApplicantInformation.Name;
                triggerCorrectiveActiveTrigger = true;
            }

            if (model.ApplicantInformation.CertificateNumber != preApprovalRequest.AfsPreApprovalRequest.ApplicantCertificateNumber)
            {
                afsPostActivityModifiedPreApprovalViewModel.ApplicantCertificateNumber = model.ApplicantInformation.CertificateNumber;
            }

            if (model.Comments != preApprovalRequest.AfsPreApprovalRequest.PreApprovalRequest.Comments)
            {
                afsPostActivityModifiedPreApprovalViewModel.Comments = model.Comments;
            }

            if (model.ApplicationInformation.AuthorizedTestOfficeId != preApprovalRequest.AfsPreApprovalRequest.AuthorizedTestOfficeId)
            {
                afsPostActivityModifiedPreApprovalViewModel.AuthorizedTestOfficeId = model.ApplicationInformation.AuthorizedTestOfficeId;
            }

            if (model.ApplicationInformation.IsMilitaryExperience != preApprovalRequest.AfsPreApprovalRequest.IsMilitaryExperience)
            {
                afsPostActivityModifiedPreApprovalViewModel.IsMilitaryExperience = model.ApplicationInformation.IsMilitaryExperience;
            }

            if (model.ApplicationInformation.IsCivilExperience != preApprovalRequest.AfsPreApprovalRequest.IsCivilExperience)
            {
                afsPostActivityModifiedPreApprovalViewModel.IsCivilExperience = model.ApplicationInformation.IsCivilExperience;
            }

            triggerCorrectiveActiveTrigger = CheckForAddress(model, preApprovalRequest, afsPostActivityModifiedPreApprovalViewModel, triggerCorrectiveActiveTrigger) || triggerCorrectiveActiveTrigger;
            return triggerCorrectiveActiveTrigger;
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
        //new code
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
                SelectedFunctionCodes = new List<int>()
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

            _postActivity.Comments = model.AfsPostActivityDmeDpre.Comments;
            _postActivity.AirManName = model.AfsPostActivityDmeDpre.AirManName;
            _postActivity.AirManCertificateNumber = model.AfsPostActivityDmeDpre.AirManCertificateNumber;
            _postActivity.ActualStartDate = model.AfsPostActivityDmeDpre.ActualStartDate.ToNullableDate();
            _postActivity.ActualEndDate = model.AfsPostActivityDmeDpre.ActualEndDate.ToNullableDate();
            _postActivity.OralPortionTestDuration = model.AfsPostActivityDmeDpre.OralPortionTestDuration.ToNullableDate();
            _postActivity.PracticalPortionTestDuration = model.AfsPostActivityDmeDpre.PracticalPortionTestDuration.ToNullableDate();
            _postActivity.ResultsSubmissionDate = model.AfsPostActivityDmeDpre.ResultsSubmissionDate.ToNullableDate();
            _postActivity.ApplicantEmail = model.AfsPostActivityDmeDpre.Email;
            _postActivity.ApplicantPhone = model.AfsPostActivityDmeDpre.Phone;
            _postActivity.IsAdditionalInstructionsProvided = model.AfsPostActivityDmeDpre.IsAdditionalInstructionsProvided.HasValue ? model.AfsPostActivityDmeDpre.IsAdditionalInstructionsProvided.Value : false;
            _postActivity.PostActivityResultTypeId = model.AfsPostActivityDmeDpre.ResultTypeId != null ? model.AfsPostActivityDmeDpre.ResultTypeId : null;
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
            if (!model.IsSubmit){
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
         private static int? GetStateId(AfsGroupsPostActivityViewModel model) => model.AfsPostActivityDmeDpre.Address.Country?.Id == 184 ? model.AfsPostActivityDmeDpre.Address.State?.Id : null;

        protected void CheckPreApprovalModifiedData(AfsGroupsPostActivityViewModel model)
        {
            model.PreApprovalRequest.ModifiedPreapprovalControls = new Dictionary<string, List<ModifiedControlViewModel>>();
            var afsPostActivityModifiedPreApprovalViewModel = new AfsPostActivityModifiedPreApprovalViewModel();
            var preApprovalRequest =  base.GetOriginalPreApprovalData(model.PreApprovalRequestId);

            var triggerCorrectiveAction = false;

            triggerCorrectiveAction = CheckForAddress(model.PreApprovalRequest.ActivityLocation.LocationAddress,
                            preApprovalRequest.AfsPreApprovalRequest.Address,
                            model.PreApprovalRequest.ModifiedPreapprovalControls,
                            model.PreApprovalRequest.ActivityLocation.FacilityonRecord
                                .GetValueOrDefault(),
                            preApprovalRequest.AfsPreApprovalRequest.FacilityOnRecord.GetValueOrDefault()) || triggerCorrectiveAction;
            bool differenceInFuncCodes = FindAnyChangeInDpreFuncCodes(model.PreApprovalRequest, preApprovalRequest);

            if (model.PreApprovalRequest.ApplicantInformation.Name != preApprovalRequest.AfsPreApprovalRequest.ApplicantName)
            {
                AddItemToModifiedPreapprovals("ApplicantInformation", new ModifiedControlViewModel { Control = "name" }, model.PreApprovalRequest.ModifiedPreapprovalControls);
                triggerCorrectiveAction = true;
            }
            if (differenceInFuncCodes)
            {
                AddItemToModifiedPreapprovals("DpreFunctionCodes", new ModifiedControlViewModel { Control = "dpreFunctionCodesGroup" }, model.PreApprovalRequest.ModifiedPreapprovalControls);
            }

            //Corrective action triggering for Post activity actual start date is difffrent than pre approval test date
            var datetriggerCorrectiveAction = false;
            var proposedStartDate = preApprovalRequest.ProposeStartDate.Value;
            var proposedEndDate = preApprovalRequest.ProposeEndDate.Value;

            if (!string.IsNullOrEmpty(model.AfsPostActivityDmeDpre.ActualStartDate))
            {
                var actualStartDate = DateTime.Parse(model.AfsPostActivityDmeDpre.ActualStartDate);
                var actualEndDate = DateTime.Parse(model.AfsPostActivityDmeDpre.ActualEndDate);

                datetriggerCorrectiveAction = !((actualStartDate.Date >= proposedStartDate.Date && actualStartDate.Date <= proposedEndDate) &&
                                                       (actualEndDate.Date >= proposedStartDate.Date && actualEndDate.Date <= proposedEndDate.Date));

            }

            triggerCorrectiveAction = differenceInFuncCodes || datetriggerCorrectiveAction || triggerCorrectiveAction;

            //Create activity with format and save
            if (triggerCorrectiveAction)
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
