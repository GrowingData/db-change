using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Data.SqlClient;
using Serilog;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using System.IO;

namespace GrowingData.DbChange {
	public class SqlServerScriptingService {

		private string _connectionString;
		private SqlConnectionStringBuilder _details;

		private static HashSet<string> IgnoreScripts = new HashSet<string>(new string[] {
			"SET ANSI_NULLS ON",
			"SET ANSI_PADDING ON\r\n",
			"SET QUOTED_IDENTIFIER ON"
		});

		private static KeyValuePair<string, string>[] Replacers = new KeyValuePair<string, string>[] {
			new KeyValuePair<string, string>("WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)", ""),
			new KeyValuePair<string, string>("WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)", ""),
			new KeyValuePair<string, string>("\r\n CONSTRAINT", "\r\n\tCONSTRAINT"),
			new KeyValuePair<string, string>("\r\nREFERENCES", "\r\n\tREFERENCES"),
			new KeyValuePair<string, string>("\r\n(", "\r\n\t("),
			new KeyValuePair<string, string>("\r\n)", "\r\n\t)"),

		};

		public SqlServerScriptingService(string connectionString) {
			if (connectionString.StartsWith("$")) {
				// Use the 
				connectionString = Environment.GetEnvironmentVariable(connectionString.Substring(1));
			}

			_connectionString = connectionString;
			_details = new SqlConnectionStringBuilder(_connectionString);
		}

		public Server GetServer() {
			var builder = new SqlConnectionStringBuilder(_connectionString);

			var connection = new ServerConnection(_details.DataSource, _details.UserID, _details.Password);
			var server = new Server(connection);
			return server;
		}

		private void VerifyDirectory(DirectoryInfo d) {
			// We only want the things that currently exist, so kill everythign else
			if (d.Exists) {
				Directory.Delete(d.FullName, true);
			}

			Directory.CreateDirectory(d.FullName);
		}


		public bool GenerateScripts(DirectoryInfo rootScriptsPath) {
			VerifyDirectory(rootScriptsPath);

			var tablesPath = new DirectoryInfo(Path.Combine(rootScriptsPath.FullName, "tables"));
			VerifyDirectory(tablesPath);
			GenerateTableScripts(tablesPath);

			var viewsPath = new DirectoryInfo(Path.Combine(rootScriptsPath.FullName, "views"));
			VerifyDirectory(viewsPath);
			GenerateViewScripts(viewsPath);


			var procsPath = new DirectoryInfo(Path.Combine(rootScriptsPath.FullName, "procs"));
			VerifyDirectory(procsPath);
			GenerateStoredProcedureScripts(procsPath);


			var udfPath = new DirectoryInfo(Path.Combine(rootScriptsPath.FullName, "functions"));
			VerifyDirectory(udfPath);
			GenerateFunctionScripts(udfPath);

			return true;
		}

		private void GenerateScript(ScriptSchemaObjectBase scriptObject, ScriptingOptions scriptOptions, DirectoryInfo path) {

			var tableScript = ((IScriptable)scriptObject).Script(scriptOptions);
			var sqlBuilder = new StringBuilder();
			foreach (string str in tableScript) {
				var script = FixScript(str);
				if (!string.IsNullOrEmpty(script)) {
					sqlBuilder.AppendLine(script);
					sqlBuilder.AppendLine("---");
				}
			}
			var filename = $"{scriptObject.Schema}.{scriptObject.Name}.sql";
			var sqlPath = Path.Combine(path.FullName, filename);

			Log.Information("Generating script for: {schema}.{table} -> {path}",
				scriptObject.Schema, scriptObject.Name, sqlPath);

			File.WriteAllText(sqlPath, sqlBuilder.ToString());

		}

		public bool GenerateFunctionScripts(DirectoryInfo functionsPath) {
			ScriptingOptions scriptOptions = DefaultOptions();
			var server = GetServer();

			var database = server.Databases[_details.InitialCatalog];
			foreach (UserDefinedFunction udf in database.UserDefinedFunctions) {
				// Check the schema first as its much faster than the IsSystemObject call which hits the db again
				if (udf.Schema != "sys" && !udf.IsSystemObject) {
					GenerateScript(udf, scriptOptions, functionsPath);
				}
			}

			return true;
		}

		public bool GenerateStoredProcedureScripts(DirectoryInfo procsPath) {
			ScriptingOptions scriptOptions = DefaultOptions();
			var server = GetServer();
			var database = server.Databases[_details.InitialCatalog];
			foreach (StoredProcedure proc in database.StoredProcedures) {
				// Check the schema first as its much faster than the IsSystemObject call which hits the db again
				if (proc.Schema != "sys" && !proc.IsSystemObject) {
					GenerateScript(proc, scriptOptions, procsPath);
				}
			}
			return true;
		}

		public bool GenerateViewScripts(DirectoryInfo viewsPath) {
			ScriptingOptions scriptOptions = DefaultOptions();
			var server = GetServer();

			var database = server.Databases[_details.InitialCatalog];
			foreach (View view in database.Views) {
				if (view.Schema != "sys" && !view.IsSystemObject) {
					GenerateScript(view, scriptOptions, viewsPath);
				}
			}

			return true;
		}


		public bool GenerateTableScripts(DirectoryInfo tablesPath) {
			ScriptingOptions scriptOptions = DefaultOptions();
			var server = GetServer();

			var database = server.Databases[_details.InitialCatalog];
			foreach (Table table in database.Tables) {
				// Check the schema first as its much faster than the IsSystemObject call which hits the db again
				if (table.Schema != "sys" && !table.IsSystemObject) {
					var tableScript = table.Script(scriptOptions);
					var sqlBuilder = new StringBuilder();
					foreach (string str in tableScript) {
						var script = FixScript(str);
						if (!string.IsNullOrEmpty(script)) {
							sqlBuilder.AppendLine(script);
							sqlBuilder.AppendLine("---");
						}
					}

					var filename = $"{table.Schema}.{table.Name}.sql";
					var sqlPath = Path.Combine(tablesPath.FullName, filename);

					Log.Information("Generating script for: {schema}.{table} -> {path}",
						table.Schema, table.Name, sqlPath);

					File.WriteAllText(sqlPath, sqlBuilder.ToString());
				}
			}

			return true;
		}

		private string FixScript(string script) {
			if (IgnoreScripts.Contains(script)) {
				return null;
			}
			var fixedString = script;
			foreach (var k in Replacers) {
				fixedString = fixedString.Replace(k.Key, k.Value);
			}

			if (fixedString.EndsWith("\t)\r\n")) {
				fixedString = fixedString.Substring(0, fixedString.Length - 4) + ")";
			}
			return fixedString;

		}

		public ScriptingOptions DefaultOptions() {
			ScriptingOptions options = new ScriptingOptions();
			options.AllowSystemObjects = false;

			//options.AnsiFile = true;
			options.AppendToFile = false;

			options.ClusteredIndexes = true;
			options.ColumnStoreIndexes = true;

			options.Default = true;

			options.DriAll = false;
			options.DriAllConstraints = true;
			options.DriAllKeys = false;
			options.DriClustered = false;
			options.DriIndexes = false;
			options.DriNonClustered = false;

			options.EnforceScriptingOptions = true;
			options.ExtendedProperties = true;
			options.ExtendedProperties = true;
			options.FullTextCatalogs = true;
			options.FullTextIndexes = true;
			options.FullTextStopLists = true;


			options.IncludeDatabaseContext = false;
			options.IncludeHeaders = false;
			options.IncludeIfNotExists = false;

			options.Indexes = true;
			options.NoCollation = true;
			options.NoCommandTerminator = false;
			options.NoExecuteAs = true;
			options.NonClusteredIndexes = true;
			options.Permissions = false;
			options.SchemaQualify = true;
			options.SchemaQualifyForeignKeysReferences = true;
			options.ScriptBatchTerminator = true;
			options.ToFileOnly = true;
			options.Triggers = true;
			options.WithDependencies = false;
			options.XmlIndexes = true;

			options.NoFileGroup = true;


			return options;
		}

	}
}
