using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Dms.Core.EntityFramework.Model.Shared;
using Dms.Core.EntityFramework.Model.Lookup;

namespace Dms.Core.EntityFramework.Model.Activity
{
    [Table("AfsPreApprovalRequest", Schema = "dbo")]
    public class AfsPreApprovalRequest
    {
        [Key]
        [ForeignKey("PreApprovalRequest")]
        public int PreApprovalRequestId { get; set; }
        public virtual PreApprovalRequest PreApprovalRequest { get; set; }

        [ForeignKey("Address")]
        public int? ActivityLocationAddressId { get; set; }
        public virtual Address Address { get; set; }

        [ForeignKey("Airport")]
        public int? AirportId { get; set; }
        public virtual Airport Airport { get; set; }
        [MaxLength(100)]
        public string PointOfContactName { get; set; }
        [MaxLength(25)]
        public string PointOfContactPhone { get; set; }
        public string LocationDirections { get; set; }
        public bool? IsOutsideOfficeDistrict { get; set; }
        public bool? IsActivityOutsideUsa { get; set; }
        [ForeignKey("Office")]
        public int? OfficeId { get; set; }
        public virtual Office Office { get; set; }
        [MaxLength(100)]
        public string ApplicantName { get; set; }

        [MaxLength(25)]
        public string ApplicantPhone { get; set; }

        [MaxLength(100)]
        public string ApplicantEmailAddress { get; set; }

        [MaxLength(100)]
        public string AircraftRegistrationNumber { get; set; }

        public DateTime? AircraftRegistrationDate { get; set; }

        [ForeignKey("AircraftMakeMode")]
        public int? AircraftMakeModelId { get; set; }
        public virtual MakeModel AircraftMakeMode { get; set; }

        [MaxLength(100)]
        public string AircraftSerialNumber { get; set; }
        [MaxLength(100)]
        public string AircraftMakeModel { get; set; }
        public bool? IsAmBuiltLightSport { get; set; }

        [MaxLength(100)]
        public string AircraftOwnerName { get; set; }

        [ForeignKey("AircraftOwnerAddress")]
        public int? AircraftOwnerAddressId { get; set; }
        public virtual Address AircraftOwnerAddress { get; set; }

        public bool? IsRegistrationIssuedInYear { get; set; }
        public bool? IsMoreThan20Passengers { get; set; }
        [MaxLength(100)]
        public string PreviousAircraftRegistrationNumber { get; set; }
        public DateTime? PreviousAircraftRegistrationDate { get; set; }

        [ForeignKey("PreviousAircraftRegistrationCountry")]
        public int? PreviousAircraftRegistrationCountryId { get; set; }
        public virtual Country PreviousAircraftRegistrationCountry { get; set; }

        [MaxLength(100)]
        public string AircraftOperatorName { get; set; }

        [ForeignKey("AircraftOperatorAddress")]
        public int? AircraftOperatorAddressId { get; set; }
        public virtual Address AircraftOperatorAddress { get; set; }

        public string AircraftOperatorCertificationNumber { get; set; }
        [MaxLength(100)]
        public string AircraftInspectionProgram { get; set; }

        [ForeignKey("CertificationBasis")]
        public int? CertificationBasisId { get; set; }
        public virtual CertificationBasis CertificationBasis { get; set; }


        [MaxLength(100)]
        public string PerformerName { get; set; }

        [MaxLength(25)]
        public string PerformerCertificateNumber { get; set; }
        [MaxLength(25)]
        public string PerformerPhoneNumber { get; set; }


        [MaxLength(100)]
        public string AssistingEngineerName { get; set; }

        [MaxLength(25)]
        public string AssistingEngineerCertificateNumber { get; set; }
        [MaxLength(25)]
        public string AssistingEngineerPhoneNumber { get; set; }

        [MaxLength(25)]
        public string AssistingEngineerDesigneeNumber { get; set; }

        public string CertificationBasisProjectDescription { get; set; }

        [ForeignKey("PracticalOralTestType")]
        public int? PracticalOralTestId { get; set; }
        public virtual PracticalOralTestType PracticalOralTestType { get; set; }
        [MaxLength(30)]
        public string ApplicantCertificateNumber { get; set; }

        [ForeignKey("CertificateRatingType")]
        public int? CertificateRatingTypeId { get; set; }
        public virtual CertificateRatingType CertificateRatingType { get; set; }

        [ForeignKey("PreApprovalRequestExperienceType")]
        public int? ExperienceTypeId { get; set; }
        public virtual PreApprovalRequestExperienceType PreApprovalRequestExperienceType { get; set; }

        [ForeignKey("School")]
        public int? SchoolId { get; set; }
        public virtual School School { get; set; }

        public bool? IsCfrSectionTest { get; set; }

        [ForeignKey("CfrSectionSchool")]
        public int? CfrSectionSchoolId { get; set; }
        public virtual School CfrSectionSchool { get; set; }

        public bool? IsCivilExperience { get; set; }

        public bool? IsMilitaryExperience { get; set; }

        [ForeignKey("AuthorizedTestOffice")]
        public int? AuthorizedTestOfficeId { get; set; }
        public virtual Office AuthorizedTestOffice { get; set; }

        [ForeignKey("ProductType")]
        public int? ProductTypeId { get; set; }
        public virtual ProductType ProductType { get; set; }

        public string EngineMake { get; set; }
        public string EngineModel { get; set; }
        public string EngineSerialNumber { get; set; }

        [ForeignKey("Category")]
        public int? CategoryId { get; set; }
        public virtual Category Category { get; set; }

        public int? IssuedExportApprovalQuantity { get; set; }
        public int? IssuedDomesticApprovalQuantity { get; set; }

        public string TemporaryAuthorizationReason { get; set; }

        [ForeignKey("PreApprovalRequestAircraftClassType")]
        public int? PreApprovalRequestAircraftClassTypeId { get; set; }
        public virtual PreApprovalRequestAircraftClassType PreApprovalRequestAircraftClassType { get; set; }
        [ForeignKey("PreApprovalRequestGradeCertificateType")]
        public int? PreApprovalRequestGradeCertificateTypeId { get; set; }
        public virtual PreApprovalRequestGradeCertificateType PreApprovalRequestGradeCertificateType { get; set; }
        [ForeignKey("PreApprovalRequestAircraftCategoryType")]
        public int? PreApprovalRequestAircraftCategoryTypeId { get; set; }
        public virtual PreApprovalRequestAircraftCategoryType PreApprovalRequestAircraftCategoryType { get; set; }
        public bool? IsAircraftNotRequired { get; set; }
        public bool? IsFlightPortionOnly { get; set; }
        public bool? IsRecommendingInstructorNotAvailable { get; set; }
        public string RecommendingInstructor { get; set; }
        public string RecommendingInstructorCertificateNumber { get; set; }
        [ForeignKey("PilotLicenseIssuedCountry")]
        public int? PilotLicenseIssuedCountryId { get; set; }
        public virtual Country PilotLicenseIssuedCountry { get; set; }
        [ForeignKey("AirCarrier")]
        public int? AirCarrierId { get; set; }
        public virtual AirCarrier AirCarrier { get; set; }
        public DateTime? RevisedDate { get; set; }
        public bool? IsMultipleApplicants { get; set; }
        public bool? IsOtherAdminActivity { get; set; }
        public virtual PreApprovalRequestCancellationType PreApprovalRequestCancellationType { get; set; }
        [ForeignKey("PreApprovalRequestCancellationType")]
        public int? CancellationTypeId { get; set; }
        public string JustificationForCancellation { get; set; }
        public string NameOfApprovedTrainingProgram { get; set; }
        public string GraduatedFromCurriculum { get; set; }
        public string DispatcherCertificationCourse { get; set; }
        public string DispatcherCertificationCourseLocation { get; set; }
        public virtual PreApprovalObservationType PreApprovalObservationType { get; set; }
        [ForeignKey("PreApprovalObservationType")]
        public int? ObservationTypeId { get; set; }
        public bool? IsTrainingDeviceTestCheck { get; set; }
        public virtual Simulator Simulator { get; set; }
        [ForeignKey("Simulator")]
        public int? SimulatorId { get; set; }
        public bool? IsAircraftTestCheck { get; set; }
        public string AirlineFlightNumber { get; set; }

        public virtual PreApprovalRequestTypeOfCheck PreApprovalRequestTypeOfCheck { get; set; }
        [ForeignKey("PreApprovalRequestTypeOfCheck")]
        public int? TypeOfCheckId { get; set; }
        public bool? IsLineCheck { get; set; }
        public bool? FacilityOnRecord { get; set; }
    }
}
