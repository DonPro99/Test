using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Dms.Core.EntityFramework.Data;
using Dms.Core.EntityFramework.Model.Activity;
using Dms.Core.EntityFramework.Model.Lookup;
using Dms.Core.EntityFramework.Model.Shared;
using Dms.Core.Extensions;
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
using Dms.Services.ViewModel.Shared;
using Microsoft.EntityFrameworkCore;

namespace Dms.Services.Implementation.Activity
{
    public class AfsGroupTwoPreApprovalRequestService : AfsPreApprovalRequestService
    {
        public AfsGroupTwoPreApprovalRequestService(DmsContext context, ITaskService taskService, IDocumentService documentService, IActivityService activityService, IMessageService messageService, ISharedService sharedService, ILookupService lookupService, IUserService userService)
        : base(context, taskService, documentService, activityService, messageService, sharedService, lookupService, userService)
        {
        }
        public override PreApprovalRequestViewModel Save(PreApprovalRequestViewModel model)
        {
            _preApprovalRequestViewModel = base.Save(model);

            return _preApprovalRequestViewModel;
        }
        public override PreApprovalRequestViewModel GetNew(int applicationId)
        {
            base.GetNew(applicationId);
            var tempauthorizations = new List<FunctionCodeViewModel>();
            var typeRatingTypes = _lookupService.LookupValues("typeRatings").Result.TypeRatings;
            var authorizations = _context.FunctionCodes.Include(c => c.Category).Where(x => x.DesigneeTypeId == _cloa.DesigneeInformation.TypeId
                                && x.IsActive
                                && x.FunctionCodeTypeId != (int)FunctionCodeTypeEnum.Admin && x.Id != 1319).ToArray();
            foreach (var item in authorizations)
            {
                bool flowControl = BuildTempAuthorizations(tempauthorizations, typeRatingTypes, authorizations, item);
                if (!flowControl)
                {
                    continue;
                }
            }
            _preApprovalRequestViewModel.TestCheckInformation = new PreApprovalTestCheckInformationViewModel()
            {
                RequestedAuthorizations = _preApprovalRequestViewModel.AuthFunctionCodes.Where(x => x.FunctionCodeTypeId != (int)FunctionCodeTypeEnum.Admin).ToArray(),
                TemporaryAuthorizations = tempauthorizations
            };

            GetAdminDpeSaeAuthorizations(_preApprovalRequestViewModel, _cloa.DesigneeFunctionCodes);
            //end line 213
            return _preApprovalRequestViewModel;
        }

        private bool BuildTempAuthorizations(List<FunctionCodeViewModel> tempauthorizations, IList<TypeRatingViewModel> typeRatingTypes, FunctionCode[] authorizations, FunctionCode item)
        {
            if (_cloa.DesigneeFunctionCodes.Any(x => x.Id == item.Id) && !_cloa.DesigneeFunctionCodes.Any(x => x.Id == item.Id && item.HasTypeRating))
            {
                return false;
            }

            var fnTypeRatings = _preApprovalRequestViewModel.AuthFunctionCodes.Any(x => x.Id == item.Id && x.HasTypeRating)
                    ? typeRatingTypes.Any(t => t.DesigneeTypeId == _cloa.DesigneeInformation.TypeId && t.CategoryId == item.CategoryId)
                        ? [.. typeRatingTypes.Where(t => t.DesigneeTypeId == _cloa.DesigneeInformation.TypeId && t.CategoryId == item.CategoryId)]
                        : [.. typeRatingTypes.Where(t => t.CategoryId == null)]
                    : new List<TypeRatingViewModel>();
            var funtionCode = _preApprovalRequestViewModel.AuthFunctionCodes.Any(x => x.Id == item.Id && x.HasTypeRating)
                                ? _preApprovalRequestViewModel.AuthFunctionCodes.First(x => x.Id == item.Id)
                                : null;
            tempauthorizations.Add(new FunctionCodeViewModel()
            {
                Id = item.Id,
                FunctionCode = item.Name,
                HasTypeRating = item.HasTypeRating,
                DesigneeTypeId = _cloa.DesigneeInformation.TypeId,
                Category = authorizations.First(f => item.Id == f.Id).Category != null ? new CategoryViewModel
                {
                    Id = authorizations.First(f => item.Id == f.Id).Category.Id,
                    Name = authorizations.First(f => item.Id == f.Id).Category.Name
                } : null,
                CategoryId = authorizations.First(f => item.Id == f.Id).CategoryId ?? 0,
                TypeRatings = funtionCode != null && item.HasTypeRating
                      ? fnTypeRatings.Where(t => !funtionCode.TypeRatings.Any(ft => ft.value == t.Id)).Select(t => new LookupItem
                      {
                          label = t.Name,
                          value = t.Id,
                      }).ToList()
                      : []
            });
            return true;
        }

        public override PreApprovalRequestViewModel Get(int preApprovalRequestId, bool loadPreapprovalModifiedData, int postActivityId, int? cloaId = null)
        {
            var preApprovalRequest = base.Get(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId, cloaId);
            GetAfsHelper(preApprovalRequest, postActivityId, loadPreapprovalModifiedData);
            {
                List<PreApprovalCertificateRatingTypeViewModel> certificateRatingTypes = GetPreApprovalCertificateTypes();
                _preApprovalRequestViewModel.ApplicationInformation.PreApprovalCertificateRatingType = certificateRatingTypes;
                _preApprovalRequestViewModel.ApplicationInformation.SelectedCertificateRatingTypeIds = _preApprovalRequest.PreApprovalRequestCertificateRatings
                                                                                                                        .Select(x => x.PreApprovalRequestCertificateRatingTypeId).ToList();
                SetAuthorizations(cloaId);
                GetAdminDpeSaeAuthorizations(_preApprovalRequestViewModel, _cloa.DesigneeFunctionCodes);
            }
            GetAfsHelperTwo(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId);
            if (loadPreapprovalModifiedData && _context.PostActivities.Where(e => e.PreApprovalRequestId == _preApprovalRequest.Id).Any() && _preApprovalRequestViewModel?.AfsPostActivity?.AfsPostActivityModifiedPreApprovalViewModel != null)
            {
                LoadModifiedDataForAfsGroup2(_preApprovalRequestViewModel, _preApprovalRequestViewModel.AfsPostActivity, _cloa.DesigneeInfo.TypeId, _preApprovalRequestViewModel.ModifiedPreapprovalControls);
            }
            CheckIfActivityOutside(_preApprovalRequestViewModel);
            return _preApprovalRequestViewModel;
        }

        private void SetAuthorizations(int? cloaId)
        {
            List<FunctionCodeViewModel> tempauthorizations = GetTempAuthorizations();

            var loadSelectedFunctionCodes = cloaId == null || _preApprovalRequest.CloaId == cloaId;
            _preApprovalRequestViewModel.TestCheckInformation.SelectedRequestAuthorizations = loadSelectedFunctionCodes ? _preApprovalRequest.PreApprovalRequestFunctionCodes.Where(x => x.IsCloaFunctionCode.HasValue && x.IsCloaFunctionCode.Value)
                                                                                                        ?.Select(p => new FunctionCodeViewModel
                                                                                                        {
                                                                                                            Id = p.FunctionCode.Id,
                                                                                                            FunctionCode = p.FunctionCode.Name,
                                                                                                            HasTypeRating = p.FunctionCode.HasTypeRating,
                                                                                                            TypeRatings = p.FunctionCode.HasTypeRating ? _cloa.DesigneeFunctionCodes.First(df => df.Id == p.FunctionCodeId).TypeRatings?.ToList() : null,
                                                                                                            Category = p.FunctionCode.Category != null ? new CategoryViewModel
                                                                                                            {
                                                                                                                Id = p.FunctionCode.Category.Id,
                                                                                                                Name = p.FunctionCode.Category.Name
                                                                                                            } : null,
                                                                                                            CategoryId = p.FunctionCode.CategoryId ?? 0,
                                                                                                            SelectedTypeRatings = p.PreApprovalRequestFunctionCodeTypeRatings?.Select(pm => pm.TypeRatingId).ToArray()
                                                                                                        }).ToArray()
                                                                                                    : null;
            _preApprovalRequestViewModel.TestCheckInformation.SelectedTemporaryAuthorizations = loadSelectedFunctionCodes ? _preApprovalRequest.PreApprovalRequestFunctionCodes.Where(x => x.IsCloaFunctionCode.HasValue && !x.IsCloaFunctionCode.Value)
                                                                                                        ?.Select(p => new FunctionCodeViewModel
                                                                                                        {
                                                                                                            Id = p.FunctionCode.Id,
                                                                                                            FunctionCode = p.FunctionCode.Name,
                                                                                                            DesigneeTypeId = p.FunctionCode.DesigneeTypeId,
                                                                                                            HasTypeRating = p.FunctionCode.HasTypeRating,
                                                                                                            Category = p.FunctionCode.Category != null ? new CategoryViewModel
                                                                                                            {
                                                                                                                Id = p.FunctionCode.Category.Id,
                                                                                                                Name = p.FunctionCode.Category.Name
                                                                                                            } : null,
                                                                                                            CategoryId = p.FunctionCode.CategoryId ?? 0,
                                                                                                            SelectedTypeRatings = p.PreApprovalRequestFunctionCodeTypeRatings?.Select(pm => pm.TypeRatingId).ToArray()
                                                                                                        }).ToArray()
                                                                                                        : null;
            _preApprovalRequestViewModel.TestCheckInformation.RequestedAuthorizations = _cloa.DesigneeFunctionCodes.Where(x => x.FunctionCodeTypeId != (int)FunctionCodeTypeEnum.Admin).ToArray();
            _preApprovalRequestViewModel.TestCheckInformation.TemporaryAuthorizations = tempauthorizations;
        }

        [SuppressMessage("Major Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Temporary authorization derivation intentionally remains in one method to preserve existing eligibility and type-rating behavior.")]
        private List<FunctionCodeViewModel> GetTempAuthorizations()
        {
            var tempauthorizations = new List<FunctionCodeViewModel>();
            var typeRatingTypes = _lookupService.LookupValues("typeRatings").Result.TypeRatings;
            var authorizations = _context.FunctionCodes.Include(x => x.Category).Where(x => x.DesigneeTypeId == _cloa.DesigneeTypeId && x.IsActive && x.FunctionCodeTypeId != (int)FunctionCodeTypeEnum.Admin && x.Id != 1319).ToArray();
            foreach (var item in authorizations)
            {
                if (!_cloa.DesigneeFunctionCodes.Any(x => x.Id == item.Id) ||
                    _cloa.DesigneeFunctionCodes.Any(x => x.Id == item.Id && item.HasTypeRating))
                {
                    var fnTypeRatings = _preApprovalRequestViewModel.AuthFunctionCodes.Any(x => x.Id == item.Id && x.HasTypeRating)
                        ? typeRatingTypes.Any(t => t.DesigneeTypeId == _cloa.DesigneeInfo.TypeId && t.CategoryId == item.CategoryId)
                            ? typeRatingTypes.Where(t => t.DesigneeTypeId == _cloa.DesigneeInfo.TypeId && t.CategoryId == item.CategoryId).ToList()
                            : typeRatingTypes.Where(t => t.CategoryId == null).ToList()
                        : new List<TypeRatingViewModel>(); var funtionCode = _preApprovalRequestViewModel.AuthFunctionCodes.Any(x => x.Id == item.Id && x.HasTypeRating)
                        ? _preApprovalRequestViewModel.AuthFunctionCodes.First(x => x.Id == item.Id)
                        : null;
                    tempauthorizations.Add(new FunctionCodeViewModel()
                    {
                        Id = item.Id,
                        FunctionCode = item.Name,
                        DesigneeTypeId = item.DesigneeTypeId,
                        HasTypeRating = item.HasTypeRating,
                        Category = authorizations.First(f => item.Id == f.Id).Category != null
                            ? new CategoryViewModel
                            {
                                Id = authorizations.First(f => item.Id == f.Id).Category.Id,
                                Name = authorizations.First(f => item.Id == f.Id).Category.Name
                            }
                            : null,
                        CategoryId = authorizations.First(f => item.Id == f.Id).CategoryId ?? 0,
                        TypeRatings = funtionCode != null && item.HasTypeRating
                            ? fnTypeRatings.Where(t => !funtionCode.TypeRatings.Any(ft => ft.value == t.Id)).Select(
                                t => new LookupItem
                                {
                                    label = t.Name,
                                    value = t.Id,
                                }).ToList()
                            : new List<LookupItem>()
                    });
                }
            }

            return tempauthorizations;
        }

        private List<PreApprovalCertificateRatingTypeViewModel> GetPreApprovalCertificateTypes()
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

            return certificateRatingTypes;
        }

        protected void GetAdminDpeSaeAuthorizations(PreApprovalRequestViewModel preApprovalRequestViewModel, List<FunctionCodeViewModel> cloaFunctionCodes)
        {
            preApprovalRequestViewModel.TestCheckInformation.AdminDpeSaeFunctionCodes = cloaFunctionCodes.Where(x => x.FunctionCodeTypeId == (int)FunctionCodeTypeEnum.Admin || (x.CategoryId == (int) CategoryEnum.Other && x.FunctionCode.EndsWith("-SMFT"))).Select(c => new FunctionCodeViewModel
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
        public override void CreateTask(PreApprovalRequestViewModel model)
        {
            base.CreateTask(model);
        }
        public override bool SaveMsDecision(PreApprovalRequestViewModel model)
        {
            return base.SaveMsDecision(model);
        }
        public override int ReInitiate(int preApprovalRequestId)
        {
            return base.ReInitiate(preApprovalRequestId);
        }
        public override PreApprovalRequestViewModel SavePerformanceResults(PreApprovalRequestViewModel adminModel)
        {
            return base.SavePerformanceResults(adminModel);
        }

        public override PreApprovalRequestViewModel SavePostActivityEvaluation(PreApprovalRequestViewModel adminModel)
        {
            var model = base.SavePostActivityEvaluation(adminModel);
            var postActivity = _postActivity;
            if (model.DesigneeInfo.TypeId == (int)DesigneeTypeEnum.SAE || model.DesigneeInfo.TypeId == (int)DesigneeTypeEnum.DPE || model.DesigneeInfo.TypeId == (int)DesigneeTypeEnum.ADMINPE || model.DesigneeInfo.TypeId == (int)DesigneeTypeEnum.ADMINPEGEN)
            {
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
                postActivity.ActualStartDate = model.AfsPostActivity.AfsPostActivityGroup2.ActualStartDate.ToNullableDate();
                postActivity.ActualEndDate = model.AfsPostActivity.AfsPostActivityGroup2.ActualEndDate.ToNullableDate();
                postActivity.GroundPortionDuration = model.AfsPostActivity.AfsPostActivityGroup2.DurationOfGroundPortion;
                postActivity.FlightPortionDuration = model.AfsPostActivity.AfsPostActivityGroup2.DurationOfFlightPortion;
                postActivity.FstdSimulatorPortionDuration = model.AfsPostActivity.AfsPostActivityGroup2.DurationOfFstdSimulatorPortion;
                postActivity.ApplicantPhone = model.AfsPostActivity.AfsPostActivityGroup2.PhoneNumber;
                postActivity.ApplicantName = model.AfsPostActivity.AfsPostActivityGroup2.NameOfApplicant;
                postActivity.ApplicantEmail = model.AfsPostActivity.AfsPostActivityGroup2.Email;
                postActivity.ApplicantCertificateNumber = model.AfsPostActivity.AfsPostActivityGroup2.CertificateNumberOfApplicant;
                postActivity.AirportId = model.AfsPostActivity.AfsPostActivityGroup2.AirportOfTraining?.Id;
                postActivity.IacraStatusTypeId = model.AfsPostActivity.AfsPostActivityGroup2.IacraStatusTypeId;
                postActivity.PostActivityPaperWorkDate = model.AfsPostActivity.AfsPostActivityGroup2.DatePaperWorkSent.ToNullableDate();
                postActivity.IsAirmanCertificateNotIssued = model.AfsPostActivity.AfsPostActivityGroup2.IsAirManCertificateNotIssue;
                postActivity.ReasionForAirManCertificateNotIssue = model.AfsPostActivity.AfsPostActivityGroup2.ReasonsForAirManCertificateNotIssue;
                postActivity.IacraFtn = model.AfsPostActivity.AfsPostActivityGroup2.IacraFtn;
                postActivity.IacraApplicationId = model.AfsPostActivity.AfsPostActivityGroup2.IacraApplicationId;
                SavePostActivityDocuments(model);

                if (!postActivity.PreApprovalRequest.AfsPreApprovalRequest.IsMultipleApplicants.HasValue || (postActivity.PreApprovalRequest.AfsPreApprovalRequest.IsMultipleApplicants.HasValue && !postActivity.PreApprovalRequest.AfsPreApprovalRequest.IsMultipleApplicants.Value))
                {
                    SavePostActivityAddress(model, postActivity);
                }
                else
                {
                    SavePostActvityApplicants(model, postActivity);
                }
                postActivity.ApplicantCountryId = model.AfsPostActivity.AfsPostActivityGroup2.ApplicantCountryId != null ? model.AfsPostActivity.AfsPostActivityGroup2.ApplicantCountryId : null;

            }

            if (model.AfsPostActivity.IsSubmit)
            {
                // delete if any Orphan documents for this PostActivity
                var deleteDocs = _context.DocumentReferences.Where(d => d.ReferenceId == model.AfsPostActivity.Id && d.DocumentTypeId == (int)DocumentReferenceEnum.Iacra).ToList();
                _context.DocumentReferences.RemoveRange(deleteDocs);
            }

            //only for group2
            SavePostActivityApplicationAndTestCheckInfo(model, postActivity);
            var finalModel = base.SavePostActivityEvaluationHelper(model);
            return finalModel;

        }

        private void SavePostActvityApplicants(PreApprovalRequestViewModel model, PostActivity postActivity)
        {
            foreach (var app in postActivity.PostActivityApplicants.ToList())
            {
                _context.PostActivityApplicants.Remove(app);
            }
            postActivity.PostActivityApplicants = [.. model.AfsPostActivity.AfsPostActivityGroup2.Applicants.Select(a => new PostActivityApplicant
                    {
                        PostActivityId = postActivity.Id,
                        ApplicantName = a.ApplicantName,
                        CertificateNumber = a.CertificateNumber
                    })];
        }

        private static void SavePostActivityAddress(PreApprovalRequestViewModel model, PostActivity postActivity)
        {
            if (postActivity.Address == null)
            {
                postActivity.Address = new Address()
                {
                    AddressLine1 = model.AfsPostActivity.AfsPostActivityGroup2.Address.Address1,
                    AddressLine2 = model.AfsPostActivity.AfsPostActivityGroup2.Address.Address2,
                    City = model.AfsPostActivity.AfsPostActivityGroup2.Address.City,
                    StateId = model.AfsPostActivity.AfsPostActivityGroup2.Address.Country?.Id == 184 ? model.AfsPostActivity.AfsPostActivityGroup2.Address.State?.Id : null,
                    CountryId = model.AfsPostActivity.AfsPostActivityGroup2.Address.Country.Id,
                    ZipCode = model.AfsPostActivity.AfsPostActivityGroup2.Address.ZipCode
                };
            }
            else
            {
                postActivity.Address.AddressLine1 = model.AfsPostActivity.AfsPostActivityGroup2.Address.Address1;
                postActivity.Address.AddressLine2 = model.AfsPostActivity.AfsPostActivityGroup2.Address.Address2;
                postActivity.Address.City = model.AfsPostActivity.AfsPostActivityGroup2.Address.City;
                postActivity.Address.StateId = model.AfsPostActivity.AfsPostActivityGroup2.Address.Country?.Id == 184 ? model.AfsPostActivity.AfsPostActivityGroup2.Address.State?.Id : null;
                postActivity.Address.CountryId = model.AfsPostActivity.AfsPostActivityGroup2.Address.Country.Id;
                postActivity.Address.ZipCode = model.AfsPostActivity.AfsPostActivityGroup2.Address.ZipCode;
            }
        }

        private void SavePostActivityDocuments(PreApprovalRequestViewModel model)
        {
            if (model.AfsPostActivity.AfsPostActivityGroup2.IacraApplicationId != null)
            {
                var documentReference = _context.DocumentReferences.FirstOrDefault(d => d.ReferenceId == model.AfsPostActivity.Id && d.DocumentTypeId == (int)DocumentReferenceEnum.Iacra && d.SecondaryReferenceId == model.AfsPostActivity.AfsPostActivityGroup2.IacraApplicationId);

                if (documentReference != null)
                {
                    documentReference.SecondaryReferenceId = null;
                    documentReference.DocumentTypeId = (int)DocumentReferenceEnum.PostActivity;
                }
                _context.SaveChanges();
            }
        }

        public override AfsGroupsPostActivityViewModel Get(int postActivityId, bool createDocumentVersion = false)
        {
            base.Get(postActivityId, createDocumentVersion);
            _afsGroupsPostActivityViewModel.AfsPostActivityGroup2 = new AfsPostActivityGroup2ViewModel
            {
                IsAdditionalInstructionsProvided = _postActivity.IsAdditionalInstructionsProvided,
                AirManName = _postActivity.AirManName,
                AirManCertificateNumber = _postActivity.AirManCertificateNumber,
                FlightInstructorName = _postActivity.FlightInstructorName,
                FlightInstructorCertificateNumber = _postActivity.FlightInstructorCertificateNumber,
                PostActivityResultTypeId = _postActivity.PostActivityResultTypeId,
                GradeOfCertificateId = _postActivity.PreApprovalRequestGradeCertificateTypeId,
                AreasOfOperationAndTaskFound = _postActivity.AreasOfOperation,
                ReasonForDiscontinue = _postActivity.ReasonsOfDiscontinuance,
                AirCraftCatergoryId = _postActivity.PreApprovalRequestAircraftCategoryTypeId,
                AirCraftClassId = _postActivity.PreApprovalRequestAircraftClassTypeId,
                IsAircraftNotRequired = _postActivity.IsAircraftNotRequired,
                AirCraftMakeModelUsedId = _postActivity.AircraftMakeModelId.HasValue
                    ? new BaseLookup
                        { Id = _postActivity.AircraftMakeMode.Id, Name = _postActivity.AircraftMakeMode.ToString() }
                    : null,
                AirCraftRegistrationNumber = _postActivity.AircraftResgistrationNumber,
                SimulatorFaaId = _postActivity.SimulatorId,
                ActualStartDate = _postActivity.ActualStartDate.HasValue
                    ? _postActivity.ActualStartDate.Value.ToString("MM/dd/yyyy")
                    : string.Empty,
                ActualEndDate = _postActivity.ActualEndDate.HasValue
                    ? _postActivity.ActualEndDate.Value.ToString("MM/dd/yyyy")
                    : string.Empty,
                DurationOfGroundPortion = _postActivity.GroundPortionDuration,
                DurationOfFlightPortion = _postActivity.FlightPortionDuration,
                DurationOfFstdSimulatorPortion = _postActivity.FstdSimulatorPortionDuration,
                PhoneNumber = _postActivity.ApplicantPhone,
                Email = _postActivity.ApplicantEmail,
                NameOfApplicant = _postActivity.ApplicantName,
                CertificateNumberOfApplicant = _postActivity.ApplicantCertificateNumber,
                AirportOfTraining = _postActivity.AirportId.HasValue
                    ? new BaseLookup
                    {
                        Id = _postActivity.Airport.Id,
                        Name = $"{_postActivity.Airport.Name.Trim()}/ {_postActivity.Airport.Code.Trim()}"
                    }
                    : null,

                IacraStatusTypeId = _postActivity.IacraStatusTypeId,
                DatePaperWorkSent = _postActivity.PostActivityPaperWorkDate.HasValue
                    ? _postActivity.PostActivityPaperWorkDate.Value.ToShortDateString()
                    : string.Empty,
                IsAirManCertificateNotIssue = _postActivity.IsAirmanCertificateNotIssued,
                ReasonsForAirManCertificateNotIssue = _postActivity.ReasionForAirManCertificateNotIssue,
                IacraApplicationId = _postActivity.IacraApplicationId,
                IacraFtn = _postActivity.IacraFtn,
                Address = _postActivity.Address != null
                    ? new AddressViewModel
                    {
                        Id = _postActivity.Address.Id,
                        Address1 = _postActivity.Address.AddressLine1,
                        Address2 = _postActivity.Address.AddressLine2,
                        City = _postActivity.Address.City,
                        State = _postActivity.Address.StateProvince != null
                            ? new StateViewModel
                            {
                                Id = _postActivity.Address.StateProvince.Id,
                                Name = _postActivity.Address.StateProvince.Name
                            }
                            : null,
                        Country = _postActivity.Address.Country != null
                            ? new CountryViewModel
                            {
                                Id = _postActivity.Address.Country.Id,
                                Name = _postActivity.Address.Country.Name
                            }
                            : null,
                        ZipCode = _postActivity.Address.ZipCode,
                    }
                    : new AddressViewModel(),

                ProposedStartDate = _postActivity.PreApprovalRequest.ProposeStartDate.DateToString(),
                ApplicantCountryId = _postActivity.ApplicantCountryId ?? 184,
                Applicants = _postActivity.PostActivityApplicants != null
                    ? _postActivity.PostActivityApplicants.Select(prod =>
                        new AfsPostActivityApplicantViewModel
                        {
                            Id = prod.Id,
                            PostActivityId = prod.PostActivityId,
                            ApplicantName = prod.ApplicantName,
                            CertificateNumber = prod.CertificateNumber
                        }).ToList()
                    : new List<AfsPostActivityApplicantViewModel>()
            };
            return _afsGroupsPostActivityViewModel;

        }

        [SuppressMessage("Major Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Group 2 preapproval modified-field mapping is intentionally centralized in one method to preserve existing behavior and field-level change tracking.")]
        protected void LoadModifiedDataForAfsGroup2(PreApprovalRequestViewModel preApprovalRequestViewModel, AfsPostActivityViewModel afsPostActivity,
                                                    int designeeTypeId, Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls)
        {
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.PracticalOralTestId != null)
            {
                preApprovalRequestViewModel.TestInformation.PracticalOralTestId = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.PracticalOralTestId;
                AddItemToModifiedPreapprovals("TestInformation", new ModifiedControlViewModel { Control = "practicalOralTestId" }, modifiedPreapprovalControls);
            }
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsOtherAdminActivity != null)
            {
                preApprovalRequestViewModel.TestCheckInformation.IsOtherAdminActivity = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsOtherAdminActivity;
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel { Control = "isOtherAdminActivity" }, modifiedPreapprovalControls);
            }

            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.ProposeStartDate))
            {
                preApprovalRequestViewModel.TestInformation.ProposeStartDate = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.ProposeStartDate;
                AddItemToModifiedPreapprovals("TestInformation", new ModifiedControlViewModel { Control = "proposeStartDate" }, modifiedPreapprovalControls);
            }
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsAircraftNotRequired != null)
            {
                preApprovalRequestViewModel.TestCheckInformation.IsAircraftNotRequired = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsAircraftNotRequired;
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel { Control = "aircraft" }, modifiedPreapprovalControls);
            }
            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.TemporaryAuthorizationReason))
            {
                preApprovalRequestViewModel.TestCheckInformation.ReasonforAuthorization = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.TemporaryAuthorizationReason;
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel { Control = "reason" }, modifiedPreapprovalControls);
            }
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId != null && afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId > 0 && preApprovalRequestViewModel.TestCheckInformation != null)
            {
                if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId.HasValue)
                {
                    var mId = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId.GetValueOrDefault();
                    preApprovalRequestViewModel.TestCheckInformation.AircraftMakeModelId = _lookupService.LookupValues("makeModelSeries").Result.MakeModelSeries.Where(s => s.Id == mId).Select(x => new BaseLookup
                    {
                        Id = x.Id,
                        Name = $"{x.Code}/{x.Make}/{x.Model}"
                    }).FirstOrDefault();

                    var poMakeModel = _context.MakeModel.Where(w => w.Id == mId).FirstOrDefault();
                    var paAfsPre = _context.AfsPreApprovalRequests.Where(w => w.PreApprovalRequestId == (preApprovalRequestViewModel.Id)).FirstOrDefault();
                    var paMakeModel = paAfsPre != null && paAfsPre.AircraftMakeModelId != null ?
                                        _context.MakeModel.Where(w => w.Id == paAfsPre.AircraftMakeModelId).FirstOrDefault() : null;

                    var foundChange = true;
                    if (poMakeModel != null && paMakeModel != null)
                    {
                        foundChange = (poMakeModel.Make.Trim().ToLower() != paMakeModel.Make.Trim().ToLower() ||
                            poMakeModel.Model.Trim().ToLower() != paMakeModel.Model.Trim().ToLower()) ? true : false;
                    }

                    AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel
                        {
                            Control = "aircraftMakeModelId",
                            TriggersCorrectiveAction = foundChange
                        }, modifiedPreapprovalControls);
                }
            }
            else if (preApprovalRequestViewModel.TestCheckInformation.AircraftMakeModelId != null && preApprovalRequestViewModel.TestCheckInformation.AircraftMakeModelId?.Id == 0)
            {
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel { Control = "aircraftMakeModelId" }, modifiedPreapprovalControls);
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
                    AddItemToModifiedPreapprovals("FacilityInformation", new ModifiedControlViewModel { Control = "airport" }, modifiedPreapprovalControls);
                    AddItemToModifiedPreapprovals("ActivityLocation", new ModifiedControlViewModel { Control = "airport" }, modifiedPreapprovalControls);
            }
        }
        protected override IEnumerable<PreApprovalDateWiseCountViewModel> GetPreApprovalDateWiseCount(int applicationId)
        {
            return base.GetPreApprovalDateWiseCount(applicationId);
        }

        public override AfsGroupsPostActivityViewModel SaveAfsPostActivity(AfsGroupsPostActivityViewModel model)
        {
            _postActivity.IsAdditionalInstructionsProvided =
                model.AfsPostActivityGroup2.IsAdditionalInstructionsProvided;
            _postActivity.AirManName = model.AfsPostActivityGroup2.AirManName;
            _postActivity.AirManCertificateNumber = model.AfsPostActivityGroup2.AirManCertificateNumber;
            _postActivity.FlightInstructorName = model.AfsPostActivityGroup2.FlightInstructorName;
            _postActivity.FlightInstructorCertificateNumber =
                model.AfsPostActivityGroup2.FlightInstructorCertificateNumber;
            _postActivity.PostActivityResultTypeId = model.AfsPostActivityGroup2.PostActivityResultTypeId;
            _postActivity.PreApprovalRequestGradeCertificateTypeId = model.AfsPostActivityGroup2.GradeOfCertificateId;
            _postActivity.AreasOfOperation = model.AfsPostActivityGroup2.AreasOfOperationAndTaskFound;
            _postActivity.ReasonsOfDiscontinuance = model.AfsPostActivityGroup2.ReasonForDiscontinue;
            _postActivity.PreApprovalRequestAircraftCategoryTypeId = model.AfsPostActivityGroup2.AirCraftCatergoryId;
            _postActivity.PreApprovalRequestAircraftClassTypeId = model.AfsPostActivityGroup2.AirCraftClassId;
            _postActivity.IsAircraftNotRequired = model.AfsPostActivityGroup2.IsAircraftNotRequired;
            _postActivity.AircraftMakeModelId = model.AfsPostActivityGroup2.AirCraftMakeModelUsedId?.Id;
            _postActivity.AircraftResgistrationNumber = model.AfsPostActivityGroup2.AirCraftRegistrationNumber;
            _postActivity.SimulatorId = model.AfsPostActivityGroup2.SimulatorFaaId;
            _postActivity.ActualStartDate = model.AfsPostActivityGroup2.ActualStartDate.ToNullableDate();
            _postActivity.ActualEndDate = model.AfsPostActivityGroup2.ActualEndDate.ToNullableDate();
            _postActivity.GroundPortionDuration = model.AfsPostActivityGroup2.DurationOfGroundPortion;
            _postActivity.FlightPortionDuration = model.AfsPostActivityGroup2.DurationOfFlightPortion;
            _postActivity.FstdSimulatorPortionDuration = model.AfsPostActivityGroup2.DurationOfFstdSimulatorPortion;
            _postActivity.ApplicantPhone = model.AfsPostActivityGroup2.PhoneNumber;
            _postActivity.ApplicantName = model.AfsPostActivityGroup2.NameOfApplicant;
            _postActivity.ApplicantEmail = model.AfsPostActivityGroup2.Email;
            _postActivity.ApplicantCertificateNumber = model.AfsPostActivityGroup2.CertificateNumberOfApplicant;
            _postActivity.AirportId = model.AfsPostActivityGroup2.AirportOfTraining?.Id;
            _postActivity.IacraStatusTypeId = model.AfsPostActivityGroup2.IacraStatusTypeId;
            _postActivity.PostActivityPaperWorkDate = model.AfsPostActivityGroup2.DatePaperWorkSent.ToNullableDate();
            _postActivity.IsAirmanCertificateNotIssued = model.AfsPostActivityGroup2.IsAirManCertificateNotIssue;
            _postActivity.ReasionForAirManCertificateNotIssue =
                model.AfsPostActivityGroup2.ReasonsForAirManCertificateNotIssue;
            _postActivity.IacraFtn = model.AfsPostActivityGroup2.IacraFtn;
            _postActivity.IacraApplicationId = model.AfsPostActivityGroup2.IacraApplicationId;

            SaveAfsIacraDocuments(model);

            if (!_postActivity.PreApprovalRequest.AfsPreApprovalRequest.IsMultipleApplicants.HasValue ||
                (_postActivity.PreApprovalRequest.AfsPreApprovalRequest.IsMultipleApplicants.HasValue &&
                 !_postActivity.PreApprovalRequest.AfsPreApprovalRequest.IsMultipleApplicants.Value))
            {
                SaveAfsPostActivityAddress(model);
            }
            else
            {
                SaveAfsPostActivityApplicants(model);
            }

            _postActivity.ApplicantCountryId = model.AfsPostActivityGroup2.ApplicantCountryId != null
                ? model.AfsPostActivityGroup2.ApplicantCountryId
                : null;

            // SaveTestCheckAndApplicationInfo
            // do not add PostActivityModifiedData fields here

            //PostActivity Test check
            _postActivity.GradeCertificateTypeId = model.PreApprovalRequest.TestCheckInformation.GradeCertificateId;
            _postActivity.AircraftClassTypeId = model.PreApprovalRequest.TestCheckInformation.AircraftClassId;
            _postActivity.AircraftCategoryTypeId = model.PreApprovalRequest.TestCheckInformation.AircraftCategoryId;
            _postActivity.IsRecommendingInstructorNotAvailable =
                model.PreApprovalRequest.TestCheckInformation.IsRecommendingInstructorNotAvailable;
            _postActivity.RecommendingInstructor = model.PreApprovalRequest.TestCheckInformation.RecommendingInstructor;
            _postActivity.RecommendingInstructorCertificateNumber = model.PreApprovalRequest.TestCheckInformation
                .RecommendingInstructorCertificateNumber;
            _postActivity.AircraftMakeModelId = model.PreApprovalRequest.TestCheckInformation.AircraftMakeModelId?.Id;
            _postActivity.PracticalOralTestId = model.PreApprovalRequest.TestCheckInformation.PracticalOralTestId;

            //_postActivity Application Information
            _postActivity.PilotLicenseIssuedCountryId =
                model.PreApprovalRequest.ApplicationInformation.PilotLicenseCountry != null &&
                model.PreApprovalRequest.ApplicationInformation.PilotLicenseCountry.Id != 0
                    ? model.PreApprovalRequest.ApplicationInformation.PilotLicenseCountry.Id
                    : (int?)null;

            _postActivity.AirCarrierId = model.PreApprovalRequest.ApplicationInformation.AirCarrierId?.Id;

            _postActivity.SchoolId =
                model.PreApprovalRequest.ApplicationInformation.PreApprovalCertificateRatingType != null &&
                model.PreApprovalRequest.ApplicationInformation.PreApprovalCertificateRatingType.Any() &&
                model.PreApprovalRequest.ApplicationInformation.PreApprovalCertificateRatingType.Any(x =>
                    x.Id == (int)PreApprovalCertificateRatingTypeEnum.GraduateofanApprovedCourse)
                    ? model.PreApprovalRequest.ApplicationInformation.SchoolId?.Id
                    : (int?)null;



            //Remove Certificate Ratings
            if (_postActivity.PostActivityCertificateRatings != null)
            {
                foreach (var p in _postActivity.PostActivityCertificateRatings.ToList())
                {
                    _context.Entry(p).State = EntityState.Deleted;
                }
            }

            _postActivity.PostActivityCertificateRatings = model.PreApprovalRequest.ApplicationInformation
                ?.SelectedCertificateRatingTypeIds?.Select(p => new PostActivityCertificateRating
                {
                    PostActivityCertificateRatingTypeId = p,
                }).ToArray();
            foreach (var postActivityCertificateRating in _postActivity.PostActivityCertificateRatings)
            {
                _context.Entry(postActivityCertificateRating).State = EntityState.Added;
            }

            return model;
        }

        private void SaveAfsPostActivityApplicants(AfsGroupsPostActivityViewModel model)
        {
            foreach (var app in _postActivity.PostActivityApplicants.ToList())
            {
                _context.Entry(app).State = EntityState.Deleted;
            }
            _postActivity.PostActivityApplicants = model.AfsPostActivityGroup2.Applicants.Select(a =>
                new PostActivityApplicant
                {
                    PostActivityId = _postActivity.Id,
                    ApplicantName = a.ApplicantName,
                    CertificateNumber = a.CertificateNumber
                }).ToList();
            foreach (var postActivityApplicant in _postActivity.PostActivityApplicants)
            {
                _context.Entry(postActivityApplicant).State = EntityState.Added;
            }
        }

        private void SaveAfsPostActivityAddress(AfsGroupsPostActivityViewModel model)
        {
            if (_postActivity.Address == null || _postActivity.Id == 0)
            {
                _postActivity.Address = new Address()
                {
                    AddressLine1 = model.AfsPostActivityGroup2.Address.Address1,
                    AddressLine2 = model.AfsPostActivityGroup2.Address.Address2,
                    City = model.AfsPostActivityGroup2.Address.City,
                    StateId = model.AfsPostActivityGroup2.Address.Country?.Id == 184
                        ? model.AfsPostActivityGroup2.Address.State?.Id
                        : null,
                    CountryId = model.AfsPostActivityGroup2.Address.Country.Id,
                    ZipCode = model.AfsPostActivityGroup2.Address.ZipCode
                };
                _context.Entry(_postActivity.Address).State = EntityState.Added;
            }
            else
            {
                _postActivity.Address.AddressLine1 = model.AfsPostActivityGroup2.Address.Address1;
                _postActivity.Address.AddressLine2 = model.AfsPostActivityGroup2.Address.Address2;
                _postActivity.Address.City = model.AfsPostActivityGroup2.Address.City;
                _postActivity.Address.StateId = model.AfsPostActivityGroup2.Address.Country?.Id == 184
                    ? model.AfsPostActivityGroup2.Address.State?.Id
                    : null;
                _postActivity.Address.CountryId = model.AfsPostActivityGroup2.Address.Country.Id;
                _postActivity.Address.ZipCode = model.AfsPostActivityGroup2.Address.ZipCode;
                _context.Entry(_postActivity.Address).State = EntityState.Modified;
            }
        }

        private void SaveAfsIacraDocuments(AfsGroupsPostActivityViewModel model)
        {
            var iacraDocuments = _context.DocumentReferences.Where(d =>
                    d.ReferenceId == model.Id && d.DocumentTypeId == (int)DocumentReferenceEnum.Iacra).ToList();
            if (model.AfsPostActivityGroup2.IacraApplicationId != null)
            {
                var documentReference = iacraDocuments.LastOrDefault(d => d.SecondaryReferenceId == model.AfsPostActivityGroup2.IacraApplicationId);

                if (documentReference != null)
                {
                    documentReference.SecondaryReferenceId = null;
                    documentReference.DocumentTypeId = (int)DocumentReferenceEnum.PostActivity;
                    _context.Entry(documentReference).State = EntityState.Modified;
                }
            }

            if (model.IsSubmit)
            {
                // Skip the final Iacra document from deletion, whose entity state was already modified in previous steps.
                foreach (var doc in iacraDocuments.Where(d => d.DocumentTypeId == (int)DocumentReferenceEnum.Iacra))
                {
                    _context.Entry(doc).State = EntityState.Deleted;
                }
            }
        }

        [SuppressMessage("Major Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "This method intentionally aggregates field-level modified-data checks and corrective-action flags for a single comparison pass.")]
        protected void CheckForPreApprovalModifiedData(AfsGroupsPostActivityViewModel model,
            PreApprovalRequest preApprovalRequest,
            ref bool triggerCorrectiveAction,
            Dictionary<string, List<ModifiedControlViewModel>> modifiedControls)
        {
            //Type Of Activity
            if (model.PreApprovalRequest.TestInformation.PracticalOralTestId !=
                preApprovalRequest.AfsPreApprovalRequest.PracticalOralTestId &&
                preApprovalRequest.AfsPreApprovalRequest.PracticalOralTestId != null)
            {
                AddItemToModifiedPreapprovals("TestInformation", new ModifiedControlViewModel {Control = "practicalOralTestId"}, model.PreApprovalRequest.ModifiedPreapprovalControls);
                triggerCorrectiveAction = true;
            }

            if (model.PreApprovalRequest.TestCheckInformation.IsAircraftNotRequired.GetValueOrDefault() != preApprovalRequest.AfsPreApprovalRequest.IsAircraftNotRequired.GetValueOrDefault())
            {
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel {Control = "aircraft"}, model.PreApprovalRequest.ModifiedPreapprovalControls);
                triggerCorrectiveAction = true;
            }
            
            bool foundChange = false;
            if (model.PreApprovalRequest.TestCheckInformation.AircraftMakeModelId?.Id != preApprovalRequest.AfsPreApprovalRequest.AircraftMakeModelId)
            {
                MakeModel poMakeModel = null;
                if (model.PreApprovalRequest.TestCheckInformation.AircraftMakeModelId != null)
                {
                    poMakeModel = _context.MakeModel.Where(w => w.Id == (model.PreApprovalRequest.TestCheckInformation.AircraftMakeModelId.Id)).FirstOrDefault();
                }

                var paMakeModel = _context.MakeModel.Where(w => w.Id == (preApprovalRequest.AfsPreApprovalRequest.AircraftMakeModelId)).FirstOrDefault();

                foundChange = true;
                if (poMakeModel != null && paMakeModel != null)
                {
                    foundChange = (poMakeModel.Make.Trim().ToLower() != paMakeModel.Make.Trim().ToLower() ||
                                   poMakeModel.Model.Trim().ToLower() != paMakeModel.Model.Trim().ToLower()) ? true : false;
                }
            }
          
            if (foundChange)
            {
                AddItemToModifiedPreapprovals("TestCheckInformation", new ModifiedControlViewModel
                {
                    Control = "aircraftMakeModelId",
                    TriggersCorrectiveAction = foundChange
                }, model.PreApprovalRequest.ModifiedPreapprovalControls);
            }

            triggerCorrectiveAction = FindChangeInFunctionCodes(
                model.PreApprovalRequest.TestCheckInformation.SelectedRequestAuthorizations,
                model.PreApprovalRequest.TestCheckInformation.SelectedTemporaryAuthorizations,
                preApprovalRequest.PreApprovalRequestFunctionCodes,
                model.PreApprovalRequest.ModifiedPreapprovalControls) || triggerCorrectiveAction || foundChange;
            
            triggerCorrectiveAction = CheckForAddress(model.PreApprovalRequest.ActivityLocation.LocationAddress,
                                          preApprovalRequest.AfsPreApprovalRequest.Address,
                                          model.PreApprovalRequest.ModifiedPreapprovalControls,
                                          model.PreApprovalRequest.ActivityLocation.FacilityonRecord
                                              .GetValueOrDefault(),
                                          preApprovalRequest.AfsPreApprovalRequest.FacilityOnRecord.GetValueOrDefault(),
                                          "ActivityLocation") ||
                                      triggerCorrectiveAction;

            triggerCorrectiveAction = triggerCorrectiveAction || false;
        }
    }
}
