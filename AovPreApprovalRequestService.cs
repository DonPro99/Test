using Dms.Core.EntityFramework;
using Dms.Core.EntityFramework.Data;
using Dms.Core.EntityFramework.Model.Activity;
using Dms.Core.EntityFramework.Model.Apply;
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
using Dms.Services.ViewModel.Message;
using Dms.Services.ViewModel.Security;
using Dms.Services.ViewModel.Shared;
using Dms.Services.ViewModel.Task;
using Dms.Services.ViewModel.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Linq.Translations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PrimeNG.TableFilter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using CloaEntity = Dms.Core.EntityFramework.Model.Cloa;

namespace Dms.Services.Implementation.Activity
{
    public class AovPreApprovalRequestService(DmsContext context, ITaskService taskService, IDocumentService documentService, IActivityService activityService, IMessageService messageService, ISharedService sharedService, ILookupService lookupService, IUserService userService)
     : PreApprovalRequestBaseService(context, taskService, documentService, activityService, messageService, sharedService, lookupService, userService)
    {
        protected PreApprovalActivityLocationViewModel _activityLocation;
        protected PostActivity _postActivity;

        public override PreApprovalRequestViewModel GetNew(int applicationId)
        {
             _cloa = CloaPreApprovalRequestViewModelMapper.GetNewCloaEntitytoViewModel(_context, applicationId);
             base.GetNew(applicationId);
             GetNewHelp(applicationId);
            _preApprovalRequestViewModel.DesigneeFunctionCodes = _cloa.DesigneeFunctionCodes.DistinctBy(d => d.Id).ToArray();
            _preApprovalRequestViewModel.IsAovType = true;
            _preApprovalRequestViewModel.PreApprovalDateWiseCount = GetPreApprovalDateWiseCount(_cloa.DesigneeInformation.ApplicationId);
            _preApprovalRequestViewModel.AuthFunctionCodes = _preApprovalRequestViewModel.AuthFunctionCodes.DistinctBy(d => d.Id).ToList(); 
            _preApprovalRequestViewModel.SelectedFunctionCodes = _preApprovalRequestViewModel.SelectedFunctionCodes != null && _preApprovalRequestViewModel.SelectedFunctionCodes.Any() ? _preApprovalRequestViewModel.SelectedFunctionCodes : _preApprovalRequestViewModel.AuthFunctionCodes.DistinctBy(d => d.Id).Select(f => f.Id).ToList();          
            _preApprovalRequestViewModel.ActivityLocation.AovCompanyTypeId = _cloa.AovCompanyTypeId;
            _preApprovalRequestViewModel.DocumentReference.UploadedFiles = null;
            return _preApprovalRequestViewModel;

        }

        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity inherent to task navigation system")]
        public override PreApprovalRequestViewModel Get(int preApprovalRequestId, bool loadPreapprovalModifiedData, int postActivityId, int? cloaId = null)
        {
            _preApprovalRequest = CloaPreApprovalRequestViewModelMapper.GetPreApprovalRequestAov(_context, preApprovalRequestId);            
            _cloa = CloaPreApprovalRequestViewModelMapper.GetAovCloaEntitytoViewModel(_context, _preApprovalRequest, cloaId);
            var preApprovalRequestCloa = _sharedService.GetDesigneeInfoByCloa(_preApprovalRequest.CloaId);
            _cloa.DesigneeInfo.Status = preApprovalRequestCloa.Status;
            _cloa.DesigneeInfo.StatusId = preApprovalRequestCloa.StatusId;
            _cloa.RequestInfo.SubmittedDate = _preApprovalRequest.SubmittedDate.HasValue ? _preApprovalRequest.SubmittedDate.Value.ToString("MM/dd/yyyy HH:mm") : String.Empty;
            _cloa.RequestInfo.RevisedDate = _preApprovalRequest.AovPreApprovalRequest != null && _preApprovalRequest.AovPreApprovalRequest.RevisedDate != null ? _preApprovalRequest.AovPreApprovalRequest.RevisedDate.Value.ToString("MM/dd/yyyy HH:mm tt") : String.Empty;
            var approvalDate = _context.PostActivities.Where(e => e.PreApprovalRequestId == preApprovalRequestId).OrderByDescending(po => po.Id)
                .Select(e => e.ApprovalDate).FirstOrDefault();
            _cloa.RequestInfo.DecisionDate = approvalDate != null && approvalDate.HasValue ? approvalDate.Value.ToString("MM/dd/yyyy HH:mm tt") : string.Empty ;
            _cloa.RequestInfo.DecisionBy = _preApprovalRequest.ApproverOfficeRole != null && _preApprovalRequest.ApproverComments != "Auto Pre-Approval" ? _preApprovalRequest.ApproverOfficeRole.User.Profile.ToFullName() : _preApprovalRequest.ApproverComments == "Auto Pre-Approval" ? "Auto Pre-Approval" : string.Empty;
            _preApprovalRequestViewModel = base.Get(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId, cloaId);
            _preApprovalRequestViewModel.DesigneeTypeId = _cloa.DesigneeInfo.TypeId;
            _preApprovalRequestViewModel.DesigneeInfo = _cloa.DesigneeInfo;
            _preApprovalRequestViewModel.IsAovType = true;
            _preApprovalRequestViewModel.Comments = _preApprovalRequest.Comments;           
            _preApprovalRequestViewModel.RequestInfo = _cloa.RequestInfo;
            if (_preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Rejected)
            {
                _preApprovalRequestViewModel.RequestInfo.DecisionDate = _preApprovalRequest.ModifiedDate?.ToString("MM/dd/yyyy HH:mm tt");
            }
            _preApprovalRequestViewModel.AuthFunctionCodes = _cloa.DesigneeFunctionCodes.DistinctBy(d => d.Id).ToList(); 
            _preApprovalRequestViewModel.InitialFunctionCodes = _preApprovalRequestViewModel.AuthFunctionCodes;
            _preApprovalRequestViewModel.SelectedFunctionCodes = _preApprovalRequest.PreApprovalRequestFunctionCodes.Select(p => p.FunctionCodeId).ToList();
            _preApprovalRequestViewModel.StatusId = _preApprovalRequest.StatusId;
            _preApprovalRequestViewModel.IsApproved = _preApprovalRequest.IsApproved;
            _preApprovalRequestViewModel.ApproverComment = _preApprovalRequest.ApproverComments;
            _preApprovalRequestViewModel.TrackingNumber = _preApprovalRequest.TrackingNumber;
            _preApprovalRequestViewModel.IsPossibleDirectObservation = _preApprovalRequest.IsPossibleDirectObservation;
            if (_preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Canceled)
            {
                _preApprovalRequestViewModel.JustificationForCancellation = !(string.IsNullOrEmpty(_preApprovalRequest.AovPreApprovalRequest.JustificationForCancellation)) ? _preApprovalRequest.AovPreApprovalRequest.JustificationForCancellation : string.Empty;
                _preApprovalRequestViewModel.CancellationTypeId = _preApprovalRequest.AovPreApprovalRequest.CancellationTypeId.HasValue ? _preApprovalRequest.AovPreApprovalRequest.CancellationTypeId : null;                
            }
            var decisionTask = _context.Tasks.Where(t => t.ActionId == preApprovalRequestId && t.SubTypeId == (int)TaskSubTypeEnum.PreApprovalRequest).FirstOrDefault();
            if (decisionTask != null)
            {
                _preApprovalRequestViewModel.DecisionTaskUserOfficeRoleIds = new List<int>();

                var msProxyList = _context.UserOfficeRoles.Where(u => u.IsActive && 
                                                                 u.FormData != null && u.IsProxyRequestApproved.Value &&
                                                                (JsonExtensions.JsonIntValue(u.FormData, "$.UserOfficeRoleId") == decisionTask.UserOfficeRoleId)).ToList();
                
                if (msProxyList.Count > 0)
                {
                    foreach(var msProxy in msProxyList) {

                        var formdData = JsonConvert.DeserializeObject<UserDelegationFormDataViewModel>(msProxy.FormData);

                        _preApprovalRequestViewModel.DecisionTaskUserOfficeRoleIds.Add(formdData.ProxyOriginalUserOfficeRoleId);
                        _preApprovalRequestViewModel.DecisionTaskUserOfficeRoleIds.Add(msProxy.Id);
                    }
                }
                
                _preApprovalRequestViewModel.DecisionTaskUserOfficeRoleIds.Add(decisionTask.UserOfficeRoleId);
            }
            
           GetAovHelper(_preApprovalRequestViewModel,postActivityId,loadPreapprovalModifiedData);  
           _preApprovalRequestViewModel.IsLessThan24Hours =  base.CheckIfLessThan24Hours( _preApprovalRequestViewModel) ;                                        
             
            return _preApprovalRequestViewModel;
        }
         
        protected PreApprovalRequestViewModel GetAovHelper(PreApprovalRequestViewModel preApprovalRequestViewModel, int postActivityId, bool loadPreapprovalModifiedData)
        {
            var preApprovalActivityLocationViewModel = GetPreApprovalActivityLocationViewModel(_preApprovalRequest);
            bool hasFacilityonRecord = _cloa.FacilityAddresses.Count > 1 ? _cloa.FacilityAddresses.Any(a => a.Id == _preApprovalRequest.AovPreApprovalRequest.ActivityLocationAddressId)
                                                              : _cloa.FacilityAddress != null ? _preApprovalRequest.AovPreApprovalRequest.ActivityLocationAddressId == _cloa.FacilityAddress.Id : false;

            preApprovalActivityLocationViewModel.FacilityonRecord = hasFacilityonRecord;
            preApprovalActivityLocationViewModel.FacilityAddress = _cloa.FacilityAddress;

            preApprovalActivityLocationViewModel.FacilityAddresses = _cloa.FacilityAddresses;
            preApprovalRequestViewModel.ActivityLocation = preApprovalActivityLocationViewModel;
            //Set Other address in viewmodel
            if (preApprovalActivityLocationViewModel.FacilityonRecord.HasValue && !preApprovalActivityLocationViewModel.FacilityonRecord.Value)
            {
               preApprovalRequestViewModel.ActivityLocation.OtherAddress = GetAddressViewModel(_preApprovalRequest);

            }
            _preApprovalRequestViewModel.TestInformation = GetPreApprovalTestInformationViewModel(_preApprovalRequest);
            if (!preApprovalRequestViewModel.TestInformation.TimeZoneId.HasValue)
            {
                _preApprovalRequestViewModel.TestInformation.TimeZoneId = _cloa.TimeZoneId;                
            }
          
            preApprovalRequestViewModel.ApplicantInformation = new PreApprovalApplicantInformationViewModel()
            {
                Name = _preApprovalRequest.AovPreApprovalRequest.ApplicantName,
                MedicalExpirationDate = _preApprovalRequest.AovPreApprovalRequest.MedicalExpirationDate.DateToString(),
                CertificateNumber = _preApprovalRequest.AovPreApprovalRequest.ApplicantCertificateNumber,
                IsEnhancedCTI = _preApprovalRequest.AovPreApprovalRequest.IsEnhancedCTI,
                IsCtopGraduate = _preApprovalRequest.AovPreApprovalRequest?.IsCtopGraduate
            };   

            preApprovalRequestViewModel.DocumentReference = _documentService.GetDocumentsByRef((int)DocumentReferenceEnum.PreapprovalRequest,preApprovalRequestViewModel.Id, null);
            preApprovalRequestViewModel.MsDecisiondocumentReference = _documentService.GetDocumentsByRef((int)DocumentReferenceEnum.AOVPreApprovalMSDecision, preApprovalRequestViewModel.Id, null);
            //Post ACtivity Data retrieval
            GetAovPostActivityInformation(preApprovalRequestViewModel.Id, postActivityId);      

            return preApprovalRequestViewModel;
        }
        public static PreApprovalTestInformationViewModel GetPreApprovalTestInformationViewModel(PreApprovalRequest preApprvoalRequest)
        {
            var preApprovalTestTestInformationViewModel = new PreApprovalTestInformationViewModel
            {
                PracticalOralTestId = preApprvoalRequest.AovPreApprovalRequest.PracticalOralTestId,
                ProposeStartDate = preApprvoalRequest.ProposeStartDate.DateToString(),
                ProposeEndDate = preApprvoalRequest.ProposeEndDate.DateToString(),
                ProposedStartTime = preApprvoalRequest.ProposedStartTime.HasValue ? preApprvoalRequest.ProposedStartTime.Value.ToString("HH:mm") : string.Empty,
                TimeZoneId = preApprvoalRequest.TimeZoneId,
                Acknowledgement = preApprvoalRequest.AovPreApprovalRequest.IsAcknowledge,
                AcknowledgeMedicallyQualified = preApprvoalRequest.AovPreApprovalRequest.IsAcknowledgeMedicallyQualified
            };
            return preApprovalTestTestInformationViewModel;
         
        }

         public static PreApprovalActivityLocationViewModel GetPreApprovalActivityLocationViewModel(PreApprovalRequest preApprvoalRequest)
        {
            var preApprovalActivityLocationViewModel = new PreApprovalActivityLocationViewModel()
            {
                AovCompanyTypeId = preApprvoalRequest.AovPreApprovalRequest.CompanyId,
                AovCompanyType = preApprvoalRequest.AovPreApprovalRequest.CompanyId.HasValue
                    ? new BaseLookup 
                        { 
                            Id = (int)preApprvoalRequest.AovPreApprovalRequest.CompanyId,
                            Name = preApprvoalRequest.AovPreApprovalRequest.Company.Name
                        }
                    : null,
                LocationAddress = preApprvoalRequest.AovPreApprovalRequest.Address != null ? new AddressViewModel
                {
                    Id = preApprvoalRequest.AovPreApprovalRequest.Address.Id,
                    Name = preApprvoalRequest.AovPreApprovalRequest.Address.Name,
                    Address1 = preApprvoalRequest.AovPreApprovalRequest.Address.AddressLine1,
                    Address2 = preApprvoalRequest.AovPreApprovalRequest.Address.AddressLine2,
                    City = preApprvoalRequest.AovPreApprovalRequest.Address.City,
                    County = preApprvoalRequest.AovPreApprovalRequest.Address.County,
                    State = preApprvoalRequest.AovPreApprovalRequest.Address.StateProvince != null ? new StateViewModel
                    {
                        Id = preApprvoalRequest.AovPreApprovalRequest.Address.StateProvince.Id,
                        Name = preApprvoalRequest.AovPreApprovalRequest.Address.StateProvince.Name
                    } : null,
                    Country = preApprvoalRequest.AovPreApprovalRequest.Address.Country != null ? new CountryViewModel
                    {
                        Id = preApprvoalRequest.AovPreApprovalRequest.Address.Country.Id,
                        Name = preApprvoalRequest.AovPreApprovalRequest.Address.Country.Name
                    } : null,
                    ZipCode = preApprvoalRequest.AovPreApprovalRequest.Address.ZipCode,
                    PhoneNumber = preApprvoalRequest.AovPreApprovalRequest.Address.Phone,
                    AirTrafficControlTower = preApprvoalRequest.AovPreApprovalRequest.Address.AirTrafficControlTower,
                    Airport = preApprvoalRequest.AovPreApprovalRequest.Address.Airport
                } : new AddressViewModel(),
                OtherAddress = new AddressViewModel(),
            };
            return preApprovalActivityLocationViewModel;

        }      
       
        public static AddressViewModel GetAddressViewModel(PreApprovalRequest preApprvoalRequest)
        {
            var addressViewModel = preApprvoalRequest.AovPreApprovalRequest.Address != null ? new AddressViewModel
            {
                Id = preApprvoalRequest.AovPreApprovalRequest.Address.Id,
                Name = preApprvoalRequest.AovPreApprovalRequest.Address.Name,
                Address1 = preApprvoalRequest.AovPreApprovalRequest.Address.AddressLine1,
                Address2 = preApprvoalRequest.AovPreApprovalRequest.Address.AddressLine2,
                City = preApprvoalRequest.AovPreApprovalRequest.Address.City,
                County = preApprvoalRequest.AovPreApprovalRequest.Address.County,
                State = preApprvoalRequest.AovPreApprovalRequest.Address.StateProvince != null ? new StateViewModel
                {
                    Id = preApprvoalRequest.AovPreApprovalRequest.Address.StateProvince.Id,
                    Name = preApprvoalRequest.AovPreApprovalRequest.Address.StateProvince.Name
                } : null,
                Country = preApprvoalRequest.AovPreApprovalRequest.Address.Country != null ? new CountryViewModel
                {
                    Id = preApprvoalRequest.AovPreApprovalRequest.Address.Country.Id,
                    Name = preApprvoalRequest.AovPreApprovalRequest.Address.Country.Name
                } : null,
                ZipCode = preApprvoalRequest.AovPreApprovalRequest.Address.ZipCode,
                PhoneNumber = preApprvoalRequest.AovPreApprovalRequest.Address.Phone,
                AirTrafficControlTower = preApprvoalRequest.AovPreApprovalRequest.Address.AirTrafficControlTower,
                Airport = preApprvoalRequest.AovPreApprovalRequest.Address.Airport
            } : new AddressViewModel();
            
            return addressViewModel;
        }
         protected PreApprovalRequestViewModel GetAovPostActivityInformation(int preApprovalRequestId, int postActivityId)
        {
            var preApprvoalRequest = _preApprovalRequest;
            PostActivity postActivity = null;
            if (postActivityId > 0)
            {
                postActivity = _context.PostActivities.Where(po => po.Id == postActivityId)
                    .Include(p => p.PostActivityPerformanceReview)
                    .Include(p => p.PostActivityProducts)
                    .Include(p => p.PostActivityCertificates)
                    .Include(p => p.PostActivityResultType)
                    .Include(p => p.AircraftMakeMode)
                    .Include(p => p.Address)
                    .Include(p => p.PostActivityFunctionCodes)
                    .Include(p => p.Address).ThenInclude(p => p.StateProvince)
                    .Include(p => p.Address).ThenInclude(p => p.Country)
                    .Include(p => p.PostActivityModifiedDatas)
                    .Include(p => p.PostActivityApplicants)
                    .Include(p => p.PostActivityCertificateRatings)
                    .Include(p => p.PilotLicenseIssuedCountry)
                    .Include(p => p.AirCarrier)
                    .Include(p => p.School)
                    .FirstOrDefault();
            }
            var preApprovalRequestViewModel = _preApprovalRequestViewModel;
            if (postActivity != null)
            {
                preApprovalRequestViewModel.PostActivity = new PostActivityViewModel();
                var aovPostActivity = new PostActivityViewModel();
                aovPostActivity.Id = postActivity.Id; 
                aovPostActivity.FormData = postActivity.FormData;             
                // Always collect first record Completed Date to allow 30 days edit after completion of Post Activity.
                var po = _context.PostActivities
                    .Where(po => po.PreApprovalRequestId == preApprovalRequestId)
                    .OrderBy(po => po.Id)
                    .Select(po => new
                    {
                        po.ModifiedDate,
                        po.CreatedDate
                    })
                    .First();

                _preApprovalRequestViewModel.IsPostActivityCurrentVersion = postActivityId > 0 && preApprvoalRequest.PostActivities.OrderByDescending(p => p.Id).First().Id == postActivityId;
                preApprovalRequestViewModel.PostActivity = aovPostActivity;
            }
            _preApprovalRequestViewModel = preApprovalRequestViewModel;
            return _preApprovalRequestViewModel;
        }

        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity inherent to task navigation system")]
        public override PaginationViewModel<IList<ActivityPaperWorkViewModel>> GetPreApprovalList(RequestListModel model)
        {
            Expression<Func<PreApprovalRequest, bool>> applicationFilter = p => p.ApplicationId == model.ApplicationId,
            dateFilter = p => !model.IsOneYear || p.CreatedDate >= DateTime.Today.AddYears(-1);

            var query = _context.PreApprovalRequests
                        .Where(applicationFilter)
                        .Where(dateFilter);

            if (model.PageModel.Filters != null)
            {
                foreach (var filter in model.PageModel.Filters)
                {
                    var filterValue = string.Empty;
                    DateTime? filterDate = null;
                    switch (filter.Key.ToLower())
                    {
                        case "proposedstartdate":
                            filterDate = JObject.Parse(filter.Value.ToString())["value"].Value<DateTime>();
                            query = query.Where(p => p.ProposeStartDate.Value.Date == filterDate.Value.Date);
                            model.PageModel.Filters.Remove(filter.Key);
                            break;
                        case "modifieddate":
                            filterDate = JObject.Parse(filter.Value.ToString())["value"].Value<DateTime>();
                            query = query.Where(p => (p.SubmittedDate.HasValue ?  p.SubmittedDate.Value.Date : p.CreatedDate.Date) == filterDate.Value.Date);
                            model.PageModel.Filters.Remove(filter.Key);
                            break;
                        case "preapprovaltype":
                            filterValue = JObject.Parse(filter.Value.ToString())["value"].Value<string>();
                            query = query.Where(p => p.PreApprovalRequestFunctionCodes.Any(p1 => p1.FunctionCode.Name.Contains(filterValue)));
                            model.PageModel.Filters.Remove(filter.Key);
                            break;
                    }
                }
            }

            
            if (!string.IsNullOrWhiteSpace(model.PageModel.SortField))
            {
                switch (model.PageModel.SortField.ToLower())
                {
                    case "preapprovaltype":
                        query = (model.PageModel.SortOrder == (int)SortingEnumeration.OrderByAsc)
                                ? query.OrderBy(p => p.PreApprovalRequestFunctionCodes.OrderBy(p1 => p1.FunctionCode.Name).First().FunctionCode.Name)
                                : query.OrderByDescending(p => p.PreApprovalRequestFunctionCodes.OrderBy(p1 => p1.FunctionCode.Name).First().FunctionCode.Name);
                        model.PageModel.SortField = string.Empty;
                    break;
                }
            }

            var data = query
                        .Select(p => new ActivityPaperWorkViewModel
                        {
                            Id = p.Id,
                            TrackingNumber = p.TrackingNumber,
                            ActivityStatus = p.PreApprovalRequestStatus != null ? p.PreApprovalRequestStatus.Name : string.Empty,
                            ModifiedDate = p.SubmittedDate ?? p.CreatedDate,
                            StatusId = p.StatusId,
                            ApplicantName = p.AovPreApprovalRequest.ApplicantName,
                            TypeOfRequest = p.Application.DesigneeType.Name,
                            ProposedStartDate = p.ProposeStartDate,
                            PreApprovalType = string.Join(", ", p.PreApprovalRequestFunctionCodes.Select(p1 => p1.FunctionCode.Name).Distinct()),
                            DesigneeTypeId = model.IsInternal ? null : p.Application.DesigneeTypeId,
                            HasPostActivity = model.IsInternal || p.PostActivities.Count != 0,
                            PostActivityStatusId = model.IsInternal ? null : p.PostActivities.OrderByDescending(p1 => p1.Id).Select(p1 => p1.StatusId).FirstOrDefault(),
                            CreatedBy = p.CreatedBy,
                            ModifiedBy = p.ModifiedBy,
                            IsPossibleObservation = p.IsPossibleDirectObservation ?? false,
                        }).PrimengTableFilter(model.PageModel, out int totalRecords)
                        .ToArray();

            data = data.Select(p => 
                {
                    var route =  SetPreApprovalUrl(p.Id, p.StatusId.Value, p.CreatedBy, p.ModifiedBy, model.IsInternal);
                    p.Url = route.Url;
                    p.IsNewWindow = route.IsNewWindow;
                    p.HasNavigation = !string.IsNullOrWhiteSpace(route.Url);
                    if (!model.IsInternal)
                    {
                        p.Cancel = (p.StatusId == (int)PreApprovalRequestStatusEnum.Approved && p.HasPostActivity && p.PostActivityStatusId == (int)PreApprovalRequestStatusEnum.Completed)
                                    || p.StatusId == (int)PreApprovalRequestStatusEnum.Canceled
                                    || p.StatusId == (int)PreApprovalRequestStatusEnum.Completed
                                    || p.StatusId == (int)PreApprovalRequestStatusEnum.Rejected
                                    || (p.IsApproved.HasValue && !p.IsApproved.Value)
                                        ? null
                                        : p.Id.ToString();
                    }
                    return p;
                }).ToArray();

            return new PaginationViewModel<IList<ActivityPaperWorkViewModel>>(totalRecords, data);
        }

        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity inherent to task navigation system")]
        public override PaginationViewModel<IList<ActivityPaperWorkViewModel>> GetPostActivityList(RequestListModel model)
        {
            Expression<Func<PreApprovalRequest, bool>> applicationFilter = p => p.ApplicationId == model.ApplicationId,
            dateFilter = p => !model.IsOneYear || p.CreatedDate >= DateTime.Today.AddYears(-1);
            var query = _context.PreApprovalRequests
                        .Where(applicationFilter)
                        .Where(dateFilter)
                        .Where(p => p.LatestPostActivity != null);
            if (model.PageModel.Filters != null)
            {
                foreach (var filter in model.PageModel.Filters)
                {
                    var filterValue = string.Empty;
                    DateTime? filterDate = null;
                    switch (filter.Key.ToLower())
                    {
                        case "proposedstartdate":
                            filterDate = JObject.Parse(filter.Value.ToString())["value"].Value<DateTime>();
                            query = query.Where(p => p.ProposeStartDate.Value.Date == filterDate.Value.Date);
                            model.PageModel.Filters.Remove(filter.Key);
                            break;
                        case "duedate":
                            filterDate = JObject.Parse(filter.Value.ToString())["value"].Value<DateTime>();
                            query = query.Where(p => (p.ProposeEndDate.HasValue ? p.ProposeEndDate.Value.AddDays(7).Date : null) == filterDate.Value.Date);
                            model.PageModel.Filters.Remove(filter.Key);
                            break;
                        case "submissiondate":
                            filterDate = JObject.Parse(filter.Value.ToString())["value"].Value<DateTime>();
                            query = query.Where(p => (p.LatestPostActivity.CompletedDate.HasValue ?  p.LatestPostActivity.CompletedDate.Value.Date : null) == filterDate.Value.Date);
                            model.PageModel.Filters.Remove(filter.Key);
                            break;
                    case "preapprovaltype":
                            filterValue = JObject.Parse(filter.Value.ToString())["value"].Value<string>();
                            query = query.Where(p => p.PreApprovalRequestFunctionCodes.Any(p1 => p1.FunctionCode.Name.Contains(filterValue)));
                            model.PageModel.Filters.Remove(filter.Key);
                            break;
                    }
                }
            }

            
            if (!string.IsNullOrWhiteSpace(model.PageModel.SortField))
            {
                switch (model.PageModel.SortField.ToLower())
                {
                    case "preapprovaltype":
                        query = (model.PageModel.SortOrder == (int)SortingEnumeration.OrderByAsc)
                                ? query.OrderBy(p => p.PreApprovalRequestFunctionCodes.OrderBy(p1 => p1.FunctionCode.Name).First().FunctionCode.Name)
                                : query.OrderByDescending(p => p.PreApprovalRequestFunctionCodes.OrderBy(p1 => p1.FunctionCode.Name).First().FunctionCode.Name);
                        model.PageModel.SortField = string.Empty;
                    break;
                }
            }

            var data =  query.Select(p => new ActivityPaperWorkViewModel
                        {
                            Id = p.LatestPostActivity.Id,
                            TrackingNumber = p.LatestPostActivity.TrackingNumber,
                            ApplicantName = (JsonExtensions.JsonStringValue(p.LatestPostActivity.FormData, "$.ApplicantInformation.Name") ?? p.LatestPostActivity.ApplicantName) ?? p.AovPreApprovalRequest.ApplicantName,
                            ActivityStatus = p.LatestPostActivity.PostActivitytStatus.Name,
                            StatusId = p.LatestPostActivity.StatusId,
                            DueDate = p.ProposeEndDate.HasValue ? p.ProposeEndDate.Value.AddDays(7) : null,
                            SubmissionDate = p.LatestPostActivity.CompletedDate,
                            ProposedStartDate = p.ProposeStartDate,
                            PreApprovalRequestId = p.LatestPostActivity.PreApprovalRequestId,
                            TypeOfRequest = p.Application.DesigneeType.Name,
                            PreApprovalType = string.Join(", ", p.PreApprovalRequestFunctionCodes.Select(p1 => p1.FunctionCode.Name).Distinct()),
                        }).WithTranslations().PrimengTableFilter(model.PageModel, out int totalRecords)
                        .ToArray();

            var preApprovalRequestIds = data.Select(p => p.PreApprovalRequestId).ToArray();

            var versions = _context.PostActivities
                                .Where(p => preApprovalRequestIds.Contains(p.PreApprovalRequestId))
                                .Select(p => new 
                                {
                                    PreApprovalId = p.PreApprovalRequestId,
                                    PostActivityId = p.Id
                                })
                                .AsEnumerable()
                                .GroupBy(p => p.PreApprovalId)
                                .Select(p => new 
                                {
                                    PreApprovalId = p.Key,
                                    Versions = p.Select((p1, i) => new BaseLookup
                                                {
                                                    Id = p1.PostActivityId,
                                                    Name = $"Version {i + 1}"
                                                }).ToArray()
                                }).ToArray();

            data = data
                    .Select(d => 
                        {
                            d.PostActivityVersions = versions.FirstOrDefault(v => v.PreApprovalId == d.PreApprovalRequestId)?.Versions ?? [];
                            var url = SetPostActivityUrl(d.Id, d.StatusId, model.IsInternal);
                            d.Url = url.Url;
                            d.IsNewWindow = url.IsNewWindow;
                            d.HasNavigation = !string.IsNullOrWhiteSpace(url.Url);
                            return d;
                        }).ToArray();
                        
            return new PaginationViewModel<IList<ActivityPaperWorkViewModel>>(totalRecords, data);
        }

        private static LinkViewModel SetPreApprovalUrl(int id, int statusId, int createdBy, int? modifiedBy, bool isInternal)
        {
            var url = string.Empty;
            var isNewWindow = false;
            if (isInternal)
            {
                if (statusId is not ((int)PreApprovalRequestStatusEnum.Saved) and
                    not ((int)PreApprovalRequestStatusEnum.Initiated))
                {
                    if (createdBy == modifiedBy)
                    {
                        url = $"/preapprovalrequest/evaluate/{CryptoExtensions.Encrypt(id)}";
                    }
                    else
                    {
                        url = $"/preapprovalrequest/aov/summary/{id.Encrypt()}?newTab=true";
                        isNewWindow = true;
                    }
                }
            }
            else
            {
                if (statusId is ((int)PreApprovalRequestStatusEnum.Completed) or
                        ((int)PreApprovalRequestStatusEnum.Canceled) or
                        ((int)PreApprovalRequestStatusEnum.Rejected))
                {
                    url = $"/preapprovalrequest/aov/summary/{id.Encrypt()}";
                }
                else
                {
                    url = $"/preapprovalrequest/aov/{id.Encrypt()}";
                }         
            }

            return new LinkViewModel
            {
                Code = "PR",
                Url = url,
                IsNewWindow = isNewWindow
            };
        }

        private static LinkViewModel SetPostActivityUrl(int id, int? statusId, bool isInternal)
        {
            var url = string.Empty;
            var isNewWindow = false;
            if (isInternal)
            {
                if (statusId is ((int)PreApprovalRequestStatusEnum.Completed) or ((int)PreApprovalRequestStatusEnum.Canceled))
                {
                    url = $"/postactivity/aovgroups/{id.Encrypt()}/false?isReadOnly=true";
                    isNewWindow = true;
                }
            }
            else
            {
                url = $"/postactivity/aov/{id.Encrypt()}";
            }

            return new LinkViewModel
            {
                Code = "PO",
                Url = url,
                IsNewWindow = isNewWindow
            };
        }

        public override int ReInitiate(int preApprovalRequestId)
        {
            var newPreapprovalRequest = Get(preApprovalRequestId, false, 0);
            CancelHelper(preApprovalRequestId);
            newPreapprovalRequest.Id = 0;
            newPreapprovalRequest.isSubmit = false;
            newPreapprovalRequest.RequestInfo.ControlNumber = _activityService.GenerateTrackingNumber(newPreapprovalRequest.DesigneeInfo.CloaId, (int)ProcessTypeEnum.PreApproval);           
            newPreapprovalRequest.IsCancel = true;
            newPreapprovalRequest = Save(newPreapprovalRequest);
            return newPreapprovalRequest.Id;
        }

        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity inherent to task navigation system")]
        public override PreApprovalRequestViewModel Save(PreApprovalRequestViewModel model)
        {
            if (model.Id != 0)
            {
                _preApprovalRequest = CloaPreApprovalRequestViewModelMapper.GetPreApprovalRequestAov(_context, model.Id); 
                if (!_preApprovalRequest.IsApproved.HasValue && _preApprovalRequest.StatusId != (int)PreApprovalRequestStatusEnum.Pending && _preApprovalRequest.StatusId != (int)PreApprovalRequestStatusEnum.Approved)
                {
                    _preApprovalRequest.StatusId = model.isSubmit ? (int)PreApprovalRequestStatusEnum.Pending : (int)PreApprovalRequestStatusEnum.Saved;
                } else {
                    if(model.isSubmit){
                        _preApprovalRequest.AovPreApprovalRequest.RevisedDate =  DateTime.Now;
                    }
                }
                _preApprovalRequest.SubmittedDate = _preApprovalRequest.SubmittedDate.HasValue ? _preApprovalRequest.SubmittedDate : model.isSubmit ? DateTime.Now : (DateTime?)null;
                 _preApprovalRequest.Comments = model.Comments;
                _preApprovalRequestViewModel = model;
                UpdateAovData(model, _preApprovalRequest);

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
                { 
                    _preApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Saved; 
                }
                _preApprovalRequest.Comments = model.Comments;
                _preApprovalRequestViewModel = model;
                InsertAovData(model, _preApprovalRequest);                
                _context.PreApprovalRequests.Add(_preApprovalRequest);
            }

            SaveFunctionCodes(model, _preApprovalRequest);

            _preApprovalRequest.TimeZoneId = model.TestInformation.TimeZoneId;
            _preApprovalRequest.AovPreApprovalRequest.PracticalOralTestId = model.TestInformation.PracticalOralTestId;      
            _preApprovalRequest.ProposeStartDate = model.TestInformation.ProposeStartDate.ToNullableDate();
            _preApprovalRequest.ProposeEndDate = model.TestInformation.ProposeEndDate.ToNullableDate();
            _preApprovalRequest.ProposedStartTime = model.TestInformation.ProposedStartTime.ToNullableDate(); 
            _preApprovalRequest.AovPreApprovalRequest.ApplicantCertificateNumber = model.ApplicantInformation.CertificateNumber;
            _preApprovalRequest.AovPreApprovalRequest.MedicalExpirationDate = model.ApplicantInformation.MedicalExpirationDate.ToNullableDate();
            _preApprovalRequest.AovPreApprovalRequest.IsEnhancedCTI = model.ApplicantInformation.IsEnhancedCTI;
            _preApprovalRequest.AovPreApprovalRequest.CompanyId = model.ActivityLocation.AovCompanyTypeId;   
            _preApprovalRequest.AovPreApprovalRequest.IsAcknowledge = model.TestInformation.Acknowledgement;  
            _preApprovalRequest.AovPreApprovalRequest.IsAcknowledgeMedicallyQualified = model.TestInformation.AcknowledgeMedicallyQualified; 
            _preApprovalRequest.AovPreApprovalRequest.IsCtopGraduate = model.ApplicantInformation.IsCtopGraduate; 
            _preApprovalRequestViewModel = model;

            SaveHelper(model);
            CompleteOrCreateTask(model, CheckIsAutoPreApproval(model));
            return _preApprovalRequestViewModel;

            
        }

      private static void SaveFunctionCodes(PreApprovalRequestViewModel model, PreApprovalRequest preApprovalRequest)
        {
            if (model.SelectedFunctionCodes != null)
            {
                preApprovalRequest.PreApprovalRequestFunctionCodes = model.SelectedFunctionCodes.Select(f => new PreApprovalRequestFunctionCode
                {
                    FunctionCodeId = f,
                    IsCloaFunctionCode = true
                }).ToList();
            }
        }

        protected virtual PreApprovalRequestViewModel SaveHelper(PreApprovalRequestViewModel model)
        {
            _context.SaveChanges();
            model.Id = _preApprovalRequest.Id;
            model.ActivityLocation.LocationAddress.Id = _preApprovalRequest.AovPreApprovalRequest != null ? _preApprovalRequest.AovPreApprovalRequest.ActivityLocationAddressId.GetValueOrDefault() : 0;
            if (model.ActivityLocation.FacilityonRecord.HasValue && !model.ActivityLocation.FacilityonRecord.Value)
            {
                model.ActivityLocation.OtherAddress = model.ActivityLocation.LocationAddress;
            }

            model.RequestInfo.RevisedDate = _preApprovalRequest.AovPreApprovalRequest != null && _preApprovalRequest.AovPreApprovalRequest.RevisedDate != null ? _preApprovalRequest.AovPreApprovalRequest.RevisedDate.Value.ToString("MM/dd/yyyy HH:mm tt") : String.Empty;
            //Get Documents 
            if (model.IsCancel)
            {
                var documents = model.DocumentReference;
                documents.ReferenceId = model.Id;
                _documentService.SaveDocument(documents);
            }
            model.DocumentReference = _documentService.GetDocumentsByRef((int)DocumentReferenceEnum.PreapprovalRequest, _preApprovalRequest.Id, null);
            var postActivity = _context.PostActivities.Where(it => it.PreApprovalRequestId == _preApprovalRequest.Id).FirstOrDefault();
            if(model.isSubmit && postActivity!= null)
            {
                postActivity.FormData = CloaPreApprovalRequestViewModelMapper.SerializePreApproval(model);
                _context.SaveChanges();
            }
            _preApprovalRequestViewModel = model;
            return _preApprovalRequestViewModel;
        }

        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
          Justification = "Complexity inherent to task navigation system")]
        protected virtual void UpdateAovData(PreApprovalRequestViewModel model, PreApprovalRequest preApprovalRequest)
        {
            // ApplicantInfo
            preApprovalRequest.AovPreApprovalRequest.ApplicantName = model.ApplicantInformation.Name;
            preApprovalRequest.Comments = model.Comments;
            //Activity Location
          
            if (model.ActivityLocation.FacilityonRecord.HasValue && model.ActivityLocation.FacilityonRecord.Value)
            {
               
                if (model.ActivityLocation.FacilityAddresses.Count > 1)
                {
                    preApprovalRequest.AovPreApprovalRequest.ActivityLocationAddressId = model.ActivityLocation.SelectedLocationAddressId;
                }
                else
                {
                    preApprovalRequest.AovPreApprovalRequest.ActivityLocationAddressId = model.ActivityLocation.FacilityAddress.Id;
                }
            }
            else
            {
                
                if (model.ActivityLocation.LocationAddress.Id != model.ActivityLocation.OtherAddress?.Id || model.ActivityLocation.LocationAddress.Id == 0)
                {
                    //Remove Other address saved previously
                    var savedAddress = preApprovalRequest.AovPreApprovalRequest.Address;
                    if (savedAddress.Id != model.ActivityLocation.SelectedLocationAddressId && savedAddress.Id != model.ActivityLocation.FacilityAddress.Id)
                    {
                        _context.Addresses.Remove(savedAddress);
                    }

                    var address = new Address
                    {
                        Name = model.ActivityLocation.LocationAddress.Name,
                        AddressLine1 = model.ActivityLocation.LocationAddress.Address1,
                        AddressLine2 = model.ActivityLocation.LocationAddress.Address2,
                        City = model.ActivityLocation.LocationAddress.City,
                        CountryId = model.ActivityLocation.LocationAddress.Country?.Id,
                        StateId = model.ActivityLocation.LocationAddress.Country?.Id == 184 ? model.ActivityLocation.LocationAddress.State?.Id : null,
                        ZipCode = model.ActivityLocation.LocationAddress.ZipCode,
                        AirTrafficControlTower = model.ActivityLocation.LocationAddress.AirTrafficControlTower,
                        Airport = model.ActivityLocation.LocationAddress.Airport
                    };
                    preApprovalRequest.AovPreApprovalRequest.Address = address;
                    preApprovalRequest.AovPreApprovalRequest.ActivityLocationAddressId = address.Id;
                }
                else
                {
                    preApprovalRequest.AovPreApprovalRequest.Address.Name = model.ActivityLocation.LocationAddress.Name; // Facility name make sure to add condition and save data for those designee types only.
                    preApprovalRequest.AovPreApprovalRequest.Address.AddressLine1 = model.ActivityLocation.LocationAddress.Address1;
                    preApprovalRequest.AovPreApprovalRequest.Address.AddressLine2 = model.ActivityLocation.LocationAddress.Address2;
                    preApprovalRequest.AovPreApprovalRequest.Address.City = model.ActivityLocation.LocationAddress.City;
                    preApprovalRequest.AovPreApprovalRequest.Address.CountryId = model.ActivityLocation.LocationAddress.Country?.Id;
                    preApprovalRequest.AovPreApprovalRequest.Address.StateId = model.ActivityLocation.LocationAddress.Country?.Id == 184 ? model.ActivityLocation.LocationAddress.State?.Id : null;
                    preApprovalRequest.AovPreApprovalRequest.Address.ZipCode = model.ActivityLocation.LocationAddress.ZipCode;
                    preApprovalRequest.AovPreApprovalRequest.Address.AirTrafficControlTower = model.ActivityLocation.LocationAddress.AirTrafficControlTower;
                    preApprovalRequest.AovPreApprovalRequest.Address.Airport = model.ActivityLocation.LocationAddress.Airport;
                }
            }

            preApprovalRequest.AovPreApprovalRequest.FacilityOnRecord = model.ActivityLocation.FacilityonRecord.Value;           
            _preApprovalRequest = preApprovalRequest;
        }
        protected void InsertAovData(PreApprovalRequestViewModel model, PreApprovalRequest preApprovalRequest)
        {
            // ApplicantInfo
            preApprovalRequest.AovPreApprovalRequest = new AovPreApprovalRequest();
            preApprovalRequest.AovPreApprovalRequest.ApplicantName = model.ApplicantInformation.Name;
            preApprovalRequest.Comments = model.Comments;
            //Activity Location
            if (model.ActivityLocation.FacilityonRecord.HasValue && model.ActivityLocation.FacilityonRecord.Value)
            {
                preApprovalRequest.AovPreApprovalRequest.ActivityLocationAddressId = model.ActivityLocation.LocationAddress != null ? model.ActivityLocation.LocationAddress.Id : model.ActivityLocation.FacilityAddress.Id;
            }
            else
            {
                preApprovalRequest.AovPreApprovalRequest.Address = new Address
                {
                    Name = model.ActivityLocation.LocationAddress.Name,
                    AddressLine1 = model.ActivityLocation.LocationAddress.Address1,
                    AddressLine2 = model.ActivityLocation.LocationAddress.Address2,
                    City = model.ActivityLocation.LocationAddress.City,
                    CountryId = model.ActivityLocation.LocationAddress.Country?.Id,
                    StateId = model.ActivityLocation.LocationAddress.Country?.Id == 184 ? model.ActivityLocation.LocationAddress.State?.Id : null,
                    ZipCode = model.ActivityLocation.LocationAddress.ZipCode,
                    AirTrafficControlTower = model.ActivityLocation.LocationAddress.AirTrafficControlTower,
                    Airport = model.ActivityLocation.LocationAddress.Airport
                };
            }

            preApprovalRequest.AovPreApprovalRequest.FacilityOnRecord = model.ActivityLocation.FacilityonRecord.Value;
            //Test info
            preApprovalRequest.AovPreApprovalRequest.PracticalOralTestId = model.TestInformation.PracticalOralTestId;
            preApprovalRequest.AovPreApprovalRequest.ApplicantCertificateNumber = model.ApplicantInformation.CertificateNumber;
            preApprovalRequest.AovPreApprovalRequest.MedicalExpirationDate = model.MedicalExpirationDate.ToNullableDate();
            preApprovalRequest.AovPreApprovalRequest.IsAcknowledge = model.TestInformation.Acknowledgement;
            preApprovalRequest.AovPreApprovalRequest.IsAcknowledgeMedicallyQualified = model.TestInformation.AcknowledgeMedicallyQualified;
            preApprovalRequest.AovPreApprovalRequest.IsEnhancedCTI = model.ApplicantInformation.IsEnhancedCTI;
            preApprovalRequest.AovPreApprovalRequest.IsCtopGraduate = model.ApplicantInformation.IsCtopGraduate;
        }

        // Complete the Task Status and also create post activity if its approved.
        protected void CompleteTask(PreApprovalRequest preApprovalRequest, PreApprovalRequestViewModel model)
        {
            preApprovalRequest.IsApproved = true;
            preApprovalRequest.ApproverComments = "Auto Pre-Approval";
            preApprovalRequest.ApproverOfficeRoleId = model.ManagingSpecialist;
            preApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Approved;
            preApprovalRequest.PostActivities = new List<PostActivity>
            {
                new PostActivity
                {
                    StatusId = (int)PreApprovalRequestStatusEnum.Initiated,
                    TrackingNumber = preApprovalRequest.TrackingNumber.Replace("PR", "PO"),
                    TimeZoneId = preApprovalRequest.TimeZoneId,
                    FormData = CloaPreApprovalRequestViewModelMapper.SerializePreApproval(model),
                    ApprovalDate = DateTime.Now
                }
            };
            
            if (preApprovalRequest.ProposeStartDate.HasValue && preApprovalRequest.ProposedStartTime.HasValue)
            {
                var proposedStart = DateTime.Parse(preApprovalRequest.ProposeStartDate.Value.ToShortDateString() + " " + preApprovalRequest.ProposedStartTime?.ToString("hh:mm tt"));
                var timeZoneName = _lookupService.LookupValues().Result.TimeZones.First( x=> x.Id == (int)model.TestInformation.TimeZoneId).StandardName;          
                proposedStart = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(proposedStart, timeZoneName, "Central Standard Time");         
             
                if (proposedStart < DateTime.Now.AddHours(24))
                {
                    var notificationViewModel = new MessageNotificationViewModel
                    {
                        MessageDictionary = new List<KeyValuePair<string, string>>()
                                            {
                                                new KeyValuePair<string, string>("@designeeName", model.DesigneeInfo.Name),
                                                new KeyValuePair<string, string>("@trackingNumber", model.RequestInfo.ControlNumber)
                                            },
                        UserOfficeRoleRecipients = new List<(int UserOfficeRoleId, bool IsCced)> { (model.DesigneeInfo.ManagingSpecialistId.Value, false) },
            
                        Code = "PR24HR",
                    };

                    _messageService.SendNotification(notificationViewModel);
                }
            }

            _context.SaveChanges();
        }

        protected virtual void CompleteOrCreateTask(PreApprovalRequestViewModel model, bool canComplete)
        {
            if (model.isSubmit && !_preApprovalRequest.IsApproved.HasValue)
            {
                if (!canComplete)
                {
                    base.CreateTask(model);
                    if (_preApprovalRequest.ProposeStartDate.HasValue && _preApprovalRequest.ProposedStartTime.HasValue)
                    {
                        var proposedStart = DateTime.Parse(_preApprovalRequest.ProposeStartDate.Value.ToShortDateString() + " " + _preApprovalRequest.ProposedStartTime?.ToString("hh:mm tt"));
                        var timeZoneName = _lookupService.LookupValues().Result.TimeZones.First(x => x.Id == (int)model.TestInformation.TimeZoneId).StandardName;
                        proposedStart = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(proposedStart, timeZoneName, "Central Standard Time");

                        if (proposedStart < DateTime.Now.AddHours(24))
                        {
                            var notificationViewModel = new MessageNotificationViewModel
                            {
                                MessageDictionary = new List<KeyValuePair<string, string>>()
                                                    {
                                                        new KeyValuePair<string, string>("@designeeName", model.DesigneeInfo.Name),
                                                        new KeyValuePair<string, string>("@trackingNumber", model.RequestInfo.ControlNumber)
                                                    },
                                UserOfficeRoleRecipients = new List<(int UserOfficeRoleId, bool IsCced)> { (model.DesigneeInfo.ManagingSpecialistId.Value, false) },

                                Code = "PR24HRMN",
                            };
                            _messageService.SendNotification(notificationViewModel);                            
                        }
                        var preapprovalNotificationViewModel = new MessageNotificationViewModel
                            {
                                MessageDictionary = new List<KeyValuePair<string, string>>()
                                                    {
                                                        new KeyValuePair<string, string>("@designeeName", model.DesigneeInfo.Name),
                                                        new KeyValuePair<string, string>("@trackingNumber", model.RequestInfo.ControlNumber)
                                                    },
                                UserOfficeRoleRecipients = new List<(int UserOfficeRoleId, bool IsCced)> { (model.DesigneeInfo.ManagingSpecialistId.Value, false) },
                    
                                Code = "PRSUB",
                            };
                            _messageService.SendNotification(preapprovalNotificationViewModel);
                            _context.SaveChanges();
                    }
                }
                else
                {
                    _preApprovalRequest.IsPreApprovalOnHold = false;
                    // Complete the Task Status and also create post activity if its approved.
                    CompleteTask(_preApprovalRequest, model);
                }
            }
        }

        public override bool Cancel(ActivityPaperWorkViewModel model)
        {
            bool flag = false;
            var preApprovalRequest = _context.PreApprovalRequests.Include(itp => itp.AovPreApprovalRequest).Include(itp => itp.PostActivities).Where(par => par.Id == model.Id).FirstOrDefault();
            if (preApprovalRequest != null)
            {
                if (!(string.IsNullOrEmpty(model.JustificationForCancellation)))
                {
                    preApprovalRequest.AovPreApprovalRequest.JustificationForCancellation = model.JustificationForCancellation;
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
                if (cloa == null)
                {
                    return flag;
                }

                var notificationViewModel = new MessageNotificationViewModel
                {
                    MessageDictionary = new List<KeyValuePair<string, string>>()
                                    {
                                        new KeyValuePair<string, string>("@trackingNumber", preApprovalRequest.TrackingNumber),
                                        new KeyValuePair<string, string>("@designeeName", cloa.DesigneeName)
                                    },
                    UserRecipients = new List<(int UserId, bool IsCced)> { (cloa.ManagingSpecialistUserId, false) },
                    Code = "PRECAN",
                };
                _messageService.SendNotification(notificationViewModel);

                _context.SaveChanges();
                flag = true;
            }
            return flag;
         }
        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity inherent to task navigation system")]
        public override bool SaveMsDecision(PreApprovalRequestViewModel model)
         {
            var preApprovalRequest = _context.PreApprovalRequests.Include(p => p.AovPreApprovalRequest)
            .Include(p => p.PreApprovalRequestFunctionCodes).Include(p => p.PostActivities).FirstOrDefault(p => p.Id == model.Id);

            if (preApprovalRequest != null)
            {
                if (preApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Approved)
                {
                    preApprovalRequest.IsPossibleDirectObservation = model.IsPossibleDirectObservation.Value;
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
                    if (preApprovalRequest.PostActivities== null || preApprovalRequest.PostActivities.Count == 0)
                    {
                        preApprovalRequest.PostActivities = new List<PostActivity>
                        {
                            new PostActivity
                            {
                                StatusId = (int)PreApprovalRequestStatusEnum.Initiated,
                                TrackingNumber = preApprovalRequest.TrackingNumber.Replace("PR", "PO"),
                                ApprovalDate = DateTime.Now,
                                TimeZoneId = preApprovalRequest.TimeZoneId,
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

        public override PreApprovalRequestViewModel SavePerformanceResults(PreApprovalRequestViewModel adminModel)
        {
            return _preApprovalRequestViewModel;
        }
        public override PreApprovalRequestViewModel Copy(int id)
        {
            var latestCloa = _context.PreApprovalRequests.Where(p => p.Id == id)
                                .Join(_context.Cloas,
                                     p => p.ApplicationId,
                                     c => c.ApplicationId,
                                     (p, c) => new
                                     {
                                         cloa = c,
                                         preApproval = p,
                                         timeZone = c.CloaAdjunct
                                     }).Select(cp => new
                                     {
                                         CloaId = cp.cloa.Id,
                                         PreApprovalCloaId = cp.preApproval.CloaId,
                                         TimeZoneId = cp.timeZone != null ? cp.timeZone.TimeZoneId : null
                                     }).AsEnumerable()
                                       .OrderByDescending(c => c.CloaId)
                                       .First();

            var preapprovalInfo = this.Get(id, false, 0, latestCloa.CloaId);
            preapprovalInfo.Id = 0;
            //clean up some of the data as part of copy
            preapprovalInfo.RequestInfo = new PreApprovalRequestInfoViewModel();
            preapprovalInfo.RequestInfo.ControlNumber = _activityService.GenerateTrackingNumber(preapprovalInfo.DesigneeInfo.CloaId, (int)ProcessTypeEnum.PreApproval, null);
            preapprovalInfo.RequestInfo.ActivityStatus = new BaseLookup() { Name = "Copied" };
            preapprovalInfo.DocumentReference = new DocumentReferenceViewModel();
            preapprovalInfo.DocumentReference.DocumentReferenceEnum = DocumentReferenceEnum.PreapprovalRequest;
            preapprovalInfo.DocumentReference.UploadedFiles = new List<DocumentViewModel>();

            if (latestCloa.TimeZoneId != null)
            {
                preapprovalInfo.TestInformation.TimeZoneId = latestCloa.TimeZoneId;
            }
           
            return preapprovalInfo;
        }
        protected override IEnumerable<PreApprovalDateWiseCountViewModel> GetPreApprovalDateWiseCount(int applicationId)
        {
            var ignoreStatusIds = new List<int>() { (int)PreApprovalRequestStatusEnum.Canceled, (int)PreApprovalRequestStatusEnum.Saved };
            var dateWiseCount = _context.PreApprovalRequests
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
            return dateWiseCount;
        }
        
        protected override PreApprovalRequest GetOriginalPreApprovalData(int id)
        {

            var preApprovalRequest = _context.PreApprovalRequests.Include(p => p.PreApprovalRequestFunctionCodes)
                .ThenInclude(it => it.Type)
                .Include(p => p.PreApprovalRequestFunctionCodes).ThenInclude(p => p.PreApprovalRequestFunctionCodeTypeRatings)
                .Include(p => p.AovPreApprovalRequest)
                .Include(p => p.AovPreApprovalRequest).ThenInclude(p => p.Address)
                .First(p => p.Id == id);

            return preApprovalRequest;
        }

        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity inherent to task navigation system")]
        public override AfsGroupsPostActivityViewModel SaveAfsPostActivity(AfsGroupsPostActivityViewModel model)
        {
            _postActivity = _context.PostActivities
                .Include(pa => pa.PostActivityPerformanceReview)
                .Include(pa => pa.PreApprovalRequest)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(pr => pr.Cloa).ThenInclude(c => c.CloaAddresses).ThenInclude(a => a.Address)
                .Include(a => a.Address)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(p => p.PreApprovalRequestFunctionCodes).ThenInclude(it => it.Type)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(p => p.AovPreApprovalRequest)
                .Include(pa => pa.PreApprovalRequest).ThenInclude(p => p.AovPreApprovalRequest).ThenInclude(p => p.Address)
                .Include(pa => pa.PostActivityApplicants)
                .Include(pa => pa.PostActivityCertificates)
                .Include(pa => pa.PostActivityProducts)
                .Include(pa => pa.PostActivityFunctionCodes)
                .AsNoTracking()
                .Single(r => r.Id == model.Id);

            bool isEdit = false;

            if (_postActivity.PreApprovalRequest.StatusId == (int)PreApprovalRequestStatusEnum.Completed)
            {
                //new version must be created
                _postActivity.Id = 0;
                _postActivity.PostActivityProducts = new List<PostActivityProduct>();
                _postActivity.PostActivityCertificates = new List<PostActivityCertificate>();
                _postActivity.PostActivityApplicants = new List<PostActivityApplicant>();
                _postActivity.CompletedDate = System.DateTime.Now;
                _postActivity.StatusId = (int)PreApprovalRequestStatusEnum.Completed;
                isEdit = true;               
            }
            else
            {
                _postActivity.StatusId = model.IsSubmit ? (int)PreApprovalRequestStatusEnum.Completed : (int)PreApprovalRequestStatusEnum.Saved;
                if (model.IsSubmit)
                {
                    _postActivity.CompletedDate = System.DateTime.Now;
                    _postActivity.PreApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Completed;
                    isEdit = false;
                }
            }
            _postActivity.GeneralComments = model.GeneralComments;
            _postActivity.ApplicantName = model.PreApprovalRequest.ApplicantInformation.Name;
            _postActivity.ApplicantCertificateNumber = model.PreApprovalRequest.ApplicantInformation.CertificateNumber;
            _postActivity.ApplicantPhone = model.PreApprovalRequest.ApplicantInformation.Phone;
            _postActivity.ActualStartDate = model.ActualStartDate.ToNullableDateTime(model.ActualStartTime);
            _postActivity.ActualEndDate = model.ActualEndDate.ToNullableDateTime(model.ActualEndTime);
            _postActivity.PracticalTestResultId = model.PracticalTestResultId;
            _postActivity.TestDuration = model.TestDuration;
            _postActivity.ReasonsOfDiscontinuance = model.ReasonsOfDiscontinuance;

            _postActivity.FormData = CloaPreApprovalRequestViewModelMapper.SerializePreApproval(model.PreApprovalRequest);


            _context.Entry(_postActivity).State = _postActivity.Id > 0 ? EntityState.Modified : EntityState.Added;
            _context.Entry(_postActivity.PreApprovalRequest).State = EntityState.Modified;
            if (_postActivity.PostActivityPerformanceReview != null)
                 {
                   _context.Entry(_postActivity.PostActivityPerformanceReview).State =
                   _postActivity.PostActivityPerformanceReview.Id > 0 ? EntityState.Modified: EntityState.Added;
                 }
            _context.SaveChanges();
            if (model.IsSubmit)
            {
                if (isEdit)
                {
                    CreateReviewPostActivityTask(model, _postActivity.Id, (int)TaskSubTypeEnum.ReviewPostActivityChanges);
                    CheckPostActivityModifiedData(model);
                    _postActivity.FormData = CloaPreApprovalRequestViewModelMapper.SerializePreApproval(model.PreApprovalRequest);
                }
                else
                {
                    CheckPreApprovalModifiedData(model);
                    _postActivity.FormData = CloaPreApprovalRequestViewModelMapper.SerializePreApproval(model.PreApprovalRequest);
                    if(!IsAutoPostActivity(model.PreApprovalRequest.Id))
                    {
                      CreateReviewPostActivityTask(model, _postActivity.Id, (int)TaskSubTypeEnum.PostActivityReview);
                    }
                }
                _context.Entry(_postActivity).State = _postActivity.Id > 0 ? EntityState.Modified : EntityState.Added;
                _context.Entry(_postActivity.PreApprovalRequest).State = EntityState.Modified;
            }
            _context.SaveChanges();
            model.Id = _postActivity.Id;
            return model;

        }

        protected void CreateReviewPostActivityTask(AfsGroupsPostActivityViewModel model, int postActivityId, int taskSubTypeId)
        {
            // Get latest MS before creating Task
            var managingSpecialistId = _context.Cloas.Where(c => c.ApplicationId ==  model.PreApprovalRequest.ApplicationId)
                .OrderByDescending(c => c.Id).First().ManagingSpecialistId.GetValueOrDefault();

            //Create Task for MS Review
            _taskService.CreateTask(new TaskViewModel
            {
                TaskSubTypeId = taskSubTypeId,
                TaskStatusId = (int)TaskStatusEnum.Pending,
                ActionId = postActivityId,
                UserOfficeRoleId = managingSpecialistId,
                ApplicationId = model.PreApprovalRequest.ApplicationId,
            });

            if (taskSubTypeId == (int)TaskSubTypeEnum.ReviewPostActivityChanges)
            {
                var notificationViewModel = new MessageNotificationViewModel
                {
                    MessageDictionary = new List<KeyValuePair<string, string>>()
                                            {
                                                new KeyValuePair<string, string>("@designeeName", model.DesigneeInfo.Name),
                                                new KeyValuePair<string, string>("@trackingNumber", model.RequestInfo.ControlNumber)
                                            },
                    UserOfficeRoleRecipients = new List<(int UserOfficeRoleId, bool IsCced)> { (model.DesigneeInfo.ManagingSpecialistId.Value, false) },

                    Code = "POUPSUB",
                };

                _messageService.SendNotification(notificationViewModel);
            }
            else
            {
                var notificationViewModel = new MessageNotificationViewModel
                {
                    MessageDictionary = new List<KeyValuePair<string, string>>()
                                            {
                                                new KeyValuePair<string, string>("@designeeName", model.DesigneeInfo.Name),
                                                new KeyValuePair<string, string>("@trackingNumber", model.RequestInfo.ControlNumber)
                                            },
                    UserOfficeRoleRecipients = new List<(int UserOfficeRoleId, bool IsCced)> { (model.DesigneeInfo.ManagingSpecialistId.Value, false) },

                    Code = "POSUB",
                };

                _messageService.SendNotification(notificationViewModel);

            }
        }


        private void CheckPreApprovalModifiedData(AfsGroupsPostActivityViewModel model)
        {
            model.PreApprovalRequest.ModifiedPreapprovalControls = new Dictionary<string, List<ModifiedControlViewModel>>();
            var afsPostActivityModifiedPreApprovalViewModel = new AfsPostActivityModifiedPreApprovalViewModel();
            var preApprovalRequest = GetOriginalPreApprovalData(model.PreApprovalRequestId);

            var triggerCorrectiveActionTrigger = false;

            triggerCorrectiveActionTrigger = FindModifiedData(model, preApprovalRequest, model.PreApprovalRequest.ModifiedPreapprovalControls);
           

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

        protected static void AddItemToModifiedPreapprovals(string key, ModifiedControlViewModel control, Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls)
        {
            if (modifiedPreapprovalControls != null)
            {
                if (!modifiedPreapprovalControls.ContainsKey(key))
                {
                    modifiedPreapprovalControls.Add(key, new List<ModifiedControlViewModel>() { control });
                }
                else
                {
                    var keyControls = modifiedPreapprovalControls[key];
                    keyControls.Add(control);
                }
            }
        }

        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity inherent to task navigation system")]
        private bool FindModifiedData(AfsGroupsPostActivityViewModel model, PreApprovalRequest preApprovalRequest,
                                                        Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls)
        {
            bool triggerCorrectiveActiveTrigger = false;

            if ((model.PreApprovalRequest.ActivityLocation.LocationAddress.Name != null) && model.PreApprovalRequest.ActivityLocation.LocationAddress.Name != preApprovalRequest.AovPreApprovalRequest.Address.Name)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("ActivityLocation", new ModifiedControlViewModel { Control = "name" }, modifiedPreapprovalControls);
            }
            if (model.PreApprovalRequest.ActivityLocation.LocationAddress.Address1 != preApprovalRequest.AovPreApprovalRequest.Address.AddressLine1)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("ActivityLocation", new ModifiedControlViewModel { Control = "address1" }, modifiedPreapprovalControls);
            }
            if (model.PreApprovalRequest.ActivityLocation.LocationAddress.Address2 != preApprovalRequest.AovPreApprovalRequest.Address.AddressLine2)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("ActivityLocation", new ModifiedControlViewModel { Control = "address2" }, modifiedPreapprovalControls);
            }
            if (model.PreApprovalRequest.ActivityLocation.LocationAddress.City != preApprovalRequest.AovPreApprovalRequest.Address.City)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("ActivityLocation", new ModifiedControlViewModel { Control = "city" }, modifiedPreapprovalControls);
            }
            if (model.PreApprovalRequest.ActivityLocation.LocationAddress.State?.Id != preApprovalRequest.AovPreApprovalRequest.Address.StateId)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("ActivityLocation", new ModifiedControlViewModel { Control = "state" }, modifiedPreapprovalControls);
            }
            if (model.PreApprovalRequest.ActivityLocation.LocationAddress.Country?.Id != preApprovalRequest.AovPreApprovalRequest.Address.CountryId || (!model.PreApprovalRequest.ActivityLocation.FacilityonRecord.Value && model.PreApprovalRequest.ActivityLocation.LocationAddress.Id == 0))
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("ActivityLocation", new ModifiedControlViewModel { Control = "country" }, modifiedPreapprovalControls);
            }
            if (model.PreApprovalRequest.ActivityLocation.LocationAddress.ZipCode != preApprovalRequest.AovPreApprovalRequest.Address.ZipCode)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("ActivityLocation", new ModifiedControlViewModel { Control = "zipCode" }, modifiedPreapprovalControls);
            }
            if (model.PreApprovalRequest.ActivityLocation.LocationAddress.AirTrafficControlTower != preApprovalRequest.AovPreApprovalRequest.Address.AirTrafficControlTower)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("ActivityLocation", new ModifiedControlViewModel { Control = "airTrafficControlTower" }, modifiedPreapprovalControls);
            }
            if (model.PreApprovalRequest.ActivityLocation.LocationAddress.Airport != preApprovalRequest.AovPreApprovalRequest.Address.Airport)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("ActivityLocation", new ModifiedControlViewModel { Control = "airport" }, modifiedPreapprovalControls);
            }

            if (model.PreApprovalRequest.ActivityLocation.AovCompanyTypeId != preApprovalRequest.AovPreApprovalRequest.CompanyId)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("ActivityLocation", new ModifiedControlViewModel { Control = "company" }, modifiedPreapprovalControls);
            }

            if (model.PreApprovalRequest.ActivityLocation.FacilityonRecord.Value != preApprovalRequest.AovPreApprovalRequest.FacilityOnRecord)
            {
                AddItemToModifiedPreapprovals("ActivityLocation", new ModifiedControlViewModel { Control = "facilityonRecord" }, modifiedPreapprovalControls);
                triggerCorrectiveActiveTrigger = true;
            }

            //verify test information 
            if (model.PreApprovalRequest.TestInformation.PracticalOralTestId != preApprovalRequest.AovPreApprovalRequest.PracticalOralTestId && preApprovalRequest.AovPreApprovalRequest.PracticalOralTestId != null)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("TestInformation", new ModifiedControlViewModel { Control = "practicalOralTestId" }, modifiedPreapprovalControls);
            }

            if (!string.IsNullOrEmpty(model.ActualStartDate))
            {
                DateTime actualStartDate = Convert.ToDateTime(model.ActualStartDate);
                DateTime? actualEndDate = null;
                if (!string.IsNullOrEmpty(model.ActualEndDate))
                {
                    actualEndDate = Convert.ToDateTime(model.ActualEndDate);
                }

                var proposedStartDate = preApprovalRequest.AovPreApprovalRequest.PreApprovalRequest.ProposeStartDate;
                var proposedEndDate = preApprovalRequest.AovPreApprovalRequest.PreApprovalRequest.ProposeEndDate;

                var isProposedDateNotWithinRange = actualEndDate != null &&
                                                    !((actualStartDate >= proposedStartDate && actualStartDate <= proposedEndDate) &&
                                                    (actualEndDate >= proposedStartDate && actualEndDate <= proposedEndDate));
                if (isProposedDateNotWithinRange)
                {
                    triggerCorrectiveActiveTrigger = true;
                    AddItemToModifiedPreapprovals("TestInformation", new ModifiedControlViewModel { Control = "proposeStartDate" }, modifiedPreapprovalControls);
                }
            }
            if (model.PreApprovalRequest.TestInformation.AcknowledgeMedicallyQualified != preApprovalRequest.AovPreApprovalRequest.IsAcknowledgeMedicallyQualified && preApprovalRequest.AovPreApprovalRequest.PracticalOralTestId != null)
            {
                triggerCorrectiveActiveTrigger = true;
                AddItemToModifiedPreapprovals("TestInformation", new ModifiedControlViewModel { Control = "acknowledgeMedicallyQualified" }, modifiedPreapprovalControls);
            }
            return triggerCorrectiveActiveTrigger;
        }
        public PostActivity GetOriginalPostActivityData(int postActivityId)
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
                .Include(pa => pa.PostActivityFunctionCodes)                             
                .Include(pa => pa.PreApprovalRequest).ThenInclude(pa => pa.AovPreApprovalRequest)
                .FirstOrDefault(pa => pa.Id == postActivityId);

            if (postActivity == null) return null;

            return postActivity;

        }

        private void CheckPostActivityModifiedData(AfsGroupsPostActivityViewModel model)
        {
            model.PreApprovalRequest.ModifiedPostActivityControls = new Dictionary<string, List<ModifiedControlViewModel>>();            
            var postActivity = GetOriginalPostActivityData(model.Id);
            var preApprovalRequestViewModel = new PreApprovalRequestViewModel();
            if (postActivity.FormData != null) 
            {
              preApprovalRequestViewModel = CloaPreApprovalRequestViewModelMapper.DeSerializePreApprovalFormData(_context, postActivity.FormData, postActivity.PreApprovalRequest.CloaId);
            }
            //verify post Activity information 
            var checks = new List<(bool changed, string control)>
            {
                (model.PreApprovalRequest.TestInformation.PracticalOralTestId
                    != preApprovalRequestViewModel.TestInformation?.PracticalOralTestId, "practicalOralTestId"),
                (model.PracticalTestResultId != postActivity.PracticalTestResultId, "practicalTestResultId"),
                (model.ActualStartDate != postActivity.ActualStartDate.DateToString(), "actualStartDate"),
                (model.ActualStartTime != postActivity.ActualStartDate.ToTimeString(), "actualStartTime"),
                (model.ActualEndDate != postActivity.ActualEndDate.DateToString(), "actualEndDate"),
                (model.ActualEndTime != postActivity.ActualEndDate.ToTimeString(), "actualEndTime"),
                (model.TimeZoneId != preApprovalRequestViewModel.TestInformation?.TimeZoneId, "timeZone"),
                (model.TestDuration != postActivity.TestDuration, "testDuration"),
                (model.GeneralComments != postActivity.GeneralComments, "generalComments"),
                (model.PreApprovalRequest.ApplicantInformation.Name != postActivity.ApplicantName, "nameOfApplicant"),
                (model.PreApprovalRequest.ApplicantInformation.CertificateNumber != postActivity.ApplicantCertificateNumber, "certificateNumberOfApplicant"),
                (model.PreApprovalRequest.ApplicantInformation.Phone != postActivity.ApplicantPhone, "phoneNumber"),
                (model.PreApprovalRequest.ApplicantInformation.MedicalExpirationDate
                    != preApprovalRequestViewModel.ApplicantInformation?.MedicalExpirationDate, "medicalExpiration"),
                (model.ReasonsOfDiscontinuance != postActivity.ReasonsOfDiscontinuance, "reason"),
                (model.IsAttachmentModified == true, "uploadId")
            };

            foreach (var (changed, control) in checks)
            {
                if (changed)
                {
                    AddItemToModifiedPostActivity(
                        "PostActivityInformation",
                        new ModifiedControlViewModel { Control = control },
                        model.PreApprovalRequest.ModifiedPostActivityControls);
                }
            }          
        }

        protected static void AddItemToModifiedPostActivity(string key, ModifiedControlViewModel control, Dictionary<string, List<ModifiedControlViewModel>> modifiedPostActivityControls)
        {
            if (modifiedPostActivityControls != null)
            {
                if (!modifiedPostActivityControls.ContainsKey(key))
                {
                   modifiedPostActivityControls.Add(key, new List<ModifiedControlViewModel>() { control });
                }
                else
                {
                    var keyControls = modifiedPostActivityControls[key];
                    keyControls.Add(control);
                }
            }
        }
       
        protected void CreateCorrectiveAction(int cloaId, int applicationId, int managingSpecialist)
        {
            var activity = new Dms.Core.EntityFramework.Model.Activity.Activity
            {
                ModuleId = (int)ProcessTypeEnum.CorrectiveAction,
                StatusId = (int)ActivityStatusEnum.Saved,
                ActivityTypeId = (int)ActivityTypeEnum.PostActivityCorrectiveAction,
                RequestDate = DateTime.Today,
                FormData = JsonConvert.SerializeObject(new { PostActivityId = _postActivity.Id, PreApprovalRequestId = _postActivity.PreApprovalRequestId }),
                TrackingNumber = _activityService.GenerateTrackingNumber(cloaId, (int)ProcessTypeEnum.CorrectiveAction),
                DueDate = DateTime.MaxValue, // to not show in any pending due date sections
                CloaId = cloaId,
                ApplicationDueDate = new ApplicationDueDate()
                {
                    ApplicationId = applicationId,
                    DueDate = DateTime.MaxValue,
                }
            };
            _context.Entry(activity).State = EntityState.Added;
            _context.SaveChanges();
            //Create Corrective action task for MS
            _taskService.CreateTask(new TaskViewModel
            {
                TaskSubTypeId = (int)TaskSubTypeEnum.PostActivityCorrectiveAction,
                TaskStatusId = (int)TaskStatusEnum.Pending,
                ActionId = activity.Id,
                UserOfficeRoleId = managingSpecialist,
                ApplicationId = applicationId,
            });
        }


        public override bool CheckPendingPostActivitiesExists(int applicationId)
        {
            var hasPendingPostActivity = _context.PreApprovalRequests.Where(par => (par.ApplicationId == applicationId)
                                                    && par.ProposeEndDate < DateTime.Now
                                                    && (par.StatusId == (int)PreApprovalRequestStatusEnum.Approved || par.StatusId == (int)PreApprovalRequestStatusEnum.Completed)
                                                    && par.ProposeEndDate.Value.AddDays(8) <= DateTime.Now)
                                           .Join(_context.PostActivities.Where(po => !(po.StatusId == (int)PreApprovalRequestStatusEnum.Completed
                                                                                        || po.StatusId == (int)PreApprovalRequestStatusEnum.Pending
                                                                                        || po.StatusId == (int)PreApprovalRequestStatusEnum.Canceled)),
                                                       p => p.Id,
                                                       po => po.PreApprovalRequestId,
                                                       (p, po) => new { p, po }).Any();
            return hasPendingPostActivity;
        }
        public bool IsAutoPostActivity(int id)
        {
            
            var preApprovalRequest = _context.PreApprovalRequests
                    .Include(par => par.PreApprovalRequestFunctionCodes)                    
                    .ThenInclude(fc => fc.FunctionCode)                    
                    .Where(par => par.Id == id).First();

            var cloaFunctionCodes = _context.CloaFunctionCodes
                                            .Where(c => c.Cloa.ApplicationId == preApprovalRequest.ApplicationId && (c.Cloa.CloaStatusId == (int)CloaStatusEnum.Active || c.Cloa.CloaStatusId == (int)CloaStatusEnum.Suspended))
                                            .Include(c => c.FunctionCode)
                                            .AsEnumerable()
                                            .SelectMany(c => new List<FunctionCodeViewModel>
                                                                {
                                                                    new FunctionCodeViewModel
                                                                    {
                                                                        FunctionCodeId = c.FunctionCodeId,
                                                                        IsAutoPostActivity = c.IsAutoPostActivity
                                                                    }
                                                                }.ToArray()
                                                        ).ToArray();

                var preApprovalFunctionCodes = preApprovalRequest.PreApprovalRequestFunctionCodes
                                                .SelectMany(p => new List<FunctionCodeViewModel>
                                                                {
                                                                    new FunctionCodeViewModel
                                                                    {
                                                                        FunctionCodeId = p.FunctionCodeId
                                                                    }
                                                                }.ToArray()
                                                            ).ToArray();

                if (cloaFunctionCodes.Any(c => !c.IsAutoPostActivity.Value &&
                   (preApprovalFunctionCodes.Any(p => p.FunctionCodeId == c.FunctionCodeId))))
                {
                    return false;
                }
                return true;
        }

        public override AfsGroupsPostActivityViewModel Get(int postActivityId, bool createDocumentVersion = false)
        {
            var postActivity = _context.PostActivities
                .Include(pa => pa.PostActivityPerformanceReview)
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
                .Include(pa => pa.PostActivityFunctionCodes)                             
                .Include(pa => pa.PreApprovalRequest).ThenInclude(pa => pa.AovPreApprovalRequest)
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
                AppointmentDate = postActivity.PreApprovalRequest.Cloa.AppointmentDate,
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
                DecisionDate = postActivity.PreApprovalRequest.PostActivities.Any() ?
                                                                 (postActivity.PreApprovalRequest.PostActivities.OrderByDescending(po => po.Id).FirstOrDefault() != null && postActivity.PreApprovalRequest.PostActivities.OrderByDescending(po => po.Id).First().ApprovalDate.HasValue ?
                                                                  postActivity.PreApprovalRequest.PostActivities.OrderByDescending(po => po.Id).First().ApprovalDate.Value.ToString("MM/dd/yyyy HH:mm tt") : string.Empty) : string.Empty,
                DecisionBy = postActivity.PreApprovalRequest.ApproverOfficeRole != null && postActivity.PreApprovalRequest.ApproverComments != "Auto Pre-Approval" ? postActivity.PreApprovalRequest.ApproverOfficeRole.User.Profile.ToFullName() : postActivity.PreApprovalRequest.ApproverComments == "Auto Pre-Approval" ? "Auto Pre-Approval" : string.Empty,
                RevisedDate = postActivity.PreApprovalRequest.AovPreApprovalRequest != null && postActivity.PreApprovalRequest.AovPreApprovalRequest.RevisedDate != null ? postActivity.PreApprovalRequest.AovPreApprovalRequest.RevisedDate.Value.ToString("MM/dd/yyyy HH:mm tt") : String.Empty

            };
           
            if (postActivity != null)
            {
                MapPostActivityToPostActivityViewModel(postActivity);
            }
            _afsGroupsPostActivityViewModel.PreApprovalRequest.TestCheckInformation = new PreApprovalTestCheckInformationViewModel() {

                IsAircraftNotRequired = true,
                TemporaryAuthorizations = new List<FunctionCodeViewModel>()
        };

            _afsGroupsPostActivityViewModel.IsLatestVersion = postActivity.PreApprovalRequest.PostActivities.OrderByDescending(pa => pa.Id).First().Id == postActivityId;
            _afsGroupsPostActivityViewModel.CompletedDate = postActivity.CompletedDate;
                      
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
                AirTrafficControlTower =  postActivity.Address.AirTrafficControlTower,
                Airport =  postActivity.Address.Airport,
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
            _afsGroupsPostActivityViewModel.GeneralComments = postActivity.GeneralComments;
            _afsGroupsPostActivityViewModel.TestDuration = postActivity.TestDuration;

            if (postActivity.PostActivityPerformanceReview != null)
            {
                _afsGroupsPostActivityViewModel.ManagingSpecialistComments = postActivity.PostActivityPerformanceReview.RequiredFollowupComments;
                _afsGroupsPostActivityViewModel.ManagingSpecialistFollowup = postActivity.PostActivityPerformanceReview.IsReviewCompleted;
            }

        }

        public override bool SaveMsPostActivityReview(AfsGroupsPostActivityViewModel model)
        {
            if (model != null)
            { 
                //Complete the Task Status.
                var task = _context.Tasks.FirstOrDefault(it => it.ActionId == model.Id && it.StatusId == (int)TaskStatusEnum.Pending && (it.SubTypeId == (int)TaskSubTypeEnum.ReviewPostActivityChanges || it.SubTypeId == (int)TaskSubTypeEnum.PostActivityReview));
                if (task != null)
                {
                    task.StatusId = (int)TaskStatusEnum.Completed;
                    if(task.SubTypeId == (int)TaskSubTypeEnum.PostActivityReview)
                    {
                        task.Comments = model.ManagingSpecialistComments;
                    
                         var reviewResult = new PostActivityPerformanceReview
                         {
                           PostActivityId = model.Id,
                           RequiredFollowupComments = model.ManagingSpecialistComments,
                           ReviewDate = DateTime.Today,
                           IsReviewCompleted = model.ManagingSpecialistFollowup ?? false
                         };                
                        _context.PostActivityPerformanceReviews.Add( reviewResult);                  
                    }
                }
                _context.SaveChanges();
                return true;
            }
            else
                return false;
        }
        public  IList<AfsGroupsPostActivityViewModel> GetAovPostActivityVersions(int postActivityId)
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
            // NOTE: For not over-writing the object from previous to current - deep copy
            var currentPostActivty = (AfsGroupsPostActivityViewModel)(Get(preApprovalRequest.CurrentVersionPostActivityId).GetClone());
            var model = new List<AfsGroupsPostActivityViewModel>()
            {
                currentPostActivty,
                Get(preApprovalRequest.PreviousVersionPostActivityId)
            };

            return model;
        }

        public override IList<AfsGroupsPostActivityViewModel> GetGroupThreePostActivityVersions(int postActivityId)
        {
           throw new NotImplementedException();
        }

        public override bool CheckIsAutoPreApproval(PreApprovalRequestViewModel model)
        {
             var preApprovalRequest = _context.PreApprovalRequests
                    .Include(par => par.PreApprovalRequestFunctionCodes)                    
                    .ThenInclude(fc => fc.FunctionCode)                    
                    .Where(par => par.Id == model.Id).FirstOrDefault();
                if(preApprovalRequest == null)
                {
                    return false;
                }

                var cloaFunctionCodes = _context.CloaFunctionCodes
                                            .Where(c => c.Cloa.ApplicationId == preApprovalRequest.ApplicationId && c.Cloa.CloaStatusId == (int)CloaStatusEnum.Active)
                                            .Include(c => c.FunctionCode)                                            
                                            .Select(c => c).ToArray();
                var flatCloaFunctionCodes = cloaFunctionCodes
                                            .SelectMany(c => new List<FunctionCodeViewModel>
                                                                {
                                                                    new FunctionCodeViewModel
                                                                    {
                                                                        FunctionCodeId = c.FunctionCodeId,
                                                                        IsAutomaticPreapproval = c.IsAutoPreApproval
                                                                    }
                                                                }.ToArray()
                                                        ).ToArray();

                var preApprovalFunctionCodes = preApprovalRequest.PreApprovalRequestFunctionCodes
                                                .SelectMany(p => new List<FunctionCodeViewModel>
                                                                {
                                                                    new FunctionCodeViewModel
                                                                    {
                                                                        FunctionCodeId = p.FunctionCodeId
                                                                    }
                                                                }.ToArray()
                                                            ).ToArray();

                if (flatCloaFunctionCodes.Any(c => !c.IsAutomaticPreapproval.Value &&
                   (preApprovalFunctionCodes.Any(p => p.FunctionCodeId == c.FunctionCodeId))))
                {
                    return false;
                }
                return true;
        }

        public override PreApprovalRequestViewModel SavePostActivityEvaluation(PreApprovalRequestViewModel adminModel)
        {
            throw new NotImplementedException();
        }
    }
}
