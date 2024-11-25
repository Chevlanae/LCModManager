using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

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
            public PackageListingVersion[] versions { get; set; }
        }

        public struct PackageListingVersion
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
            private string? _ReadMe;
            private string? _ChangeLog;
            private ModManifest? _Manifest;

            public string? ReadMe => _ReadMe;
            public string? ChangeLog => _ChangeLog;

            public override void FromUri(Uri uri)
            {
                //uri is a file path, and is a local path that points to an existing file
                if (uri.IsFile && uri.IsLoopback && File.Exists(uri.LocalPath))
                {
                    SourceName = Path.GetDirectoryName(uri.LocalPath).Split("\\")[^1];

                    using (ZipArchive zip = ZipFile.OpenRead(uri.LocalPath))
                    {
                        foreach (ZipArchiveEntry entry in zip.Entries)
                        {
                            switch (entry.Name)
                            {
                                case "README.md":
                                    using (StreamReader file = new(entry.Open()))
                                    {
                                        _ReadMe = file.ReadToEnd();
                                    }
                                    break;

                                case "CHANGELOG.md":
                                    using (StreamReader file = new(entry.Open()))
                                    {
                                        _ChangeLog = file.ReadToEnd();
                                    }
                                    break;

                                case "icon.png":
                                    using (Stream file = entry.Open())
                                    using (MemoryStream stream = new())
                                    {
                                        file.CopyTo(stream);
                                        stream.Position = 0;

                                        Icon = new BitmapImage();
                                        Icon.BeginInit();
                                        Icon.StreamSource = stream;
                                        Icon.DecodePixelWidth = 64;
                                        Icon.DecodePixelHeight = 64;
                                        Icon.CacheOption = BitmapCacheOption.OnLoad;
                                        Icon.EndInit();
                                    }
                                    break;

                                case "manifest.json":
                                    using (StreamReader file = new(entry.Open()))
                                    {
                                        _Manifest = new ModManifest(file.ReadToEnd(), new(JsonSerializerDefaults.Web));
                                    }

                                    Name = _Manifest?.Name ?? "";
                                    Description = _Manifest?.Description;
                                    Website = _Manifest?.Website_Url;
                                    Dependencies = _Manifest?.Dependencies;

                                    break;
                            }
                        }
                    }
                }
            }

            public override void FromModEntry(IModEntry modEntry)
            {
                Icon = new BitmapImage();
                Icon.BeginInit();
                Icon.UriSource = new Uri("pack://application:,,,/Resources/PackageNotFound.png");
                Icon.DecodePixelWidth = 64;
                Icon.DecodePixelHeight = 64;
                Icon.CacheOption = BitmapCacheOption.OnLoad;
                Icon.EndInit();
                SourceName = modEntry.SourceName;
                Name = modEntry.Name;
                Author = modEntry.Author;
                Description = modEntry.Description;
                Versions = modEntry.Versions;
                Website = modEntry.Website;
                Dependencies = modEntry.Dependencies;
            }

            public void FromPackageListing(PackageListing listing)
            {
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

                foreach (PackageListingVersion entry in listing.versions)
                {
                    Versions.Add(entry.version_number, new Uri(entry.download_url));

                    foreach (string dep in entry.dependencies)
                    {
                        if (!Dependencies.Contains(dep)) Dependencies.Append(dep);
                    }
                }
            }
        }

        static internal class PackageManager
        {
            static public string StorePath = AppConfig.PackageStores["Thunderstore"].LocalPath;

            //Filename regex used primarily for AddPackage()
            static private Regex WinDuplicateFileReg = new("\\([0-9]+\\)\\.[a-zA-Z0-9]+$", RegexOptions.Compiled);
            static private Regex FileExtensionReg = new("\\.[a-zA-Z0-9]+$", RegexOptions.Compiled);
            static private Regex PackageNameReg = new(".*-.+\\..+\\..+", RegexOptions.Compiled);
            static private Regex PackageSourceFluffReg = new(WinDuplicateFileReg + "|" + FileExtensionReg, RegexOptions.Compiled);

            static private Mod GetModFromPath(string localPath)
            {
                Mod mod = new();

                mod.FromLocalPath(localPath);

                mod.Author = Path.GetFileNameWithoutExtension(localPath).Split("-")[0];

                return mod;
            }

            async static public Task<IModEntry?> AddMod(string sourcePath)
            {
                string sourceFilename = sourcePath.Split("\\").Last(); //lop off path

                string[] nameparts = sourceFilename[..^(PackageSourceFluffReg.Match(sourceFilename).Length)].Split("-"); //Lop off file extension and/or duplicate file patterns, then split string at each "-"

                if (nameparts.Length != 3 || !PackageNameReg.IsMatch(sourceFilename)) return null; //return null if sourceName does not match desired format

                if (File.Exists(sourcePath))
                {
                    try
                    {
                        string newFilename = StorePath + String.Join("-", nameparts) + ".zip";

                        if(!File.Exists(newFilename)) File.Copy(sourcePath, newFilename, true);

                        return GetModFromPath(newFilename);
                    }
                    catch (Exception ex)
                    {
                        Debug.Write(ex);
                        return null;
                    }
                }
                else return null;
            }

            static public List<IModEntry> GetMods()
            {
                List<IModEntry> mods = [];

                foreach (string file in Directory.GetFiles(StorePath))
                {
                    string[] nameparts = Path.GetFileNameWithoutExtension(file).Split("-");
                    Mod newMod = GetModFromPath(file);

                    if (mods.Find(m => m.Author == nameparts[0] && m.Name == nameparts[1]) is Mod existingMod)
                    {
                        existingMod.Versions.Add(nameparts[2], new Uri(file));
                        existingMod.Dependencies = existingMod.Dependencies.Union(newMod.Dependencies).ToArray();
                    }
                    else
                    {
                        newMod.Versions.Add(nameparts[2], new Uri(file));
                        mods.Add(newMod);
                    }
                }

                return mods;
            }

            static public List<IModEntry> GetMods(string name)
            {
                Regex regex = new Regex(name);
                return GetMods(regex);
            }

            static public List<IModEntry> GetMods(Regex regex)
            {
                List<IModEntry> mods = [];

                foreach (string file in Directory.GetFiles(StorePath).Where(path => regex.IsMatch(path)))
                {
                    string[] nameparts = Path.GetFileNameWithoutExtension(file).Split("-");
                    Mod newMod = GetModFromPath(file);

                    if (mods.Find(m => m.Author == nameparts[0] && m.Name == nameparts[1]) is Mod existingMod)
                    {
                        existingMod.Versions.Add(nameparts[2], new Uri(file));
                        existingMod.Dependencies = existingMod.Dependencies.Union(newMod.Dependencies).ToArray();
                    }
                    else
                    {
                        newMod.Versions.Add(nameparts[2], new Uri(file));
                        mods.Add(newMod);
                    }
                }

                return mods;
            }

            async static public Task RemoveMod(IModEntry mod)
            {
                // for each package who's name matches package.Name
                foreach (Mod m in GetMods().Where(p => p.Name == mod.Name && p.Author == mod.Author))
                {
                    // if no versions are selected, or all versions are selected, delete every version
                    if (mod.SelectedVersions.Count == 0 || mod.SelectedVersions.Count == mod.Versions.Count)
                    {
                        foreach (KeyValuePair<string, Uri> v in mod.Versions)
                        {
                            if (File.Exists(v.Value.LocalPath)) File.Delete(v.Value.LocalPath);
                        }
                    }
                    else
                    {
                        foreach (string version in mod.SelectedVersions)
                        {
                            string path = mod.Versions[version].LocalPath;

                            if (File.Exists(path)) File.Delete(path);
                        }
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
            static public string GameDir = GameDirectory.Find();
            static public class RegularExpressions
            {
                static public Regex BepInEx = new("bepinex", RegexOptions.IgnoreCase);
                static public Regex BepInExPack = new("bepinexpack", RegexOptions.IgnoreCase);
                static public Regex plugins = new("plugins", RegexOptions.IgnoreCase);
            }

            static public string InferEntryParentDirectory(ZipArchiveEntry entry)
            {
                string topDir;

                if (entry.FullName.Contains("/"))
                {
                    topDir = entry.FullName.Split("/")[0];
                }
                else if (entry.FullName.Contains("\\"))
                {
                    topDir = entry.FullName.Split("\\")[0];
                }
                else if(entry.Name == "doorstop_config.ini" || entry.Name == "winhttp.dll")
                {
                    return GameDir;
                }
                else
                {
                    return GameDir + "BepInEx\\plugins";
                }

                if (RegularExpressions.BepInEx.IsMatch(topDir)) topDir = "BepInEx";
                if (RegularExpressions.BepInExPack.IsMatch(topDir)) topDir = "BepInExPack";
                if (RegularExpressions.plugins.IsMatch(topDir)) topDir = "plugins";

                return topDir switch
                {
                    "BepInEx" => GameDir,
                    "BepInExPack" => GameDir,
                    "core" => GameDir + "BepInEx",
                    "plugins" => GameDir + "BepInEx",
                    "config" => GameDir + "BepInEx",
                    "patchers" => GameDir + "BepInEx",
                    _ => GameDir + "BepInEx\\plugins",
                };
            }

            static public void DeployMod(IModEntry mod)
            {
                string selectedVersionPath = mod.Versions[mod.SelectedVersions[0]].AbsolutePath;
                string[] excludedFilenames = ["icon.png", "manifest.json", "README.md", "CHANGELOG.md"];
                string bepinexpack = "BepInExPack/";
                string destinationPath;

                using (ZipArchive zip = ZipFile.OpenRead(selectedVersionPath))
                {
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        bool excluded = false;
                        foreach (string name in excludedFilenames)
                        {
                            if (entry.Name.Contains(name))
                            {
                                excluded = true;
                                break;
                            }
                        }

                        if (excluded) continue;

                        string extractPath = InferEntryParentDirectory(entry);

                        if (entry.FullName.Contains(bepinexpack) && entry.FullName.Count() >= bepinexpack.Count())
                        {
                            destinationPath = Path.Combine(extractPath, entry.FullName.Substring(bepinexpack.Count()));
                        }
                        else
                        {
                            destinationPath = Path.Combine(extractPath, entry.FullName);
                        }

                        if (entry.Name == "")
                        {
                            Directory.CreateDirectory(destinationPath);
                        }
                        else
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                            entry.ExtractToFile(destinationPath, overwrite: true);
                        }
                    }
                }
            }

            async static public Task DeployProfile(ModProfile profile)
            {
                if(profile.ModList.Find(m => m.Name == "BepInExPack") is IModEntry modEntry)
                {
                    DeployMod(modEntry);
                }
                else return;

                foreach (IModEntry mod in profile.ModList.FindAll(m => m.Name != "BepInExPack")) DeployMod(mod);

                while (!File.Exists(GameDir + "\\" + "doorstop_config.ini")) await Task.Delay(100);
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
                        continue;
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
                            continue;
                        }

                        Task.Delay(100);
                    }
                }
            }
        }

        static internal class WebClient
        {
            static public class Endpoints
            {
                static public string BaseURL = "https://thunderstore.io/c/lethal-company/api/v1";
                static public string PackageList = BaseURL + "/package";
            }

            static public string PackageCachePath = AppConfig.PackageStore.LocalPath + "ThunderstoreCache.json";
            static private HttpClient HTTPClient = new();


            //Cache singleton
            public class PackageCache
            {
                static private Dictionary<string, PackageListing> _Cache = new();

                static public TimeSpan RefreshInterval = new TimeSpan(AppConfig.WebCacheRefreshInterval["Hours"],
                                                                      AppConfig.WebCacheRefreshInterval["Minutes"],
                                                                      AppConfig.WebCacheRefreshInterval["Seconds"]);

                static public DateTime LastRefresh = new FileInfo(PackageCachePath).LastWriteTime;

                static public bool NeedsRefresh
                {
                    get
                    {
                        if (File.Exists(PackageCachePath)) return ((DateTime.Now - LastRefresh) > RefreshInterval);
                        else return true;
                    }
                }

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

                async static public Task LoadCache()
                {
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

            async static public Task<HttpResponseMessage?> DownloadPackageListHeaders()
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

            async static public Task<List<PackageListing>?> GetPackageList()
            {
                try
                {
                    PackageListing[] packages;

                    using (FileStream file = File.OpenRead(PackageCachePath))
                    {
                        packages = JsonSerializer.Deserialize<PackageListing[]>(file);
                    }

                    return new List<PackageListing>(packages);
                }
                catch (Exception ex)
                {
                    Debug.Write(ex);
                    return null;
                }
            }

            async static public Task<HttpResponseMessage?> DownloadPackageHeaders(PackageListingVersion versionEntry)
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