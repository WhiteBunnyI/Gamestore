using Gamestore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace Gamestore.Controllers;

[ApiController]
public abstract class AppControllerBase
{
    protected readonly DbCtx _ctx;
    protected readonly ILogger _logger;

    protected abstract string Entity { get; }

    protected static readonly string FOREIGN_KEY_VIOLATION_MESSAGE = "На данную строку ссылаются другие строки из других таблиц";
    protected static readonly string NOT_FOUND_MESSAGE = "Строка не найдена";

    protected static readonly Func<string, string> NOT_FOUND_EXACT_MESSAGE = (data) => $"{data} не найден(а)";
    protected static readonly Func<string, string> CONFLICT_EXACT_MESSAGE = (data) => $"{data} уже существует";
    protected static readonly Func<string, string> SUCCESS_EXACT_MESSAGE = (data) => $"{data} был(а) успешно добавлен(а)";

    protected readonly string NOT_FOUND_AUTO_MESSAGE;
    protected readonly string CONFLICT_AUTO_MESSAGE;
    protected readonly string SUCCESS_AUTO_MESSAGE;

    public AppControllerBase(DbCtx db, ILogger logger)
    {
        _ctx = db;
        _logger = logger;

        NOT_FOUND_AUTO_MESSAGE = NOT_FOUND_EXACT_MESSAGE(Entity);
        SUCCESS_AUTO_MESSAGE = SUCCESS_EXACT_MESSAGE(Entity);
        CONFLICT_AUTO_MESSAGE = CONFLICT_EXACT_MESSAGE(Entity);
    }
}