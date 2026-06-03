using Gamestore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace Gamestore.Controllers;

[ApiController]
public abstract class AppControllerBase : ControllerBase
{
    protected readonly DbCtx _ctx;
    protected readonly ILogger _logger;

    public AppControllerBase(DbCtx db, ILogger logger)
    {
        _ctx = db;
        _logger = logger;
    }
}