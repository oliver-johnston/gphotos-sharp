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

            (var libraries, var clientSecret) = GetConfig(opts.ConfigFolder);

            var fileSystem = new FileSystem();
            foreach (var config in libraries)
            {
                var library = new LocalPhotoLibrary(config, fileSystem);
                var database = new DatabaseRepository(config.Path);
                var api = new GooglePhotosApi(config.Email, clientSecret, SystemClock.Default);

                var uploader = new Uploader(config, library, database, api);
                uploader.UploadPhotos().Wait();
            }
        }

        private static (IList<PhotoLibraryConfig>, string) GetConfig(string configFolder)
        {
            var configFile = Path.Combine(configFolder, "config.json");
            if (!File.Exists(configFile))
            {
                throw new ArgumentException($"{configFile} doesn't exist");
            }

            var config = JsonConvert.DeserializeObject<IList<PhotoLibraryConfig>>(configFile);

            var clientSecretFile = Path.Combine(configFolder, "client_secret.json");
            if (!File.Exists(clientSecretFile))
            {
                throw new ArgumentException($@"{clientSecretFile} doesn't exist");
            }

            return (config, clientSecretFile);
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
            [Option("configFolder", Default = "~/.config/gphotos-sharp")]
            public string ConfigFolder { get; set; }
        }
    }
}