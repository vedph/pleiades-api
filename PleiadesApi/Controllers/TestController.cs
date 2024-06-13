using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MessagingApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace PleiadesApi.Controllers;

/// <summary>
/// Diagnostic functions.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TestController" /> class.
/// </remarks>
/// <param name="messageBuilderService">The message builder service.</param>
/// <param name="mailerService">The mailer service.</param>
/// <param name="config">The configuration.</param>
/// <exception cref="ArgumentNullException">logger</exception>
[ApiController]
public sealed class TestController(
    IMessageBuilderService messageBuilderService,
    IMailerService mailerService,
    IConfiguration config) : ControllerBase
{
    private readonly IMessageBuilderService _messageBuilderService =
        messageBuilderService;
    private readonly IMailerService _mailerService = mailerService;
    private readonly IConfiguration _config = config;
    private readonly bool _enabled = config.GetSection("Diagnostics")
        .GetValue<bool>("IsTestEnabled");

    /// <summary>
    /// Adds a diagnostic entry to the log.
    /// </summary>
    /// <returns>OK</returns>
    [HttpPost("api/test/log")]
    [ProducesResponseType(200)]
    public void AddLogEntry()
    {
        if (!_enabled) return;

        Log.Logger.Information("Diagnostic log entry posted at {Now} UTC " +
                               "from IP {IP}",
            DateTime.UtcNow,
            HttpContext.Connection.RemoteIpAddress);
    }

    /// <summary>
    /// Raises an exception to test for logging.
    /// </summary>
    /// <exception cref="Exception">error</exception>
    [HttpGet("api/test/exception")]
    [ProducesResponseType(500)]
    public void RaiseError()
    {
        if (!_enabled) return;

        Exception exception = new("Fake exception raised for test purposes");
        Log.Logger.Error(exception, "Fake exception");
        throw exception;
    }

    /// <summary>
    /// Sends a test email message.
    /// </summary>
    [HttpGet("api/test/email")]
    [ProducesResponseType(200)]
    public async Task SendEmail()
    {
        if (!_enabled) return;

        string? to = _config.GetValue<string>("Mailer:TestRecipient");
        if (string.IsNullOrEmpty(to))
        {
            Log.Logger.Warning("No recipient defined for test email");
            return;
        }

        Log.Logger.Information("Building test email message for {Recipient}", to);
        Message? message = _messageBuilderService.BuildMessage("test-message",
            new Dictionary<string, string>());

        if (message != null)
        {
            Log.Logger.Information("Sending test email message");
            await _mailerService.SendEmailAsync(
                to,
                "Test Recipient",
                message);
            Log.Logger.Information("Test email message sent");
        }
    }
}