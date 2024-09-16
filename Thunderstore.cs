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

                try
                {
                    _ReadMe = File.ReadAllText(sourcePath + "\\README.md");
                }
                catch
                {
                    _ReadMe = null;
                }

                try
                {
                    _ChangeLog = File.ReadAllText(sourcePath + "\\CHANGELOG.md");
                }
                catch
                {
                    _ChangeLog = null;
                }

                Icon = new BitmapImage();
                Icon.BeginInit();
                Icon.UriSource = new Uri(sourcePath + "\\icon.png");
                Icon.DecodePixelWidth = 64;
                Icon.DecodePixelHeight = 64;
                Icon.CacheOption = BitmapCacheOption.OnLoad;
                Icon.EndInit();

                _Manifest = new ModManifest(File.ReadAllText(sourcePath + "\\manifest.json"), new(JsonSerializerDefaults.Web));
                Name = _Manifest.Name ?? "";
                Description = _Manifest.Description;
                Version = _Manifest.Version_Number;
                Website = _Manifest.Website_Url;
                Dependencies = _Manifest.Dependencies;
            }

            public ModPackage(ModEntry entry, bool notFound = false)
            {
                if (notFound)
                {
                    Icon = new BitmapImage();
                    Icon.BeginInit();
                    Icon.UriSource = new Uri("pack://application:,,,/Resources/PackageNotFound.png");
                    Icon.DecodePixelWidth = 64;
                    Icon.DecodePixelHeight = 64;
                    Icon.CacheOption = BitmapCacheOption.OnLoad;
                    Icon.EndInit();
                    Name = entry.Name;
                    Description = entry.Description;
                    Version = entry.Version;
                    Website = entry.Website;
                    Dependencies = entry.Dependencies;
                    ExistsInPackageStore = false;
                }
                else
                {
                    Icon = null;
                    Name = entry.Name;
                    Description = entry.Description;
                    Version = entry.Version;
                    Website = entry.Website;
                    Dependencies = entry.Dependencies;
                }
            }
        }

        static internal class PackageManager
        {
            static public string StorePath = AppConfig.PackageStorePath + "\\Thunderstore";
            static private Regex WinDuplicateFileReg = new("\\([0-9]+\\)\\.[a-zA-Z0-9]+$", RegexOptions.Compiled);
            static private Regex WinDuplicateDirReg = new("\\([0-9]+\\)$", RegexOptions.Compiled);
            static private Regex FileReg = new("\\.[a-zA-Z0-9]+$", RegexOptions.Compiled);
            static private Regex PackageSourceFluffReg = new(WinDuplicateFileReg + "|" + WinDuplicateDirReg + "|" + FileReg, RegexOptions.Compiled);

            async static public void AddPackage(string packageSourcePath)
            {
                string dirName = packageSourcePath.Split("\\").Last(); //Lop off end of path
                dirName = dirName[..^(PackageSourceFluffReg.Match(dirName).Length)]; //Lop off file extension and/or duplicate item patterns

                string destPath = StorePath + "\\" + dirName;
                if (Directory.Exists(packageSourcePath) && !Directory.Exists(destPath))
                {
                    Utils.CopyDirectory(packageSourcePath, destPath, true);
                }
                else if (File.Exists(packageSourcePath) && !Directory.Exists(destPath))
                {
                    File.SetAttributes(packageSourcePath, FileAttributes.Normal);
                    using Stream file = File.Open(packageSourcePath, FileMode.Open, FileAccess.Read);
                    using ZipArchive zip = new(file);
                    zip.ExtractToDirectory(destPath);
                    file.Close();
                    file.Dispose();
                    zip.Dispose();
                }

                try
                {
                    ModPackage newPackage = new(destPath);
                    string packName = newPackage.Name + "-" + newPackage.Version;

                    if(dirName != packName)
                    {
                        string newDestPath = StorePath + "\\" + packName;
                        Directory.Move(destPath, newDestPath);
                    }
                }
                catch (Exception e)
                {
                    Directory.Delete(destPath, true);
                    Debug.WriteLine(e);
                }
            }

            static public void RemovePackage(ModPackage package)
            {
                if (Directory.Exists(package.Path)) Directory.Delete(package.Path, true);
            }

            static public void RemovePackage(ModEntryDisplay package)
            {
                List<ModPackage> packages = GetPackages();

                foreach (ModPackage p in packages.Where(p => p.Name == package.Name && p.Version == package.Version))
                {
                    RemovePackage(p);
                }
            }

            static public void RemovePackages(IEnumerable<ModPackage> packages)
            {
                foreach (ModPackage p in packages) RemovePackage(p);
            }

            static public void RemovePackages(IEnumerable<ModEntryDisplay> packages)
            {
                foreach (ModEntryDisplay p in packages) RemovePackage(p);
            }

            static public List<ModPackage> GetPackages()
            {
                List<ModPackage> packages = [];

                foreach (string file in Directory.GetDirectories(StorePath))
                {
                    try
                    {
                        packages.Add(new ModPackage(file));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }

                return packages;
            }

            static public List<ModPackage> GetPackages(Regex regex)
            {
                List<ModPackage> packages = [];

                foreach (string file in Directory.GetDirectories(StorePath).Where(path => regex.IsMatch(path)))
                {
                    try
                    {
                        packages.Add(new ModPackage(file));
                    }
                    catch (Exception e)
                    {
                        Debug.Write(e);
                    }
                }

                return packages;
            }
        }

        static internal class ModDeployer
        {
            static public string GameDir = GameDirectory.Find();

            static public string InferModParentDirectory(ModEntry modEntry)
            {
                List<string> childDirs = [];

                foreach (string dir in Directory.GetDirectories(modEntry.Path))
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

            static public void DeployMod(ModEntry modEntry)
            {
                if (modEntry.Name == "BepInExPack")
                {
                    Utils.CopyDirectory(modEntry.Path + "\\BepInExPack", GameDir, true);
                }
                else
                {
                    string destinationPath = InferModParentDirectory(modEntry);

                    Utils.CopyDirectory(modEntry.Path, destinationPath, true);
                }
            }

            async static public Task DeployProfile(ModProfile profile)
            {
                string profileInstanceDir = AppConfig.ProfileStorePath + "\\" + profile.Name;

                if (Directory.Exists(profileInstanceDir) && (DateTime.Now - File.GetCreationTime(profileInstanceDir + ".xml")).TotalMinutes > 1)
                {
                    if (Directory.Exists(profileInstanceDir + "\\BepInEx")) Directory.Move(profileInstanceDir + "\\BepInEx", GameDir + "\\BepInEx");
                    if (File.Exists(profileInstanceDir + "\\winhttp.dll")) File.Move(profileInstanceDir + "\\winhttp.dll", GameDir + "\\winhttp.dll");
                    if (File.Exists(profileInstanceDir + "\\doorstop_config.ini")) File.Move(profileInstanceDir + "\\doorstop_config.ini", GameDir + "\\doorstop_config.ini");
                }
                else
                {
                    if (Directory.Exists(profileInstanceDir)) Directory.Delete(profileInstanceDir);
                    foreach (ModEntry modEntry in profile.ModList) DeployMod(modEntry);
                }

                while (!File.Exists(GameDir + "\\" + "doorstop_config.ini")) await Task.Delay(100);

                string bepinexDir = GameDir + "\\BepInEx";
                foreach (string path in Directory.GetFiles(bepinexDir, "icon.png", SearchOption.AllDirectories)) File.Delete(path);
                foreach (string path in Directory.GetFiles(bepinexDir, "manifest.json", SearchOption.AllDirectories)) File.Delete(path);
                foreach (string path in Directory.GetFiles(bepinexDir, "README.md", SearchOption.AllDirectories)) File.Delete(path);
                foreach (string path in Directory.GetFiles(bepinexDir, "CHANGELOG.md", SearchOption.AllDirectories)) File.Delete(path);
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

        static internal class WebClient
        {
            static private string PackageListCache = PackageManager.StorePath + "\\WebPackageListCache.json";
            static private HttpClient HTTPClient = new();

            static private class Endpoints
            {
                static public string BaseURL = "https://thunderstore.io/c/lethal-company/api/v1";
                static public string PackageList = BaseURL + "/package";
            }

            async static public Task<bool> DownloadPackageList()
            {
                try
                {
                    using HttpResponseMessage response = await HTTPClient.GetAsync(Endpoints.PackageList);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        byte[] body = await response.Content.ReadAsByteArrayAsync();
                        string jsonString = Encoding.UTF8.GetString(body);

                        File.WriteAllText(PackageListCache, jsonString, Encoding.UTF8);

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (HttpRequestException e)
                {
                    Debug.WriteLine(e.Message);
                    return false;
                }
            }

            async static public Task<List<PackageListing>> GetPackageList()
            {
                FileInfo fileInfo = new FileInfo(PackageListCache);

                if (!File.Exists(PackageListCache) || (DateTime.Now - fileInfo.LastWriteTime).TotalHours > 12)
                {
                    await DownloadPackageList();
                }

                FileStream file = File.OpenRead(PackageListCache);

                PackageListing[] packages = JsonSerializer.Deserialize<PackageListing[]>(file);

                file.Close();

                return new List<PackageListing>(packages);
            }

            async static public Task<PackageListing?> SearchPackageList(Predicate<PackageListing> predicate)
            {
                List<PackageListing> packages = await GetPackageList();

                int index = packages.FindIndex(predicate);

                return index >= 0 ? packages[index] : null;
            }

            async static public Task<string?> DownloadPackage(PackageListing listing)
            {
                string filename = listing.versions[0].full_name + ".zip";
                string downloadLocation = AppConfig.DownloadStorePath + "\\" + filename;

                if (!File.Exists(downloadLocation))
                {
                    using HttpResponseMessage response = await HTTPClient.GetAsync(listing.versions[0].download_url);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {

                        File.WriteAllBytes(downloadLocation, await response.Content.ReadAsByteArrayAsync());

                        return downloadLocation;
                    }
                    else
                    {
                        return null;
                    }
                }
                else return downloadLocation;
            }

            async static public Task<string?> DownloadPackage(PackageListingVersionEntry versionEntry)
            {
                string filename = versionEntry.full_name + ".zip";
                string downloadLocation = AppConfig.DownloadStorePath + "\\" + filename;

                if (!File.Exists(downloadLocation))
                {
                    using HttpResponseMessage response = await HTTPClient.GetAsync(versionEntry.download_url);

                    if (response.StatusCode == HttpStatusCode.OK)
                    {

                        File.WriteAllBytes(downloadLocation, await response.Content.ReadAsByteArrayAsync());

                        return downloadLocation;
                    }
                    else
                    {
                        return null;
                    }
                }
                else return null;
            }
        }
    }
}