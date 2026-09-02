using System;
using System.Collections.Generic;
using System.Linq;
using Dms.Core.EntityFramework.Data;
using Dms.Core.EntityFramework.Model.Activity;
using Dms.Core.Utils;
using Dms.Services.Interface.Activity;
using Dms.Services.Interface.Message;
using Dms.Services.Interface.Shared;
using Dms.Services.Interface.Task;
using Dms.Services.ViewModel;
using Dms.Services.ViewModel.Activity;
using Dms.Services.ViewModel.Task;
using Dms.Core.Extensions;
using Dms.Core.EntityFramework.Model.Shared;
using Microsoft.EntityFrameworkCore;
using Dms.Services.Interface.Lookup;
using Dms.Services.ViewModel.Shared;
using Dms.Services.Assembler;
using Dms.Services.Interface.Security;
using Newtonsoft.Json;
using Dms.Services.ViewModel.Security;
using Dms.Services.ViewModel.Utils;
using PrimeNG.TableFilter;
using Newtonsoft.Json.Linq;
using Dms.Services.ViewModel.Lookup;
using System.Linq.Expressions;
using Dms.Core.EntityFramework.Model.Lookup;
using Microsoft.Linq.Translations;
using System.Globalization;

namespace Dms.Services.Implementation.Activity
{
    public class ManufacturingPreApprovalRequestService : PreApprovalRequestBaseService
    {
        public ManufacturingPreApprovalRequestService(DmsContext context, ITaskService taskService, IDocumentService documentService, IActivityService activityService, IMessageService messageService, ISharedService sharedService, ILookupService lookupService, IUserService userService)
        : base(context, taskService, documentService, activityService, messageService, sharedService, lookupService, userService)
        {
            _designeeType = (int)DesigneeTypeEnum.DMIR;
        }
        public override void CreateTask(PreApprovalRequestViewModel model)
        {
            base.CreateTask(model);
        }
        public override PreApprovalRequestViewModel Get(int preApprovalRequestId, bool loadPreapprovalModifiedData, int postActivityId, int? cloaId = null)
        {
            _preApprovalRequest = CloaPreApprovalRequestViewModelMapper.GetPreApprovalRequestManufacturing(_context, preApprovalRequestId);
            _cloa = CloaPreApprovalRequestViewModelMapper.GetManufacturingEntitytoViewModel(_context, _preApprovalRequest);
            _cloa.DesigneeInfo.Status = _sharedService.GetDesigneeInfoByCloa(_preApprovalRequest.CloaId).Status;
            _cloa.RequestInfo.SubmittedDate = _preApprovalRequest.SubmittedDate.HasValue ? _preApprovalRequest.SubmittedDate.Value.ToString("MM/dd/yyyy  HH:mm tt") : String.Empty;
            _cloa.RequestInfo.RevisedDate = _preApprovalRequest.AfsPreApprovalRequest != null && _preApprovalRequest.AfsPreApprovalRequest.RevisedDate != null ? _preApprovalRequest.AfsPreApprovalRequest.RevisedDate.Value.ToString("MM/dd/yyyy HH:mm tt") : String.Empty;

            var approvalDate = _context.PostActivities.Where(e => e.PreApprovalRequestId == preApprovalRequestId).OrderByDescending(po => po.Id)
                .Select(e => e.ApprovalDate).FirstOrDefault();
            _cloa.RequestInfo.DecisionDate = approvalDate != null ? approvalDate.HasValue ? approvalDate.Value.ToString("MM/dd/yyyy HH:mm tt") : string.Empty : string.Empty;
            _cloa.RequestInfo.DecisionBy = _preApprovalRequest.GeoExpansionSelectedApprover != null?
                                            _preApprovalRequest.GeoExpansionSelectedApprover.User.Profile.ToFullName() :
                                            _preApprovalRequest.ApproverOfficeRole != null ? _preApprovalRequest.ApproverOfficeRole.User.Profile.ToFullName() : string.Empty;
            _preApprovalRequestViewModel = base.Get(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId, cloaId);
            _preApprovalRequestViewModel.IsAfsType = false;
            var otherPreApprovalRequest = CloaPreApprovalRequestViewModelMapper.GetOtherPreApprovalRequestViewModel(_preApprovalRequest);
            otherPreApprovalRequest.PreApprovalRequestExperimental = _cloa.PreApprovalExperimentalPurposes;
            otherPreApprovalRequest.IsAutoPostActivity = !(_cloa.SelectedOtherFunctionCodes.Any(a => a.IsAutoPostActivity.HasValue && !a.IsAutoPostActivity.Value));
            _preApprovalRequestViewModel.DocumentReference = _documentService.GetDocumentsByRef((int)DocumentReferenceEnum.PreapprovalRequest, preApprovalRequestId, null);
            _preApprovalRequestViewModel.SelectedFunctionCodes = _cloa.SelectedOtherFunctionCodes != null ? _cloa.SelectedOtherFunctionCodes.Select(it => it.Id).ToList() : new List<int>();
            _preApprovalRequestViewModel.SelectedOtherFunctionCodes = _cloa.SelectedOtherFunctionCodes;
            _preApprovalRequestViewModel.OtherPreApprovalRequest = otherPreApprovalRequest;
            _preApprovalRequestViewModel.DesigneeFunctionCodes = _cloa.DesigneeTypeId == (int)DesigneeTypeEnum.DMIR ? _cloa.DesigneeFunctionCodes.DistinctBy(f => f.Id).ToList() : _cloa.DesigneeFunctionCodes;

            PopulateProxy(preApprovalRequestId);
            PopulatePostActivity(preApprovalRequestId);

            base.GetHelper(_preApprovalRequestViewModel);

            return _preApprovalRequestViewModel;
        }

        private void PopulateProxy(int preApprovalRequestId)
        {
            var decisionTask = _context.Tasks.Where(t => t.ActionId == preApprovalRequestId && t.SubTypeId == (int)TaskSubTypeEnum.PreApprovalRequest).FirstOrDefault();
            if (decisionTask != null)
            {
                _preApprovalRequestViewModel.DecisionTaskUserOfficeRoleIds = new List<int>();

                var msProxyList = _context.UserOfficeRoles.Where(u => u.IsActive &&
                                                                 u.FormData != null && u.IsProxyRequestApproved.Value &&
                                                                 JsonExtensions.JsonIntValue(u.FormData, "$.UserOfficeRoleId") == decisionTask.UserOfficeRoleId).ToList();

                if (msProxyList.Count > 0)
                {
                    foreach (var msProxy in msProxyList)
                    {

                        var formdData = JsonConvert.DeserializeObject<UserDelegationFormDataViewModel>(msProxy.FormData);

                        _preApprovalRequestViewModel.DecisionTaskUserOfficeRoleIds.Add(formdData.ProxyOriginalUserOfficeRoleId);
                        _preApprovalRequestViewModel.DecisionTaskUserOfficeRoleIds.Add(msProxy.Id);
                    }
                }

                _preApprovalRequestViewModel.DecisionTaskUserOfficeRoleIds.Add(decisionTask.UserOfficeRoleId);
            }
        }

        private void PopulatePostActivity(int preApprovalRequestId)
        {
            var poExist = _context.PostActivities.Where(e => e.PreApprovalRequestId == preApprovalRequestId).OrderByDescending(e => e.Id).Any();
               
            if (poExist)
            {
                _preApprovalRequestViewModel.PostActivity = CloaPreApprovalRequestViewModelMapper.GetPostActivityToPostActivityViewModel(base._preApprovalRequest);

                if (_context.PostActivities.Where(e => e.PreApprovalRequestId == preApprovalRequestId).OrderByDescending(po => po.Id).First().PostActivityPerformanceReview != null)
                {
                    _preApprovalRequestViewModel.PostActivity.PerformanceResult = CloaPreApprovalRequestViewModelMapper.GetPerformanceResultViewModel(base._preApprovalRequest);
                }
            }
        }

        private static void ApplyPreApprovalFilters(RequestListModel model, ref IQueryable<PreApprovalRequest> query)
        {
            if (model.PageModel.Filters == null)
                return;

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
                        query = query.Where(p => (p.SubmittedDate.HasValue ? p.SubmittedDate.Value.Date : p.CreatedDate.Date) == filterDate.Value.Date);
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

        private static void ApplySortField(RequestListModel model, ref IQueryable<PreApprovalRequest> query)
        {
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
        }

        public override PaginationViewModel<IList<ActivityPaperWorkViewModel>> GetPreApprovalList(RequestListModel model)
        {
            Expression<Func<PreApprovalRequest, bool>> applicationFilter = p => p.ApplicationId == model.ApplicationId,
            dateFilter = p => !model.IsOneYear || p.CreatedDate >= DateTime.Today.AddYears(-1);

            var query = _context.PreApprovalRequests
                        .Where(applicationFilter)
                        .Where(dateFilter);

            ApplyPreApprovalFilters(model, ref query);
            ApplySortField(model, ref query);            

            var data = query
                        .Select(p => new ActivityPaperWorkViewModel
                        {
                            Id = p.Id,
                            TrackingNumber = p.TrackingNumber,
                            ActivityStatus = p.PreApprovalRequestStatus != null ? p.PreApprovalRequestStatus.Name : string.Empty,
                            ModifiedDate = p.SubmittedDate ?? p.CreatedDate,
                            StatusId = p.StatusId,
                            ApplicantName = p.OtherPreApprovalRequest.ApplicantName,
                            TypeOfRequest = p.OtherPreApprovalRequest.Category != null ? p.OtherPreApprovalRequest.Category.Name : string.Empty,
                            ProposedStartDate = p.ProposeStartDate,
                            PreApprovalType = string.Join(", ", p.PreApprovalRequestFunctionCodes.Select(p1 => p1.FunctionCode.Name)),
                            DesigneeTypeId = model.IsInternal ? null : p.Application.DesigneeTypeId,
                            HasPostActivity = model.IsInternal || p.PostActivities.Count != 0,
                            PostActivityStatusId = model.IsInternal ? null : p.PostActivities.OrderByDescending(p1 => p1.Id).Select(p1 => p1.StatusId).FirstOrDefault()
                        }).PrimengTableFilter(model.PageModel, out int totalRecords)
                        .ToArray();

            data = data.Select(p => 
                {
                    var route =  SetPreApprovalUrl(p.Id, p.StatusId.Value, model.IsInternal);
                    p.Url = route.Url;
                    p.IsNewWindow = route.IsNewWindow;
                    p.HasNavigation = !string.IsNullOrWhiteSpace(route.Url);
                    if (!model.IsInternal)
                    {
                        p.Cancel = IsCancel(p);
                    }
                    return p;
                }).ToArray();

            return new PaginationViewModel<IList<ActivityPaperWorkViewModel>>(totalRecords, data);
        }
        private static string IsCancel(ActivityPaperWorkViewModel p)
        {
            return (p.StatusId == (int)PreApprovalRequestStatusEnum.Approved && p.HasPostActivity && p.PostActivityStatusId == (int)PreApprovalRequestStatusEnum.Completed)
                                    || p.StatusId == (int)PreApprovalRequestStatusEnum.Canceled
                                    || p.StatusId == (int)PreApprovalRequestStatusEnum.Completed
                                    || p.StatusId == (int)PreApprovalRequestStatusEnum.Rejected
                                    || (p.IsApproved.HasValue && !p.IsApproved.Value)
                                        ? null
                                            : p.Id.ToString();
        }

        private static void ApplyPostActivityFilters(RequestListModel model, ref IQueryable<PreApprovalRequest> query)
        {
            if (model.PageModel.Filters == null)
                return;

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
                        query = query.Where(p => (p.LatestPostActivity.PostActivityPaperWorkDate.HasValue ? p.LatestPostActivity.PostActivityPaperWorkDate.Value.Date : null) == filterDate.Value.Date);
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

        public override PaginationViewModel<IList<ActivityPaperWorkViewModel>> GetPostActivityList(RequestListModel model)
        {
            Expression<Func<PreApprovalRequest, bool>> applicationFilter = p => p.ApplicationId == model.ApplicationId,
            dateFilter = p => !model.IsOneYear || p.CreatedDate >= DateTime.Today.AddYears(-1);
            var query = _context.PreApprovalRequests
                        .Where(applicationFilter)
                        .Where(dateFilter)
                        .Where(p => p.LatestPostActivity != null);

            ApplyPostActivityFilters(model, ref query);
            
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
                            ApplicantName = p.OtherPreApprovalRequest.ApplicantName,
                            ActivityStatus = p.LatestPostActivity.PostActivitytStatus.Name,
                            StatusId = p.LatestPostActivity.StatusId,
                            DueDate = p.ProposeEndDate.HasValue ? p.ProposeEndDate.Value.AddDays(7) : null,
                            SubmissionDate = p.LatestPostActivity.PostActivityPaperWorkDate,
                            ProposedStartDate = p.ProposeStartDate,
                            PreApprovalRequestId = p.LatestPostActivity.PreApprovalRequestId,
                            TypeOfRequest = p.OtherPreApprovalRequest.Category != null ? p.OtherPreApprovalRequest.Category.Name : string.Empty,
                            PreApprovalType = string.Join(", ", p.PreApprovalRequestFunctionCodes.Select(p1 => p1.FunctionCode.Name)),
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
                            var url = SetPostActivityUrl(d.PreApprovalRequestId, d.StatusId, model.IsInternal);
                            d.Url = url.Url;
                            d.IsNewWindow = url.IsNewWindow;
                            d.HasNavigation = !string.IsNullOrWhiteSpace(url.Url);
                            return d;
                        }).ToArray();
                        
            return new PaginationViewModel<IList<ActivityPaperWorkViewModel>>(totalRecords, data);
        }

        private static LinkViewModel SetPreApprovalUrl(int id, int statusId, bool isInternal)
        {
            var url = string.Empty;
            var isNewWindow = false;
            if (isInternal)
            {
                
                if (statusId is not ((int)PreApprovalRequestStatusEnum.Saved) and
                    not ((int)PreApprovalRequestStatusEnum.Initiated))
                {
                    url = $"/preapprovalrequest/evaluate/{CryptoExtensions.Encrypt(id)}";
                }     
            } 
            else 
            {
                url = $"/preapprovalrequest/{CryptoExtensions.Encrypt(id)}";
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
                if (statusId is ((int)PreApprovalRequestStatusEnum.Completed) or ((int)PreApprovalRequestStatusEnum.Pending) or ((int)PreApprovalRequestStatusEnum.Rejected) or ((int)PreApprovalRequestStatusEnum.Approved))
                {
                    url = $"/performanceresult/{id}";
                }
            }
            else
            {
                url = $"/postactivity/{id.Encrypt()}";
            }

            return new LinkViewModel
            {
                Code = "PO",
                Url = url,
                IsNewWindow = isNewWindow
            };
        }

        public override PreApprovalRequestViewModel GetNew(int applicationId)
        {
            _cloa = CloaPreApprovalRequestViewModelMapper.GetNewCloaEntitytoViewModel(_context, applicationId);
            base.GetNew(applicationId);
            GetNewHelp(applicationId);
            _preApprovalRequestViewModel.OtherPreApprovalRequest = new OtherPreApprovalRequestViewModel
            {
                Address = new AddressViewModel()
            };

            //end line 236
            _preApprovalRequestViewModel.DesigneeFunctionCodes = _cloa.DesigneeInformation.TypeId == (int)DesigneeTypeEnum.DMIR ? _cloa.DesigneeFunctionCodes.DistinctBy(f => f.Id).ToList() : _cloa.DesigneeFunctionCodes;
            _preApprovalRequestViewModel.IsAfsType = false;
            _preApprovalRequestViewModel.PreApprovalDateWiseCount = GetPreApprovalDateWiseCount(_cloa.DesigneeInformation.ApplicationId);
            return _preApprovalRequestViewModel;
        }

        public override int ReInitiate(int preApprovalRequestId)
        {
            return base.ReInitiate(preApprovalRequestId);
        }
        private static DateTime? GetProposedStartDate(PreApprovalRequestViewModel model) => string.IsNullOrEmpty(model.OtherPreApprovalRequest.ActivityFromDate) ? null : DateTime.Parse(model.OtherPreApprovalRequest.ActivityFromDate);
        private static DateTime? GetProposedEndDate(PreApprovalRequestViewModel model) => string.IsNullOrEmpty(model.OtherPreApprovalRequest.ActivityToDate) ? null : DateTime.Parse(model.OtherPreApprovalRequest.ActivityToDate);
        public override PreApprovalRequestViewModel Save(PreApprovalRequestViewModel model)
        {
            base.Save(model);

            _preApprovalRequest.ProposeStartDate = GetProposedStartDate(model);
            _preApprovalRequest.ProposeEndDate = GetProposedEndDate(model);
            if (model.Id != 0)
            {

                _preApprovalRequest.PreApprovalRequestFunctionCodes = model.SelectedOtherFunctionCodes.Select(f => new PreApprovalRequestFunctionCode
                {
                    FunctionCodeId = f.Id,
                    IsCloaFunctionCode = true
                }).ToList();
                if (model.OtherPreApprovalRequest.PreApprovalRequestExperimental != null)
                {
                    foreach (var ex in _preApprovalRequest.PreApprovalRequestExperimentals.ToList())
                    {
                        _preApprovalRequest.PreApprovalRequestExperimentals.Remove(ex);
                    }

                    _preApprovalRequest.PreApprovalRequestExperimentals = model.OtherPreApprovalRequest.PreApprovalRequestExperimental.Select(e => new PreApprovalRequestExperimental
                    {
                        PreApprovalRequestExperimentalTypeId = e.PreapprovalRequestExperimentalTypeId,
                        PreapprovalRequestExperimentalCategoryTypeId = e.PreapprovalRequestExperimentalCategoryTypeId,
                        OtherText = e.OtherText
                    }).ToList();
                }
                _preApprovalRequest.OtherPreApprovalRequest.CategoryId = model.OtherPreApprovalRequest.SelectedPreApprovalRequestTypeId;
                _preApprovalRequest.OtherPreApprovalRequest.PreApprovalRequestSelectTypeId = model.OtherPreApprovalRequest.PreApprovalSelectTypeId;
                _preApprovalRequest.OtherPreApprovalRequest.PreApprovalRequestSelectSubTypeId = model.OtherPreApprovalRequest.PreApprovalSelectSubTypeId;
                _preApprovalRequest.OtherPreApprovalRequest.AirportId = model.OtherPreApprovalRequest.Airport?.Id;
                _preApprovalRequest.OtherPreApprovalRequest.IsOutsideOfficeDistrict = model.OtherPreApprovalRequest.IsOutSideArea;
                _preApprovalRequest.OtherPreApprovalRequest.AirportName = model.OtherPreApprovalRequest.AirportName;
                _preApprovalRequest.OtherPreApprovalRequest.ApplicantName = model.OtherPreApprovalRequest.ApplicantName;
                _preApprovalRequest.OtherPreApprovalRequest.ApplicantPhone = model.OtherPreApprovalRequest.ApplicantNumber;
                _preApprovalRequest.OtherPreApprovalRequest.AwcApplicationNumber = model.OtherPreApprovalRequest.AwcApplicationNumber;
                _preApprovalRequest.OtherPreApprovalRequest.Comments = model.OtherPreApprovalRequest.Comments;
                _preApprovalRequest.OtherPreApprovalRequest.AircraftRegistrationNumber = model.OtherPreApprovalRequest.AirCraftRegNumber;
                _preApprovalRequest.OtherPreApprovalRequest.AircraftMake = model.OtherPreApprovalRequest.AirCraftMake;
                _preApprovalRequest.OtherPreApprovalRequest.AircraftModel = model.OtherPreApprovalRequest.AirCraftModel;
                _preApprovalRequest.OtherPreApprovalRequest.ComponentName = model.OtherPreApprovalRequest.ComponentName;
                _preApprovalRequest.OtherPreApprovalRequest.ComponentNumber = model.OtherPreApprovalRequest.ComponentNumber;
                _preApprovalRequest.OtherPreApprovalRequest.NacipFormLoginNumber = model.OtherPreApprovalRequest.NacipFormLoginNumber;
                _preApprovalRequest.OtherPreApprovalRequest.OfficeId = model.OtherPreApprovalRequest.SelectedOfficeId;
                _preApprovalRequest.OtherPreApprovalRequest.RegisteredOwner = model.OtherPreApprovalRequest.RegisteredOwner;
                _preApprovalRequest.OtherPreApprovalRequest.RegisteredOwnerPhone = model.OtherPreApprovalRequest.RegisteredOwnerPhone;
                _preApprovalRequest.OtherPreApprovalRequest.Address.Name = model.OtherPreApprovalRequest.Address.Name;
                _preApprovalRequest.OtherPreApprovalRequest.Address.AddressLine1 = model.OtherPreApprovalRequest.Address.Address1;
                _preApprovalRequest.OtherPreApprovalRequest.Address.AddressLine2 = model.OtherPreApprovalRequest.Address.Address2;
                _preApprovalRequest.OtherPreApprovalRequest.Address.City = model.OtherPreApprovalRequest.Address.City;
                _preApprovalRequest.OtherPreApprovalRequest.Address.StateId = model.OtherPreApprovalRequest.Address.State?.Id;
                _preApprovalRequest.OtherPreApprovalRequest.Address.ZipCode = model.OtherPreApprovalRequest.Address.ZipCode;
                _preApprovalRequest.OtherPreApprovalRequest.IsOtherAirport = model.OtherPreApprovalRequest.IsOtherAirport;
                _preApprovalRequest.OtherPreApprovalRequest.OtherAirportName = model.OtherPreApprovalRequest.OtherAirportName;
                _preApprovalRequest.OtherPreApprovalRequest.IsUsedRemoteVideo = model.OtherPreApprovalRequest.IsUsedRemoteVideo;
                _preApprovalRequest.OtherPreApprovalRequest.RemoteVideoComments = model.OtherPreApprovalRequest.RemoteVideoComments;
            }
            else
            {
                _preApprovalRequest.PreApprovalRequestFunctionCodes = model.SelectedOtherFunctionCodes.Select(f => new PreApprovalRequestFunctionCode
                {
                    FunctionCodeId = f.Id,
                    IsCloaFunctionCode = true
                }).ToList();
                _preApprovalRequest.PreApprovalRequestExperimentals = model.OtherPreApprovalRequest.PreApprovalRequestExperimental.Select(e => new PreApprovalRequestExperimental
                {
                    PreApprovalRequestExperimentalTypeId = e.PreapprovalRequestExperimentalTypeId,
                    PreapprovalRequestExperimentalCategoryTypeId = e.PreapprovalRequestExperimentalCategoryTypeId,
                    OtherText = e.OtherText
                }).ToList();
                _preApprovalRequest.OtherPreApprovalRequest = new OtherPreApprovalRequest
                {
                    CategoryId = model.OtherPreApprovalRequest.SelectedPreApprovalRequestTypeId,
                    PreApprovalRequestSelectTypeId = model.OtherPreApprovalRequest.PreApprovalSelectTypeId,
                    PreApprovalRequestSelectSubTypeId = model.OtherPreApprovalRequest.PreApprovalSelectSubTypeId,
                    AirportId = model.OtherPreApprovalRequest.Airport?.Id,
                    IsOutsideOfficeDistrict = model.OtherPreApprovalRequest.IsOutSideArea,
                    AirportName = model.OtherPreApprovalRequest.AirportName,
                    ApplicantName = model.OtherPreApprovalRequest.ApplicantName,
                    ApplicantPhone = model.OtherPreApprovalRequest.ApplicantNumber,
                    AwcApplicationNumber = model.OtherPreApprovalRequest.AwcApplicationNumber,
                    Comments = model.OtherPreApprovalRequest.Comments,
                    AircraftRegistrationNumber = model.OtherPreApprovalRequest.AirCraftRegNumber,
                    AircraftMake = model.OtherPreApprovalRequest.AirCraftMake,
                    AircraftModel = model.OtherPreApprovalRequest.AirCraftModel,
                    ComponentName = model.OtherPreApprovalRequest.ComponentName,
                    ComponentNumber = model.OtherPreApprovalRequest.ComponentNumber,
                    NacipFormLoginNumber = model.OtherPreApprovalRequest.NacipFormLoginNumber,
                    OfficeId = model.OtherPreApprovalRequest.SelectedOfficeId,
                    RegisteredOwner = model.OtherPreApprovalRequest.RegisteredOwner,
                    RegisteredOwnerPhone = model.OtherPreApprovalRequest.RegisteredOwnerPhone,
                    IsOtherAirport = model.OtherPreApprovalRequest.IsOtherAirport,
                    OtherAirportName = model.OtherPreApprovalRequest.OtherAirportName,
                    IsUsedRemoteVideo = model.OtherPreApprovalRequest.IsUsedRemoteVideo,
                    RemoteVideoComments = model.OtherPreApprovalRequest.RemoteVideoComments,
                    Address = new Address
                    {
                        Name = model.OtherPreApprovalRequest.Address.Name,
                        AddressLine1 = model.OtherPreApprovalRequest.Address.Address1,
                        AddressLine2 = model.OtherPreApprovalRequest.Address.Address2,
                        City = model.OtherPreApprovalRequest.Address.City,
                        StateId = model.OtherPreApprovalRequest.Address.State?.Id,
                        ZipCode = model.OtherPreApprovalRequest.Address.ZipCode
                    }
                };
                _context.PreApprovalRequests.Add(_preApprovalRequest);
            }
            _context.SaveChanges();
            model.Id = _preApprovalRequest.Id;

            if (model.isSubmit)
            {
                if (!IsAutoPreApprovalforDmirandDarf(model) && _preApprovalRequest.IsApproved != true)
                {
                    base.CreateTask(model);
                }
                else
                {
                    _preApprovalRequest.IsPreApprovalOnHold = false;
                }
            }
            else
            {
                _context.SaveChanges();
            }
            return model;
        }
        public override bool SaveMsDecision(PreApprovalRequestViewModel model)
        {

            _preApprovalRequest = _context.PreApprovalRequests.Include(p => p.OtherPreApprovalRequest)
                                  .Include(p => p.PreApprovalRequestFunctionCodes).Include(p => p.PostActivities).FirstOrDefault(p => p.Id == model.Id);
            var isSaved = false;
            if (_preApprovalRequest == null)
            {
                return isSaved;
            }

            if (model.OtherPreApprovalRequest.IsOutSideArea)
            {
                _preApprovalRequest.GeoMsIsApproved = model.GeoMsIsApprove;
                _preApprovalRequest.GeoExpansionSelectedOfficeId = model.GeoExpansionSelectedOfficeId;
                _preApprovalRequest.GeoExpansionSelectedApproverId = model.GeoExpansionSelectedApproverId;
                _preApprovalRequest.GeoExpansionJustification = model.GeoExpansionJustification;
                if (model.GeoMsIsApprove == true)
                {
                    var aoGeoPreApprpvalRequestTask = new TaskViewModel
                    {
                        TaskSubTypeId = (int)TaskSubTypeEnum.GeographicExpansionRecommendation,
                        TaskStatusId = (int)TaskStatusEnum.Pending,
                        UserOfficeRoleId = model.GeoExpansionSelectedApproverId.Value,
                        ActionId = model.Id,
                        ApplicationId = model.ApplicationId,
                    };

                    _taskService.CreateTask(aoGeoPreApprpvalRequestTask, true);

                }
                else
                {
                    _preApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Rejected;
                    //send denied notification.
                    SendPreapprovalDecisionNotification(model.TrackingNumber, model.DesigneeInfo.Name, model.DesigneeInfo.Id, "PRERJT");
                }
            }
            if (model.OtherPreApprovalRequest != null && !model.OtherPreApprovalRequest.IsOutSideArea)
            {
                _preApprovalRequest.IsApproved = model.IsApproved;
                _preApprovalRequest.ApproverComments = model.ApproverComment;
                _preApprovalRequest.ApproverJustification = model.ApproverJusitification;
                _preApprovalRequest.ApproverOfficeRoleId = model.ApprovedBy;

                if (model.IsApproved == true)
                {
                    _preApprovalRequest.IsPreApprovalOnHold = false;
                    _preApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Approved;
                    if (_context.PostActivities.Where(e => e.PreApprovalRequestId == _preApprovalRequest.Id).FirstOrDefault() == null)
                    {
                        _preApprovalRequest.PostActivities =
                            [
                                new PostActivity
                                {
                                    StatusId = (int)PreApprovalRequestStatusEnum.Initiated,
                                    TrackingNumber = model.TrackingNumber.Replace("PR", "PO"),
                                    ApprovalDate = DateTime.Now
                                }
                            ];
                    }
                    //Send approved notification.
                    SendPreapprovalDecisionNotification(model.TrackingNumber, model.DesigneeInfo.Name, model.DesigneeInfo.Id, "PREAPRMANU");
                }
                else
                {
                    _preApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Rejected;
                    //send denied notification.
                    SendPreapprovalDecisionNotification(model.TrackingNumber, model.DesigneeInfo.Name, model.DesigneeInfo.Id, "PRERJT");
                }
            }
            isSaved = SaveMsDecisionHelp(model);
            return isSaved;
        }

        private bool IsAutoPreApprovalforDmirandDarf(PreApprovalRequestViewModel model)
        {
            if (model.DesigneeTypeId == (int)DesigneeTypeEnum.DMIR || model.DesigneeTypeId == (int)DesigneeTypeEnum.DARF)
            {
                if(model.OtherPreApprovalRequest.IsUsedRemoteVideo.GetValueOrDefault())
                {
                    return false;
                }

                var preApprovalRequest = _context.PreApprovalRequests
                    .Include(par => par.OtherPreApprovalRequest)
                    .Where(par => par.Id == model.Id).FirstOrDefault();

                var cloa = _context.Cloas.Include(c => c.CloaFunctionCodes)
                        .Where(c => c.ApplicationId == preApprovalRequest.ApplicationId && c.CloaStatusId == (int)CloaStatusEnum.Active).FirstOrDefault();

                if (cloa == null)
                {
                    return false;
                }

                if (cloa.CloaFunctionCodes.Any(cfc => (!cfc.IsAutoPreApproval) && (preApprovalRequest.PreApprovalRequestFunctionCodes.Any(parfc => parfc.FunctionCodeId == cfc.FunctionCodeId))))
                {
                    return false;
                }

                preApprovalRequest.IsApproved = true;
                preApprovalRequest.ApproverComments = "Auto Pre Approval";
                preApprovalRequest.ApproverOfficeRoleId = model.ManagingSpecialist;
                preApprovalRequest.StatusId = (int)PreApprovalRequestStatusEnum.Approved;
                preApprovalRequest.PostActivities = new List<PostActivity>
                {
                    new PostActivity
                    {
                        StatusId = (int)PreApprovalRequestStatusEnum.Initiated,
                        TrackingNumber = preApprovalRequest.TrackingNumber.Replace("PR", "PO"),
                        ApprovalDate = DateTime.Now
                    }
                };
                //Complete the Task Status.
                CompleteTaskFromAutoPreApproval(preApprovalRequest.Id);
                _context.SaveChanges();
                return true;
            }
            else
            {
                return false;
            }
        }
        private void CompleteTaskFromAutoPreApproval(int preApprovalRequestId)
        {
            var task = _context.Tasks.FirstOrDefault(it => it.ActionId == preApprovalRequestId && it.StatusId == (int)TaskStatusEnum.Pending
                && it.SubTypeId == (int)TaskSubTypeEnum.PreApprovalRequest);
            if (task != null)
            {
                task.StatusId = (int)TaskStatusEnum.Completed;
                _context.SaveChanges();
            }
        }
        public override bool Cancel(ActivityPaperWorkViewModel model)
        {
            return base.Cancel(model);
        }
        public override bool SaveGeoGraphical(PreApprovalRequestViewModel model)
        {
            throw new NotImplementedException();
        }

        public override PreApprovalRequestViewModel SavePerformanceResults(PreApprovalRequestViewModel adminModel)
        {
            var numberOfCertificates = adminModel.PostActivity.QuantityOfCertificate.HasValue ? adminModel.PostActivity.QuantityOfCertificate.GetValueOrDefault() : adminModel.PostActivity.QuantityOfCertificate;
            if (adminModel.PostActivity.PerformanceResult.Id == 0)
            {
                var performanceResult = new PostActivityPerformanceReview
                {
                    PostActivityId = adminModel.PostActivity.Id,
                    PerformanceResultTypeId = adminModel.PostActivity.PerformanceResult.OverSightResultId,
                    TechnicalComments = adminModel.PostActivity.PerformanceResult.Technical,
                    ProfessionalComments = adminModel.PostActivity.PerformanceResult.Professional,
                    ProceduralComments = adminModel.PostActivity.PerformanceResult.Procedural,
                    RequiredFollowupComments = adminModel.PostActivity.PerformanceResult.FollowUpActions,
                    ReviewDate = adminModel.PostActivity.PerformanceResult.ReviewDate != null ? DateTime.Parse(adminModel.PostActivity.PerformanceResult.ReviewDate, CultureInfo.InvariantCulture) : (DateTime?)null,
                    IsReviewCompleted = adminModel.PostActivity.PerformanceResult.CompletedReview
                };
                var PostActivity = _context.PostActivities.First(a => a.PreApprovalRequestId == adminModel.Id);
                if (PostActivity != null)
                {
                    PostActivity.NumberOfCertificates = numberOfCertificates;
                }
                _context.PostActivityPerformanceReviews.Add(performanceResult);
                _context.SaveChanges();
                adminModel.PostActivity.PerformanceResult.Id = performanceResult.Id;
            }
            else
            {
                var performanceResult = _context.PostActivityPerformanceReviews.Include(pr => pr.PostActivity).FirstOrDefault(r => r.Id == adminModel.PostActivity.PerformanceResult.Id);
                if(performanceResult != null){
                    performanceResult.PostActivity.NumberOfCertificates = numberOfCertificates;
                    performanceResult.PerformanceResultTypeId = adminModel.PostActivity.PerformanceResult.OverSightResultId;
                    performanceResult.TechnicalComments = adminModel.PostActivity.PerformanceResult.Technical;
                    performanceResult.ProfessionalComments = adminModel.PostActivity.PerformanceResult.Professional;
                    performanceResult.ProceduralComments = adminModel.PostActivity.PerformanceResult.Procedural;
                    performanceResult.RequiredFollowupComments = adminModel.PostActivity.PerformanceResult.FollowUpActions;
                    performanceResult.ReviewDate = DateTime.Parse(DateTime.Now.ToShortDateString());
                    performanceResult.IsReviewCompleted = adminModel.PostActivity.PerformanceResult.CompletedReview;
                    _context.SaveChanges();
                }
            }
            if (adminModel.PostActivity.PerformanceResult.IsSubmit)
            {
                var postActivity = _context.PostActivities.First(p => p.Id == adminModel.PostActivity.Id);
                postActivity.StatusId = (int)PreApprovalRequestStatusEnum.Completed;
                var task = _context.Tasks.FirstOrDefault(t => t.ActionId == adminModel.Id && t.SubTypeId == (int)TaskSubTypeEnum.PostActivity);
                if (task != null)
                {
                    task.StatusId = (int)TaskStatusEnum.Completed;
                }
                _context.SaveChanges();
            }
            var otherPreApprovalRequest = base._context.OtherPreApprovalRequests.FirstOrDefault(r => r.PreApprovalRequestId == adminModel.Id);
            if(otherPreApprovalRequest != null){
                otherPreApprovalRequest.AwcApplicationNumber = adminModel.OtherPreApprovalRequest.AwcApplicationNumber;
                _context.SaveChanges();
            }
            return adminModel;
        }
        public override PreApprovalRequestViewModel SavePostActivityEvaluation(PreApprovalRequestViewModel adminModel)
        {

            var postActivity = base._context.PostActivities.Include(p => p.PreApprovalRequest).ThenInclude(o => o.OtherPreApprovalRequest).FirstOrDefault(r => r.Id == adminModel.PostActivity.Id);
            if(postActivity ==  null)
            {
                return adminModel;
            }
            postActivity.PreApprovalRequestId = adminModel.Id;
            postActivity.Comments = adminModel.PostActivity.AddiotionalComment;
            postActivity.DenialReason = adminModel.PostActivity.DenialComment;
            postActivity.NumberOfCertificates = adminModel.PostActivity.QuantityOfCertificate.HasValue ? adminModel.PostActivity.QuantityOfCertificate.GetValueOrDefault() : adminModel.PostActivity.QuantityOfCertificate;
            postActivity.PostActivityResultTypeId = adminModel.PostActivity.ResultId != null ? adminModel.PostActivity.ResultId : null;
            postActivity.CompletedDate = string.IsNullOrWhiteSpace(adminModel.PostActivity.DateActivityCompleted) ? DateTime.Now : adminModel.PostActivity.DateActivityCompleted.ToNullableOnlyDate();
            postActivity.StatusId = adminModel.PostActivity.IsSubmit ? (int)PreApprovalRequestStatusEnum.Pending : (int)PreApprovalRequestStatusEnum.Saved;
            postActivity.PreApprovalRequest.OtherPreApprovalRequest.AwcApplicationNumber = adminModel.OtherPreApprovalRequest.AwcApplicationNumber;

            if (adminModel.PostActivity.IsSubmit)
            {
                postActivity.PostActivityPaperWorkDate = DateTime.Now;

                if (adminModel.OtherPreApprovalRequest.SelectedPreApprovalRequestTypeId == (int)CategoryEnum.Tags || (adminModel.OtherPreApprovalRequest.IsAutoPostActivity))
                {
                    postActivity.StatusId = (int)PreApprovalRequestStatusEnum.Completed;
                }
                else
                {
                    var msPostActivityTask = new TaskViewModel
                    {
                        TaskSubTypeId = (int)TaskSubTypeEnum.PostActivity,
                        TaskStatusId = (int)TaskStatusEnum.Pending,
                        UserOfficeRoleId = adminModel.ManagingSpecialist,
                        ActionId = adminModel.Id,
                        ApplicationId = adminModel.ApplicationId,
                    };

                    _taskService.CreateTask(msPostActivityTask, true);

                }
            }
            _context.SaveChanges();
            return adminModel;
        }
        //Manufacturing deosnot have to implement copy
        public override PreApprovalRequestViewModel Copy(int id)
        {
            return _preApprovalRequestViewModel;
        }
        //Manufacturing deosnot Check Pending Post Activities Existance
        public override bool CheckPendingPostActivitiesExists(int applicationId)
        {
            return false;
        }

        public override bool SaveMsPostActivityReview(AfsGroupsPostActivityViewModel model)
        {
            throw new NotImplementedException();
        }

        public override IList<AfsGroupsPostActivityViewModel> GetGroupThreePostActivityVersions(int postActivityId)
        {
            throw new NotImplementedException();
        }

        protected override PreApprovalRequest GetOriginalPreApprovalData(int id)
        {
            throw new NotImplementedException();
        }

        public override bool CheckIsAutoPreApproval(PreApprovalRequestViewModel model)
        {
            throw new NotImplementedException();
        }
    }
}
