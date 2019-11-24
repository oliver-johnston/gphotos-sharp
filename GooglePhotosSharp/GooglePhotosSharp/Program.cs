using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using CommandLine;
using Google.Apis.Util;
using GooglePhotosSharp.Config;
using GooglePhotosSharp.Database;
using GooglePhotosSharp.GooglePhotos;
using GooglePhotosSharp.LocalPhotos;
using Newtonsoft.Json;
using Serilog;

namespace GooglePhotosSharp
{
    class Program
    {
        public static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptionsAndReturnExitCode)
                .WithNotParsed(HandleParseError);
        }

        private static void RunOptionsAndReturnExitCode(Options opts)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            var configs = GetConfigs(opts.ConfigFile);

            var fileSystem = new FileSystem();
            foreach (var config in configs)
            {
                var library = new LocalPhotoLibrary(config, fileSystem);
                var database = new DatabaseRepository(config.Path);
                var api = new GooglePhotosApi(config.Email, SystemClock.Default);

                var uploader = new Uploader(config, library, database, api);
                uploader.UploadPhotos().Wait();
            }
        }

        private static IList<PhotoLibraryConfig> GetConfigs(string configFile)
        {
            var text = File.ReadAllText(configFile);
            return JsonConvert.DeserializeObject<IList<PhotoLibraryConfig>>(text);
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            foreach (var e in errs)
            {
                Console.WriteLine(e);
            }
        }

        class Options
        {
            [Option("configFile")] public string ConfigFile { get; set; }
        }
    }
}