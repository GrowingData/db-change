using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace GrowingData.DbChange {
	[Verb("script")]
	public class ScriptCliOptions {

		[Option('p', "path", Required = true, HelpText = "The path to SQL files to apply")]
		public string SqlPath { get; set; }


		[Option('c', "connection", Required = true, HelpText = "The connection string to apply changes to")]
		public string ConnectionString { get; set; }


		[Option('e', "engine", Required = true, HelpText = "The engine to target (e.g. 'mssql')")]
		public string Engine{ get; set; }

	}
}
