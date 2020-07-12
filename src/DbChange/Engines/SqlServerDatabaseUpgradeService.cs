using System;
using DbUp;
using DbUp.ScriptProviders;
using System.Data.SqlClient;
using System.Reflection;
using Serilog;
using DbUp.Engine;
using System.Collections.Generic;
using DbUp.Engine.Transactions;
using System.IO;

namespace GrowingData.DbChange {
	class SqlServerDatabaseUpgradeService {


		public bool ApplyUpgrades(string scriptPath, string connectionString) {
			if (connectionString.StartsWith("$")) {
				// Use the 
				connectionString = Environment.GetEnvironmentVariable(connectionString.Substring(1));
			}
			var scriptPathInfo = new DirectoryInfo(scriptPath);


			var connectionParser = new SqlConnectionStringBuilder(connectionString);
			Log.Information("Executing upgrade on: {database}", connectionParser.InitialCatalog);

			var fsOptions = new FileSystemScriptOptions() {
				//Extensions = new string[] { ".sql" },
				IncludeSubDirectories = true,
				Filter = (file) => {
					Log.Information("Found file {filename} for consideration", file);
					return true;
				}
			};
			var upgrader =
				DeployChanges.To
					.SqlDatabase(connectionString)
					//.WithScripts(provider)
					.WithScriptsFromFileSystem(scriptPathInfo.FullName, fsOptions)
					.LogToAutodetectedLog()
					.Build();

			var result = upgrader.PerformUpgrade();
			if (!result.Successful) {
				Log.Error(result.Error, "DatabaseUpgradeService.UpgradeSchema Failed: {message}\r\n {stack}", result.Error.Message, result.Error.StackTrace);
				return false;
			}
			return true;
		}
	}

}
