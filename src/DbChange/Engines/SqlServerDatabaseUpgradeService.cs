using System;
using System.Linq;
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
			var customLogger = new DbUpLogger();
			var upgrader =
				DeployChanges.To
					.SqlDatabase(connectionString)
					//.WithScripts(provider)
					.WithScriptsFromFileSystem(scriptPathInfo.FullName, fsOptions)
					//.LogToAutodetectedLog()
					.LogToNowhere()
					.LogTo(customLogger)
					.Build();

			var result = upgrader.PerformUpgrade();
			if (!result.Successful) {
				var scripts = customLogger.BrokenScripts;
				var sqlException = result.Error as SqlException;

				var scriptName = "unknown";
				if (scripts.Count > 0){
					scriptName = scripts.FirstOrDefault();
				}

				Log.Error("UpgradeSchema failed: {message} in {scriptName} (Line number: {lineNumber})", 
					result.Error.Message, scriptName, sqlException.LineNumber);
				return false;
			}
			return true;
		}
	}
	public class DbUpLogger : DbUp.Engine.Output.IUpgradeLog {
		public List<string> BrokenScripts = new List<string>();

		public void WriteError(string format, params object[] args) {
			// The error messages here contain a lot of junk I am not interested in
			return;
			Log.Error(format, args);
			//throw new NotImplementedException();
		}

		public void WriteInformation(string format, params object[] args) {
			if (format == "SQL exception has occured in script: '{0}'") {
				BrokenScripts.Add((string)args[0]);
				Log.Error(format, args);
				return;
			}
			if (format == "DB exception has occured in script: '{0}'") {
				// We will have already logged it as above...
				return;
			}

			Log.Information(format, args);

		}

		public void WriteWarning(string format, params object[] args) {
			Log.Warning(format, args);
			//throw new NotImplementedException();
		}
	}

}
