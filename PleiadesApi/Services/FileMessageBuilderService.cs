using MessagingApi;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Text;

namespace PleiadesApi.Services;

/// <summary>
/// File-based message builder service. This service uses templates from
/// the <c>messages</c> folder in the web root path.
/// </summary>
public sealed class FileMessageBuilderService : MessageBuilderServiceBase
{
    private readonly string _messageDir;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMessageBuilderService"/>
    /// class.
    /// </summary>
    /// <param name="environment">The environment.</param>
    /// <param name="options">The options.</param>
    public FileMessageBuilderService(MessagingOptions options,
        IHostEnvironment environment) : base(options)
    {
        // http://stackoverflow.com/questions/35842547/read-solution-data-files-asp-net-core/35863315
        _messageDir = Path.Combine(
            environment.ContentRootPath,
            "wwwroot",
            "messages") + Path.DirectorySeparatorChar;
    }

    /// <summary>
    /// Loads the template with the specified name.
    /// </summary>
    /// <param name="templateName">Name of the template.</param>
    /// <returns>
    /// template text, or null if not found
    /// </returns>
    protected override string? LoadTemplate(string templateName)
    {
        // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/file-providers?view=aspnetcore-3.0

        IFileProvider provider = new PhysicalFileProvider(_messageDir);
        IFileInfo info = provider.GetFileInfo(templateName + ".html");
        if (!info.Exists) return null;

        string template;
        using (StreamReader reader = new(info.CreateReadStream(), Encoding.UTF8))
        {
            template = reader.ReadToEnd();
        }
        return template;
    }
}
