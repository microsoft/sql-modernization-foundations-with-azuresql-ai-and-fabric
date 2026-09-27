using ZavaLending.Web.Models;

namespace ZavaLending.Web.Data;

public interface IDatabaseCapabilityService
{
    Task<DatabaseCapabilities> GetCapabilitiesAsync(
        CancellationToken cancellationToken = default);

    void Invalidate();
}
