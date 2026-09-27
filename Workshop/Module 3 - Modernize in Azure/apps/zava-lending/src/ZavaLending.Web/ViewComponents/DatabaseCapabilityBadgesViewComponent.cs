using Microsoft.AspNetCore.Mvc;
using ZavaLending.Web.Data;
using ZavaLending.Web.Models;

namespace ZavaLending.Web.ViewComponents;

public sealed class DatabaseCapabilityBadgesViewComponent(
    IDatabaseCapabilityService capabilityService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        DatabaseCapabilityWorkspace workspace)
    {
        var capabilities = await capabilityService.GetCapabilitiesAsync(
            HttpContext.RequestAborted);

        return View(new DatabaseCapabilityBadgesViewModel(
            capabilities,
            workspace));
    }
}
