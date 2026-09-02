using Dms.Core.EntityFramework.Data;
using Dms.Core.Utils;
using Dms.Services.Interface.Activity;
using Dms.Services.Interface.Lookup;
using Dms.Services.Interface.Message;
using Dms.Services.Interface.Security;
using Dms.Services.Interface.Shared;
using Dms.Services.Interface.Task;

namespace Dms.Services.Implementation.Activity
{
    // Admin-PE-GEN reuses the same Pre-Approval / Post Activity process as Admin-PE 
    // This class only overrides the designee type; all other behavior(Save, Get, SaveAfsPostActivity, etc.) is inherited unchanged.
    public class AdminPeGenPreApprovalRequestService : AdminPePreApprovalRequestService
    {
        public AdminPeGenPreApprovalRequestService(DmsContext context, ITaskService taskService,
            IDocumentService documentService, IActivityService activityService, IMessageService messageService,
            ISharedService sharedService, ILookupService lookupService, IUserService userService)
            : base(context, taskService, documentService, activityService, messageService, sharedService, lookupService,
                userService)
        {
            _designeeType = (int)DesigneeTypeEnum.ADMINPEGEN;
        }
    }
}