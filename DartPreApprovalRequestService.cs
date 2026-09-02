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
    public class DartPreApprovalRequestService : AfsGroupOnePreApprovalRequestService
    {
        public DartPreApprovalRequestService(DmsContext context, ITaskService taskService, IDocumentService documentService, IActivityService activityService, IMessageService messageService, ISharedService sharedService, ILookupService lookupService, IUserService userService)
        : base(context, taskService, documentService, activityService, messageService, sharedService, lookupService, userService)
        {
        }
        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity involved")]
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
            _preApprovalRequestViewModel.FacilityInformation = CloaPreApprovalRequestViewModelMapper.GetPreApprovalFacilityInformationViewModel(_preApprovalRequest);
            _preApprovalRequestViewModel.AirCraftInformation = CloaPreApprovalRequestViewModelMapper.GetPreApprovalAirCraftInformationViewModel(_preApprovalRequest);
            //AirCraftOwner
            _preApprovalRequestViewModel.AirCraftOwnerInformation = new PreApprovalAirCraftOwnerInformationViewModel();
            _preApprovalRequestViewModel.AirCraftOwnerInformation.AircraftOwnerName = _preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerName;
            _preApprovalRequestViewModel.AirCraftOwnerInformation.AircraftOwnerAddress = new AddressViewModel();
            if (_preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress != null)
            {
                _preApprovalRequestViewModel.AirCraftOwnerInformation.AircraftOwnerAddress.Id = _preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.Id;
                _preApprovalRequestViewModel.AirCraftOwnerInformation.AircraftOwnerAddress.Address1 = _preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.AddressLine1;
                _preApprovalRequestViewModel.AirCraftOwnerInformation.AircraftOwnerAddress.Address2 = _preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.AddressLine2;
                _preApprovalRequestViewModel.AirCraftOwnerInformation.AircraftOwnerAddress.City = _preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.City;
                _preApprovalRequestViewModel.AirCraftOwnerInformation.AircraftOwnerAddress.County = _preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.County;
                _preApprovalRequestViewModel.AirCraftOwnerInformation.AircraftOwnerAddress.State = _preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.StateProvince != null ? new StateViewModel
                {
                    Id = _preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.StateProvince.Id,
                    Name = _preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.StateProvince.Name
                } : null;
                _preApprovalRequestViewModel.AirCraftOwnerInformation.AircraftOwnerAddress.Country = _preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.Country != null ? new CountryViewModel
                {
                    Id = _preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.Country.Id,
                    Name = _preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.Country.Name
                } : null;
                _preApprovalRequestViewModel.AirCraftOwnerInformation.AircraftOwnerAddress.ZipCode = _preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.ZipCode;
                _preApprovalRequestViewModel.AirCraftOwnerInformation.AircraftOwnerAddress.PhoneNumber = _preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.Phone;

            }
            _preApprovalRequestViewModel.AirCraftOwnerInformation.IsRegistrationIssuedInYear = _preApprovalRequest.AfsPreApprovalRequest.IsRegistrationIssuedInYear;
            _preApprovalRequestViewModel.AirCraftOwnerInformation.IsMoreThan20Passengers = _preApprovalRequest.AfsPreApprovalRequest.IsMoreThan20Passengers;
            _preApprovalRequestViewModel.AirCraftOwnerInformation.PreviousAircraftRegistrationNumber = _preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationNumber;
            _preApprovalRequestViewModel.AirCraftOwnerInformation.PreviousAircraftRegistrationDate = _preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationDate.HasValue ?
                                                                  _preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationDate.Value.ToShortDateString() : String.Empty;
            _preApprovalRequestViewModel.AirCraftOwnerInformation.PreviousAircraftRegistrationCountryId = _preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationCountryId;

            //AirCraftOperator
            _preApprovalRequestViewModel.AirCraftOperatorInformation = new PreApprovalAirCraftOperatorInformationViewModel();
            _preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftOperatorName = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorName;
            _preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftOperatorCertificationNumber = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorCertificationNumber;
            _preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftInspectionProgram = _preApprovalRequest.AfsPreApprovalRequest.AircraftInspectionProgram;
            _preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftOperatorAddress = new AddressViewModel();
            if (_preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress != null)
            {
                _preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftOperatorAddress.Id = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.Id;
                _preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftOperatorAddress.Address1 = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.AddressLine1;
                _preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftOperatorAddress.Address2 = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.AddressLine2;
                _preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftOperatorAddress.City = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.City;
                _preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftOperatorAddress.County = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.County;
                _preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftOperatorAddress.State = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.StateProvince != null ?
                 new StateViewModel
                 {
                     Id = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.StateProvince.Id,
                     Name = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.StateProvince.Name
                 } : null;
                _preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftOperatorAddress.Country = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.Country != null ? new CountryViewModel
                {
                    Id = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.Country.Id,
                    Name = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.Country.Name
                } : null;
                _preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftOperatorAddress.ZipCode = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.ZipCode;
                _preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftOperatorAddress.PhoneNumber = _preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.Phone;
            };
            //CertificationBasis
            _preApprovalRequestViewModel.CertificationBasis = new PreApprovalCertificationBasisViewModel();
            _preApprovalRequestViewModel.CertificationBasis.CertificationBasisId = _preApprovalRequest.AfsPreApprovalRequest.CertificationBasisId;
            _preApprovalRequestViewModel.CertificationBasis.PerformerName = _preApprovalRequest.AfsPreApprovalRequest.PerformerName;
            _preApprovalRequestViewModel.CertificationBasis.PerformerCertificateNumber = _preApprovalRequest.AfsPreApprovalRequest.PerformerCertificateNumber;
            _preApprovalRequestViewModel.CertificationBasis.PerformerPhoneNumber = _preApprovalRequest.AfsPreApprovalRequest.PerformerPhoneNumber;
            _preApprovalRequestViewModel.CertificationBasis.AssistingEngineerName = _preApprovalRequest.AfsPreApprovalRequest.AssistingEngineerName;
            _preApprovalRequestViewModel.CertificationBasis.AssistingEngineerPhoneNumber = _preApprovalRequest.AfsPreApprovalRequest.AssistingEngineerPhoneNumber;
            _preApprovalRequestViewModel.CertificationBasis.AssistingEngineerDesigneeNumber = _preApprovalRequest.AfsPreApprovalRequest.AssistingEngineerDesigneeNumber;
            _preApprovalRequestViewModel.CertificationBasis.AssistingEngineerCertificateNumber = _preApprovalRequest.AfsPreApprovalRequest.AssistingEngineerCertificateNumber;
            _preApprovalRequestViewModel.CertificationBasis.CertificationBasisProjectDescription = _preApprovalRequest.AfsPreApprovalRequest.CertificationBasisProjectDescription;
            GetAfsHelperTwo(preApprovalRequestId, loadPreapprovalModifiedData, postActivityId);

            if (_preApprovalRequestViewModel.AfsPostActivity != null && _preApprovalRequestViewModel.AfsPostActivity.StatusId != (int)PreApprovalRequestStatusEnum.Initiated && _preApprovalRequestViewModel.FacilityInformation != null)
            {
                if (_preApprovalRequestViewModel.FacilityInformation.LocationDirections != _preApprovalRequestViewModel.AfsPostActivity.LocationDirections)
                {
                    _preApprovalRequestViewModel.FacilityInformation.LocationDirections = _preApprovalRequestViewModel.AfsPostActivity.LocationDirections;
                    _preApprovalRequestViewModel.FacilityInformation.IsDirectionToLocationNeeded = String.IsNullOrEmpty(_preApprovalRequestViewModel.AfsPostActivity.LocationDirections) ? false : true;
                }
                if (_preApprovalRequestViewModel.FacilityInformation.pointOfContactPhone != _preApprovalRequestViewModel.AfsPostActivity.PointOfContactPhone)
                {
                    _preApprovalRequestViewModel.FacilityInformation.pointOfContactPhone = _preApprovalRequestViewModel.AfsPostActivity.PointOfContactPhone;
                }
                if (_preApprovalRequestViewModel.FacilityInformation.PointOfContactName != _preApprovalRequestViewModel.AfsPostActivity.PointOfContactName)
                {
                    _preApprovalRequestViewModel.FacilityInformation.PointOfContactName = _preApprovalRequestViewModel.AfsPostActivity.PointOfContactName;
                }
            }

            if (loadPreapprovalModifiedData && _preApprovalRequestViewModel.AfsPostActivity.AfsPostActivityModifiedPreApprovalViewModel != null && _context.PostActivities.Where(e => e.PreApprovalRequestId == _preApprovalRequest.Id).Any())
            {
                LoadModifiedDataForDart(_preApprovalRequestViewModel, _preApprovalRequestViewModel.AfsPostActivity, _preApprovalRequestViewModel.ModifiedPreapprovalControls);
                LoadPreApprovalModifiedData(_preApprovalRequestViewModel, _preApprovalRequestViewModel.AfsPostActivity, _cloa.DesigneeInfo.TypeId, _preApprovalRequestViewModel.ModifiedPreapprovalControls);
            }
            GetHelper(_preApprovalRequestViewModel);
            CheckIfActivityOutside(_preApprovalRequestViewModel);
            return _preApprovalRequestViewModel;
        }
        public override PreApprovalRequestViewModel GetNew(int applicationId)
        {
            base.GetNew(applicationId);
            GetNewHelp(applicationId);
            _preApprovalRequestViewModel.FacilityInformation = new PreApprovalFacilityInformationViewModel();
            _preApprovalRequestViewModel.AirCraftInformation = new PreApprovalAirCraftInformationViewModel();
            _preApprovalRequestViewModel.AirCraftOwnerInformation = new PreApprovalAirCraftOwnerInformationViewModel()
            {
                AircraftOwnerAddress = new AddressViewModel()
            };
            _preApprovalRequestViewModel.AirCraftOperatorInformation = new PreApprovalAirCraftOperatorInformationViewModel()
            {
                AircraftOperatorAddress = new AddressViewModel()
            };
            _preApprovalRequestViewModel.CertificationBasis = new PreApprovalCertificationBasisViewModel();
            _preApprovalRequestViewModel.DesigneeFunctionCodes = _cloa.DesigneeFunctionCodes;
            _preApprovalRequestViewModel.IsAfsType = true;
            _preApprovalRequestViewModel.PreApprovalDateWiseCount = GetPreApprovalDateWiseCount(_cloa.DesigneeInformation.ApplicationId);
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
        public override PreApprovalRequestViewModel Save(PreApprovalRequestViewModel model)
        {
            base.Save(model);
            // the same for both model.id==0 and model.id!=0
            _preApprovalRequest.PreApprovalRequestFunctionCodes = model.SelectedFunctionCodes.Select(f => new PreApprovalRequestFunctionCode
            {
                FunctionCodeId = f,
                IsCloaFunctionCode = true,
            }).ToList();

            if (model.Id != 0)
            {
                //Delete Products if already
                if (model.PlannedActivity != null && model.PlannedActivity.Products != null && model.PlannedActivity.Products.Any())
                {
                    _context.PreApprovalRequestProducts.RemoveRange(_preApprovalRequest.PreApprovalRequestProducts);
                }
                this.UpdateAfsData(model, _preApprovalRequest);
            }
            else
            {
                InsertAfsData(model, _preApprovalRequest);
                InsertAfsDataDart(model);
                _context.PreApprovalRequests.Add(_preApprovalRequest);
            }
            SaveHelper(model);
            CompleteOrCreateTask(model, IsAutoPreApproval(model));

            return _preApprovalRequestViewModel;
        }
        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity involved")]
        private new void UpdateAfsData(PreApprovalRequestViewModel model, PreApprovalRequest preApprovalRequest)
        {
            preApprovalRequest.AfsPreApprovalRequest.ApplicantName = model.ApplicantInformation.Name;

            //Activity Location
            if (model.ActivityLocation.FacilityonRecord.HasValue && model.ActivityLocation.FacilityonRecord.Value)
            {
                preApprovalRequest.AfsPreApprovalRequest.ActivityLocationAddressId = model.ActivityLocation.FacilityAddress.Id;
            }
            else
            {
                if (model.ActivityLocation.LocationAddress.Id != model.ActivityLocation.OtherAddress?.Id || model.ActivityLocation.LocationAddress.Id == 0)
                {
                    //Remove Other address saved previously
                    var savedAddress = preApprovalRequest.AfsPreApprovalRequest.Address;
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
                    };
                    preApprovalRequest.AfsPreApprovalRequest.Address = address;
                    preApprovalRequest.AfsPreApprovalRequest.ActivityLocationAddressId = address.Id;
                }
                else
                {
                    preApprovalRequest.AfsPreApprovalRequest.Address.Name = model.ActivityLocation.LocationAddress.Name; // Facility name make sure to add condition and save data for those designee types only.
                    preApprovalRequest.AfsPreApprovalRequest.Address.AddressLine1 = model.ActivityLocation.LocationAddress.Address1;
                    preApprovalRequest.AfsPreApprovalRequest.Address.AddressLine2 = model.ActivityLocation.LocationAddress.Address2;
                    preApprovalRequest.AfsPreApprovalRequest.Address.City = model.ActivityLocation.LocationAddress.City;
                    preApprovalRequest.AfsPreApprovalRequest.Address.CountryId = model.ActivityLocation.LocationAddress.Country?.Id;
                    preApprovalRequest.AfsPreApprovalRequest.Address.StateId = model.ActivityLocation.LocationAddress.Country?.Id == 184 ? model.ActivityLocation.LocationAddress.State?.Id : null;
                    preApprovalRequest.AfsPreApprovalRequest.Address.ZipCode = model.ActivityLocation.LocationAddress.ZipCode;
                }
            }

            preApprovalRequest.AfsPreApprovalRequest.IsOutsideOfficeDistrict = model.ActivityLocation.IsOutsideOfficeDistrict;

            if (model.ActivityLocation.IsOutsideOfficeDistrict.HasValue && model.ActivityLocation.IsOutsideOfficeDistrict.Value)
            {
                preApprovalRequest.AfsPreApprovalRequest.IsActivityOutsideUsa = model.ActivityLocation.IsActivityOutsideUsa;
                preApprovalRequest.AfsPreApprovalRequest.OfficeId = model.ActivityLocation.OfficeId;
            }
            else
            {
                preApprovalRequest.AfsPreApprovalRequest.IsActivityOutsideUsa = null;
                preApprovalRequest.AfsPreApprovalRequest.OfficeId = null;
            }
            // ends of


            if (model.SelectedFunctionCodes.Contains((int)DartSpecialFunctionCodeEnums.FunctionCode180) || model.SelectedFunctionCodes.Contains((int)DartSpecialFunctionCodeEnums.FunctionCode191))
            {
                preApprovalRequest.AfsPreApprovalRequest.IssuedExportApprovalQuantity = model.PlannedActivity.IssuedExportApprovalQuantity;
                preApprovalRequest.AfsPreApprovalRequest.IssuedDomesticApprovalQuantity = model.PlannedActivity.IssuedDomesticApprovalQuantity;
            }
            //Planned Activity Products
            AddPreApprovalRequestsProducts(model, preApprovalRequest);
            // ApplicantInfo
            preApprovalRequest.AfsPreApprovalRequest.ApplicantName = model.ApplicantInformation.Name;
            preApprovalRequest.AfsPreApprovalRequest.ApplicantPhone = model.ApplicantInformation.Phone;
            preApprovalRequest.AfsPreApprovalRequest.ApplicantEmailAddress = model.ApplicantInformation.Email;
            //Facility Info
            preApprovalRequest.AfsPreApprovalRequest.AirportId = model.FacilityInformation.Airport != null ? model.FacilityInformation.Airport.Id : (int?)null;
            preApprovalRequest.AfsPreApprovalRequest.PointOfContactName = model.FacilityInformation.PointOfContactName;
            preApprovalRequest.AfsPreApprovalRequest.PointOfContactPhone = model.FacilityInformation.pointOfContactPhone;
            preApprovalRequest.AfsPreApprovalRequest.LocationDirections = model.FacilityInformation.IsDirectionToLocationNeeded ? model.FacilityInformation.LocationDirections : null;
            // Aircraft only for cat 10, 12 and 13
            if (model.CategoryId.HasValue && (model.CategoryId.Value == (int)CategoryEnum.Grp10 || model.CategoryId.Value == (int)CategoryEnum.Grp12 || model.CategoryId.Value == (int)CategoryEnum.Grp13))
            {
                preApprovalRequest.AfsPreApprovalRequest.ProductTypeId = model.AirCraftInformation.ProductTypeId;

                preApprovalRequest.AfsPreApprovalRequest.AircraftRegistrationNumber = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.AirFrame
                                                             ? model.AirCraftInformation.AircraftRegistrationNumber : null;
                preApprovalRequest.AfsPreApprovalRequest.AircraftRegistrationDate = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.AirFrame
                                                             ? model.AirCraftInformation.AircraftRegistrationDate.ToNullableDate() : (DateTime?)null;
                preApprovalRequest.AfsPreApprovalRequest.AircraftMakeModelId = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.AirFrame
                                                             ? model.AirCraftInformation.IsAmBuiltLightSport == null || !model.AirCraftInformation.IsAmBuiltLightSport.Value
                                                             ? model.AirCraftInformation.AircraftMakeModelId?.Id : null
                                                             : null;
                preApprovalRequest.AfsPreApprovalRequest.AircraftSerialNumber = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.AirFrame
                                                             ? model.AirCraftInformation.AircraftSerialNumber : null;

                preApprovalRequest.AfsPreApprovalRequest.IsAmBuiltLightSport = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.AirFrame
                                                            ? model.AirCraftInformation.IsAmBuiltLightSport : null;
                preApprovalRequest.AfsPreApprovalRequest.AircraftMakeModel = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.AirFrame
                                                            ? model.AirCraftInformation.IsAmBuiltLightSport.HasValue && model.AirCraftInformation.IsAmBuiltLightSport.Value
                                                            ? model.AirCraftInformation.AircraftMakeModel : null
                                                            : null;

                preApprovalRequest.AfsPreApprovalRequest.EngineMake = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.Engine
                                                            ? model.AirCraftInformation.EngineMake : null;
                preApprovalRequest.AfsPreApprovalRequest.EngineModel = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.Engine
                                                            ? model.AirCraftInformation.EngineModel : null;
                preApprovalRequest.AfsPreApprovalRequest.EngineSerialNumber = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.Engine
                                                            ? model.AirCraftInformation.EngineSerialNumber : null;

            }
            // Aircraft Owner && only for cat 10 ,12 and 13
            if (model.CategoryId.HasValue && model.CategoryId.Value != (int)CategoryEnum.Grp11)
            {
                preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerName = model.AirCraftOwnerInformation.AircraftOwnerName;
                preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.AddressLine1 = model.AirCraftOwnerInformation.AircraftOwnerAddress.Address1;
                preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.AddressLine2 = model.AirCraftOwnerInformation.AircraftOwnerAddress.Address2;
                preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.City = model.AirCraftOwnerInformation.AircraftOwnerAddress.City;
                preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.CountryId = model.AirCraftOwnerInformation.AircraftOwnerAddress.Country?.Id;
                preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.StateId = model.AirCraftOwnerInformation.AircraftOwnerAddress.Country?.Id == 184 ? model.AirCraftOwnerInformation.AircraftOwnerAddress.State?.Id : null;
                preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress.ZipCode = model.AirCraftOwnerInformation.AircraftOwnerAddress.ZipCode;
                //Grp10
                preApprovalRequest.AfsPreApprovalRequest.IsRegistrationIssuedInYear = model.CategoryId.Value == (int)CategoryEnum.Grp10 ? model.AirCraftOwnerInformation.IsRegistrationIssuedInYear : null;
                preApprovalRequest.AfsPreApprovalRequest.IsMoreThan20Passengers = model.CategoryId.Value == (int)CategoryEnum.Grp10 ? model.AirCraftOwnerInformation.IsMoreThan20Passengers : null;

                if (model.AirCraftOwnerInformation != null && model.AirCraftOwnerInformation.IsRegistrationIssuedInYear.HasValue && model.AirCraftOwnerInformation.IsRegistrationIssuedInYear.Value)
                {
                    preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationNumber = model.AirCraftOwnerInformation.PreviousAircraftRegistrationNumber;
                    preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationDate = model.AirCraftOwnerInformation.PreviousAircraftRegistrationDate.ToNullableDate();
                    preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationCountryId = model.AirCraftOwnerInformation.PreviousAircraftRegistrationCountryId;
                }
                else
                {
                    preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationNumber = null;
                    preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationDate = (DateTime?)null;
                    preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationCountryId = null;
                }
                // only on Grp 12 or isMoreThan20Passengers is selected
                if (model.CategoryId.HasValue && model.CategoryId.Value == (int)CategoryEnum.Grp12 || (model.AirCraftOwnerInformation.IsMoreThan20Passengers.HasValue && model.AirCraftOwnerInformation.IsMoreThan20Passengers.Value))
                {
                    preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorName = model.AirCraftOperatorInformation.AircraftOperatorName;
                    preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress = new Address();
                    preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.AddressLine1 = model.AirCraftOperatorInformation.AircraftOperatorAddress.Address1;
                    preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.AddressLine2 = model.AirCraftOperatorInformation.AircraftOperatorAddress.Address2;
                    preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.City = model.AirCraftOperatorInformation.AircraftOperatorAddress.City;
                    preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.CountryId = model.AirCraftOperatorInformation.AircraftOperatorAddress.Country?.Id;
                    preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.StateId = model.AirCraftOperatorInformation.AircraftOperatorAddress.Country?.Id == 184 ? model.AirCraftOperatorInformation.AircraftOperatorAddress.State?.Id : null;
                    preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress.ZipCode = model.AirCraftOperatorInformation.AircraftOperatorAddress.ZipCode;
                    preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorCertificationNumber = model.AirCraftOperatorInformation.AircraftOperatorCertificationNumber;
                    preApprovalRequest.AfsPreApprovalRequest.AircraftInspectionProgram = model.AirCraftOperatorInformation.AircraftInspectionProgram;
                }
                //Certification Basis for Grp 13
                if (model.CategoryId.HasValue && model.CategoryId.Value == (int)CategoryEnum.Grp13)
                {
                    preApprovalRequest.AfsPreApprovalRequest.CertificationBasisId = model.CertificationBasis.CertificationBasisId;
                    preApprovalRequest.AfsPreApprovalRequest.PerformerName = model.CertificationBasis.PerformerName;
                    preApprovalRequest.AfsPreApprovalRequest.PerformerCertificateNumber = model.CertificationBasis.PerformerCertificateNumber;
                    preApprovalRequest.AfsPreApprovalRequest.PerformerPhoneNumber = model.CertificationBasis.PerformerPhoneNumber;
                    preApprovalRequest.AfsPreApprovalRequest.AssistingEngineerName = model.CertificationBasis.AssistingEngineerName;
                    preApprovalRequest.AfsPreApprovalRequest.AssistingEngineerCertificateNumber = model.CertificationBasis.AssistingEngineerCertificateNumber;
                    preApprovalRequest.AfsPreApprovalRequest.AssistingEngineerPhoneNumber = model.CertificationBasis.AssistingEngineerPhoneNumber;
                    preApprovalRequest.AfsPreApprovalRequest.AssistingEngineerDesigneeNumber = model.CertificationBasis.AssistingEngineerDesigneeNumber;
                    preApprovalRequest.AfsPreApprovalRequest.CertificationBasisProjectDescription = model.CertificationBasis.CertificationBasisProjectDescription;
                }
            }

            preApprovalRequest.AfsPreApprovalRequest.RevisedDate = (model.RequestInfo.ActivityStatus.Id == (int)PreApprovalRequestStatusEnum.Approved || model.RequestInfo.ActivityStatus.Id == (int)ActivityStatusEnum.Pending) && model.isSubmit
                   && (!string.IsNullOrEmpty(model.RequestInfo.SubmittedDate) && Convert.ToDateTime(model.RequestInfo.SubmittedDate) != DateTime.Now)
             ? DateTime.Now : (preApprovalRequest.AfsPreApprovalRequest.RevisedDate != null) ? preApprovalRequest.AfsPreApprovalRequest.RevisedDate : (DateTime?)null;
            _preApprovalRequest = preApprovalRequest;
        }
        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity involved")]
        public override PreApprovalRequestViewModel SavePostActivityEvaluation(PreApprovalRequestViewModel adminModel)
        {
            var model = base.SavePostActivityEvaluation(adminModel);
            var postActivity = _postActivity;

            if (model.CategoryId == (int)CategoryEnum.Grp10)
            {

                postActivity.PostActivityResultTypeId = model.AfsPostActivity.AfsPostActivityDart176.ResultTypeId;
                postActivity.IsCertificateIssued = Convert.ToBoolean(model.AfsPostActivity.AfsPostActivityDart176.IsCertificateIssued);
                postActivity.ResultsSubmissionDate = model.AfsPostActivity.AfsPostActivityDart176.ResultsSubmissionDate.ToNullableDate();
                postActivity.ActionReasonSummary = model.AfsPostActivity.AfsPostActivityDart176.ActionReasonSummary;
                //Remvoe Certificate
                foreach (var fc in postActivity.PostActivityCertificates.ToList())
                {
                    _context.PostActivityCertificates.Remove(fc);
                }
                //Insert Certificate
                if (model.AfsPostActivity.AfsPostActivityDart176 != null && model.AfsPostActivity.AfsPostActivityDart176.PostActivityCertificates != null && model.AfsPostActivity.AfsPostActivityDart176.PostActivityCertificates.Any())
                {
                    postActivity.PostActivityCertificates = model.AfsPostActivity.AfsPostActivityDart176.PostActivityCertificates
                    .Where(poc => poc.IsSelected)
                    .Select(prod =>
                       new PostActivityCertificate
                       {
                           PostActivityId = postActivity.Id,
                           CertificateTypeId = Convert.ToInt32(prod.CertificateType.Id),
                           CertificateCategoryId = prod.CertificateCategoryId,
                           IssuanceTypeId = prod.IssuanceTypeId,
                           IssuanceDate = DateTime.Parse(prod.IssuanceDate)
                       }).ToList();
                }
            }
            else if (model.CategoryId == (int)CategoryEnum.Grp11)
            {
                //Remvoe Products
                foreach (var fc in postActivity.PostActivityProducts.ToList())
                {
                    _context.PostActivityProducts.Remove(fc);
                }
                //Insert Products
                if (model.AfsPostActivity.AfsPostActivityDart196 != null && model.AfsPostActivity.AfsPostActivityDart196.Products != null && model.AfsPostActivity.AfsPostActivityDart196.Products.Any())
                {
                    postActivity.PostActivityProducts = model.AfsPostActivity.AfsPostActivityDart196.Products.Select(prod =>
                       new PostActivityProduct
                       {
                           PostActivityId = postActivity.Id,
                           Make = prod.Make,
                           Model = prod.Model,
                           SerialNumber = prod.SerialNumber,
                           ResultTypeId = prod.ResultTypeId
                       }).ToList();
                }
                postActivity.IssuedExportApprovalQuantity = model.AfsPostActivity.AfsPostActivityDart196.IssuedExportApprovalQuantity;
                postActivity.DeniedExportApprovalQuantity = model.AfsPostActivity.AfsPostActivityDart196.DeniedExportApprovalQuantity;
                postActivity.CancelledExportApprovalQuantity = model.AfsPostActivity.AfsPostActivityDart196.CancelledExportApprovalQuantity;
                postActivity.DomesticIssuedApprovalQuantity = model.AfsPostActivity.AfsPostActivityDart196.DomesticIssuedApprovalQuantity;
                postActivity.DomesticDeniedtApprovalQuantity = model.AfsPostActivity.AfsPostActivityDart196.DomesticDeniedtApprovalQuantity;
                postActivity.DomesticCancelledApprovalQuantity = model.AfsPostActivity.AfsPostActivityDart196.DomesticCancelledApprovalQuantity;

                postActivity.WorkCompletedDate = model.AfsPostActivity.AfsPostActivityDart196.WorkCompletedDate.ToNullableDate();
            }
            else if (model.CategoryId == (int)CategoryEnum.Grp12)
            {
                postActivity.IsAircraftRecordsReview = Convert.ToBoolean(model.AfsPostActivity.AfsPostActivityDart198.IsAircraftRecordsReview);
                if (postActivity.IsAircraftRecordsReview == true)
                {
                    postActivity.AircraftRecordsReviewDate = model.AfsPostActivity.AfsPostActivityDart198.AircraftRecordsReviewDate.ToNullableDate();
                }
                else
                {
                    postActivity.AircraftRecordsReviewDate = null;
                }
                postActivity.IsAircraftInspection = Convert.ToBoolean(model.AfsPostActivity.AfsPostActivityDart198.IsAircraftInspection);

                if (postActivity.IsAircraftInspection == true)
                {
                    postActivity.AircraftInspectionDate = model.AfsPostActivity.AfsPostActivityDart198.AircraftInspectionDate.ToNullableDate();
                }
                else
                {
                    postActivity.AircraftInspectionDate = null;
                }
                postActivity.ResultsSubmissionDate = model.AfsPostActivity.AfsPostActivityDart198.ResultsSubmissionDate.ToNullableDate();
                postActivity.SubmittedOfficeId = model.AfsPostActivity.AfsPostActivityDart198.SubmittedOfficeId;
                postActivity.ActivityResultSummary = model.AfsPostActivity.AfsPostActivityDart198.ActivityResultSummary;
            }
            else if (model.CategoryId == (int)CategoryEnum.Grp13)
            {
                postActivity.PostActivityResultTypeId = model.AfsPostActivity.AfsPostActivityDart208.ResultTypeId != null ? model.AfsPostActivity.AfsPostActivityDart208.ResultTypeId : null;
                if (postActivity.PostActivityResultTypeId == (int)ResultTypeDartEnums.Denied || postActivity.PostActivityResultTypeId == (int)ResultTypeDartEnums.Cancelled)
                {
                    postActivity.ActionReasonSummary = model.AfsPostActivity.AfsPostActivityDart208.ActionReasonSummary;
                }
                else
                {
                    postActivity.ActionReasonSummary = null;
                }
                postActivity.ApprovalDate = model.AfsPostActivity.AfsPostActivityDart208.ApprovalDate.ToNullableDate();
                postActivity.ActivityResultSummary = model.AfsPostActivity.AfsPostActivityDart208.ActivityResultSummary;
                postActivity.ResultsSubmissionDate = model.AfsPostActivity.AfsPostActivityDart208.ResultsSubmissionDate.ToNullableDate();
            }

            postActivity.LocationDirections = model.FacilityInformation.IsDirectionToLocationNeeded ? model.FacilityInformation.LocationDirections : null;
            postActivity.PointOfContactName = model.FacilityInformation.PointOfContactName;
            postActivity.PointOfContactPhone = model.FacilityInformation.pointOfContactPhone;

            _postActivity = postActivity;
            _preApprovalRequestViewModel = model;
            return base.SavePostActivityEvaluationHelper(_preApprovalRequestViewModel);
        }
        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity inherent to task navigation system")]
        private void InsertAfsDataDart(PreApprovalRequestViewModel model)
        {
            PreApprovalRequest preApprovalRequest = _preApprovalRequest;
            if (model.DesigneeTypeId == (int)DesigneeTypeEnum.DART)
            {
                if (model.SelectedFunctionCodes.Contains((int)DartSpecialFunctionCodeEnums.FunctionCode180) || model.SelectedFunctionCodes.Contains((int)DartSpecialFunctionCodeEnums.FunctionCode191))
                {
                    preApprovalRequest.AfsPreApprovalRequest.IssuedExportApprovalQuantity = model.PlannedActivity.IssuedExportApprovalQuantity;
                    preApprovalRequest.AfsPreApprovalRequest.IssuedDomesticApprovalQuantity = model.PlannedActivity.IssuedDomesticApprovalQuantity;
                }
                //Planned Activity Products
                AddPreApprovalRequestsProducts(model, preApprovalRequest);
                // ApplicantInfo
                preApprovalRequest.AfsPreApprovalRequest.ApplicantName = model.ApplicantInformation.Name;
                preApprovalRequest.AfsPreApprovalRequest.ApplicantPhone = model.ApplicantInformation.Phone;
                preApprovalRequest.AfsPreApprovalRequest.ApplicantEmailAddress = model.ApplicantInformation.Email;
                preApprovalRequest.AfsPreApprovalRequest.CategoryId = model.CategoryId;
                //Facility Info
                preApprovalRequest.AfsPreApprovalRequest.AirportId = model.FacilityInformation.Airport != null ? model.FacilityInformation.Airport.Id : (int?)null;
                preApprovalRequest.AfsPreApprovalRequest.PointOfContactName = model.FacilityInformation.PointOfContactName;
                preApprovalRequest.AfsPreApprovalRequest.PointOfContactPhone = model.FacilityInformation.pointOfContactPhone;
                preApprovalRequest.AfsPreApprovalRequest.LocationDirections = model.FacilityInformation.IsDirectionToLocationNeeded ? model.FacilityInformation.LocationDirections : null;
                // Aircraft only for cat 10, 12 and 13
                if (model.CategoryId.HasValue && (model.CategoryId.Value == (int)CategoryEnum.Grp10 || model.CategoryId.Value == (int)CategoryEnum.Grp12 || model.CategoryId.Value == (int)CategoryEnum.Grp13))
                {
                    preApprovalRequest.AfsPreApprovalRequest.ProductTypeId = model.AirCraftInformation.ProductTypeId;

                    preApprovalRequest.AfsPreApprovalRequest.AircraftRegistrationNumber = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.AirFrame
                                                                  ? model.AirCraftInformation.AircraftRegistrationNumber : null;
                    preApprovalRequest.AfsPreApprovalRequest.AircraftRegistrationDate = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.AirFrame && !string.IsNullOrEmpty(model.AirCraftInformation.AircraftRegistrationDate)
                                                                  ? DateTime.Parse(model.AirCraftInformation.AircraftRegistrationDate) : (DateTime?)null;
                    preApprovalRequest.AfsPreApprovalRequest.AircraftMakeModelId = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.AirFrame
                                                                  ? model.AirCraftInformation.IsAmBuiltLightSport == null || !model.AirCraftInformation.IsAmBuiltLightSport.Value
                                                                  ? model.AirCraftInformation.AircraftMakeModelId?.Id : null
                                                                  : null;
                    preApprovalRequest.AfsPreApprovalRequest.AircraftSerialNumber = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.AirFrame
                                                                  ? model.AirCraftInformation.AircraftSerialNumber : null;

                    preApprovalRequest.AfsPreApprovalRequest.IsAmBuiltLightSport = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.AirFrame
                                                            ? model.AirCraftInformation.IsAmBuiltLightSport : null;
                    preApprovalRequest.AfsPreApprovalRequest.AircraftMakeModel = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.AirFrame
                                                            ? model.AirCraftInformation.IsAmBuiltLightSport.HasValue && model.AirCraftInformation.IsAmBuiltLightSport.Value
                                                            ? model.AirCraftInformation.AircraftMakeModel : null
                                                            : null;

                    preApprovalRequest.AfsPreApprovalRequest.EngineMake = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.Engine
                                                                 ? model.AirCraftInformation.EngineMake : null;
                    preApprovalRequest.AfsPreApprovalRequest.EngineModel = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.Engine
                                                                 ? model.AirCraftInformation.EngineModel : null;
                    preApprovalRequest.AfsPreApprovalRequest.EngineSerialNumber = model.AirCraftInformation.ProductTypeId == (int)ProductTypeEnum.Engine
                                                                 ? model.AirCraftInformation.EngineSerialNumber : null;
                }
                // Aircraft Owner && only for cat 10 ,12 and 13
                if (model.CategoryId.HasValue && model.CategoryId.Value != (int)CategoryEnum.Grp11 && model.AirCraftOwnerInformation != null)
                {
                    preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerName = model.AirCraftOwnerInformation.AircraftOwnerName;
                    preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerAddress = new Address
                    {
                        AddressLine1 = model.AirCraftOwnerInformation.AircraftOwnerAddress.Address1,
                        AddressLine2 = model.AirCraftOwnerInformation.AircraftOwnerAddress.Address2,
                        City = model.AirCraftOwnerInformation.AircraftOwnerAddress.City,
                        CountryId = model.AirCraftOwnerInformation.AircraftOwnerAddress.Country?.Id,
                        StateId = model.AirCraftOwnerInformation.AircraftOwnerAddress.Country?.Id == 184 ? model.AirCraftOwnerInformation.AircraftOwnerAddress.State?.Id : null,
                        ZipCode = model.AirCraftOwnerInformation.AircraftOwnerAddress.ZipCode,
                    };
                    //Grp10
                    preApprovalRequest.AfsPreApprovalRequest.IsRegistrationIssuedInYear = model.CategoryId.Value == (int)CategoryEnum.Grp10 ? model.AirCraftOwnerInformation.IsRegistrationIssuedInYear : null;
                    preApprovalRequest.AfsPreApprovalRequest.IsMoreThan20Passengers = model.CategoryId.Value == (int)CategoryEnum.Grp10 ? model.AirCraftOwnerInformation.IsMoreThan20Passengers : null;

                    if (model.AirCraftOwnerInformation != null && model.AirCraftOwnerInformation.IsRegistrationIssuedInYear.HasValue && model.AirCraftOwnerInformation.IsRegistrationIssuedInYear.Value)
                    {
                        preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationNumber = model.AirCraftOwnerInformation.PreviousAircraftRegistrationNumber;
                        preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationDate = model.AirCraftOwnerInformation.PreviousAircraftRegistrationDate.ToNullableDate();
                        preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationCountryId = model.AirCraftOwnerInformation.PreviousAircraftRegistrationCountryId;
                    }
                    else
                    {
                        preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationNumber = null;
                        preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationDate = (DateTime?)null;
                        preApprovalRequest.AfsPreApprovalRequest.PreviousAircraftRegistrationCountryId = null;
                    }
                    // only on Grp 12 or isMoreThan20Passengers is selected
                    if (model.AirCraftOperatorInformation != null && model.CategoryId.HasValue && model.CategoryId.Value == (int)CategoryEnum.Grp12 || (model.AirCraftOwnerInformation.IsMoreThan20Passengers.HasValue && model.AirCraftOwnerInformation.IsMoreThan20Passengers.Value))
                    {
                        preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorName = model.AirCraftOperatorInformation.AircraftOperatorName;
                        preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorAddress = new Address
                        {
                            AddressLine1 = model.AirCraftOperatorInformation.AircraftOperatorAddress.Address1,
                            AddressLine2 = model.AirCraftOperatorInformation.AircraftOperatorAddress.Address2,
                            City = model.AirCraftOperatorInformation.AircraftOperatorAddress.City,
                            CountryId = model.AirCraftOperatorInformation.AircraftOperatorAddress.Country?.Id,
                            StateId = model.AirCraftOperatorInformation.AircraftOperatorAddress.Country?.Id == 184 ? model.AirCraftOperatorInformation.AircraftOperatorAddress.State?.Id : null,
                            ZipCode = model.AirCraftOperatorInformation.AircraftOperatorAddress.ZipCode,
                        };
                        preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorCertificationNumber = model.AirCraftOperatorInformation.AircraftOperatorCertificationNumber;
                        preApprovalRequest.AfsPreApprovalRequest.AircraftInspectionProgram = model.AirCraftOperatorInformation.AircraftInspectionProgram;
                    }
                    //Certification Basis for Grp 13
                    if (model.CategoryId.HasValue && model.CategoryId.Value == (int)CategoryEnum.Grp13 && model.CertificationBasis != null)
                    {
                        preApprovalRequest.AfsPreApprovalRequest.CertificationBasisId = model.CertificationBasis.CertificationBasisId;
                        preApprovalRequest.AfsPreApprovalRequest.PerformerName = model.CertificationBasis.PerformerName;
                        preApprovalRequest.AfsPreApprovalRequest.PerformerCertificateNumber = model.CertificationBasis.PerformerCertificateNumber;
                        preApprovalRequest.AfsPreApprovalRequest.PerformerPhoneNumber = model.CertificationBasis.PerformerPhoneNumber;

                        preApprovalRequest.AfsPreApprovalRequest.AssistingEngineerName = model.CertificationBasis.AssistingEngineerName;
                        preApprovalRequest.AfsPreApprovalRequest.AssistingEngineerCertificateNumber = model.CertificationBasis.AssistingEngineerCertificateNumber;
                        preApprovalRequest.AfsPreApprovalRequest.AssistingEngineerPhoneNumber = model.CertificationBasis.AssistingEngineerPhoneNumber;
                        preApprovalRequest.AfsPreApprovalRequest.AssistingEngineerDesigneeNumber = model.CertificationBasis.AssistingEngineerDesigneeNumber;
                        preApprovalRequest.AfsPreApprovalRequest.CertificationBasisProjectDescription = model.CertificationBasis.CertificationBasisProjectDescription;
                    }
                }
            }
            _preApprovalRequest = preApprovalRequest;
        }
        public override bool SaveMsDecision(PreApprovalRequestViewModel model)
        {
            return base.SaveMsDecision(model);
        }
        protected override IEnumerable<PreApprovalDateWiseCountViewModel> GetPreApprovalDateWiseCount(int applicationId)
        {
            return base.GetPreApprovalDateWiseCount(applicationId);
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
        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity involved")]
        public static bool FindModifiedDataForDart(PreApprovalRequestViewModel model, PreApprovalRequest preApprovalRequest, AfsPostActivityModifiedPreApprovalViewModel afsPostActivityModifiedPreApprovalViewModel, bool triggerCorrectiveActiveTrigger)
        {
            if (model.FacilityInformation.Airport.Id != preApprovalRequest.AfsPreApprovalRequest.AirportId)
            {
                afsPostActivityModifiedPreApprovalViewModel.NearestAirportId = model.FacilityInformation.Airport.Id;
                triggerCorrectiveActiveTrigger = true;
            }
            if (model.ApplicantInformation.Name != preApprovalRequest.AfsPreApprovalRequest.ApplicantName)
            {
                afsPostActivityModifiedPreApprovalViewModel.ApplicantName = model.ApplicantInformation.Name;
                triggerCorrectiveActiveTrigger = true;
            }

            if (model.CategoryId == (int)CategoryEnum.Grp10 || model.CategoryId == (int)CategoryEnum.Grp12 || model.CategoryId == (int)CategoryEnum.Grp13)
            {
                if (model.AirCraftInformation.AircraftRegistrationNumber != preApprovalRequest.AfsPreApprovalRequest.AircraftRegistrationNumber)
                {
                    afsPostActivityModifiedPreApprovalViewModel.AircraftRegistrationNumber = model.AirCraftInformation.AircraftRegistrationNumber;
                    triggerCorrectiveActiveTrigger = true;
                }
                if ((model.AirCraftInformation.IsAmBuiltLightSport == null || (model.AirCraftInformation.IsAmBuiltLightSport.HasValue && !model.AirCraftInformation.IsAmBuiltLightSport.Value))
                                && model.AirCraftInformation.AircraftMakeModelId?.Id != preApprovalRequest.AfsPreApprovalRequest.AircraftMakeModelId)
                {
                    afsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId = model.AirCraftInformation.AircraftMakeModelId?.Id;
                    model.AirCraftInformation.AircraftMakeModel = "";
                    model.AirCraftInformation.IsAmBuiltLightSport = false;
                }
                if (model.AirCraftInformation.AircraftSerialNumber != preApprovalRequest.AfsPreApprovalRequest.AircraftSerialNumber)
                {
                    afsPostActivityModifiedPreApprovalViewModel.AircraftSerialNumber = model.AirCraftInformation.AircraftSerialNumber;
                    triggerCorrectiveActiveTrigger = true;
                }
                if (model.AirCraftInformation.IsAmBuiltLightSport.HasValue && model.AirCraftInformation.IsAmBuiltLightSport.Value && model.AirCraftInformation.AircraftMakeModel != preApprovalRequest.AfsPreApprovalRequest.AircraftMakeModel)
                {
                    afsPostActivityModifiedPreApprovalViewModel.AircraftMakeModel = model.AirCraftInformation.AircraftMakeModel;
                    afsPostActivityModifiedPreApprovalViewModel.IsAmBuiltLightSport = true;
                    afsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId = null;
                    triggerCorrectiveActiveTrigger = true;

                }

                if (model.CategoryId == (int)CategoryEnum.Grp10 || model.CategoryId == (int)CategoryEnum.Grp12)
                {
                    if (model.AirCraftOwnerInformation.AircraftOwnerName != preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerName)
                    {
                        afsPostActivityModifiedPreApprovalViewModel.AirCraftOwnerName = model.AirCraftOwnerInformation.AircraftOwnerName;
                        triggerCorrectiveActiveTrigger = true;
                    }
                    if (model.AirCraftOperatorInformation.AircraftOperatorName != preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorName)
                    {
                        afsPostActivityModifiedPreApprovalViewModel.AirCraftOperatorName = model.AirCraftOperatorInformation.AircraftOperatorName;
                        triggerCorrectiveActiveTrigger = true;
                    }
                }
                if (model.CategoryId == (int)CategoryEnum.Grp10 && model.AirCraftOperatorInformation.AircraftInspectionProgram != preApprovalRequest.AfsPreApprovalRequest.AircraftInspectionProgram)
                {
                        afsPostActivityModifiedPreApprovalViewModel.AircraftInspectionProgram = model.AirCraftOperatorInformation.AircraftInspectionProgram;
                        triggerCorrectiveActiveTrigger = true;
                }
                if (model.CategoryId == (int)CategoryEnum.Grp12 && model.AirCraftOperatorInformation.AircraftOperatorCertificationNumber != preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorCertificationNumber)
                {
                        afsPostActivityModifiedPreApprovalViewModel.AircraftOperatorCertificationNumber = model.AirCraftOperatorInformation.AircraftOperatorCertificationNumber;
                        triggerCorrectiveActiveTrigger = true;
                }

                if (model.CategoryId == (int)CategoryEnum.Grp13)
                {
                    if (model.AirCraftInformation.EngineMake != preApprovalRequest.AfsPreApprovalRequest.EngineMake)
                    {
                        afsPostActivityModifiedPreApprovalViewModel.EngineMake = model.AirCraftInformation.EngineMake;
                        triggerCorrectiveActiveTrigger = true;
                    }
                    if (model.AirCraftInformation.EngineModel != preApprovalRequest.AfsPreApprovalRequest.EngineModel)
                    {
                        afsPostActivityModifiedPreApprovalViewModel.EngineModel = model.AirCraftInformation.EngineModel;
                        triggerCorrectiveActiveTrigger = true;
                    }
                    if (model.AirCraftInformation.EngineSerialNumber != preApprovalRequest.AfsPreApprovalRequest.EngineSerialNumber)
                    {
                        afsPostActivityModifiedPreApprovalViewModel.EngineSerialNumber = model.AirCraftInformation.EngineSerialNumber;
                        triggerCorrectiveActiveTrigger = true;
                    }
                }
            }

            triggerCorrectiveActiveTrigger = CheckForAddress(model, preApprovalRequest, afsPostActivityModifiedPreApprovalViewModel, triggerCorrectiveActiveTrigger) || triggerCorrectiveActiveTrigger;

            return triggerCorrectiveActiveTrigger;
        }

        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity inherent to task navigation system")]
        private void LoadModifiedDataForDart(PreApprovalRequestViewModel preApprovalRequestViewModel, AfsPostActivityViewModel afsPostActivity, Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls)
        {
            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.NearestAirportId != null && afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.NearestAirportId > 0 && preApprovalRequestViewModel.FacilityInformation != null)
            {
                if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.NearestAirportId.HasValue)
                {
                    var airId = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.NearestAirportId.GetValueOrDefault();
                    preApprovalRequestViewModel.FacilityInformation.Airport = _lookupService.LookupValues("airports").Result.Airports.Where(s => s.Id == airId).Select(x => new BaseLookup
                    {
                        Id = x.Id,
                        Name = x.Name
                    }).FirstOrDefault();
                }

                AddItemToModifiedPreapprovals("FacilityInformation", new ModifiedControlViewModel { Control = "airport" }, modifiedPreapprovalControls);

            }
            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.ApplicantName))
            {
                preApprovalRequestViewModel.ApplicantInformation.Name = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.ApplicantName;
                AddItemToModifiedPreapprovals("ApplicantInformation", new ModifiedControlViewModel { Control = "name" }, modifiedPreapprovalControls);
            }
            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftRegistrationNumber))
            {
                preApprovalRequestViewModel.AirCraftInformation.AircraftRegistrationNumber = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftRegistrationNumber;
                AddItemToModifiedPreapprovals("AirCraftInformation", new ModifiedControlViewModel { Control = "aircraftRegistrationNumber" }, modifiedPreapprovalControls);
            }
            if ((afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsAmBuiltLightSport == null || (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsAmBuiltLightSport.HasValue && !afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsAmBuiltLightSport.Value))
                && afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId != null && afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId > 0 && preApprovalRequestViewModel.AirCraftInformation != null
                && afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId.HasValue)
            {
                    var mId = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftMakeModelId.GetValueOrDefault();
                    preApprovalRequestViewModel.AirCraftInformation.AircraftMakeModelId = _lookupService.LookupValues("makeModelSeries").Result.MakeModelSeries.Where(s => s.Id == mId).Select(x => new BaseLookup
                    {
                        Id = x.Id,
                        Name = $"{x.Code}/{x.Make}/{x.Model}"
                    }).FirstOrDefault();
                    AddItemToModifiedPreapprovals("AirCraftInformation", new ModifiedControlViewModel { Control = "aircraftMakeModelId" }, modifiedPreapprovalControls);
                    preApprovalRequestViewModel.AirCraftInformation.AircraftMakeModel = "";
                    preApprovalRequestViewModel.AirCraftInformation.IsAmBuiltLightSport = false;
            }

            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftSerialNumber))
            {
                preApprovalRequestViewModel.AirCraftInformation.AircraftSerialNumber = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftSerialNumber;
                AddItemToModifiedPreapprovals("AirCraftInformation", new ModifiedControlViewModel { Control = "aircraftSerialNumber" }, modifiedPreapprovalControls);
            }

            if (afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsAmBuiltLightSport.HasValue && afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.IsAmBuiltLightSport.Value)
            {
                preApprovalRequestViewModel.AirCraftInformation.AircraftMakeModel = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftMakeModel;
                preApprovalRequestViewModel.AirCraftInformation.IsAmBuiltLightSport = true;
                AddItemToModifiedPreapprovals("AirCraftInformation", new ModifiedControlViewModel { Control = "aircraftMakeModel" }, modifiedPreapprovalControls);
                preApprovalRequestViewModel.AirCraftInformation.AircraftMakeModelId = null;
            }

            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AirCraftOwnerName))
            {
                preApprovalRequestViewModel.AirCraftOwnerInformation.AircraftOwnerName = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AirCraftOwnerName;
                AddItemToModifiedPreapprovals("AirCraftOwnerInformation", new ModifiedControlViewModel { Control = "aircraftOwnerName" }, modifiedPreapprovalControls);
            }
            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AirCraftOperatorName))
            {
                preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftOperatorName = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AirCraftOperatorName;
                AddItemToModifiedPreapprovals("AirCraftOperatorInformation", new ModifiedControlViewModel { Control = "aircraftOperatorName" }, modifiedPreapprovalControls);
            }
            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftInspectionProgram))
            {
                preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftInspectionProgram = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftInspectionProgram;
                AddItemToModifiedPreapprovals("AirCraftOperatorInformation", new ModifiedControlViewModel { Control = "aircraftInspectionProgram" }, modifiedPreapprovalControls);
            }
            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftOperatorCertificationNumber))
            {
                preApprovalRequestViewModel.AirCraftOperatorInformation.AircraftOperatorCertificationNumber = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.AircraftOperatorCertificationNumber;
                AddItemToModifiedPreapprovals("AirCraftOperatorInformation", new ModifiedControlViewModel { Control = "aircraftOperatorCertificationNumber" }, modifiedPreapprovalControls);
            }

            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.EngineMake))
            {
                preApprovalRequestViewModel.AirCraftInformation.EngineMake = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.EngineMake;
                AddItemToModifiedPreapprovals("AirCraftInformation", new ModifiedControlViewModel { Control = "engineMake" }, modifiedPreapprovalControls);
            }
            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.EngineModel))
            {
                preApprovalRequestViewModel.AirCraftInformation.EngineModel = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.EngineModel;
                AddItemToModifiedPreapprovals("AirCraftInformation", new ModifiedControlViewModel { Control = "engineModel" }, modifiedPreapprovalControls);
            }
            if (!string.IsNullOrEmpty(afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.EngineSerialNumber))
            {
                preApprovalRequestViewModel.AirCraftInformation.EngineSerialNumber = afsPostActivity.AfsPostActivityModifiedPreApprovalViewModel.EngineSerialNumber;
                AddItemToModifiedPreapprovals("AirCraftInformation", new ModifiedControlViewModel { Control = "engineSerialNumber" }, modifiedPreapprovalControls);
            }

        }

        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity involved")]
        public override AfsGroupsPostActivityViewModel Get(int postActivityId, bool createDocumentVersion = false)
        {
            base.Get(postActivityId, createDocumentVersion);

            _afsGroupsPostActivityViewModel.GeneralComments = _postActivity.GeneralComments;
            if (_preApprovalRequestViewModel.CategoryId == (int)CategoryEnum.Grp10)
            {
                var certificateCategories = _context.PostActivityCertificateCategories.Where(it => it.IsActive).ToArray();
                //Prepare collection items by data and lookup data
                var postActivityCertificates = new List<PostActivityCertificateViewModel>();
                var postActivityCertificateType = _context.PostActivityCertificateTypes.Where(it => it.IsActive).ToArray();
                foreach (var poct in postActivityCertificateType)
                {
                    var result = _postActivity.PostActivityCertificates.FirstOrDefault(itp => itp.CertificateTypeId == poct.Id);
                    postActivityCertificates.Add(new PostActivityCertificateViewModel()
                    {
                        Id = result == null ? 0 : result.Id,
                        CertificateType = new BaseLookup() { Id = poct.Id, Name = poct.Name },
                        IsSelected = result == null ? false : true,
                        CertificateTypeId = result == null ? (int?)null : result.CertificateTypeId,
                        CertificateCategoryId = result == null ? (int?)null : result.CertificateCategoryId,
                        IssuanceTypeId = result == null ? (int?)null : result.IssuanceTypeId,
                        IssuanceDate = result == null ? string.Empty : result.IssuanceDate.HasValue ? result.IssuanceDate.Value.ToString("MM/dd/yyyy"): string.Empty,
                        CertificateCategories = certificateCategories.Where(it => it.CertficateTypeId == poct.Id).Select(its => new LookupItem
                        {
                            value = its.Id,
                            label = its.Name
                        }).ToArray()
                    });
                }
                _afsGroupsPostActivityViewModel.AfsPostActivityDart176 = new AfsPostActivityDart176ViewModel
                {
                    ResultTypeId = _postActivity.PostActivityResultTypeId,
                    ActionReasonSummary = _postActivity.ActionReasonSummary,
                    IsCertificateIssued = _postActivity.IsCertificateIssued,
                    PostActivityCertificates = postActivityCertificates,
                    ResultsSubmissionDate = _postActivity.ResultsSubmissionDate.HasValue ? _postActivity.ResultsSubmissionDate.Value.ToShortDateString() : string.Empty
                };
            }
            else if (_preApprovalRequestViewModel.CategoryId == (int)CategoryEnum.Grp11)
            {
                var preapprovalActivityProducts = _context.PreApprovalRequestProducts
                                                                .Where(pr => pr.PreApprovalRequestId == _afsGroupsPostActivityViewModel.PreApprovalRequestId)
                                                                .Select(prod =>
                                                                     new AfsPostActivityProductViewModel
                                                                     {
                                                                         Id = prod.Id,
                                                                         PostActivityId = _postActivity.Id,
                                                                         Make = prod.Make,
                                                                         Model = prod.Model,
                                                                         SerialNumber = prod.SerialNumber,
                                                                         ResultTypeId = prod.ResultTypeId
                                                                     }).ToList();
                var postActivityProducts = _postActivity.PostActivityProducts.Select(prod =>
                                                                         new AfsPostActivityProductViewModel
                                                                         {
                                                                             Id = prod.Id,
                                                                             PostActivityId = prod.PostActivityId,
                                                                             Make = prod.Make,
                                                                             Model = prod.Model,
                                                                             SerialNumber = prod.SerialNumber,
                                                                             ResultTypeId = prod.ResultTypeId
                                                                         }).ToList();
                _afsGroupsPostActivityViewModel.AfsPostActivityDart196 = new AfsPostActivityDart196ViewModel
                {
                    IssuedExportApprovalQuantity = _postActivity.IssuedExportApprovalQuantity,
                    DeniedExportApprovalQuantity = _postActivity.DeniedExportApprovalQuantity,
                    CancelledExportApprovalQuantity = _postActivity.CancelledExportApprovalQuantity,
                    DomesticIssuedApprovalQuantity = _postActivity.DomesticIssuedApprovalQuantity,
                    DomesticDeniedtApprovalQuantity = _postActivity.DomesticDeniedtApprovalQuantity,
                    DomesticCancelledApprovalQuantity = _postActivity.DomesticCancelledApprovalQuantity,
                    WorkCompletedDate = _postActivity.WorkCompletedDate.HasValue ? _postActivity.WorkCompletedDate.Value.ToShortDateString() : string.Empty,
                    Products = _postActivity.StatusId == (int)PreApprovalRequestStatusEnum.Initiated ? preapprovalActivityProducts : postActivityProducts
                };
            }
            else if (_preApprovalRequestViewModel.CategoryId == (int)CategoryEnum.Grp12)
            {
                _afsGroupsPostActivityViewModel.AfsPostActivityDart198 = new AfsPostActivityDart198ViewModel
                {
                    IsAircraftRecordsReview = _postActivity.IsAircraftRecordsReview,
                    AircraftRecordsReviewDate = _postActivity.AircraftRecordsReviewDate.HasValue ? _postActivity.AircraftRecordsReviewDate.Value.ToShortDateString() : string.Empty,
                    IsAircraftInspection = _postActivity.IsAircraftInspection,
                    AircraftInspectionDate = _postActivity.AircraftInspectionDate.HasValue ? _postActivity.AircraftInspectionDate.Value.ToShortDateString() : string.Empty,
                    ResultsSubmissionDate = _postActivity.ResultsSubmissionDate.HasValue ? _postActivity.ResultsSubmissionDate.Value.ToShortDateString() : string.Empty,
                    SubmittedOfficeId = _postActivity.SubmittedOfficeId,
                    ActivityResultSummary = _postActivity.ActivityResultSummary
                };
            }
            else if (_preApprovalRequestViewModel.CategoryId == (int)CategoryEnum.Grp13)
            {
                _afsGroupsPostActivityViewModel.AfsPostActivityDart208 = new AfsPostActivityDart208ViewModel
                {
                    ResultTypeId = _postActivity.PostActivityResultTypeId,
                    ApprovalDate = _postActivity.ApprovalDate.HasValue ? _postActivity.ApprovalDate.Value.ToShortDateString() : string.Empty,
                    ActionReasonSummary = _postActivity.ActionReasonSummary,
                    ActivityResultSummary = _postActivity.ActivityResultSummary,
                    ResultsSubmissionDate = _postActivity.ResultsSubmissionDate.HasValue ? _postActivity.ResultsSubmissionDate.Value.ToShortDateString() : string.Empty
                };
            }

            _afsGroupsPostActivityViewModel.LocationDirections = _postActivity.LocationDirections;
            _afsGroupsPostActivityViewModel.PointOfContactName = _postActivity.PointOfContactName;
            _afsGroupsPostActivityViewModel.PointOfContactPhone = _postActivity.PointOfContactPhone;

            return _afsGroupsPostActivityViewModel;
        }
        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity", Justification = "Complexity involved")]
        public override AfsGroupsPostActivityViewModel SaveAfsPostActivity(AfsGroupsPostActivityViewModel model)
        {
            base.SaveAfsPostActivityHelper(model);

            if (model.PreApprovalRequest.CategoryId == (int)CategoryEnum.Grp10)
            {
                _postActivity.PostActivityResultTypeId = model.AfsPostActivityDart176.ResultTypeId;
                _postActivity.IsCertificateIssued = Convert.ToBoolean(model.AfsPostActivityDart176.IsCertificateIssued);
                _postActivity.ResultsSubmissionDate = model.AfsPostActivityDart176.ResultsSubmissionDate.ToNullableDate();
                _postActivity.ActionReasonSummary = model.AfsPostActivityDart176.ActionReasonSummary;
                //Insert Certificates
                if (model.AfsPostActivityDart176 != null && model.AfsPostActivityDart176.PostActivityCertificates != null && model.AfsPostActivityDart176.PostActivityCertificates.Any())
                {
                    //Remove existing Certificates
                    foreach (var fc in _postActivity.PostActivityCertificates.ToList())
                    {
                        _context.Entry(fc).State = EntityState.Deleted;
                    }
                    var postActivityCertificates = model.AfsPostActivityDart176.PostActivityCertificates
                     .Where(poc => poc.IsSelected)
                     .Select(prod =>
                        new PostActivityCertificate
                        {
                            PostActivityId = _postActivity.Id,
                            CertificateTypeId = Convert.ToInt32(prod.CertificateType.Id),
                            CertificateCategoryId = prod.CertificateCategoryId,
                            IssuanceTypeId = prod.IssuanceTypeId,
                            IssuanceDate = !string.IsNullOrEmpty(prod.IssuanceDate) ? DateTime.Parse(prod.IssuanceDate) : (DateTime?)null
                        }).ToList();

                    foreach (var newCert in postActivityCertificates)
                    {
                        _postActivity.PostActivityCertificates.Add(newCert);
                        _context.Entry(newCert).State = EntityState.Added;
                    }
                }
            }
            else if (model.PreApprovalRequest.CategoryId == (int)CategoryEnum.Grp11)
            {
                //Remove Existing Products
                foreach (var fc in _postActivity.PostActivityProducts.ToList())
                {
                    _context.Entry(fc).State = EntityState.Deleted;
                }                
                //Insert Products
                if (model.AfsPostActivityDart196 != null && model.AfsPostActivityDart196.Products != null && model.AfsPostActivityDart196.Products.Any())
                {

                    var postActivityProducts = model.AfsPostActivityDart196.Products.Select(prod =>
                       new PostActivityProduct
                       {
                           PostActivityId = _postActivity.Id,
                           Make = prod.Make,
                           Model = prod.Model,
                           SerialNumber = prod.SerialNumber,
                           ResultTypeId = prod.ResultTypeId
                       }).ToList();

                    foreach (var newProd in postActivityProducts)
                    {
                        _postActivity.PostActivityProducts.Add(newProd);
                        _context.Entry(newProd).State = EntityState.Added;
                    }
                }
                _postActivity.IssuedExportApprovalQuantity = model.AfsPostActivityDart196.IssuedExportApprovalQuantity;
                _postActivity.DeniedExportApprovalQuantity = model.AfsPostActivityDart196.DeniedExportApprovalQuantity;
                _postActivity.CancelledExportApprovalQuantity = model.AfsPostActivityDart196.CancelledExportApprovalQuantity;
                _postActivity.DomesticIssuedApprovalQuantity = model.AfsPostActivityDart196.DomesticIssuedApprovalQuantity;
                _postActivity.DomesticDeniedtApprovalQuantity = model.AfsPostActivityDart196.DomesticDeniedtApprovalQuantity;
                _postActivity.DomesticCancelledApprovalQuantity = model.AfsPostActivityDart196.DomesticCancelledApprovalQuantity;

                _postActivity.WorkCompletedDate = model.AfsPostActivityDart196.WorkCompletedDate.ToNullableDate();
            }
            else if (model.PreApprovalRequest.CategoryId == (int)CategoryEnum.Grp12)
            {
                _postActivity.IsAircraftRecordsReview = Convert.ToBoolean(model.AfsPostActivityDart198.IsAircraftRecordsReview);
                if (_postActivity.IsAircraftRecordsReview == true)
                {
                    _postActivity.AircraftRecordsReviewDate = model.AfsPostActivityDart198.AircraftRecordsReviewDate.ToNullableDate();
                }
                else
                {
                    _postActivity.AircraftRecordsReviewDate = null;
                }
                _postActivity.IsAircraftInspection = Convert.ToBoolean(model.AfsPostActivityDart198.IsAircraftInspection);

                if (_postActivity.IsAircraftInspection == true)
                {
                    _postActivity.AircraftInspectionDate = model.AfsPostActivityDart198.AircraftInspectionDate.ToNullableDate();
                }
                else
                {
                    _postActivity.AircraftInspectionDate = null;
                }
                _postActivity.ResultsSubmissionDate = model.AfsPostActivityDart198.ResultsSubmissionDate.ToNullableDate();
                _postActivity.SubmittedOfficeId = model.AfsPostActivityDart198.SubmittedOfficeId;
                _postActivity.ActivityResultSummary = model.AfsPostActivityDart198.ActivityResultSummary;
            }
            else if (model.PreApprovalRequest.CategoryId == (int)CategoryEnum.Grp13)
            {
                _postActivity.PostActivityResultTypeId = model.AfsPostActivityDart208.ResultTypeId != null ? model.AfsPostActivityDart208.ResultTypeId : null;
                if (_postActivity.PostActivityResultTypeId == (int)ResultTypeDartEnums.Denied || _postActivity.PostActivityResultTypeId == (int)ResultTypeDartEnums.Cancelled)
                {
                    _postActivity.ActionReasonSummary = model.AfsPostActivityDart208.ActionReasonSummary;
                }
                else
                {
                    _postActivity.ActionReasonSummary = null;
                }
                _postActivity.ApprovalDate = model.AfsPostActivityDart208.ApprovalDate.ToNullableDate();
                _postActivity.ActivityResultSummary = model.AfsPostActivityDart208.ActivityResultSummary;
                _postActivity.ResultsSubmissionDate = model.AfsPostActivityDart208.ResultsSubmissionDate.ToNullableDate();
            }

            _postActivity.LocationDirections = model.PreApprovalRequest.FacilityInformation.IsDirectionToLocationNeeded ? model.PreApprovalRequest.FacilityInformation.LocationDirections : null;
            _postActivity.PointOfContactName = model.PreApprovalRequest.FacilityInformation.PointOfContactName;
            _postActivity.PointOfContactPhone = model.PreApprovalRequest.FacilityInformation.pointOfContactPhone;

             if (!model.IsSubmit)
             {
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
            model.PreApprovalRequest.ModifiedPreapprovalControls = new Dictionary<string, List<ModifiedControlViewModel>>();
            var afsPostActivityModifiedPreApprovalViewModel = new AfsPostActivityModifiedPreApprovalViewModel();
            var preApprovalRequest = base.GetOriginalPreApprovalData(model.PreApprovalRequestId);

            var triggerCorrectiveActionTrigger = false;

            triggerCorrectiveActionTrigger = FindModifiedData(model.PreApprovalRequest, preApprovalRequest, model.PreApprovalRequest.ModifiedPreapprovalControls);
            var changedMakeModelDart = FindChangeInMakeOrModel(
                model.PreApprovalRequest.AirCraftInformation.AircraftMakeModelId?.Id,
                preApprovalRequest.AfsPreApprovalRequest.AircraftMakeModelId,
                model.PreApprovalRequest.ModifiedPreapprovalControls);
            triggerCorrectiveActionTrigger = changedMakeModelDart ? triggerCorrectiveActionTrigger || changedMakeModelDart : triggerCorrectiveActionTrigger;

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
        [SuppressMessage("SonarQube", "S3776:Refactor this method to reduce its Cognitive Complexity",
        Justification = "Complexity involved")]
        private bool FindModifiedData(PreApprovalRequestViewModel model, PreApprovalRequest preApprovalRequest,
                                                        Dictionary<string, List<ModifiedControlViewModel>> modifiedPreapprovalControls)
        {
            bool triggerCorrectiveAction = false;
            if (model.FacilityInformation.Airport.Id != preApprovalRequest.AfsPreApprovalRequest.AirportId)
            {
                AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("FacilityInformation", new ModifiedControlViewModel { Control = "airport" }, modifiedPreapprovalControls);
                triggerCorrectiveAction = true;
            }
            if (model.ApplicantInformation.Name != preApprovalRequest.AfsPreApprovalRequest.ApplicantName)
            {
                AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("ApplicantInformation", new ModifiedControlViewModel { Control = "name" }, modifiedPreapprovalControls);
                triggerCorrectiveAction = true;
            }

            if (model.CategoryId == (int)CategoryEnum.Grp10 || model.CategoryId == (int)CategoryEnum.Grp12 || model.CategoryId == (int)CategoryEnum.Grp13)
            {
                if (model.AirCraftInformation.AircraftRegistrationNumber != preApprovalRequest.AfsPreApprovalRequest.AircraftRegistrationNumber)
                {
                    AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("AirCraftInformation", new ModifiedControlViewModel { Control = "aircraftRegistrationNumber" }, modifiedPreapprovalControls);
                    triggerCorrectiveAction = true;
                }
                if ((model.AirCraftInformation.IsAmBuiltLightSport == null 
                                || (model.AirCraftInformation.IsAmBuiltLightSport.HasValue && !model.AirCraftInformation.IsAmBuiltLightSport.Value))
                                && model.AirCraftInformation.AircraftMakeModelId?.Id != preApprovalRequest.AfsPreApprovalRequest.AircraftMakeModelId)
                {
                    AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("AirCraftInformation", new ModifiedControlViewModel { Control = "aircraftMakeModelId" }, modifiedPreapprovalControls);
                    model.AirCraftInformation.AircraftMakeModel = "";
                    model.AirCraftInformation.IsAmBuiltLightSport = false;
                }
                if (model.AirCraftInformation.IsAmBuiltLightSport.HasValue 
                                && model.AirCraftInformation.IsAmBuiltLightSport.Value 
                                && model.AirCraftInformation.AircraftMakeModel != preApprovalRequest.AfsPreApprovalRequest.AircraftMakeModel)
                {
                    AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("AirCraftInformation", new ModifiedControlViewModel { Control = "aircraftMakeModel" }, modifiedPreapprovalControls);
                    triggerCorrectiveAction = true;
                }
                if (model.AirCraftInformation.AircraftSerialNumber != preApprovalRequest.AfsPreApprovalRequest.AircraftSerialNumber)
                {
                    AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("AirCraftInformation", new ModifiedControlViewModel { Control = "aircraftSerialNumber" }, modifiedPreapprovalControls);
                    triggerCorrectiveAction = true;
                }

                if (model.CategoryId == (int)CategoryEnum.Grp10 || model.CategoryId == (int)CategoryEnum.Grp12)
                {
                    if (model.AirCraftOwnerInformation.AircraftOwnerName != preApprovalRequest.AfsPreApprovalRequest.AircraftOwnerName)
                    {
                        AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("AirCraftOwnerInformation", new ModifiedControlViewModel { Control = "aircraftOwnerName" }, modifiedPreapprovalControls);
                        triggerCorrectiveAction = true;
                    }
                    if (model.AirCraftOperatorInformation.AircraftOperatorName != preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorName)
                    {
                        AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("AirCraftOperatorInformation", new ModifiedControlViewModel { Control = "aircraftOperatorName" }, modifiedPreapprovalControls);
                        triggerCorrectiveAction = true;
                    }
                }
                if (model.CategoryId == (int)CategoryEnum.Grp10 && model.AirCraftOperatorInformation.AircraftInspectionProgram != preApprovalRequest.AfsPreApprovalRequest.AircraftInspectionProgram)
                {
                        AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("AirCraftOperatorInformation", new ModifiedControlViewModel { Control = "aircraftInspectionProgram" }, modifiedPreapprovalControls);
                        triggerCorrectiveAction = true;
                }
                if (model.CategoryId == (int)CategoryEnum.Grp12 && model.AirCraftOperatorInformation.AircraftOperatorCertificationNumber != preApprovalRequest.AfsPreApprovalRequest.AircraftOperatorCertificationNumber)
                {
                        AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("AirCraftOperatorInformation", new ModifiedControlViewModel { Control = "aircraftOperatorCertificationNumber" }, modifiedPreapprovalControls);
                        triggerCorrectiveAction = true;
                }

                if (model.CategoryId == (int)CategoryEnum.Grp13)
                {
                    if (model.AirCraftInformation.EngineMake != preApprovalRequest.AfsPreApprovalRequest.EngineMake)
                    {
                        AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("AirCraftInformation", new ModifiedControlViewModel { Control = "engineMake" }, modifiedPreapprovalControls);
                        triggerCorrectiveAction = true;
                    }
                    if (model.AirCraftInformation.EngineModel != preApprovalRequest.AfsPreApprovalRequest.EngineModel)
                    {
                        AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("AirCraftInformation", new ModifiedControlViewModel { Control = "engineModel" }, modifiedPreapprovalControls);
                        triggerCorrectiveAction = true;
                    }
                    if (model.AirCraftInformation.EngineSerialNumber != preApprovalRequest.AfsPreApprovalRequest.EngineSerialNumber)
                    {
                        AfsPreApprovalRequestService.AddItemToModifiedPreapprovals("AirCraftInformation", new ModifiedControlViewModel { Control = "engineSerialNumber" }, modifiedPreapprovalControls);
                        triggerCorrectiveAction = true;
                    }
                }
            }

            triggerCorrectiveAction = CheckForAddress(model.ActivityLocation.LocationAddress,
                            preApprovalRequest.AfsPreApprovalRequest.Address,
                            modifiedPreapprovalControls,
                            model.ActivityLocation.FacilityonRecord
                                .GetValueOrDefault(),
                            preApprovalRequest.AfsPreApprovalRequest.FacilityOnRecord.GetValueOrDefault()) ||
                                triggerCorrectiveAction;

            return triggerCorrectiveAction;
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
