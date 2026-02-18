using Microsoft.AspNetCore.Mvc;
using ZeltekWeb.Constants;

namespace ZeltekWeb.Controllers
{
    public abstract class BaseController : Controller
    {
        protected int GetTenantId()
        {
            var claim = User.FindFirst(AppConstants.TenantIdClaim) 
                     ?? User.FindFirst("TenantID") // Legacy/Fallback
                     ?? User.FindFirst("TenantId"); // Just in case constant changes

            return claim != null && int.TryParse(claim.Value, out int id) ? id : 0;
        }
    }
}
