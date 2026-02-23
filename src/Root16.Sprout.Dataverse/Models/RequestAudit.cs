using Microsoft.Xrm.Sdk;
using Root16.Sprout.Logging;

namespace Root16.Sprout.Dataverse.Models;

public sealed record RequestAudit(OrganizationRequest? Request, Audit? Audit);