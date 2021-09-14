using System;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Pleiades.Tool.Commands
{
    public sealed class RootCommand : ICommand
    {
        private readonly CommandLineApplication _app;

        public RootCommand(CommandLineApplication app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public static void Configure(CommandLineApplication app, AppOptions options)
        {
            // configure all the app commands here
            app.Command("scan-graph", c => ScanGraphCommand.Configure(c, options));
            app.Command("import-graph", c => ImportGraphCommand.Configure(c, options));
            app.Command("create-db", c => CreateDbCommand.Configure(c, options));
            app.Command("build-index", c => BuildIndexCommand.Configure(c, options));
            app.Command("val-geo", c => ValidateGeoCommand.Configure(c, options));
            app.Command("pop-spatial", c => PopulateSpatialCommand.Configure(c, options));
            app.Command("build-query", c => BuildQueryCommand.Configure(c, options));
            app.Command("export", c => BulkExportCommand.Configure(c, options));

            app.OnExecute(() =>
            {
                options.Command = new RootCommand(app);
                return 0;
            });
        }

        public Task Run()
        {
            _app.ShowHelp();
            return Task.FromResult(0);
        }
    }
}
