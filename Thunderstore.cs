using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace LCModManager
{
    namespace Thunderstore
    {
        public struct ModManifest
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

        public struct Listing
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
            public ListingVersion[] versions { get; set; }
        }

        public struct ListingVersion
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

        [DataContract]
        public class Mod : ModPackage
        {
            public string? ReadMe { get; set; }
            public string? ChangeLog { get; set; }
            public ModManifest? Manifest { get; set; }

            static public IModEntry FromUri(Uri uri)
            {
                Mod newMod = new();

                //uri is a file path, and is a local path that points to an existing file
                if (uri.IsFile && uri.IsLoopback && File.Exists(uri.LocalPath))
                {
                    newMod.SourceName = Path.GetDirectoryName(uri.LocalPath).Split("\\")[^1];
                    newMod.Author = Path.GetFileNameWithoutExtension(uri.LocalPath).Split("-")[0];

                    using (ZipArchive zip = ZipFile.OpenRead(uri.LocalPath))
                    {
                        foreach (ZipArchiveEntry entry in zip.Entries)
                        {
                            switch (entry.Name)
                            {
                                case "README.md":
                                    using (StreamReader file = new(entry.Open()))
                                    {
                                        newMod.ReadMe = file.ReadToEnd();
                                    }
                                    break;

                                case "CHANGELOG.md":
                                    using (StreamReader file = new(entry.Open()))
                                    {
                                        newMod.ChangeLog = file.ReadToEnd();
                                    }
                                    break;

                                case "icon.png":
                                    using (Stream file = entry.Open())
                                    {
                                        newMod.GetIcon(file);
                                    }
                                    break;

                                case "manifest.json":
                                    using (StreamReader file = new(entry.Open()))
                                    {
                                        newMod.Manifest = new ModManifest(file.ReadToEnd(), new(JsonSerializerDefaults.Web));
                                    }

                                    newMod.Name = newMod.Manifest?.Name ?? "";
                                    newMod.Description = newMod.Manifest?.Description;
                                    newMod.Website = newMod.Manifest?.Website_Url;
                                    newMod.Dependencies = newMod.Manifest?.Dependencies;

                                    break;
                            }
                        }
                    }
                }

                return newMod;
            }

            static public IModEntry FromLocalPath(string localPath)
            {
                return FromUri(new Uri(localPath));
            }

            static public IModEntry FromModEntry(IModEntry modEntry)
            {
                Mod newMod = new();

                newMod.GetIcon(new Uri("pack://application:,,,/Resources/PackageNotFound.png"));
                newMod.SourceName = modEntry.SourceName;
                newMod.Name = modEntry.Name;
                newMod.Author = modEntry.Author;
                newMod.Description = modEntry.Description;
                newMod.Versions = modEntry.Versions;
                newMod.Website = modEntry.Website;
                newMod.Dependencies = modEntry.Dependencies;

                return newMod;
            }

            static public IModEntry FromListing(Listing listing)
            {
                Mod newMod = new();
                newMod.GetIcon(new Uri(listing.versions[0].icon));
                newMod.Name = listing.name;
                newMod.Author = listing.owner;
                newMod.Description = listing.versions[0].description;
                newMod.Website = listing.package_url;
                newMod.Versions = [];
                newMod.Dependencies = [];

                foreach (ListingVersion entry in listing.versions)
                {
                    newMod.Versions.Add(entry.version_number, new Uri(entry.download_url));

                    foreach (string dep in entry.dependencies)
                    {
                        if (!newMod.Dependencies.Contains(dep)) newMod.Dependencies.Append(dep);
                    }
                }

                return newMod;
            }
        }

        static internal class PackageManager
        {
            static public string StorePath = AppConfig.PackageStores["Thunderstore"].LocalPath;

            static private class RegexPatterns
            {
                static public string WindowsDuplicateFilename = "\\([0-9]+\\)\\.[a-zA-Z0-9]+$";
                static public string FilenameExtension = "\\.[a-zA-Z0-9]+$";
                static public string WindowsFilenameFluff = WindowsDuplicateFilename + "|" + FilenameExtension;
                static public string PackageName = ".*-.+\\..+\\..+";
            }

            static private class RegExpressions
            {
                static public Regex WindowsDuplicateFilename = new(RegexPatterns.WindowsDuplicateFilename, RegexOptions.Compiled);
                static public Regex FilenameExtension = new(RegexPatterns.FilenameExtension, RegexOptions.Compiled);
                static public Regex WindowsFilenameFluff = new(RegexPatterns.WindowsFilenameFluff, RegexOptions.Compiled);
                static public Regex PackageName = new(RegexPatterns.PackageName, RegexOptions.Compiled);
            }

            async static public Task AddMod(MemoryStream ms, string fullName)
            {
                if(!RegExpressions.PackageName.IsMatch(fullName)) return;

                string newFilename = StorePath + fullName + ".zip";

                using (FileStream file = File.Create(newFilename))
                {
                    ms.Position = 0;
                    await ms.CopyToAsync(file);
                }
            }

            async static public Task AddMod(string sourcePath)
            {
                string sourceFilename = sourcePath.Split("\\").Last(); //lop off parent path

                string[] nameParts = sourceFilename[..^(RegExpressions.WindowsFilenameFluff.Match(sourceFilename).Length)].Split("-"); //Lop off file extension and/or duplicate file patterns, then split string at each "-"
                string fullName = String.Join("-", nameParts);
                string newFilename = StorePath + fullName + ".zip";

                if (nameParts.Length != 3 || !RegExpressions.PackageName.IsMatch(sourceFilename)) return; //return if sourceFilename does not match desired format
                else if (File.Exists(sourcePath))
                {
                    try
                    {
                        await Task.Run(() =>
                        {
                            File.Copy(sourcePath, newFilename, true);
                        });
                    }
                    catch (Exception ex)
                    {
                        Debug.Write(ex);
                    }
                }
            }

            async static public Task<List<IModEntry>> GetMods(Regex? regex = null)
            {
                List<IModEntry> mods = [];
                List<string> storeFiles;

                if(regex == null)
                {
                    storeFiles = new(Directory.GetFiles(StorePath));
                }
                else
                {
                    storeFiles = new(Directory.GetFiles(StorePath).Where(path => regex.IsMatch(path)));
                }

                foreach(string filePath in storeFiles)
                {
                    string[] nameparts = Path.GetFileNameWithoutExtension(filePath).Split("-");
                    string author = nameparts[0];
                    string name = nameparts[1];
                    string version = nameparts[2];

                    if (mods.Any(m => m.Name == name && m.Author == author)) continue;

                    IModEntry mod = Mod.FromLocalPath(filePath);

                    List<string> matches = storeFiles.FindAll(f =>
                    {
                        string[] fParts = Path.GetFileNameWithoutExtension(f).Split("-");

                        return fParts[0] == author && fParts[1] == name;
                    });

                    foreach (string match in matches)
                    {
                        string matchVersion = Path.GetFileNameWithoutExtension(match).Split("-")[2];

                        mod.Versions[matchVersion] = new(match);
                    }

                    mods.Add(mod);
                }

                return mods;
            }

            async static public Task<List<IModEntry>> GetMods(string filename)
            {
                Regex regex = new Regex(Path.GetFileName(filename));
                return await GetMods(regex);
            }

            async static public Task RemoveMod(IModEntry mod)
            {
                // if no versions are selected, or all versions are selected, delete every version
                if (mod.SelectedVersions.Count == 0 || mod.SelectedVersions.Count == mod.Versions.Count)
                {
                    foreach (KeyValuePair<string, Uri> v in mod.Versions)
                    {
                        await Task.Run(() =>
                        {
                            if (File.Exists(v.Value.LocalPath)) File.Delete(v.Value.LocalPath);
                        });
                    }
                }
                else
                {
                    foreach (string version in mod.SelectedVersions)
                    {
                        string path = mod.Versions[version].LocalPath;

                        await Task.Run(() =>
                        {
                            if (File.Exists(path)) File.Delete(path);
                        });
                    }
                }
            }

            async static public Task RemoveMods(IEnumerable<IModEntry> packages)
            {
                foreach (Mod m in packages) await RemoveMod(m);
            }
        }

        static internal class ModDeployer
        {
            //find Lethal Company game directory on system
            static public string? GameDir = GameDirectory.Find();
            static public string[] ExcludedFilenames = ["icon.png", "manifest.json", "README.md", "CHANGELOG.md", "LICENSE.MD", "LICENSE"];
            static private Regex BepInExPackFSlashPattern = new("bepinexpack/", RegexOptions.IgnoreCase);
            static private Dictionary<string, Regex> BepInExDirPatterns = new()
            {
                {"BepInEx", new("bepinex", RegexOptions.IgnoreCase)},
                {"BepInExPack", new("bepinexpack", RegexOptions.IgnoreCase)},
                {"plugins", new("plugins", RegexOptions.IgnoreCase)},
                {"core", new("core", RegexOptions.IgnoreCase)},
                {"config",  new("config", RegexOptions.IgnoreCase)},
                {"patchers", new("patchers", RegexOptions.IgnoreCase)}
            };

            static public string InferEntryTarget(ZipArchiveEntry entry)
            {

                string substring = entry.FullName[BepInExPackFSlashPattern.Match(entry.FullName).Length..];
                string topDir;

                if (entry.Name == "doorstop_config.ini" || entry.Name == "winhttp.dll")
                {
                    return Path.GetFullPath(Path.Combine(GameDir, substring));
                }

                if (substring.Contains("/"))
                {
                    topDir = entry.FullName.Split("/")[0];
                }
                else if (substring.Contains("\\"))
                {
                    topDir = entry.FullName.Split("\\")[0];
                }
                else topDir = "";

                //match top level dir name to known bepinex directory patterns
                foreach(KeyValuePair<string, Regex> reg in BepInExDirPatterns)
                {
                    if(reg.Value.IsMatch(topDir))
                    {
                        topDir = reg.Key;
                        break;
                    }
                }

                //infer target directory from matched top level directory
                return topDir switch
                {
                    "BepInEx" => Path.GetFullPath(Path.Combine(GameDir, substring)),
                    "BepInExPack" => Path.GetFullPath(Path.Combine(GameDir, substring)),
                    "plugins" => Path.GetFullPath(Path.Combine(GameDir, "BepInEx\\", substring)),
                    "core" => Path.GetFullPath(Path.Combine(GameDir, "BepInEx\\", substring)),
                    "config" => Path.GetFullPath(Path.Combine(GameDir, "BepInEx\\", substring)),
                    "patchers" => Path.GetFullPath(Path.Combine(GameDir, "BepInEx\\", substring)),
                    _ => Path.GetFullPath(Path.Combine(GameDir, "BepInEx\\plugins\\", substring)),
                };
            }

            static public void DeployMod(IModEntry mod)
            {
                string selectedVersionPath = mod.Versions[mod.SelectedVersions[0]].AbsolutePath;
                string targetLocation;

                //open mod archive
                using (ZipArchive zip = ZipFile.OpenRead(selectedVersionPath))
                {
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        //if filename is an exlcuded filename, skip entry
                        if (ExcludedFilenames.Any(n => n == entry.Name) || entry.FullName == "BepInExPack/") continue;

                        //infer parent dir from archive structure
                        targetLocation = InferEntryTarget(entry);

                        //if given entry is a directory, create a directory at the targetLocation
                        if (entry.Name == "")
                        {
                            Directory.CreateDirectory(targetLocation);
                        }
                        //else ensure parent directory exists, and then extract the entry to the targetLocation
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(targetLocation));
                            entry.ExtractToFile(targetLocation, overwrite: true);
                        }
                    }
                }
            }

            async static public Task DeployProfile(ModProfile profile)
            {
                if(profile.ModList.Find(m => m.Name == "BepInExPack") is IModEntry modEntry) DeployMod(modEntry);

                foreach (IModEntry mod in profile.ModList.FindAll(m => m.Name != "BepInExPack")) DeployMod(mod);
            }

            async static public Task<bool> StartGameWithProfile(ModProfile profile, int deployTimeout = 300)
            {
                if (GameDir != null)
                {
                    await CleanupGameDir();

                    await DeployProfile(profile);

                    while (!File.Exists(GameDir + "\\" + "doorstop_config.ini") && deployTimeout > 0)
                    {
                        await Task.Delay(1000);

                        deployTimeout--;

                        if (deployTimeout <= 0) return false;
                    }

                    try
                    {
                        ProcessStartInfo info = new(GameDir + "\\Lethal Company.exe");

                        Process? process = Process.Start(info);

                        await process.WaitForExitAsync();

                        await CleanupGameDir();

                        return true;
                    }
                    catch (Exception ex)
                    {
                        Debug.Write(ex);
                        return false;
                    }
                }
                else return false;
            }

            async static public Task CleanupGameDir()
            {
                string bepinexDir = GameDir + "BepInEx";
                string[] bepinexFiles = [
                    GameDir + "winhttp.dll",
                    GameDir + "doorstop_config.ini"
                ];

                while (Directory.Exists(bepinexDir))
                {
                    try
                    {
                        Directory.Delete(bepinexDir, true);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        break;
                    }

                    Task.Delay(100);
                }

                foreach(string file in bepinexFiles)
                {
                    while (File.Exists(file))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (UnauthorizedAccessException e)
                        {
                            break;
                        }

                        Task.Delay(100);
                    }
                }
            }
        }

        static internal class WebClient
        {
            //Cache singleton
            private class Cache
            {
                static private Dictionary<string, Listing> _Cache = new();

                static public Dictionary<string, Listing> Instance
                {
                    get
                    {
                        lock (_Cache)
                        {
                            return _Cache;
                        }
                    }
                }

                Cache() { }
            }

            static public class Endpoints
            {
                static public string BaseURL = "https://thunderstore.io/c/lethal-company/api/v1";
                static public string PackageList = BaseURL + "/package";
            }

            static public string CacheFilePath = AppConfig.PackageStore.LocalPath + "ThunderstoreCache.dat";

            static private HttpClient HTTPClient = new();

            static public TimeSpan RefreshInterval = new TimeSpan(AppConfig.WebCacheRefreshInterval["Hours"],
                                                                  AppConfig.WebCacheRefreshInterval["Minutes"],
                                                                  AppConfig.WebCacheRefreshInterval["Seconds"]);

            static public DateTime? LastRefresh
            {
                get
                {
                    try
                    {
                        if (File.Exists(CacheFilePath))
                        {
                            return new FileInfo(CacheFilePath).LastWriteTime;
                        }
                        else return null;
                    }
                    catch { return null; }
                }
            }

            static public bool NeedsRefresh
            {
                get
                {
                    return LastRefresh == null || (DateTime.Now - LastRefresh) > RefreshInterval;
                }
            }

            async static public Task LoadCache()
            {
                if (await GetCache() is List<Listing> list)
                {
                    Cache.Instance.Clear();
                    foreach (Listing package in list) Cache.Instance.Add(package.full_name, package);
                }
            }

            static public List<Listing> SearchCache(Func<KeyValuePair<string, Listing>, bool> predicate)
            {
                List<Listing> list = new();

                foreach (KeyValuePair<string, Listing> listing in Cache.Instance.Where(predicate))
                {
                    list.Add(listing.Value);
                }

                return list;
            }

            static public Listing? GetCachedListing(string fullName)
            {
                try
                {
                    return Cache.Instance[fullName];
                }
                catch (Exception ex)
                {
                    Debug.Write(ex);
                    return null;
                }
            }

            async static public Task SetCache(MemoryStream ms)
            {
                try
                {
                    using (FileStream file = File.Create(CacheFilePath))
                    {
                        ms.Position = 0;
                        await ms.CopyToAsync(file);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Write(ex);
                }
            }

            async static public Task<List<Listing>?> GetCache()
            {
                try
                {
                    Listing[] packages;

                    using (FileStream file = File.OpenRead(CacheFilePath))
                    {
                        packages = JsonSerializer.Deserialize<Listing[]>(file);
                    }

                    return new List<Listing>(packages);
                }
                catch (Exception ex)
                {
                    Debug.Write(ex);
                    return null;
                }
            }

            async static public Task<HttpResponseMessage?> DownloadPackageListHeaders()
            {
                try
                {
                    HttpRequestMessage request = new(HttpMethod.Get, Endpoints.PackageList);
                    request.Headers.Add("Accept-Encoding", "gzip");
                    return await HTTPClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return null;
                }
            }

            async static public Task<HttpResponseMessage?> DownloadPackageHeaders(ListingVersion versionEntry)
            {
                try
                {
                    return await HTTPClient.GetAsync(versionEntry.download_url, HttpCompletionOption.ResponseHeadersRead);
                }
                catch (HttpRequestException ex)
                {
                    Debug.Write(ex);
                    return null;
                }
            }
        }
    }
}