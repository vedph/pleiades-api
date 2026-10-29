using Fusi.Api.Auth.Controllers;
using Fusi.Api.Auth.Models;
using Fusi.Api.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace PleiadesApi.Controllers;

/// <summary>
/// Users browsing and updates.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UserController"/> class.
/// </remarks>
/// <param name="repository">The repository.</param>
/// <param name="logger">The logger.</param>
[ApiController]
public sealed class UserController(IUserRepository<NamedUser> repository,
    ILogger<UserControllerBase<NamedUser, NamedUserBindingModel>> logger) :
    UserControllerBase<NamedUser, NamedUserBindingModel>(repository, logger)
{
}
