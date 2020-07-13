using System;
using DbUp;
using DbUp.ScriptProviders;
using System.Data.SqlClient;
using System.Reflection;
using Serilog;
using CommandLine;
using Serilog.Sinks.SystemConsole.Themes;
using System.IO;

namespace GrowingData.DbChange {
	class Program {
		static int Main(string[] args) {
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.WriteTo.Console(theme: AnsiConsoleTheme.Literate)
				.CreateLogger();

			Log.Information("db-change: Starting");
			var result = Parser.Default.ParseArguments<UpgradeCliOptions, ScriptCliOptions>(args)
				.MapResult(
					(UpgradeCliOptions o) => Upgrade(o),
					(ScriptCliOptions o) => GenerateScripts(o),
					errs => -1
				);


			if (result == 0) {
				Log.Information("db-change: Success!");
				return 0;
			}

			Log.Information("db-change: Failed, exiting");
			return -1;
		}

		private static int Upgrade(UpgradeCliOptions o) {
			// Looks like we are upgrading!   
			if (o.Engine == "mssql") {

				var sqlUpgrade = new SqlServerDatabaseUpgradeService();
				var success = sqlUpgrade.ApplyUpgrades(o.SqlPath, o.ConnectionString);
				return success ? 0 : -1;
			}
			return -1;
		}

		private static int GenerateScripts(ScriptCliOptions o) {
			if (o.Engine == "mssql") {

				var sqlUpgrade = new SqlServerScriptingService(o.ConnectionString);
				var success = sqlUpgrade.GenerateScripts(new DirectoryInfo(o.SqlPath));
				return success ? 0 : -1;
			}
			return -1;
		}
	}

}

