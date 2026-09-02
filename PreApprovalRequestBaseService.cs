using System;
using System.Linq;
using System.Collections.Generic;
using Dms.Core.EntityFramework.Data;
using Dms.Core.EntityFramework.Model.Activity;
using Dms.Services.Interface.Activity;
using Dms.Services.Interface.Message;
using Dms.Services.Interface.Shared;
using Dms.Services.Interface.Task;
using Dms.Services.ViewModel;
using Dms.Services.ViewModel.Activity;
using Microsoft.EntityFrameworkCore;
using Dms.Core.Utils;
using Dms.Services.ViewModel.Shared;
using Dms.Services.ViewModel.Lookup;
using Dms.Core.Extensions;
using Dms.Services.ViewModel.Task;
using Dms.Services.Interface.Lookup;
using Dms.Services.ViewModel.Message;
using Dms.Services.ViewModel.Cloa;
using Dms.Services.Assembler;
using Dms.Core.EntityFramework.Model.Lookup;
using Dms.Services.ViewModel.Security;
using Dms.Services.Interface.Security;
using Dms.Services.ViewModel.Utils;
using System.Diagnostics.CodeAnalysis;

namespace Dms.Services.Implementation.Activity
{
    public abstract class PreApprovalRequestBaseService(DmsContext context, ITaskService taskService, IDocumentService documentService, 
        IActivityService activityService, IMessageService messageService, ISharedService sharedService, ILookupService lookupService, 
        IUserService userService) : IPreApprovalRequestService
    {
        protected readonly DmsContext _context = context;
        protected readonly ITaskService _taskService = taskService;//Dms.Services.Interface.Task
        protected readonly IDocumentService _documentService = documentService;//Dms.Services.Interface.Shared
        protected readonly IActivityService _activityService = activityService;//Dms.Services.Interface.Activity
        protected readonly IMessageService _messageService = messageService;//Dms.Services.Interface.Message
        protected readonly ISharedService _sharedService = sharedService;//Dms.Services.Interface.Shared
        protected PreApprovalRequest _preApprovalRequest;
        protected PreApprovalRequestViewModel _preApprovalRequestViewModel = new PreApprovalRequestViewModel();
        protected AfsGroupsPostActivityViewModel _afsGroupsPostActivityViewModel = new AfsGroupsPostActivityViewModel();
        protected ILookupService _lookupService = lookupService;
        protected CloaPreApprovalRequestViewModel _cloa;
        protected int _designeeType;
        protected readonly IUserService _userService = userService;

        //This method is used in activity paperwork to cancel the preapproval request and any associated tasks or post approvals
        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity inherent to task navigation system")]
        public virtual bool Cancel(ActivityPaperWorkViewModel model)
        {
            bool flag = false;
            var preApprovalRequest = _context.PreApprovalRequests.Include(itp => itp.AfsPreApprovalRequest).Include(itp => itp.PostActivities).Where(par => par.Id == model.Id).FirstOrDefault();
            if (preApprovalRequest != null)
            {
                if (!(string.IsNullOrEmpty(model.JustificationForCancellation)))
                {
                    preApprovalRequest.AfsPreApprovalRequest.JustificationForCancellation = model.JustificationForCancellation;
                }
                if (model.DesigneeTypeId == (int)DesigneeTypeEnum.DPE || model.DesigneeTypeId == (int)DesigneeTypeEnum.ADMINPE || model.DesigneeTypeId == (int)DesigneeTypeEnum.SAE)
                {
                    preApprovalRequest.AfsPreApprovalRequest.CancellationTypeId = model.CancellationTypeId.Value;
                }

                preApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Canceled;
                var tasks = _context.Tasks.Where(t =>
                    (t.ActionId == preApprovalRequest.Id) && t.TaskSubType.TaskTypeId == (int)TaskTypeEnum.PreApproval &&
                    ((t.StatusId == (int)TaskStatusEnum.Pending) || (t.StatusId == (int)TaskStatusEnum.InProgress))).ToArray();
                foreach (var t in tasks)
                {
                    t.StatusId = (int)TaskStatusEnum.Canceled;
                }
                if (preApprovalRequest.PostActivities.Any())
                {
                    preApprovalRequest.PostActivities.OrderByDescending(po => po.Id).First().StatusId = (int)PreApprovalRequestStatusEnum.Canceled;
                }

                //send preapproval cancelled notification.
                var cloa = _context.Cloas
                                    .Include(c => c.Application).ThenInclude(c => c.User).ThenInclude(c => c.Profile)
                                    .Where(c => c.ApplicationId == preApprovalRequest.ApplicationId && (c.CloaStatusId == (int)CloaStatusEnum.Active || c.CloaStatusId == (int)CloaStatusEnum.Suspended || c.CloaStatusId == (int)CloaStatusEnum.Terminated))
                                    .Select(a => new
                                    {
                                        DesigneeName = a.ProfileVersion.ToFullName(),
                                        DesigneeUserOfficeRoleUserId = a.Application.User.Id,
                                        ManagingSpecialistUserId = a.MsUserOfficeRole.UserId,
                                    }).FirstOrDefault();

                var notificationViewModel = new MessageNotificationViewModel
                {
                    MessageDictionary = new List<KeyValuePair<string, string>>()
                                    {
                                        new KeyValuePair<string, string>("@trackingNumber", preApprovalRequest.TrackingNumber),
                                        new KeyValuePair<string, string>("@designeeName", cloa != null ? cloa.DesigneeName : "")
                                    },
                    UserRecipients = new List<(int UserId, bool IsCced)> { (cloa != null ? cloa.ManagingSpecialistUserId : 0, false) },
                    Code = "PRECAN",
                };
                _messageService.SendNotification(notificationViewModel);

                _context.SaveChanges();
                flag = true;
            }
            return flag;
        }
        protected void CancelHelper(int preApprovalRequestId)
        {
            var preApprovalRequest = _context.PreApprovalRequests.Include(itp => itp.PostActivities).Where(it => it.Id == preApprovalRequestId).FirstOrDefault();
            if(preApprovalRequest == null){return;}
            //Set Cancelled status for current one
            preApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Canceled;
            if (preApprovalRequest.PostActivities.Any())
            {
                preApprovalRequest.PostActivities.OrderByDescending(po => po.Id).First().StatusId = (int)PreApprovalRequestStatusEnum.Canceled;
            }
        }
        public virtual void CreateTask(PreApprovalRequestViewModel model)
        {

            var msPreApprpvalRequestTask = new TaskViewModel
            {
                TaskSubTypeId = (model.OtherPreApprovalRequest != null && model.OtherPreApprovalRequest.IsOutSideArea)
                   ? (int)TaskSubTypeEnum.GeographicExpansionRequest : (int)TaskSubTypeEnum.PreApprovalRequest,
                TaskStatusId = (int)TaskStatusEnum.Pending,
                UserOfficeRoleId = model.ManagingSpecialist,
                ActionId = model.Id,
                ApplicationId = model.DesigneeInfo.ApplicationId,
            };

            // find any re-assign pending task
            var existingTask = _context.Tasks.FirstOrDefault(t => t.ActionId == msPreApprpvalRequestTask.ActionId
                                                            && t.SubTypeId == msPreApprpvalRequestTask.TaskSubTypeId && t.UserOfficeRoleId != model.ManagingSpecialist
                                                            && t.StatusId == (int)TaskStatusEnum.Pending);
            if(existingTask != null)
            {
              existingTask.StatusId = (int)TaskStatusEnum.Canceled;
            }

            _taskService.CreateTask(msPreApprpvalRequestTask, true);

        }

        public virtual PreApprovalRequestViewModel Get(int preApprovalRequestId, bool loadPreapprovalModifiedData, int postActivityId, int? cloaId = null)
        {

            return Get();
        }

        private PreApprovalRequestViewModel Get()
        {
            _preApprovalRequestViewModel.Id = _cloa.Id;
            _preApprovalRequestViewModel.ApplicationId = _cloa.ApplicationId;
            _preApprovalRequestViewModel.ManagingSpecialist = _cloa.ManagingSpecialist.Value;
            _preApprovalRequestViewModel.ManagingSpecialistName = _cloa.ManagingSpecialistName;
            _preApprovalRequestViewModel.GeoExpansionDecision = _preApprovalRequest.GeoExpansionDecision;
            _preApprovalRequestViewModel.GeoExpansionJustification = _preApprovalRequest.GeoExpansionJustification;
            _preApprovalRequestViewModel.ManagingOffice = _cloa.ManagingOffice;
            _preApprovalRequestViewModel.UserId = _cloa.UserId;
            _preApprovalRequestViewModel.DesigneeTypeId = _cloa.DesigneeInfo.TypeId;
            _preApprovalRequestViewModel.DesigneeInfo = _cloa.DesigneeInfo;
            _preApprovalRequestViewModel.IsPossibleDirectObservation = _preApprovalRequest.IsPossibleDirectObservation;
            return _preApprovalRequestViewModel;
        }
        public virtual AfsGroupsPostActivityViewModel Get(int postActivityId, bool createDocumentVersion = false)
        {
            Get();
            return _afsGroupsPostActivityViewModel;
        }

        protected PreApprovalRequestViewModel GetHelper(PreApprovalRequestViewModel preApprovalRequestViewModel)
        {
            _preApprovalRequestViewModel = preApprovalRequestViewModel;
            _preApprovalRequestViewModel.TrackingNumber = _preApprovalRequest.TrackingNumber;
            _preApprovalRequestViewModel.IsApproved = _preApprovalRequest.IsApproved;
            _preApprovalRequestViewModel.ApproverComment = _preApprovalRequest.ApproverComments;
            _preApprovalRequestViewModel.ApproverJusitification = _preApprovalRequest.ApproverJustification;
            _preApprovalRequestViewModel.GeoMsIsApprove = _preApprovalRequest.GeoMsIsApproved;
            _preApprovalRequestViewModel.GeoExpansionSelectedOfficeId = _preApprovalRequest.GeoExpansionSelectedOffice != null ? _preApprovalRequest.GeoExpansionSelectedOfficeId : null;
            _preApprovalRequestViewModel.GeoExpansionSelectedApproverId = _preApprovalRequest.GeoExpansionSelectedApproverId;
            _preApprovalRequestViewModel.IsPreApprovalReadOnly = _preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Canceled || _preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Pending || _preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Approved || _preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Rejected || _preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Completed;
            _preApprovalRequestViewModel.DateFieldsEditableForDmirDarf = (_preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Saved || _preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Pending)
             ? false : _preApprovalRequestViewModel.IsPreApprovalReadOnly;

            var po = _context.PostActivities
                            .Where(e => e.PreApprovalRequestId == _preApprovalRequest.Id)
                            .OrderByDescending(po => po.Id)
                            .Select(po => new
                            {
                                po.StatusId,
                                po.CompletedDate
                            })
                            .FirstOrDefault();
            int [] poStatus = {(int)PreApprovalRequestStatusEnum.Saved, (int)PreApprovalRequestStatusEnum.Initiated};
            _preApprovalRequestViewModel.IsPostActivityReadOnly = po != null && !poStatus.Contains((int)po.StatusId) ? po.StatusId == (int)PreApprovalRequestStatusEnum.Canceled || !(po.StatusId == (int)PreApprovalRequestStatusEnum.Pending && po.CompletedDate != null) || po.StatusId == (int)PreApprovalRequestStatusEnum.Completed : false;
            _preApprovalRequestViewModel.IsEdit = po != null ? (poStatus.Contains((int)po.StatusId) || (po.StatusId == (int)PreApprovalRequestStatusEnum.Pending && po.CompletedDate == null)) : false;
            _preApprovalRequestViewModel.IsPreApprovalMsDecisionReadOnly = _preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Approved || _preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Rejected || _preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Completed || _preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Canceled || (_preApprovalRequest.GeoMsIsApproved != null && (bool)_preApprovalRequest.GeoMsIsApproved);
            _preApprovalRequestViewModel.IsPerformanceResultReadOnly = po != null? po.StatusId == (int)PreApprovalRequestStatusEnum.Completed : false;
            _preApprovalRequestViewModel.PreApprovalDateWiseCount = GetPreApprovalDateWiseCount(_cloa.DesigneeInfo.ApplicationId);   //DesigneeTypeId = c.Application.DesigneeTypeId,
            _preApprovalRequestViewModel.Comments = _preApprovalRequest.Comments;
            _preApprovalRequestViewModel.StatusId = _preApprovalRequest.StatusId;
            _preApprovalRequestViewModel.IsMsDecisionPending = _context.Tasks.Any(it => it.ActionId == _preApprovalRequest.Id && it.StatusId == (int)TaskStatusEnum.Pending
                                                                    && it.SubTypeId == (int)TaskSubTypeEnum.PreApprovalRequest);
            return _preApprovalRequestViewModel;
        }
        protected virtual IEnumerable<PreApprovalDateWiseCountViewModel> GetPreApprovalDateWiseCount(int applicationId)
        {
            var dateWiseCount = CloaPreApprovalRequestViewModelMapper.GetPreApprovalDateWiseCountViewModel(_context, applicationId);
            return dateWiseCount;
        }
        public virtual PreApprovalRequestViewModel GetNew(int applicationId)
        {
            var cloaInfo =  _sharedService.GetDesigneeInfoByCloa(_cloa.Id);
            _cloa.DesigneeInformation.Status = cloaInfo.Status;
            _cloa.DesigneeInformation.StatusId = cloaInfo.StatusId;
            _cloa.RequestInfo.ControlNumber = _activityService.GenerateTrackingNumber(_cloa.Id, (int)ProcessTypeEnum.PreApproval, null);
            return _preApprovalRequestViewModel;
        }
        protected PreApprovalRequestViewModel GetNewHelp(int applicationId)
        {
            var preApprovalRequestViewModel = _preApprovalRequestViewModel;
            var cloa = _cloa;
            preApprovalRequestViewModel.DesigneeTypeId = cloa.DesigneeInformation.TypeId;
            preApprovalRequestViewModel.ApplicationId = applicationId;
            preApprovalRequestViewModel.ManagingSpecialist = cloa.ManagingSpecialist.Value;
            preApprovalRequestViewModel.UserId = cloa.UserId;
            preApprovalRequestViewModel.DesigneeInfo = cloa.DesigneeInformation;
            preApprovalRequestViewModel.AuthFunctionCodes = cloa.DesigneeFunctionCodes;
            preApprovalRequestViewModel.SelectedFunctionCodes = new List<int>();
            preApprovalRequestViewModel.RequestInfo = cloa.RequestInfo;
            preApprovalRequestViewModel.ActivityLocation = new PreApprovalActivityLocationViewModel()
            {
                FacilityonRecord = (cloa.DesigneeInformation.TypeId == (int)DesigneeTypeEnum.DME || cloa.DesigneeInformation.TypeId == (int)DesigneeTypeEnum.DPRE || 
                cloa.DesigneeInformation.TypeId == (int)DesigneeTypeEnum.DPE || cloa.DesigneeInformation.TypeId == (int)DesigneeTypeEnum.SAE ||
                cloa.DesigneeInformation.TypeId == (int)DesigneeTypeEnum.ADMINPEGEN ||
                cloa.DesigneeInformation.TypeId == (int)DesigneeTypeEnum.ADMINPE || cloa.DesigneeInformation.TypeId == (int)DesigneeTypeEnum.APD || 
                cloa.DesigneeInformation.TypeId == (int)DesigneeTypeEnum.TCE || cloa.DesigneeInformation.TypeId == (int)DesigneeTypeEnum.DADE || 
                cloa.DesigneeInformation.TypeId == (int)DesigneeTypeEnum.DCTOE),
                FacilityAddress = cloa.FacilityAddress,
                FacilityAddresses = cloa.FacilityAddresses,
                LocationAddress = new AddressViewModel(),
            };
            preApprovalRequestViewModel.TestInformation = new PreApprovalTestInformationViewModel()
            {
                ProposedStartTime = "06:00",//DateTime.Now.ToString("HH:mm")
                TimeZoneId = cloa.TimeZoneId
            };
            preApprovalRequestViewModel.ApplicationInformation = new PreApprovalApplicationInformationViewModel();
            preApprovalRequestViewModel.ApplicationInformation.DesignatorName = cloa.DesingatorName;
            preApprovalRequestViewModel.ApplicantInformation = new PreApprovalApplicantInformationViewModel();
            preApprovalRequestViewModel.PlannedActivity = new PreAprovalPlannedActivityViewModel()
            {
                Products = new List<PreApprovalRequestProductViewModel>()
                {
                }
            };
            preApprovalRequestViewModel.DocumentReference =
                _documentService.GetDocumentsByRef((int)DocumentReferenceEnum.PreapprovalRequest, 0, null);
            return preApprovalRequestViewModel;
        }

        public abstract PaginationViewModel<IList<ActivityPaperWorkViewModel>> GetPreApprovalList(RequestListModel model);

        public abstract PaginationViewModel<IList<ActivityPaperWorkViewModel>> GetPostActivityList(RequestListModel model);

        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
            Justification = "Builds the pre-approval and post-activity action URLs via designee-group/category and status dispatch, each a mutually-exclusive routing branch. The complexity is breadth across the URL-routing cases; control-flow restructuring does not bring it to threshold.")]
        protected IList<LinkViewModel> SetUrls(int preApprovalStatusId, int? postActivityStatusId, int id, int designeeGroupId, string comments, int createdBy, int? modifiedBy, int designeeCategoryId, int? postActivityId = 0, bool? isAdmin = false)
        {
            var links = new List<LinkViewModel>();
            var url = string.Empty;
            var isNewWindow = false;
            // PreApproval
            if ((int)Core.Utils.PreApprovalRequestStatusEnum.Saved != preApprovalStatusId &&
                (int)Core.Utils.PreApprovalRequestStatusEnum.Initiated != preApprovalStatusId)
            {
                if (designeeGroupId == (int)DesigneeGroupEnum.Manufacturing || designeeCategoryId == (int)DesigneeCategoryEnum.FlightStandardsAirTransportationService)
                {
                    url = $"/preapprovalrequest/evaluate/{CryptoExtensions.Encrypt(id)}";
                }
                else if (designeeGroupId == (int)DesigneeGroupEnum.AFS && (int)Core.Utils.PreApprovalRequestStatusEnum.Approved == preApprovalStatusId &&
                    comments != null && comments.Equals("Auto Pre Approval") && createdBy == modifiedBy)
                {
                    url = $"/preapprovalrequest/afs/evaluate/{CryptoExtensions.Encrypt(id)}";
                }
                else if (designeeGroupId == (int)DesigneeGroupEnum.AOV && createdBy == modifiedBy)
                {
                    url = $"/preapprovalrequest/aov/summary/{id.Encrypt()}";
                }
                else
                {
                    url = designeeGroupId == (int)DesigneeGroupEnum.AOV && createdBy != modifiedBy ? $"/preapprovalrequest/aov/summary/{id.Encrypt()}?newTab=true" : $"/preapprovalrequest/afs/summary/{CryptoExtensions.Encrypt(id)}";
                    isNewWindow = true;
                }
            }
            links.Add(new LinkViewModel
            {
                Code = "PR",
                Url = url,
                IsNewWindow = isNewWindow
            });
            // PostActivity
            url = string.Empty;
            isNewWindow = false;
            if (designeeCategoryId == (int)DesigneeCategoryEnum.FlightStandardsAirTransportationService)
            {
                if ((int)Core.Utils.PreApprovalRequestStatusEnum.Completed == postActivityStatusId ||
                    (int)Core.Utils.PreApprovalRequestStatusEnum.Canceled == postActivityStatusId)
                {
                    url = $"/postactivity/group3/summary/{postActivityId}/{isAdmin}/false/false";
                    isNewWindow = true;
                }
            }
            else if (designeeGroupId == (int)DesigneeGroupEnum.Manufacturing)
            {
                if ((int)Core.Utils.PreApprovalRequestStatusEnum.Completed == postActivityStatusId ||
                    (int)Core.Utils.PreApprovalRequestStatusEnum.Pending == postActivityStatusId ||
                    (int)Core.Utils.PreApprovalRequestStatusEnum.Rejected == postActivityStatusId ||
                    (int)Core.Utils.PreApprovalRequestStatusEnum.Approved == postActivityStatusId)
                {
                    url = $"/performanceresult/{id}";
                }
            }
            else
            {
                if ((int)Core.Utils.PreApprovalRequestStatusEnum.Completed == postActivityStatusId ||
                    (int)Core.Utils.PreApprovalRequestStatusEnum.Canceled == postActivityStatusId)
                {
                    url = designeeGroupId == (int)DesigneeGroupEnum.AOV?  $"/postactivity/aovgroups/{postActivityId.Encrypt()}/false?isReadOnly=true" : $"/postactivity/afsgroups/{postActivityId}/{isAdmin}/false";
                    isNewWindow = true;
                }
            }

            links.Add(new LinkViewModel
            {
                Code = "PO",
                Url = url,
                IsNewWindow = isNewWindow
            });

            return links;
        }

        public virtual int ReInitiate(int preApprovalRequestId)
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
        
        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
            Justification = "Persists the pre-approval request via update-existing (reload graph, conditional status transition, function-code reset) vs create-new branches. The complexity is breadth across the two persistence paths and the function-code reset loop; the available guard inversion does not bring it to threshold.")]
        public virtual PreApprovalRequestViewModel Save(PreApprovalRequestViewModel model)
        {
            if (model.Id != 0)
            {

                _preApprovalRequest = _context.PreApprovalRequests.Include(p => p.PreApprovalRequestFunctionCodes).ThenInclude(p => p.PreApprovalRequestFunctionCodeTypeRatings)
                                                                    .Include(p => p.PreApprovalRequestFunctionCodes).ThenInclude(p => p.PreApprovalRequestFunctionCodeMakeModels)
                                                                    .Include(p => p.PreApprovalRequestExperimentals)
                                                                    .Include(p => p.OtherPreApprovalRequest).ThenInclude(p => p.Address)
                                                                    .Include(p => p.AfsPreApprovalRequest)
                                                                    .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.Address)
                                                                    .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.AircraftOwnerAddress)
                                                                    .Include(p => p.AfsPreApprovalRequest).ThenInclude(p => p.AircraftOperatorAddress)
                                                                    .Include(p => p.PreApprovalRequestProducts)
                                                                    .First(p => p.Id == model.Id);

                if (!_preApprovalRequest.IsApproved.HasValue && _preApprovalRequest.StatusId != (int)PreApprovalRequestStatusEnum.Pending && _preApprovalRequest.StatusId != (int)PreApprovalRequestStatusEnum.Approved)
                {
                    _preApprovalRequest.StatusId = model.isSubmit ? (int)PreApprovalRequestStatusEnum.Pending : (int)PreApprovalRequestStatusEnum.Saved;
                }
                _preApprovalRequest.SubmittedDate = _preApprovalRequest.SubmittedDate.HasValue ? _preApprovalRequest.SubmittedDate : model.isSubmit ? DateTime.Now : (DateTime?)null;

                //TO Do : Create Record Status in Data base and save from here.

                //Remove all Function code and add them back from model in the designee types specific.
                foreach (var fc in _preApprovalRequest.PreApprovalRequestFunctionCodes.ToList())
                {
                    if (fc.PreApprovalRequestFunctionCodeTypeRatings != null && fc.PreApprovalRequestFunctionCodeTypeRatings.Any())
                    {
                        fc.PreApprovalRequestFunctionCodeTypeRatings.Clear();
                    }

                    if (fc.PreApprovalRequestFunctionCodeMakeModels != null && fc.PreApprovalRequestFunctionCodeMakeModels.Any())
                    {
                        fc.PreApprovalRequestFunctionCodeMakeModels.Clear();
                    }

                    _preApprovalRequest.PreApprovalRequestFunctionCodes.Remove(fc);
                }
                _preApprovalRequestViewModel = model;
                _preApprovalRequest.Comments = model.Comments;

                // _preApprovalRequest is a protected member of the base class that will be accesible by the the drived classes
                return _preApprovalRequestViewModel;
            }
            else
            {
                _preApprovalRequest = new PreApprovalRequest
                {
                    ApplicationId = model.ApplicationId.Value,
                    CloaId = model.DesigneeInfo.CloaId,
                    TrackingNumber = _activityService.GenerateTrackingNumber(model.DesigneeInfo.CloaId, (int)ProcessTypeEnum.PreApproval),
                    SubmittedDate = model.isSubmit ? DateTime.Now : (DateTime?)null,
                };
                if (model.isSubmit)
                {
                    _preApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Pending;
                }
                else
                { _preApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Saved; }
                _preApprovalRequest.Comments = model.Comments;
                _preApprovalRequestViewModel = model;
                return _preApprovalRequestViewModel;
            }

        }


        public virtual bool SaveGeoGraphical(PreApprovalRequestViewModel model)
        {
            var preApprovalRequest = _context.PreApprovalRequests.FirstOrDefault(p => p.Id == model.Id);

            if (preApprovalRequest == null)
            {
                return false;
            }
            var tasks = new List<Core.EntityFramework.Model.Task>();

            if (model.IsAfsType)
            {
                tasks = _context.Tasks.Where(t => (t.ActionId == model.Id && t.SubTypeId == (int)TaskSubTypeEnum.GeographicExpansionRequest)).ToList();
                if (tasks.Any(t => t.StatusId == (int)TaskStatusEnum.Completed))
                {
                    return false;
                }
            }

            preApprovalRequest.GeoExpansionDecision = model.GeoExpansionDecision;
            preApprovalRequest.GeoExpansionJustification = model.GeoExpansionJustification;
            if (model.GeoExpansionDecision == true)
            {
                preApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Approved;
                preApprovalRequest.IsPreApprovalOnHold = false;
                //send Approval notification.
                SendPreapprovalDecisionNotification(model.TrackingNumber, model.DesigneeInfo.Name, model.DesigneeInfo.Id, "PREAPR");

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
            else
            {
                preApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Rejected;
                //send denied notification.
                SendPreapprovalDecisionNotification(model.TrackingNumber, model.DesigneeInfo.Name, model.DesigneeInfo.Id, "PRERJT");
            }
            if (model.IsAfsType)
            {
                preApprovalRequest.GeoExpansionSelectedApproverId = model.GeoExpansionSelectedApproverId;

                foreach (var task in tasks)
                {
                    task.StatusId = (int)TaskStatusEnum.Completed;
                }
            }
            else
            {
                var task = _context.Tasks.FirstOrDefault(t => t.ActionId == model.Id && t.TaskSubType.Id == (int)TaskSubTypeEnum.GeographicExpansionRecommendation && t.TaskStatus.Id == (int)TaskStatusEnum.Pending);
                if (task != null)
                {
                    task.StatusId = (int)TaskStatusEnum.Completed;
                }
            }
            _context.SaveChanges();
            return true;
        }

        public virtual bool SaveMsDecision(PreApprovalRequestViewModel model)
        {
            _preApprovalRequest = _context.PreApprovalRequests.Include(p => p.AfsPreApprovalRequest)
           .Include(p => p.PreApprovalRequestFunctionCodes).Include(p => p.PostActivities).FirstOrDefault(p => p.Id == model.Id);

            return true;

        }
        protected bool SaveMsDecisionHelp(PreApprovalRequestViewModel model)
        {
            if (_preApprovalRequest != null)
            {

                //Complete the Task Status.
                var task = _context.Tasks.FirstOrDefault(it => it.ActionId == _preApprovalRequest.Id && it.StatusId == (int)TaskStatusEnum.Pending && (it.SubTypeId == (int)TaskSubTypeEnum.PreApprovalRequest || it.SubTypeId == (int)TaskSubTypeEnum.GeographicExpansionRequest));
                if (task != null)
                {
                    task.StatusId = (int)TaskStatusEnum.Completed;
                    _context.SaveChanges();
                }
                _context.SaveChanges();
                return true;
            }
            else
                return false;
        }
        protected virtual void CreateTaskForGeographicExpansionRequest(PreApprovalRequestViewModel model)
        {
            foreach (var userOfficeRoleId in model.GeographicExpansionUserOfficeRoleIds)
            {
                var geographicExpansionRequestTask = new TaskViewModel
                {
                    TaskSubTypeId = (int)TaskSubTypeEnum.GeographicExpansionRequest,
                    TaskStatusId = (int)TaskStatusEnum.Pending,
                    UserOfficeRoleId = userOfficeRoleId,
                    ActionId = model.Id,
                    ApplicationId = model.ApplicationId,
                };

                _taskService.CreateTask(geographicExpansionRequestTask, false);
            }
        }
        protected void SendPreapprovalDecisionNotification(string trackingNumber, string designeeName, int designeeUserOfficeRoleUserId, string approvalDecisionCode)
        {
            var notificationViewModel = new MessageNotificationViewModel
            {
                MessageDictionary = new List<KeyValuePair<string, string>>()
                                    {
                                        new KeyValuePair<string, string>("@trackingNumber", trackingNumber),
                                        new KeyValuePair<string, string>("@designeeName", designeeName)
                                    },
                UserRecipients = new List<(int UserId, bool IsCced)> { (designeeUserOfficeRoleUserId, false) },
                Code = approvalDecisionCode,
            };
            _messageService.SendNotification(notificationViewModel);
        }

        public abstract PreApprovalRequestViewModel SavePerformanceResults(PreApprovalRequestViewModel adminModel);
        public abstract PreApprovalRequestViewModel SavePostActivityEvaluation(PreApprovalRequestViewModel adminModel);
        public virtual AfsGroupsPostActivityViewModel SaveAfsPostActivity(AfsGroupsPostActivityViewModel model)
        {
            return new AfsGroupsPostActivityViewModel();
        }

        public abstract PreApprovalRequestViewModel Copy(int id);

        public abstract bool CheckPendingPostActivitiesExists(int applicationId);

        public virtual IList<AfsGroupsPostActivityViewModel> GetPostActivityVersions(int postActivityId)
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
            var currentPostActivty = Get(preApprovalRequest.CurrentVersionPostActivityId);
            var model = new List<AfsGroupsPostActivityViewModel>()
            {
                currentPostActivty,
                Get(preApprovalRequest.PreviousVersionPostActivityId)
            };

            return model;
        }

        public bool SaveMsPostActivityReview(PreApprovalRequestViewModel model)
        {
            if (model != null && model.AfsPostActivity != null)
            {
                //Complete the Task Status.
                var task = _context.Tasks.FirstOrDefault(it => it.ActionId == model.AfsPostActivity.Id && it.StatusId == (int)TaskStatusEnum.Pending && it.SubTypeId == (int)TaskSubTypeEnum.ReviewPostActivityChanges);
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

        public abstract bool SaveMsPostActivityReview(AfsGroupsPostActivityViewModel model);
        protected PostActivity GetPostActivityById(int postActivityId)
        {
            var postActivity = _context.PostActivities
                .Include(pa => pa.PreApprovalRequest).ThenInclude(pa => pa.PreApprovalRequestStatus)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(pa => pa.PostActivities)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(c => c.Cloa).ThenInclude(c => c.Designator)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(c => c.Cloa).ThenInclude(c => c.Application).ThenInclude(c => c.User).ThenInclude(c => c.UserSecurityInfo)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(c => c.Cloa).ThenInclude(c => c.Application).ThenInclude(c => c.DesigneeType)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(c => c.Cloa).ThenInclude(c => c.Application).ThenInclude(c => c.Office)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(c => c.Cloa).ThenInclude(c => c.CloaStatus)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(c => c.Cloa).ThenInclude(c => c.ProfileVersion)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(c => c.Cloa).ThenInclude(c => c.MsUserOfficeRole).ThenInclude(c => c.User).ThenInclude(c => c.Profile)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(c => c.ApproverOfficeRole).ThenInclude(c => c.User).ThenInclude(c => c.Profile)
                .Include(pa => pa.Address).ThenInclude(s => s.StateProvince)
                .Include(pa => pa.Address).ThenInclude(s => s.Country)
                .Include(pa => pa.ApplicantCountry)
                .Include(pa => pa.PostActivityApplicants)
                .Include(pa => pa.PostActivityProducts)
                .Include(pa => pa.PostActivityCertificates)
                .Include(pa => pa.PostActivityFunctionCodes)
                .Include(pa => pa.PostActivityCertificateRatings)
                .Include(pa => pa.AircraftMakeMode)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(pa => pa.AfsPreApprovalRequest)
                .Include(pa => pa.Airport)
                .FirstOrDefault(pa => pa.Id == postActivityId);

            if (postActivity == null) return null;

            var preApprovalRequestCloa = _sharedService.GetDesigneeInfoByCloa(postActivity.PreApprovalRequest.CloaId);
            _afsGroupsPostActivityViewModel.DesigneeInfo = new DesigneeViewModel
            {
                Name = postActivity.PreApprovalRequest.Cloa.ProfileVersion.ToFullName(),
                Number = postActivity.PreApprovalRequest.Cloa.Application.User.UserSecurityInfo.DesigneeNumber,
                Type = postActivity.PreApprovalRequest.Cloa.Application.DesigneeType.Code,
                Code = postActivity.PreApprovalRequest.Cloa.Designator?.Code,
                TypeId = postActivity.PreApprovalRequest.Cloa.Application.DesigneeTypeId.GetValueOrDefault(),
                ExpirationDate = postActivity.PreApprovalRequest.Cloa.ExpirationDate.ToShortDateString(),
                CloaId = postActivity.PreApprovalRequest.Cloa.Id,
                ManagingSpecialistId = postActivity.PreApprovalRequest.Cloa.ManagingSpecialistId,
                ApplicationId = postActivity.PreApprovalRequest.Cloa.ApplicationId,
                Id = postActivity.PreApprovalRequest.Cloa.Application.User.Id,
                Status = preApprovalRequestCloa.Status,
                StatusId= preApprovalRequestCloa.StatusId
            };
            _afsGroupsPostActivityViewModel.RequestInfo = new PreApprovalRequestInfoViewModel()
            {
                ControlNumber = postActivity.PreApprovalRequest.TrackingNumber,
                ActivityStatus = postActivity.PreApprovalRequest.PreApprovalRequestStatus != null
                                 && postActivity.PreApprovalRequest.PreApprovalRequestStatus.Id ==
                                 (int)PreApprovalRequestStatusEnum.Pending
                    ? new BaseLookup()
                    { Id = postActivity.PreApprovalRequest.PreApprovalRequestStatus.Id, Name = "Submitted" }
                    : postActivity.PreApprovalRequest.PreApprovalRequestStatus,
                IsApproved = postActivity.PreApprovalRequest.IsApproved.GetValueOrDefault(),
                SubmittedDate = postActivity.PreApprovalRequest.SubmittedDate.HasValue ? postActivity.PreApprovalRequest.SubmittedDate.Value.ToString("MM/dd/yyyy  HH:mm tt") : String.Empty,
                DecisionDate = postActivity.PreApprovalRequest.PostActivities.Any()
                               && postActivity.PreApprovalRequest.PostActivities.OrderByDescending(po => po.Id).FirstOrDefault() != null
                               && postActivity.PreApprovalRequest.PostActivities.OrderByDescending(po => po.Id).First().ApprovalDate.HasValue
                    ? postActivity.PreApprovalRequest.PostActivities.OrderByDescending(po => po.Id).First().ApprovalDate.Value.ToString("MM/dd/yyyy HH:mm tt")
                    : string.Empty,
                DecisionBy = postActivity.PreApprovalRequest.ApproverOfficeRole != null && postActivity.PreApprovalRequest.ApproverComments != "Auto Pre Approval" ? postActivity.PreApprovalRequest.ApproverOfficeRole.User.Profile.ToFullName() : postActivity.PreApprovalRequest.ApproverComments == "Auto Pre Approval" ? "Auto Pre Approval" : string.Empty,
                RevisedDate = postActivity.PreApprovalRequest.AfsPreApprovalRequest != null && postActivity.PreApprovalRequest.AfsPreApprovalRequest.RevisedDate != null ? postActivity.PreApprovalRequest.AfsPreApprovalRequest.RevisedDate.Value.ToString("MM/dd/yyyy HH:mm tt") : String.Empty

            };
            if(postActivity.PreApprovalRequest.GeoExpansionSelectedApproverId!= null){            
                _afsGroupsPostActivityViewModel.RequestInfo.DecisionBy = _context.UserOfficeRoles
                                                                                 .Where(u => u.Id == postActivity.PreApprovalRequest.GeoExpansionSelectedApproverId)
                                                                                 .Include(u => u.User).ThenInclude(u => u.Profile)
                                                                                 .Select(p => new {
                                                                                     ApprovedBy = p.User.Profile.ToFullName()
                                                                                  }).First().ApprovedBy;
            }
            _afsGroupsPostActivityViewModel.IsAdmin = postActivity.PreApprovalRequest.AfsPreApprovalRequest != null && (postActivity.PreApprovalRequest.AfsPreApprovalRequest.CategoryId == (int)CategoryEnum.AdminApd || postActivity.PreApprovalRequest.AfsPreApprovalRequest.CategoryId == (int)CategoryEnum.AdminTce);
            return postActivity;
        }
        public abstract IList<AfsGroupsPostActivityViewModel> GetGroupThreePostActivityVersions(int postActivityId);
        protected abstract PreApprovalRequest GetOriginalPreApprovalData(int id);

        public abstract bool CheckIsAutoPreApproval(PreApprovalRequestViewModel model);
       
        public virtual (int postActivityId, int? statusId) GetLatestPostActivityVersion(int preApprovalRequestId)
        {
            var activity = _context.PostActivities.Where(p => p.PreApprovalRequestId == preApprovalRequestId).OrderByDescending(p => p.Id).Select(p => new { p.Id, p.StatusId }).First();
            return (activity.Id, activity.StatusId);
        }
        public virtual bool CheckIfLessThan24Hours(PreApprovalRequestViewModel model)
        {
          if (model.RequestInfo?.ActivityStatus?.Id ==  (int)PreApprovalRequestStatusEnum.Pending) 
           {
             var startDate =  Convert.ToDateTime(model.TestInformation.ProposeStartDate).Add(TimeSpan.Parse(_preApprovalRequestViewModel.TestInformation.ProposedStartTime));
             var timeZoneName = _lookupService.LookupValues().Result.TimeZones.First( x=> x.Id == (int)model.TestInformation.TimeZoneId).StandardName;          
             startDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(startDate, timeZoneName, "Central Standard Time");
             var submittedDate = Convert.ToDateTime(model.RequestInfo.SubmittedDate);  
             return startDate < submittedDate.AddHours(24);
           }
          return false;
        }
        public virtual void CheckIfActivityOutside(PreApprovalRequestViewModel model)
        {
          if (model.RequestInfo?.ActivityStatus?.Id ==  (int)PreApprovalRequestStatusEnum.Pending) 
           {
             var startDate =  Convert.ToDateTime(model.TestInformation.ProposeStartDate).Add(TimeSpan.Parse(_preApprovalRequestViewModel.TestInformation.ProposedStartTime));
             var timeZoneName = _lookupService.LookupValues().Result.TimeZones.First( x=> x.Id == (int)model.TestInformation.TimeZoneId).StandardName;          
             startDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(startDate, timeZoneName, "Central Standard Time");
             var submittedDate = Convert.ToDateTime(model.RequestInfo.SubmittedDate); 
             if (model.ActivityLocation?.IsOutsideOfficeDistrict == true ) {
                model.IsOutSideActivityMessage = (bool)model.ActivityLocation?.IsActivityOutsideUsa ?
                    startDate <= submittedDate.AddDays(7) :  startDate <= submittedDate.AddDays(10);
            } else {
                model.IsOutSideActivityMessage = ( startDate <= submittedDate.AddDays(1 ) );
            }  
            return;          
           }
         model.IsOutSideActivityMessage = false;
        }
    }
}
