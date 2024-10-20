using System.Collections;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace LCModManager
{
    namespace Thunderstore
    {
        internal struct ModManifest
        {
            public string? Name { get; set; }
            public string? Author { get; set; }
            public string? Version_Number { get; set; }
            public string? Website_Url { get; set; }
            public string? Description { get; set; }
            public string[]? Dependencies { get; set; }

            public ModManifest(Stream fileData, JsonSerializerOptions options)
            {
                this = JsonSerializer.Deserialize<ModManifest>(fileData, options);
            }

            public ModManifest(string fileString, JsonSerializerOptions options)
            {
                this = JsonSerializer.Deserialize<ModManifest>(fileString, options);
            }
        }

        public struct PackageListingVersionEntry
        {
            public string name { get; set; }
            public string full_name { get; set; }
            public string description { get; set; }
            public string icon { get; set; }
            public string version_number { get; set; }
            public string[] dependencies { get; set; }
            public string download_url { get; set; }
            public long downloads { get; set; }
            public string date_created { get; set; }
            public string website_url { get; set; }
            public bool is_active { get; set; }
            public string uuid4 { get; set; }
            public long file_size { get; set; }
        }

        public struct PackageListing
        {
            public string name { get; set; }
            public string full_name { get; set; }
            public string owner { get; set; }
            public string package_url { get; set; }
            public string donation_link { get; set; }
            public string date_created { get; set; }
            public string date_updated { get; set; }
            public string uuid4 { get; set; }
            public int rating_score { get; set; }
            public bool is_pinned { get; set; }
            public bool is_deprecated { get; set; }
            public bool has_nsfw_content { get; set; }
            public string[] categories { get; set; }
            public PackageListingVersionEntry[] versions { get; set; }
        }

        internal class ModPackage : ModEntryDisplay
        {
            private string? _ReadMe;
            private string? _ChangeLog;
            private ModManifest _Manifest;

            public string? ReadMe => _ReadMe;
            public string? ChangeLog => _ChangeLog;

            public ModPackage(string sourcePath)
            {
                Path = sourcePath;

                foreach (string versionPath in Directory.GetDirectories(sourcePath).OrderDescending().ToArray())
                {
                    Versions.Add(versionPath.Split("\\").Last());
                }

                string selectedVersionPath = Path + "\\" + Versions[0];

                try
                {
                    _ReadMe = File.ReadAllText(selectedVersionPath + "\\README.md");
                }
                catch
                {
                    _ReadMe = null;
                }

                try
                {
                    _ChangeLog = File.ReadAllText(selectedVersionPath + "\\CHANGELOG.md");
                }
                catch
                {
                    _ChangeLog = null;
                }

                Icon = new BitmapImage();
                Icon.BeginInit();
                Icon.UriSource = new Uri(selectedVersionPath + "\\icon.png");
                Icon.DecodePixelWidth = 64;
                Icon.DecodePixelHeight = 64;
                Icon.CacheOption = BitmapCacheOption.OnLoad;
                Icon.EndInit();

                _Manifest = new ModManifest(File.ReadAllText(selectedVersionPath + "\\manifest.json"), new(JsonSerializerDefaults.Web));
                Name = _Manifest.Name ?? "";
                Author = _Manifest.Author ?? "";
                Description = _Manifest.Description;
                Website = _Manifest.Website_Url;
                Dependencies = _Manifest.Dependencies;
                ExistsInPackageStore = true;
            }

            public ModPackage(PackageListing listing)
            {
                Path = listing.package_url;
                Icon = new BitmapImage();
                Icon.BeginInit();
                Icon.UriSource = new Uri(listing.versions[0].icon);
                Icon.DecodePixelWidth = 64;
                Icon.DecodePixelHeight = 64;
                Icon.CacheOption = BitmapCacheOption.OnLoad;
                Icon.EndInit();
                Name = listing.name;
                Author = listing.owner;
                Description = listing.versions[0].description;
                Website = listing.package_url;
                Versions = [];
                Dependencies = [];

                foreach (PackageListingVersionEntry entry in listing.versions)
                {
                    Versions.Add(entry.version_number);

                    foreach (string dep in entry.dependencies)
                    {
                        if (!Dependencies.Contains(dep)) Dependencies.Append(dep);
                    }
                }
            }

            public ModPackage(PackageListing listing, string selectedVersion)
            {
                PackageListingVersionEntry entry = listing.versions.First(v => v.version_number == selectedVersion);

                Path = entry.download_url;
                Icon = new BitmapImage();
                Icon.BeginInit();
                Icon.UriSource = new Uri(entry.icon);
                Icon.DecodePixelWidth = 64;
                Icon.DecodePixelHeight = 64;
                Icon.CacheOption = BitmapCacheOption.OnLoad;
                Icon.EndInit();
                Name = entry.name;
                Author = listing.owner;
                Description = entry.description;
                Versions = [entry.version_number];
                Website = entry.website_url;
                Dependencies = entry.dependencies;
            }

            public ModPackage(PackageListingVersionEntry entry)
            {
                Path = entry.download_url;
                Icon = new BitmapImage();
                Icon.BeginInit();
                Icon.UriSource = new Uri(entry.icon);
                Icon.DecodePixelWidth = 64;
                Icon.DecodePixelHeight = 64;
                Icon.CacheOption = BitmapCacheOption.OnLoad;
                Icon.EndInit();
                Name = entry.name;
                Description = entry.description;
                Versions = [entry.version_number];
                Website = entry.website_url;
                Dependencies = entry.dependencies;
            }

            public ModPackage(ModEntry entry, bool notFound = false)
            {
                if (notFound)
                {
                    Path = entry.Path;
                    Icon = new BitmapImage();
                    Icon.BeginInit();
                    Icon.UriSource = new Uri("pack://application:,,,/Resources/PackageNotFound.png");
                    Icon.DecodePixelWidth = 64;
                    Icon.DecodePixelHeight = 64;
                    Icon.CacheOption = BitmapCacheOption.OnLoad;
                    Icon.EndInit();
                    Name = entry.Name;
                    Description = entry.Description;
                    Versions = entry.Versions;
                    Website = entry.Website;
                    Dependencies = entry.Dependencies;
                    ExistsInPackageStore = false;
                }
                else
                {
                    Path = entry.Path;
                    Icon = null;
                    Name = entry.Name;
                    Description = entry.Description;
                    Versions = entry.Versions;
                    Website = entry.Website;
                    Dependencies = entry.Dependencies;
                    ExistsInPackageStore = false;
                }
            }
        }

        static internal class PackageManager
        {
            static public string StorePath = AppConfig.PackageStorePath + "\\Thunderstore";

            //Filename regex used primarily for AddPackage()
            static private Regex WinDuplicateFileReg = new("\\([0-9]+\\)\\.[a-zA-Z0-9]+$", RegexOptions.Compiled);
            static private Regex WinDuplicateDirReg = new("\\([0-9]+\\)$", RegexOptions.Compiled);
            static private Regex FileExtensionReg = new("\\.[a-zA-Z0-9]+$", RegexOptions.Compiled);
            static private Regex PackageNameReg = new(".*-.+\\..+\\..+", RegexOptions.Compiled);
            static private Regex PackageSourceFluffReg = new(WinDuplicateFileReg + "|" + WinDuplicateDirReg + "|" + FileExtensionReg, RegexOptions.Compiled);

            async static public Task<ModPackage?> AddPackage(string packageSourcePath)
            {
                string sourceName = packageSourcePath.Split("\\").Last(); //lop off path

                string[] nameparts = sourceName[..^(PackageSourceFluffReg.Match(sourceName).Length)].Split("-"); //Lop off file extension and/or duplicate file patterns, then split string at each "-"

                if (nameparts.Length != 3 || !PackageNameReg.IsMatch(sourceName)) return null; //return null if sourceName does not match desired format

                string owner = nameparts[^3]; //package owner
                string name = nameparts[^2]; //package name
                string version = nameparts[^1]; //package version
                string nameDir = StorePath + "\\" + owner + "-" + name; //destination directory in package store
                string versionDir = nameDir + "\\" + version; //package path in destination directory

                if (!Directory.Exists(nameDir)) Directory.CreateDirectory(nameDir); //if nameDir does not exist, create nameDir
                if (Directory.Exists(versionDir)) Directory.Delete(versionDir, true); //if versionDir already exists, delete versionDir

                if (File.Exists(packageSourcePath))
                {
                    //unzip file at packageSourcePath and extract to versionDir
                    try
                    {
                        File.SetAttributes(packageSourcePath, FileAttributes.Normal); // ensure correct file permissions
                        using Stream file = File.Open(packageSourcePath, FileMode.Open, FileAccess.Read); //open read stream
                        using ZipArchive zip = new(file); //unzip file
                        zip.ExtractToDirectory(versionDir); //extract zip
                        file.Close(); //close file
                        file.Dispose(); //release memory
                        zip.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.Write(ex);
                        return null;
                    }

                    //set manifest author and return new ModPackage class derived from versionDir
                    try
                    {
                        string manifestPath = versionDir + "\\manifest.json";
                        ModManifest manifest = new(File.ReadAllText(manifestPath), new(JsonSerializerDefaults.Web));
                        manifest.Author = owner;
                        File.WriteAllBytes(manifestPath, JsonSerializer.SerializeToUtf8Bytes<ModManifest>(manifest));

                        return new ModPackage(nameDir);
                    }

                    //Delete directory if there is a raised exception
                    catch (Exception ex)
                    {
                        Directory.Delete(nameDir, true);
                        Debug.WriteLine(ex);
                        return null;
                    }
                }
                else return null;
            }

            async static public Task RemovePackage(ModEntryDisplay package)
            {
                // for each package who's name matches package.Name
                foreach (ModPackage p in GetPackages().Where(p => p.Name == package.Name && p.Author == package.Author))
                {
                    // if no versions are selected, or all versions are selected, delete the entire package directory
                    if (package.SelectedVersions.Count == 0 || package.SelectedVersions.Count == package.Versions.Count)
                    {
                        Directory.Delete(p.Path, true);
                    }

                    // else delete each directory for selected versions in package
                    else
                    {
                        foreach (string version in package.SelectedVersions)
                        {
                            string versionPath = Path.Combine(p.Path, version);
                            if (Directory.Exists(versionPath)) Directory.Delete(versionPath, true);
                        }
                    }
                }
            }

            async static public Task RemovePackages(IEnumerable<ModEntryDisplay> packages)
            {
                foreach (ModEntryDisplay p in packages) await RemovePackage(p);
            }

            static public List<ModPackage> GetPackages()
            {
                List<ModPackage> packages = [];

                foreach (string directory in Directory.GetDirectories(StorePath))
                {
                    packages.Add(new ModPackage(directory));
                }

                return packages;
            }

            static public List<ModPackage> GetPackages(string name)
            {
                Regex regex = new Regex(name);
                List<ModPackage> packages = [];

                foreach (string file in Directory.GetDirectories(StorePath).Where(path => regex.IsMatch(path)))
                {
                    packages.Add(new ModPackage(file));
                }

                return packages;
            }

            static public List<ModPackage> GetPackages(Regex regex)
            {
                List<ModPackage> packages = [];

                foreach (string file in Directory.GetDirectories(StorePath).Where(path => regex.IsMatch(path)))
                {
                    packages.Add(new ModPackage(file));
                }

                return packages;
            }
        }

        static internal class ModDeployer
        {
            //find Lethal Company game directory on system
            static public string GameDir = GameDirectory.Find();


            static public string InferModParentDirectory(ModEntrySelection package)
            {
                List<string> childDirs = [];

                foreach (string dir in Directory.GetDirectories(package.ModEntry.Path + "\\" + package.SelectedVersion))
                {
                    childDirs.Add(dir.Split("\\")[^1]);
                }

                switch (childDirs.Count)
                {
                    case 0: return GameDir + "\\BepInEx\\plugins";
                    case 1:
                        return childDirs[0] switch
                        {
                            "BepInEx" => GameDir,
                            "BepInExPack" => GameDir,
                            _ => GameDir + "\\BepInEx",
                        };
                    default:
                        string? inferredDir = null;

                        foreach (string dir in childDirs)
                        {
                            inferredDir = dir switch
                            {
                                "BepInEx" => GameDir,
                                "plugins" => GameDir + "\\BepInEx",
                                _ => null
                            };

                            if (inferredDir != null) break;
                        }

                        return inferredDir ?? GameDir + "\\BepInEx";
                }
            }

            static public void DeployMod(ModEntrySelection package)
            {
                string selectedVersionPath = package.ModEntry.Path + "\\" + package.SelectedVersion;

                if (package.ModEntry.Name == "BepInExPack")
                {
                    Utils.CopyDirectory(selectedVersionPath + "\\BepInExPack", GameDir, true);
                }
                else
                {
                    string destinationPath = InferModParentDirectory(package);

                    Utils.CopyDirectory(selectedVersionPath, destinationPath, true);
                }
            }

            async static public Task DeployProfile(ModProfile profile)
            {
                string profileInstanceDir = AppConfig.ProfileStorePath + "\\" + profile.Name;

                if (Directory.Exists(profileInstanceDir))
                {
                    if (Directory.Exists(profileInstanceDir + "\\BepInEx")) Directory.Move(profileInstanceDir + "\\BepInEx", GameDir + "\\BepInEx");
                    if (File.Exists(profileInstanceDir + "\\winhttp.dll")) File.Move(profileInstanceDir + "\\winhttp.dll", GameDir + "\\winhttp.dll");
                    if (File.Exists(profileInstanceDir + "\\doorstop_config.ini")) File.Move(profileInstanceDir + "\\doorstop_config.ini", GameDir + "\\doorstop_config.ini");
                }
                else
                {
                    if (Directory.Exists(profileInstanceDir)) Directory.Delete(profileInstanceDir);
                    foreach (ModEntrySelection package in profile.PackageList) DeployMod(package);
                }

                while (!File.Exists(GameDir + "\\" + "doorstop_config.ini")) await Task.Delay(100);

                string bepinexDir = GameDir + "\\BepInEx";
                string[] filenames = ["icon.png", "manifest.json", "README.md", "CHANGELOG.md"];
                foreach (string filename in filenames)
                {
                    foreach (string path in Directory.GetFiles(bepinexDir, filename, SearchOption.AllDirectories)) File.Delete(path);
                }
            }

            static public void ExfiltrateProfile(ModProfile profile)
            {
                string profileInstanceDir = AppConfig.ProfileStorePath + "\\" + profile.Name;

                if (!Directory.Exists(profileInstanceDir)) Directory.CreateDirectory(profileInstanceDir);

                if (Directory.Exists(GameDir + "\\BepInEx")) Directory.Move(GameDir + "\\BepInEx", profileInstanceDir + "\\BepInEx");
                if (File.Exists(GameDir + "\\doorstop_config.ini")) File.Move(GameDir + "\\doorstop_config.ini", profileInstanceDir + "\\doorstop_config.ini");
                if (File.Exists(GameDir + "\\winhttp.dll")) File.Move(GameDir + "\\winhttp.dll", profileInstanceDir + "\\winhttp.dll");
            }
        }

        static internal class WebClient
        {
            static public string PackageListCachePath = PackageManager.StorePath + "\\WebPackageListCache.json";
            static private HttpClient HTTPClient = new();

            static private class Endpoints
            {
                static public string BaseURL = "https://thunderstore.io/c/lethal-company/api/v1";
                static public string PackageList = BaseURL + "/package";
            }

            //Cache singleton
            public sealed class PackageCache
            {
                static private Dictionary<string, PackageListing> _Cache = new();

                static public TimeSpan RefreshInterval = new TimeSpan(AppConfig.WebCacheRefreshInterval.Item1,
                                                                      AppConfig.WebCacheRefreshInterval.Item2,
                                                                      AppConfig.WebCacheRefreshInterval.Item3);

                static public DateTime LastRefresh = new FileInfo(PackageListCachePath).LastWriteTime;

                static public Dictionary<string, PackageListing> Instance
                {
                    get
                    {
                        lock (_Cache)
                        {
                            return _Cache;
                        }
                    }
                }

                PackageCache() { }

                async static public Task Refresh(StatusBarControl statusBarControl)
                {
                    if ((DateTime.Now - LastRefresh) > RefreshInterval) await WebClient.RefreshPackageListCache(statusBarControl);

                    if (await GetPackageList() is List<PackageListing> list)
                    {
                        _Cache.Clear();
                        foreach (PackageListing package in list) _Cache.Add(package.full_name, package);
                        LastRefresh = DateTime.Now;
                    }
                }
            }

            static public List<PackageListing> SearchPackageCache(Func<KeyValuePair<string, PackageListing>, bool> predicate)
            {
                List<PackageListing> list = new();

                foreach (KeyValuePair<string, PackageListing> listing in PackageCache.Instance.Where(predicate))
                {
                    list.Add(listing.Value);
                }

                return list;
            }

            static public PackageListing? GetCachedPackage(string fullName)
            {
                try
                {
                    return PackageCache.Instance[fullName];
                }
                catch (Exception ex)
                {
                    Debug.Write(ex);
                    return null;
                }
            }

            async static public Task<HttpResponseMessage?> DownloadPackageList()
            {
                try
                {
                    return await HTTPClient.GetAsync(Endpoints.PackageList, HttpCompletionOption.ResponseHeadersRead); ;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return null;
                }
            }

            async static public Task RefreshPackageListCache(StatusBarControl statusBarControl)
            {
                if (File.Exists(PackageListCachePath)) File.Delete(PackageListCachePath);

                if (await DownloadPackageList() is HttpResponseMessage response)
                {
                    await statusBarControl.DownloadWithProgress(response, PackageListCachePath, AppState.RefreshingPackageList, false);
                }
            }

            async static public Task RefreshPackageListCache()
            {
                if (File.Exists(PackageListCachePath)) File.Delete(PackageListCachePath);

                if (await DownloadPackageList() is HttpResponseMessage response)
                {
                    string jsonString = Encoding.UTF8.GetString(await response.Content.ReadAsByteArrayAsync());

                    try
                    {
                        File.WriteAllText(PackageListCachePath, jsonString, Encoding.UTF8);
                    }
                    catch (IOException ex)
                    {
                        Debug.Write(ex);
                    }

                }
            }

            async static public Task<List<PackageListing>?> GetPackageList()
            {
                try
                {
                    FileStream file = File.OpenRead(PackageListCachePath);

                    PackageListing[] packages = JsonSerializer.Deserialize<PackageListing[]>(file);

                    file.Close();
                    file.Dispose();

                    return new List<PackageListing>(packages);
                }
                catch (Exception ex)
                {
                    Debug.Write(ex);
                    return null;
                }
            }

            async static public Task<HttpResponseMessage?> DownloadPackage(PackageListingVersionEntry versionEntry, HttpCompletionOption completionOption = HttpCompletionOption.ResponseHeadersRead)
            {
                try
                {
                    return await HTTPClient.GetAsync(versionEntry.download_url, completionOption);
                }
                catch (HttpRequestException ex)
                {
                    Debug.Write(ex);
                    return null;
                }
            }

            async static public Task<HttpResponseMessage?> DownloadDependency(ModEntry entry, string version)
            {
                if (entry.Author != null)
                {
                    return await DownloadDependency(entry.Author + "-" + entry.Name + "-" + version);
                }
                else return null;
            }

            async static public Task<HttpResponseMessage?> DownloadDependency(string dependencyString, HttpCompletionOption completionOption = HttpCompletionOption.ResponseHeadersRead)
            {
                string[] depParts = dependencyString.Split("-");

                PackageListing? query = WebClient.GetCachedPackage(depParts[^3] + "-" + depParts[^2]);

                if (query != null)
                {
                    PackageListingVersionEntry versionEntry = query.Value.versions.First(v => v.version_number == depParts[^1]);

                    if (await DownloadPackage(versionEntry, completionOption) is HttpResponseMessage response)
                    {
                        return response;
                    }
                    else return null;
                }
                else return null;
            }

            async static public Task<HttpResponseMessage?[]> DownloadDependencies(string[] dependencies)
            {
                HttpResponseMessage?[] responses = new HttpResponseMessage[dependencies.Length];

                foreach (string dep in dependencies)
                {
                    responses.Append(await DownloadDependency(dep));
                }

                return responses;
            }
        }
    }
}