using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LCModManager
{
    public interface IModEntry
    {
        public bool ExistsInPackageStore { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string? Author { get; set; }
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? IconUri { get; set; }
        public List<string> Versions { get; set; }
        public string[]? Dependencies { get; set; }
        public string[]? MissingDependencies { get; set; }
        public string[]? MismatchedDependencies { get; set; }
    }

    public class ModEntry : IModEntry
    {
        public bool ExistsInPackageStore { get; set; } = false;
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Author { get; set; }
        public string? Description { get; set; }
        public string? Website { get; set; }
        public string? IconUri { get; set; }
        public List<string> Versions { get; set; } = [];
        public string[]? Dependencies { get; set; }
        public string[]? MissingDependencies { get; set; }
        public string[]? MismatchedDependencies { get; set; }
    }

    public class ModEntrySelection
    {
        public string SelectedVersion;
        public ModEntry ModEntry;

        public ModEntrySelection()
        {
            SelectedVersion = "";
            ModEntry = new ModEntry();
        }

        public ModEntrySelection(string selectedVersion, ModEntry modEntry)
        {
            SelectedVersion = selectedVersion;
            ModEntry = modEntry;
        }
    }

    public class ModEntryDisplay : ModEntry
    {
        public BitmapImage? Icon { get; set; }
        public bool HasMissingDependencies
        {
            get
            {
                return MissingDependencies != null && MissingDependencies.Length > 0;
            }
        }

        public bool HasMismatchedDependencies
        {
            get
            {
                return MismatchedDependencies != null && MismatchedDependencies.Length > 0;
            }
        }

        public bool HasIncompatibility
        {
            get
            {
                return HasMissingDependencies || HasMismatchedDependencies;
            }
        }

        public List<string> SelectedVersions = [];

        public void ProcessDependencies(List<ModEntryDisplay> modlist)
        {
            if (Dependencies != null)
            {
                List<string> missingDeps = [];
                List<string> mismatchedDeps = [];

                foreach (string depStr in Dependencies)
                {
                    string[] depStrParts = depStr.Split('-');
                    string owner = depStrParts[^3];
                    string name = depStrParts[^2];
                    string version = depStrParts[^1];

                    List<ModEntryDisplay> foundDependencies = modlist.FindAll(e => e.Name == name);

                    switch (foundDependencies.Count)
                    {
                        case 0:

                            missingDeps.Add(depStr);
                            break;

                        case 1:

                            if (!foundDependencies[0].ExistsInPackageStore)
                            {
                                missingDeps.Add(depStr);
                            }
                            else if (!foundDependencies[0].Versions.Contains(version))
                            {
                                mismatchedDeps.Add(depStr);
                            }
                            break;

                        default:
                            bool match = false;
                            foreach (ModEntryDisplay item in foundDependencies)
                            {
                                if (item.Versions.Contains(version))
                                {
                                    match = true;
                                    break;
                                }

                            }
                            if (!match) mismatchedDeps.Add(depStr);
                            break;
                    }
                }

                MismatchedDependencies = [.. mismatchedDeps];
                MissingDependencies = [.. missingDeps];
            }
        }

        public ModEntry ToModEntry()
        {
            return new ModEntry
            {
                Path = Path,
                Name = Name,
                Author = Author,
                Description = Description,
                Versions = Versions,
                Website = Website,
                IconUri = IconUri,
                Dependencies = Dependencies,
                MissingDependencies = MissingDependencies,
                MismatchedDependencies = MismatchedDependencies,
                ExistsInPackageStore = ExistsInPackageStore
            };
        }

        public ModEntrySelection ToModEntrySelection(string selectedVersion)
        {
            return new ModEntrySelection
            {
                SelectedVersion = selectedVersion,
                ModEntry = ToModEntry()
            };
        }
    }
}
