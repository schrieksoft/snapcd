// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

// using System.Diagnostics;
// using System.IO.Compression;
// using System.Text.Json;
// using System.Text.RegularExpressions;
// using MassTransit;
// using Microsoft.Extensions.Options;
// using SnapCd.Server.Clients;
// using SnapCd.Server.Clients.ModuleCache;
// using SnapCd.Server.Dto;
// using SnapCd.Server.Entities.TerraformModuleManager;
// using SnapCd.Server.Events.Processing;
// using SnapCd.Server.Misc.Extensions;
// using SnapCd.Server.Settings.Runner;
//
// namespace SnapCd.Server.Services;
//
// public class LegacyTerraformModuleManager<TRequest> where TRequest : ProcessingModuleRequestBase
// {
//     private readonly Git<TRequest> _git;
//
//     private readonly ILogger<LegacyTerraformModuleManager<TRequest>> _logger;
//     private readonly IModuleCacheClient _moduleCacheClient;
//     private readonly ModuleManagerSettings _settings;
//     private readonly ConsumeContext<TRequest> _context;
//     private readonly string _tempPathModuleZip;
//     private readonly string _tempPathGitExcludedStash;
//
//
//     public LegacyTerraformModuleManager(
//         ILogger<LegacyTerraformModuleManager<TRequest>> logger,
//         IOptions<ModuleManagerSettings> options,
//         IModuleCacheClient moduleCacheClient,
//         Git<TRequest> git,
//         ConsumeContext<TRequest> context
//     )
//     {
//         _logger = logger;
//         _settings = options.Value;
//         _git = git;
//         _moduleCacheClient = moduleCacheClient;
//         _context = context;
//
//         CreateDirectoryIfNotExists(_settings.RepoRootPath);
//         CreateDirectoryIfNotExists(_settings.TempPath);
//         _tempPathModuleZip = Path.Combine(_settings.TempPath, "modulezip");
//         _tempPathGitExcludedStash = Path.Combine(_settings.TempPath, "gitexcludedstash");
//         CreateDirectoryIfNotExists(_tempPathModuleZip);
//         CreateDirectoryIfNotExists(_tempPathGitExcludedStash);
//     }
//
//     public async Task AddExtraFiles(HashSet<string> filesAddedPostCheckout, List<ExtraFileDto> extraFiles, string moduleRelativePath)
//     {
//         var modulePath = Path.Combine(_settings.RepoRootPath, moduleRelativePath);
//         _context.LogInformation(_logger, $"Now adding extra files to folder \"{moduleRelativePath}\"", nameof(AddExtraFiles));
//
//         foreach (var file in extraFiles)
//         {
//             var path = Path.Combine(modulePath, file.FileName);
//             if (File.Exists(path) && !filesAddedPostCheckout.Contains(path))
//             {
//                 if (file.Overwrite == true)
//                     await File.WriteAllTextAsync(path, file.Contents);
//             }
//             else
//             {
//                 await File.WriteAllTextAsync(path, file.Contents);
//             }
//         }
//     }
//
//
//     public async Task<HashSet<string>> CloneGitRepository(string repoPath, string targetRepoUrl, string targetRepoRevision)
//     {
//         var objectName =
//             $"{targetRepoUrl}?ref={targetRepoRevision}";
//
//         if (targetRepoRevision == "") objectName = targetRepoUrl;
//
//         var latestLocalSha = _git.GetLatestLocalSha(repoPath);
//
//         var latestSha = _git.GetLatestRemoteSha(
//             targetRepoUrl,
//             targetRepoRevision
//         );
//         if (latestSha == "")
//         {
//             var err =
//                 $"Unable to determine latest remote sha for \"{targetRepoUrl}\" at target revision \"{targetRepoRevision}\".";
//             _context.LogError(_logger, err, nameof(CloneGitRepository));
//             throw new Exception(err);
//         }
//
//         // Get the files that are not checked into Git. These are the files that were added either by terraform
//         // (for example in .terraform folder) or snapcd (for example in .snapcd folder). Other locations
//         // are possible if user has made manual changes.
//         //
//         // Want want to discover the above because we do not want to re-download them if we do not need to, so 
//         // we will temporarily store them, then delete the current folder, recreate it and then attempt to copy them
//         // back in. If their parent folder no longer exists, they will be redownloaded.
//
//         var tempDir = "";
//         var filesAddedPostCheckout = new HashSet<string>();
//         try
//         {
//             if (Directory.Exists(GetFullPath(repoPath)))
//             {
//                 filesAddedPostCheckout = GetGitExcludedFiles(GetFullPath(repoPath));
//                 tempDir = await StashGitExcludedFiles(filesAddedPostCheckout, GetFullPath(repoPath));
//             }
//         }
//         catch
//         {
//             _context.LogWarning(_logger,
//                 "Unexpected error when attempting to identify files to keep. Now continuing with deleting and recreating entire repo");
//         }
//
//         var latestCachedSha = await _moduleCacheClient.GetShaAsync(FormatPath(objectName));
//         if (latestLocalSha != latestSha)
//         {
//             _git.DeleteRepoIfExists(repoPath);
//
//
//             var downloadedFromCache = false;
//             if (latestSha == latestCachedSha)
//             {
//                 downloadedFromCache = await DownloadFolder(repoPath, objectName);
//                 _context.LogInformation(_logger,
//                     $"Downloaded {objectName} from cache", nameof(CloneGitRepository));
//             }
//
//             if (!downloadedFromCache)
//             {
//                 _git.ShallowClone(
//                     repoPath,
//                     targetRepoUrl,
//                     targetRepoRevision);
//
//                 _context.LogInformation(_logger,
//                     $"Shallow cloned {objectName} from source", nameof(CloneGitRepository));
//             }
//
//             if (tempDir != "")
//                 await UnstashGitExcludedFiles(filesAddedPostCheckout, GetFullPath(repoPath), tempDir);
//         }
//         else
//         {
//             _context.LogInformation(_logger,
//                 $"Local SHA equals latest remote SHA ({latestSha}), continuing with current local revision.");
//         }
//
//         if (latestCachedSha != latestSha)
//         {
//             await UploadFolder(
//                 repoPath,
//                 objectName,
//                 _git.GetLatestLocalSha(
//                     repoPath
//                 )
//             );
//
//             _context.LogInformation(_logger,
//                 $"Uploaded {objectName} to cache", nameof(CloneGitRepository));
//         }
//         else
//         {
//             _context.LogInformation(_logger,
//                 $"Module with current local SHA ({latestSha}) already uploaded to cache. Doing nothing further.");
//         }
//
//         return filesAddedPostCheckout;
//     }
//
//
//     public async Task InitializeModules(string mainModulePath)
//     {
//         var moduleFile = Path.Combine(mainModulePath, ".terraform/modules", "modules.json");
//         //string downloadDir = Path.Combine(".terraform/modules");
//
//         //TODO need to find a way to keep already downloaded modules instead of always redownloading. Must somehow ensure integrity, or have some fallback. "terraform init" will (always? should confirm...) fail if modules have been tampered with. Also need a way to ensure we downloaded the latest.
//         CleanUp(Path.Combine(mainModulePath, ".terraform/modules"));
//         CreateDir(Path.Combine(mainModulePath, ".terraform/modules"));
//
//         // Parse the main module
//         var modules = new List<TerraformModule>();
//
//         // Start with the main module
//         var initModule = new TerraformModule
//         {
//             TerraformModuleInfo = new TerraformModuleInfo { Key = "", Source = "", Dir = "." }
//         };
//         modules.Add(initModule);
//
//         // Download the modules recursively
//         await DownloadModules(mainModulePath, ".", modules);
//
//         // Write the modules to modules.json
//         var moduleInfos = new List<TerraformModuleInfo>();
//         foreach (var module in modules) moduleInfos.Add(module.TerraformModuleInfo);
//         WriteModulesJson(moduleFile, moduleInfos);
//
//         _context.LogInformation(_logger,
//             "All modules downloaded and written to modules.json.", nameof(InitializeModules));
//     }
//
//
//     // Send the zip file to the UploadFolder API
//     public async Task UploadZipFile(string zipFilePath, string fileName, string sha)
//     {
//         using (var fileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
//         {
//             // Create IFormFile from the file stream
//             // Note the name field *must* equal the parameter name expected by the ModuleCache controller. In this case "folderZip"
//             // TODO this dependency is currently hidden. This function will break if the "folderZip" parameter is every renamed in the controller. Must find a way to make it statically defined
//             var formFile = new FormFile(fileStream, 0, fileStream.Length, "folderZip", $"{fileName}.zip")
//             {
//                 Headers = new HeaderDictionary(),
//                 ContentType = "application/zip"
//             };
//
//             await _moduleCacheClient.UploadAsync(sha, formFile);
//         }
//     }
//
//
//     public async Task UploadFolder(string folderPath, string objectName, string sha)
//     {
//         // TODO, since we have to create some files within the repo, the best approach here would be to copy the folder to a temporary path, then do a hard git reset before eventually zipping it.
//         var fullPath = GetFullPath(folderPath);
//
//         if (!Directory.Exists(fullPath))
//             throw new DirectoryNotFoundException($"The folder path '{fullPath}' does not exist.");
//
//         var formattedObjecName = FormatPath(objectName);
//
//         var tempFileName = $"{formattedObjecName}_temp_{DateTime.UtcNow.Ticks}.zip";
//         var tempZipPath = Path.Combine(_tempPathModuleZip, tempFileName);
//
//         // Step 1: Zip the folder contents
//         ZipFolder(fullPath, tempZipPath);
//
//         _context.LogInformation(_logger,
//             $"Folder '{fullPath}' has been zipped to '{tempZipPath}'.", nameof(UploadFolder));
//
//         // Step 2: Upload the zipped folder
//         await UploadZipFile(tempZipPath, formattedObjecName, sha);
//
//         // Step 3: Clean up temporary zip file
//         File.Delete(tempZipPath);
//     }
//
//
//     public async Task<bool> DownloadFolder(string folderPath, string objectName)
//     {
//         var fullPath = GetFullPath(folderPath);
//         // Step 1: Check if the local folder path exists, create it if not
//         if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
//
//         // Step 2: Format the object name to match the format used on the server
//         var formattedObjectName = FormatPath(objectName);
//
//         // Step 5: Read the content of the zip file from the response
//         var fileBytes = await _moduleCacheClient.DownloadAsync(formattedObjectName);
//
//         // Step 6: Define the temporary zip file path for downloading the content
//         var tempZipPath = Path.Combine(_tempPathModuleZip, $"{formattedObjectName}_latest.zip");
//
//         // Step 7: Write the downloaded bytes to the zip file
//         await File.WriteAllBytesAsync(tempZipPath, fileBytes);
//
//         // Step 8: Extract the zip file to the specified folder path
//         ZipFile.ExtractToDirectory(tempZipPath, fullPath, true);
//
//         // Step 9: Clean up the temporary zip file
//         File.Delete(tempZipPath);
//
//         _context.LogInformation(_logger,
//             $"Folder '{formattedObjectName}' has been downloaded and extracted to '{fullPath}'.",
//             nameof(DownloadFolder));
//
//         return true;
//     }
//
//     public string FormatPath(string inputPath)
//     {
//         return inputPath.Replace("//", "--").Replace("/", "-");
//     }
//
//     private void CreateDirectoryIfNotExists(string directory)
//     {
//         if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
//     }
//
//
//     // Download modules recursively by parsing the source of each module
//     private async Task DownloadModules(string mainModuleDir, string subModuleDir,
//         List<TerraformModule> modules, string prefix = "")
//     {
//         // Find all module sources in the current directory
//
//         var moduleSources = ParseModuleSources(mainModuleDir, subModuleDir, prefix);
//
//         foreach (var module in moduleSources)
//             // Skip if already downloaded (break recursion)
//             if (module.SourceType == TerraformModuleSource.GitRepository)
//             {
//                 var fullPath = Path.Combine(_settings.RepoRootPath, module.DownloadToDir);
//                 if (!Directory.Exists(fullPath))
//                 {
//                     _context.LogInformation(_logger, $"Now downloading module {module.TerraformModuleInfo.Key} from {module.RepoSource}");
//                     var parsedRepoPath = ParseRepoPath(module.RepoSource);
//                     await CloneGitRepository(
//                         module.DownloadToDir,
//                         parsedRepoPath.Url,
//                         parsedRepoPath.Revision
//                     );
//
//                     // Add to the list of modules
//                     modules.Add(module);
//
//                     // Recursively download submodules
//                     await DownloadModules(mainModuleDir, module.TerraformModuleInfo.Dir, modules,
//                         $"{module.TerraformModuleInfo.Key}.");
//                 }
//                 else
//                 {
//                     // TODO, if this is the case then we actually need to check whether the module's data integrity is still in place!
//                     _context.LogInformation(_logger, $"Module \"{module.TerraformModuleInfo.Key}\" from \"{module.RepoSource}\" already exists at \"{fullPath}\".");
//                 }
//             }
//             else if (module.SourceType == TerraformModuleSource.TerraformRegistry)
//             {
//                 // TODO implement this
//             }
//             else if (module.SourceType == TerraformModuleSource.DownloadedLocalPath)
//             {
//                 // Add to the list of modules
//                 modules.Add(module);
//
//                 // Recursively download submodules
//                 await DownloadModules(mainModuleDir, module.TerraformModuleInfo.Dir, modules,
//                     $"{module.TerraformModuleInfo.Key}.");
//             }
//             else
//             {
//                 throw new Exception("Unknown source: " + module.RepoSource);
//             }
//     }
//
//     private List<TerraformModule> ParseModuleSources(string mainModuleDir, string subModuleDir,
//         string prefix)
//     {
//         var modules = new List<TerraformModule>();
//         var fullModulePath = Path.Combine(_settings.RepoRootPath, mainModuleDir, subModuleDir);
//
//         _context.LogInformation(_logger, $"Now parsing all modules in subfolder of {fullModulePath}",
//             nameof(ParseModuleSources));
//
//         foreach (var file in Directory.GetFiles(fullModulePath, "*.tf", SearchOption.TopDirectoryOnly))
//             try
//             {
//                 var jsonFile = ConvertHCLToJson(file);
//                 if (jsonFile == null)
//                     continue;
//
//                 var jsonDoc = JsonDocument.Parse(jsonFile);
//
//                 if (jsonDoc.RootElement.TryGetProperty("module", out var modulesElement))
//                     foreach (var moduleProp in modulesElement.EnumerateObject())
//                     {
//                         var moduleKey = moduleProp.Name;
//
//                         foreach (var arrayItem in moduleProp.Value.EnumerateArray())
//                             if (arrayItem.TryGetProperty("source", out var sourceElement))
//                             {
//                                 var moduleSource = sourceElement.GetString();
//
//                                 var sourceType = SourceHelper.CategorizeSource(moduleSource, fullModulePath, prefix);
//
//                                 switch (sourceType)
//                                 {
//                                     case TerraformModuleSource.GitRepository:
//                                         modules.Add(CreateRepoModule(mainModuleDir, moduleSource, sourceType, prefix,
//                                             moduleKey));
//                                         break;
//                                     case TerraformModuleSource.TerraformRegistry:
//                                         break;
//                                     case TerraformModuleSource.LocalPath:
//                                         modules.Add(CreateLocalPathModule(moduleSource, moduleSource, moduleKey));
//                                         break;
//                                     case TerraformModuleSource.DownloadedLocalPath:
//                                         modules.Add(CreateDownloadedLocalPathModule(mainModuleDir, moduleSource, sourceType,
//                                             prefix,
//                                             moduleKey, subModuleDir));
//                                         break;
//                                 }
//                             }
//                     }
//             }
//             catch (Exception e)
//             {
//                 _context.LogWarning(_logger, $"Failed to parse file \"{file}\": {e.Message}");
//             }
//
//         return modules;
//     }
//
//     private TerraformModule CreateRepoModule(string mainModuleDir, string moduleSource,
//         TerraformModuleSource sourceType, string prefix, string moduleKey)
//     {
//         var stringParts =
//             moduleSource.Split(new[] { "//" },
//                 StringSplitOptions.None); //TODO, need to also do this for local files. Split on "./"?
//         var modulePath = stringParts.Length > 1 ? $"/{stringParts[1]}" : "";
//
//         return new TerraformModule
//         {
//             RepoSource = stringParts[0],
//             RepoRevision = "",
//             SourceType = sourceType,
//             DownloadToDir = $"{mainModuleDir}/.terraform/modules/{prefix}{moduleKey}",
//             TerraformModuleInfo = new TerraformModuleInfo
//             {
//                 Key = $"{prefix}{moduleKey}",
//                 Source = FormatRepoSource(moduleSource),
//                 Dir = $".terraform/modules/{prefix}{moduleKey}{modulePath}"
//             }
//         };
//     }
//
//     private string FormatRepoSource(string moduleSource)
//     {
//         bool IsValidSshUrl()
//         {
//             return moduleSource.StartsWith("git@", StringComparison.OrdinalIgnoreCase);
//         }
//
//         if (IsValidSshUrl())
//         {
//             var strings = moduleSource.Split("@");
//             return $"git::ssh://git@{strings[1].Replace(":", "/")}";
//         }
//
//         //TODO https, Github etc.
//         return moduleSource;
//     }
//
//
//     private TerraformModule CreateDownloadedLocalPathModule(string mainModuleDir, string moduleSource,
//         TerraformModuleSource sourceType, string prefix,
//         string moduleKey, string subModuleDir)
//     {
//         var dir = CombinePaths(subModuleDir, moduleSource);
//
//         return new TerraformModule
//         {
//             RepoSource = "",
//             RepoRevision = "",
//             SourceType = sourceType,
//             DownloadToDir = "",
//             TerraformModuleInfo = new TerraformModuleInfo
//             {
//                 Key = $"{prefix}{moduleKey}",
//                 Source = moduleSource,
//                 Dir = dir
//             }
//         };
//     }
//
//     private string CombinePaths(string firstPath, string secondPath)
//     {
//         var baseSegments = firstPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();
//         var relativeSegments = secondPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
//
//         foreach (var segment in relativeSegments)
//             if (segment == "..")
//             {
//                 if (baseSegments.Count > 0) baseSegments.RemoveAt(baseSegments.Count - 1);
//             }
//             else if (segment != ".")
//             {
//                 baseSegments.Add(segment);
//             }
//
//         return string.Join("/", baseSegments);
//     }
//
//     private TerraformModule CreateLocalPathModule(string moduleSource, string moduleDir, string moduleKey)
//     {
//         moduleDir = moduleDir.StartsWith("./") ? moduleDir.Substring(2) : moduleDir;
//
//         return new TerraformModule
//         {
//             RepoSource = "",
//             RepoRevision = "",
//             SourceType = TerraformModuleSource.DownloadedLocalPath,
//             DownloadToDir = "",
//             TerraformModuleInfo = new TerraformModuleInfo
//             {
//                 Key = moduleKey,
//                 Source = moduleSource,
//                 Dir = moduleDir
//             }
//         };
//     }
//
//     // Converts an HCL file to JSON using the external `hcl2json` tool
//     private string ConvertHCLToJson(string hclFile)
//     {
//         // Run the external hcl2json command to convert HCL to JSON
//         var startInfo = new ProcessStartInfo
//         {
//             FileName = "hcl2json", // Make sure hcl2json is installed and available in the PATH
//             Arguments = $"\"{hclFile}\"",
//             RedirectStandardOutput = true,
//             RedirectStandardError = true,
//             UseShellExecute = false,
//             CreateNoWindow = true
//         };
//
//         using (var process = Process.Start(startInfo))
//         {
//             var output = process.StandardOutput.ReadToEnd();
//             var error = process.StandardError.ReadToEnd();
//
//             process.WaitForExit();
//
//             if (process.ExitCode != 0)
//             {
//                 _context.LogError(_logger,
//                     $"Error converting HCL to JSON: {error}", nameof(ConvertHCLToJson));
//
//                 return null; //TODO raise exception instead?
//             }
//
//             return output;
//         }
//     }
//
//     // Write the module list to modules.json
//     private void WriteModulesJson(string outputFile, List<TerraformModuleInfo> modules)
//     {
//         var jsonObject = new { Modules = modules };
//
//         var options = new JsonSerializerOptions { WriteIndented = true };
//         var json = JsonSerializer.Serialize(jsonObject, options);
//
//         File.WriteAllText(Path.Combine(_settings.RepoRootPath, outputFile), json);
//     }
//
//     private void CleanUp(string dir)
//     {
//         var fullDir = Path.Combine(_settings.RepoRootPath, dir);
//         try
//         {
//             if (Directory.Exists(fullDir))
//             {
//                 // Delete the directory and all its contents
//                 Directory.Delete(fullDir, true);
//                 _context.LogInformation(_logger,
//                     $"Directory '{fullDir}' deleted successfully.", nameof(WriteModulesJson));
//             }
//             else
//             {
//                 _context.LogInformation(_logger,
//                     $"Directory '{fullDir}' does not exist.", nameof(WriteModulesJson));
//             }
//         }
//         catch (Exception ex)
//         {
//             _context.LogError(_logger,
//                 $"An error occurred: {ex.Message}", nameof(WriteModulesJson));
//         }
//     }
//
//     private void CreateDir(string dir)
//     {
//         var fullDir = Path.Combine(_settings.RepoRootPath, dir);
//         Directory.CreateDirectory(fullDir);
//         _context.LogInformation(_logger,
//             $"Directory '{fullDir}' created.", nameof(CreateDir));
//     }
//
//     private (string Url, string Revision, string Path) ParseRepoPath(string moduleSource)
//     {
//         // Initialize return values to empty strings
//         string url = "", revision = "", path = "";
//
//         try
//         {
//             // Split the input string by "//"
//             var firstSplit = moduleSource.Split(new[] { "//" }, StringSplitOptions.None);
//
//             // Check if the firstSplit contains the expected second part (path)
//             if (firstSplit.Length > 1) path = firstSplit[1];
//
//             // Split the first part by "?ref="
//             var secondSplit = firstSplit[0].Split(new[] { "?ref=" }, StringSplitOptions.None);
//
//             // Assign URL if the split contains at least one element
//             if (secondSplit.Length > 0) url = secondSplit[0];
//
//             // Assign revision if the split contains the "?ref=" part
//             if (secondSplit.Length > 1) revision = secondSplit[1];
//         }
//         catch (Exception ex)
//         {
//             // You can log the exception or handle it as needed
//             _context.LogInformation(_logger,
//                 $"An error occurred: {ex.Message}", nameof(ParseRepoPath));
//
//             // Returning empty strings in case of any failure
//         }
//
//         // Return the parsed values (empty strings if any error occurred or values weren't found)
//         return (url, revision, path);
//     }
//
//     private string GetFullPath(string folderPath)
//     {
//         return Path.Combine(_settings.RepoRootPath, folderPath);
//     }
//
//
//     private void ZipFolder(string sourceFolderPath, string destinationZipPath)
//     {
//         if (File.Exists(destinationZipPath)) File.Delete(destinationZipPath); // Overwrite if zip file already exists
//
//         // Fetch untracked and ignored files. We do not want to include these in the module upload.
//         var excludedFiles = GetGitExcludedFiles(sourceFolderPath);
//
//         using (var zipToOpen = new FileStream(destinationZipPath, FileMode.Create))
//         {
//             using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
//             {
//                 // Get all files and subdirectories
//                 foreach (var file in Directory.EnumerateFiles(sourceFolderPath, "*", SearchOption.AllDirectories))
//                 {
//                     // Get the relative path of the file
//                     var relativePath = Path.GetRelativePath(sourceFolderPath, file);
//
//                     // Skip files that are untracked or ignored by Git
//                     if (excludedFiles.Contains(file)) continue;
//
//                     // Add the file to the zip archive
//                     archive.CreateEntryFromFile(file, relativePath, CompressionLevel.Optimal);
//                 }
//             }
//         }
//     }
//
//     private HashSet<string> GetGitExcludedFiles(string sourceFolderPath)
//     {
//         var excludedFiles = new HashSet<string>();
//
//         var command = "-c \"git ls-files --others --exclude-standard && git ls-files --others -i --exclude-standard\"";
//
//         // Run the Git command to get excluded files
//         var startInfo = new ProcessStartInfo
//         {
//             FileName = "/bin/bash",
//             Arguments = command,
//             RedirectStandardOutput = true,
//             RedirectStandardError = true,
//             UseShellExecute = false,
//             CreateNoWindow = true,
//             WorkingDirectory = sourceFolderPath
//         };
//
//         using (var process = new Process { StartInfo = startInfo })
//         {
//             process.Start();
//             var output = process.StandardOutput.ReadToEnd();
//             process.WaitForExit();
//
//             if (process.ExitCode != 0)
//             {
//                 var error = process.StandardError.ReadToEnd();
//                 throw new Exception($"Git process failed with exit code {process.ExitCode}. Error: {error}");
//             }
//
//             // Normalize paths to absolute paths for comparison
//             foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
//                 excludedFiles.Add(Path.GetFullPath(Path.Combine(sourceFolderPath, line)));
//         }
//
//         static List<string> ExpandIfDirectory(string path)
//         {
//             // For some reason the above will sometimes return a directory instead of the files inside the dir. This
//             // function expands into 
//             if (Directory.Exists(path))
//                 return new List<string>(Directory.GetFiles(path, "*", SearchOption.AllDirectories));
//             return new List<string>() { path };
//         }
//
//         var expandedFiles = new HashSet<string>();
//         foreach (var path in excludedFiles) expandedFiles.UnionWith(ExpandIfDirectory(path));
//
//         return expandedFiles;
//     }
//
//     public async Task<string> StashGitExcludedFiles(HashSet<string> sourcePaths, string fullRepoPath)
//     {
//         var tempFolderName = $"{DateTime.UtcNow.Ticks}_{Guid.NewGuid().ToString("N")}";
//         if (!sourcePaths.Any()) return "";
//
//         var tempDir = Path.Combine(_tempPathGitExcludedStash, tempFolderName);
//         Directory.CreateDirectory(tempDir);
//
//         foreach (var sourcePath in sourcePaths)
//             try
//             {
//                 var destPath =
//                     Regex.Replace(sourcePath, $"^{Regex.Escape(fullRepoPath)}",
//                         tempDir); // replace fullRepoPath with tempDir at beginning of path string.
//                 try
//                 {
//                     var destDir = Path.GetDirectoryName(destPath);
//                     Directory.CreateDirectory(destDir);
//                     File.Copy(sourcePath, destPath, false);
//                 }
//                 catch (Exception e)
//                 {
//                     _context.LogWarning(_logger,
//                         $"Unexpected error occured attempting to stash the file \"{sourcePath}\" to \"{destPath}\". Exception: {e.Message}");
//                 }
//             }
//             catch (Exception e)
//             {
//                 _context.LogWarning(_logger,
//                     $"Unexpected error occured attempting to stash the file \"{sourcePath}\". Exception: {e.Message}");
//             }
//
//         _context.LogInformation(_logger, $"Stashed {sourcePaths.Count} excluded files in temporary storage.");
//
//         return tempDir;
//     }
//
//     public async Task UnstashGitExcludedFiles(HashSet<string> excludedFiles, string fullRepoPath, string tempDir)
//     {
//         if (!excludedFiles.Any()) return;
//
//         foreach (var destPath in excludedFiles)
//             try
//             {
//                 var sourcePath =
//                     Regex.Replace(destPath, $"^{Regex.Escape(fullRepoPath)}",
//                         tempDir); // replace fullRepoPath with tempDir at beginning of path string.
//                 try
//                 {
//                     var destDir = Path.GetDirectoryName(destPath);
//                     Directory.CreateDirectory(destDir);
//                     File.Move(sourcePath, destPath, true);
//                 }
//                 catch (Exception e)
//                 {
//                     _context.LogWarning(_logger,
//                         $"Unexpected error occured attempting to unstash the file \"{sourcePath}\" to \"{destPath}\". Exception: {e.Message}");
//                 }
//             }
//             catch (Exception e)
//             {
//                 _context.LogWarning(_logger,
//                     $"Unexpected error occured attempting to unstash to \"{destPath}\". Exception: {e.Message}");
//             }
//
//
//         _context.LogInformation(_logger, $"Restored {excludedFiles.Count} excluded files from temporary storage.");
//     }
// }

