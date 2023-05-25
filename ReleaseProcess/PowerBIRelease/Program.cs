namespace PowerBIRelease
{
    using System.ComponentModel;
    using System.Drawing;
    using System.IO.Compression;
    using System.Text.Json.Serialization;
    using System.Threading;
    using Business;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using NLog.Config;
    using Shared.General;

    internal class Program
    {
        private static Action<String, ConsoleColor> write;

        public static Action<String> writeNormal;

        public static Action<String> writePositive;

        public static Action<String> writeNegative;

        static Boolean whatIf = false;

        static async Task Main(string[] args)
        {
            // Arguments are 
            //   Customer Name
            //   Version To Release   

            // Init logging
            write = (msg, color) => {
                        Console.ForegroundColor = color;
                        Console.WriteLine(msg);
                        Console.ForegroundColor = ConsoleColor.White;
                    };
            writeNormal = (msg) => write(msg, ConsoleColor.White);
            writePositive = (msg) => write(msg, ConsoleColor.Green);
            writeNegative = (msg) => write(msg, ConsoleColor.Red);

            if (args.Length < 2){
                Program.writeNegative("No Arguments specified");
            }
            
            var customer = args[0];
            var releaseVersion = args[1];
            if (args.Length == 3){
                Boolean.TryParse(args[2], out whatIf);
            }

            CancellationToken cancellationToken = CancellationToken.None;
            
            // get all the configuration needed
            LoadConfiguration(customer);

            await InitialiseServices();

            await GetReleasePackage(releaseVersion, configuration.OutputPath, cancellationToken);

            // Assets have now been downloaded
            // Create a working folder for the customer
            String customerDirectory = CreateCustomerDirectory(customerConfiguration.Name, configuration.OutputPath);

            await DeployDataModel(customerDirectory, cancellationToken);

            await DeployDataSets(customerDirectory, cancellationToken);

            await DeployReports(customerDirectory, cancellationToken);

            // TODO: Set App Version number
        }

        private static void LoadConfiguration(String customer){
            IConfigurationBuilder builder = new ConfigurationBuilder();
            
            String a = Directory.GetCurrentDirectory();
            builder.SetBasePath(Directory.GetCurrentDirectory());
            // TODO: Include development files
            builder.AddJsonFile("appsettings.json", true);
            builder.AddJsonFile($"appsettings.{customer}.json", true);

            IConfigurationRoot configurationRoot = builder.Build();

            ConfigurationReader.Initialise(configurationRoot);

            // Build the config objects
            Program.configuration = new Configuration{
                                                         PowerBiApiUrl = ConfigurationReader.GetValue("PowerBiApiUrl"),
                                                         AuthorityUri = ConfigurationReader.GetValue("AuthorityUri"),
                                                         ClientId = Guid.Parse(ConfigurationReader.GetValue("ClientId")),
                                                         TenantId = Guid.Parse(ConfigurationReader.GetValue("TenantId")),
                                                         Scopes = ConfigurationReader.GetValue("Scopes"),
                                                         ClientSecret = ConfigurationReader.GetValue("ClientSecret"),
                                                         FileImportCheckRetryAttempts = Int32.Parse(ConfigurationReader.GetValue("FileImportCheckRetryAttempts")),
                                                         FileImportCheckSleepIntervalInSeconds = Int32.Parse(ConfigurationReader.GetValue("FileImportCheckSleepIntervalInSeconds")),
                                                         OutputPath = ConfigurationReader.GetValue("OutputPath")
                                                     };

            Program.customerConfiguration = new CustomerConfiguration{
                                                                         DatabaseServer = ConfigurationReader.GetValue("DatabaseServer"),
                                                                         GroupId = Guid.Parse(ConfigurationReader.GetValue("GroupId")),
                                                                         DatabaseName = ConfigurationReader.GetValue("DatabaseName"),
                                                                         CustomerId = Guid.Parse(ConfigurationReader.GetValue("CustomerId")),
                                                                         DatabasePassword = ConfigurationReader.GetValue("DatabasePassword"),
                                                                         DatabaseUserId = ConfigurationReader.GetValue("DatabaseUserId"),
                                                                         Name = ConfigurationReader.GetValue("Name")
                                                                     };
        }

        private static ITokenService tokenService;
        private static IPowerBiService powerBiService;
        private static Configuration configuration;
        private static IDatabaseManager databaseManager;

        private static CustomerConfiguration customerConfiguration;

        private static async Task InitialiseServices(){
            databaseManager = new DatabaseManager();
            tokenService = new TokenService(configuration.AuthorityUri,
                                                          configuration.ClientId,
                                                          Program.configuration.Scopes,
                                                          configuration.TenantId,
                                                          configuration.ClientSecret);
            powerBiService = new PowerBiService(tokenService, configuration.PowerBiApiUrl);
            powerBiService.TraceMessage += (sender, s) => Program.writeNormal(s);
            powerBiService.SuccessMessage += (sender, s) => Program.writePositive(s);
            powerBiService.ErrorMessage+= (sender, s) => Program.writeNegative(s);
            await Program.powerBiService.Initialise();
        }

        private static async Task DeployReports(String customerDirectory, CancellationToken cancellationToken){
            if (Directory.Exists($"{customerDirectory}/Reports") == false){
                writeNormal($"No reports to deploy found at path {customerDirectory}/Reports");
                return;
            }

            String[] reportFiles = Directory.GetFiles($"{customerDirectory}/Reports",
                                                      "*.pbix",
                                                      SearchOption.AllDirectories);

            if (reportFiles.Any() == false){
                writeNormal($"No reports to deploy found at path {customerDirectory}/Reports");
                return;
            }

            // We have some files to release
            writeNormal($"About to deploy {reportFiles.Length} reports to customers workspace");

            foreach (String reportFile in reportFiles){
                try{
                    writeNormal($"About to deploy {reportFile}");
                    if (Program.whatIf == false){
                        Boolean result = await powerBiService.DeployReport(customerConfiguration,
                                                                           reportFile,
                                                                           cancellationToken);
                        if (result == false){
                            throw new Exception("deployment failed");
                        }
                    }
                }
                catch(Exception ex){
                    throw new Exception($"Report Name: {reportFile}, {ex.Message}");
                }
            }
        }

        private static async Task DeployDataSets(String customerDirectory, CancellationToken cancellationToken)
        {
            if (Directory.Exists($"{customerDirectory}/Datasets") == false){
                writeNormal($"No datasets to deploy found at path {customerDirectory}/Datasets");
                return;
            }

            String[] datasetFiles = Directory.GetFiles($"{customerDirectory}/Datasets", "*.pbix", SearchOption.AllDirectories);
            
            if (datasetFiles.Any() == false){
                writeNormal($"No datasets to deploy found at path {customerDirectory}/Datasets");
                return;
            }

            writeNormal($"About to deploy {datasetFiles.Length} datasets to customers workspace");

            foreach (String datasetFile in datasetFiles)
            {
                try
                {
                    writeNormal($"About to deploy {datasetFile}");
                    if (Program.whatIf == false){
                        Boolean result = await powerBiService.DeployDataset(customerConfiguration,
                                                                            datasetFile,
                                                                            cancellationToken);
                        if (result == false){
                            throw new Exception("deployment failed");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Dataset Name: {datasetFile}, {ex.Message}");
                }
            }
        }

        private static async Task GetReleasePackage(String versionNumber, String outputPath, CancellationToken cancellationToken)
        {
            String extractFolder = $"{outputPath}/extract";
            if (Directory.Exists(extractFolder))
            {
                Directory.Delete(extractFolder, true);
            }
            Directory.CreateDirectory(extractFolder);

            ZipFile.ExtractToDirectory($"{outputPath}/v{versionNumber}.zip", extractFolder, overwriteFiles: true);
            
            writeNormal($"Assets for release {versionNumber} successfully extracted");
        }

        private static async Task DeployDataModel(String customerDirectory, CancellationToken cancellationToken){
            if (Directory.Exists($"{customerDirectory}/DataModel") == false){
                writeNormal("No database scripts to be released");
                return;
            }

            String[]? databaseScripts = Directory.GetFiles($"{customerDirectory}/DataModel",
                                                           "*.sql",
                                                           SearchOption.AllDirectories);

            if (databaseScripts.Any() == false){
                writeNormal("No database scripts to be released");
                return;
            }

            writeNormal($"About to deploy {databaseScripts.Length} scripts to customers read model");
            writeNormal($"Connection String: {Program.customerConfiguration.DatabaseConnectionString}");
            foreach (String databaseScript in databaseScripts){
                try{
                    writeNormal($"About to deploy {databaseScript}");

                    if (Program.whatIf == false){
                        await databaseManager.ExecuteScript(Program.customerConfiguration.DatabaseConnectionString, databaseScript, cancellationToken);
                    }

                    writePositive($"{databaseScript} deployed successfully");
                }
                catch(Exception ex){
                    throw new Exception($"Script: {databaseScript} deployment failed, {ex.Message}");
                }
            }
        }
        
        private static String CreateCustomerDirectory(String customer, String outputPath){
            String customerDirectory = $"{outputPath}/{customer}";
            if (Directory.Exists(customerDirectory)){
                Directory.Delete(customerDirectory, true);
            }

            Directory.CreateDirectory(customerDirectory);

            CopyFilesRecursively($"{outputPath}/extract", customerDirectory);
            writeNormal($"Copied extracted files to customer working directory [{customerDirectory}]");
            return customerDirectory;
        }

        private static Boolean CopyFilesRecursively(String sourcePath, String targetPath){
            // Now Create all of the directories
            foreach (String dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)){
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            // Copy all the files & Replaces any files with the same name
            foreach (String newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories)){
                String destFileName = newPath.Replace(sourcePath, targetPath);
                File.Copy(newPath, destFileName, true);
            }

            return true;
        }
    }

    public record Configuration{
        public String PowerBiApiUrl { get; init; }
        public String AuthorityUri { get; init; }
        public Guid ClientId { get; init; }
        public Guid TenantId { get; init; }
        public String Scopes { get; init; }
        public String ClientSecret { get; init; }
        public Int32 FileImportCheckRetryAttempts { get; init; }
        public Int32 FileImportCheckSleepIntervalInSeconds { get; init; }
        public String OutputPath { get; init; }
    }

    public record CustomerConfiguration{
        public String Name { get; init; }
        public Guid GroupId { get; init; }
        public Guid CustomerId { get; init; }
        public String DatabaseServer { get; init; }
        public String DatabaseUserId { get; init; }
        public String DatabasePassword { get; init; }
        public String DatabaseName { get; init; }
        public String DatabaseConnectionString => $"Server={DatabaseServer};Database={DatabaseName};User Id={DatabaseUserId};Password={DatabasePassword};";
    }
}