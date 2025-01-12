using System.Diagnostics;
using System.IO.Compression;

void print(string message) { Console.WriteLine(message); }

string errorLogPath = Path.Combine(Directory.GetCurrentDirectory(), "errorLog.txt");
void createErrorLog(string message)
{
    int errorLogNum = 0;
    while (true)
    {
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "errorLog_" + errorLogNum + ".txt")))
            errorLogNum++;
        else
            break;
    }

    string finalErrorPath = Path.Combine(Directory.GetCurrentDirectory(), "errorLog_" + errorLogNum + ".txt");
    print("Error occured. Creating error log at: " + finalErrorPath);
    using (var writer = new StreamWriter(finalErrorPath))
    {
        writer.Write(message);
    }
}

print("Starting mods downloader");

// File paths
string newZippedVersionPath = Path.Combine(Directory.GetCurrentDirectory(), "newVersion.zip");
string newZippedModsPath = Path.Combine(Directory.GetCurrentDirectory(), "zippedMods.zip");
string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
string modsFolderPath = Path.Combine(appDataPath, ".minecraft/mods/");
string shadersFolderPath = Path.Combine(appDataPath, ".minecraft/shaderpacks/");
string newZippedShaderPath = Path.Combine(shadersFolderPath, "ComplementaryShaders.zip");

// Download links
string versionTextFileDownload = "https://dl.dropboxusercontent.com/scl/fi/a24v6aqodr5nmi572wpp7/c-codermodsprogramversion.txt?rlkey=5kvn0te9sq2rbscjmsndy2ncj&dl=0";
string programDownload = "https://dl.dropboxusercontent.com/scl/fi/w613bi57egarbkoudmt9s/C-CodersModsProgram.zip?rlkey=7sw906fr33yugkfgcr16cr51j&dl=0";
string complementaryShadersDownload = "https://files.forgecdn.net/files/4746/341/ComplementaryUnbound_r5.0.1.zip";

// Server ip's and modpack links
Dictionary<string, string> serverModpacks = new Dictionary<string, string>()
{
    ["Example1.server.com (1.19.2)"] = "https://downloadlink1.zip",
    ["Example2.server.com (1.20.1)"] = "https://downloadlink2.zip"
};

bool doneCheckingVersion = false;

// Download and write to the program's version text file
async void DownloadNewestVersionFile()
{
    using (var client = new HttpClient())
    {
        using (var download = await client.GetStreamAsync(versionTextFileDownload))
        {
            string currentDir = Directory.GetCurrentDirectory();
            string currentFilePath = Path.Combine(currentDir, "version.txt");
            using (var fileStream = new FileStream(currentFilePath, FileMode.OpenOrCreate))
            {
                print("New version written");
                download.CopyTo(fileStream);
                doneCheckingVersion = true;
            }
        }
    }
}

// Download the actual latest program version, then starting it and deleting this current version
async void DownloadNewestVersion(string versionName)
{
    using (var client = new HttpClient())
    {
        using (var download = await client.GetStreamAsync(programDownload))
        {
            try
            {
                print("New version downloaded...");

                // Download zipped file and write it to the zip
                using (var fileStream = new FileStream(newZippedVersionPath, FileMode.OpenOrCreate))
                {
                    download.CopyTo(fileStream);
                }

                // Unzip new version
                print("Unzipping version...");
                ZipFile.ExtractToDirectory(newZippedVersionPath, Directory.GetCurrentDirectory(), true);

                // Stop the current process
                print("Stopping current version...");
                //Process.GetCurrentProcess().Kill();

                // Startup new version
                print("Starting new version...");
                string newVersionExe = Path.Combine(Directory.GetCurrentDirectory(), "C-CodersMods.exe");
                Process newVersionProcess = Process.Start(newVersionExe);

                // Delete this directory
                print("Cleaning up...");
                DirectoryInfo directoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());

                // Finally stopping
                print("Finishing.");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                createErrorLog("Error updating C-CodersMods: " + ex.Message);
            }
        }
    }
}

// Check current program version
async void CheckVersion()
{
    try
    {
        string currentDir = Directory.GetCurrentDirectory();
        string currentFilePath = Path.Combine(currentDir, "version.txt");

        // Check if file doesn't exist (first time download or user deleted version file)
        if (!File.Exists(currentFilePath))
        {
            print("Downloading new version file...");

            // Download newest version file, don't bother downloading the newest version, since they could've just downloaded the program, and, if the user deleted the version file, it's their fault lol
            DownloadNewestVersionFile();
        }
        else
        {
            print("Comparing versions...");

            // If the file does exist, just read it
            string currentVersion;
            using (FileStream stream = new FileStream(currentFilePath, FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    currentVersion = reader.ReadToEndAsync().Result;
                }
            }

            // Download online version
            string updatedVersion;
            using (var client = new HttpClient())
            {
                using (var download = await client.GetStreamAsync(versionTextFileDownload))
                {
                    using (var reader = new StreamReader(download))
                    {
                        updatedVersion = reader.ReadToEnd();
                    }
                }
            }

            // Then compare the versions, and decide whether an update is needed
            if (currentVersion != updatedVersion)
            {
                print("Updating program...");

                // Update current version text file (would use DownloadNewestVersionFile() but that would download it again)
                File.WriteAllTextAsync(currentFilePath, updatedVersion);

                // Download new version
                DownloadNewestVersion(updatedVersion);
            }
            else
                doneCheckingVersion = true;
        }
    }
    catch (Exception ex)
    {
        print("Error checking version...");
        print("Error: " + ex.Message);
        Environment.Exit(0);
    }
}

void PackageOldMods()
{
    print("Putting old mods away...");

    int emptyArchive = 0;
    bool foundEmptyArchiveSlot = false;
    while (!foundEmptyArchiveSlot)
    {
        if (Path.Exists(Path.Combine(modsFolderPath, "OLD_MODS_ARCHIVE_" + emptyArchive)))
        {
            emptyArchive++;
        }
        else
        {
            break;
        }
    }

    DirectoryInfo info = Directory.CreateDirectory(Path.Combine(modsFolderPath, "OLD_MODS_ARCHIVE_" + emptyArchive));
    string[] files = Directory.GetFiles(modsFolderPath, "*.jar");
    foreach (string file in files)
    {
        string fileName = Path.GetFileName(file);
        string destinationPath = Path.Combine(info.FullName, fileName);

        File.Move(file, destinationPath);
    }

    print("Old mods put away.");
}

void RemoveOldMods()
{
    print("Removing old mods...");

    string[] files = Directory.GetFiles(modsFolderPath, "*.jar");
    foreach (string file in files)
    {
        File.Delete(file);
    }

    print("Old mods removed.");
}

// Actual function calls and program workings and stuff
Task.Run(() => CheckVersion());

while (!doneCheckingVersion)
{
    Thread.Sleep(1000 / 5);
}

print("Version check completed.");
print("Would you like to delete your old mods? (Y/N)");
bool? removeOldMods = null;
while (!removeOldMods.HasValue)
{
    ConsoleKeyInfo input = Console.ReadKey();
    if (input.Key == ConsoleKey.Y)
        removeOldMods = true;
    else if (input.Key == ConsoleKey.N)
        removeOldMods = false;
    else
        print("Please enter the keys Y or N");
}
print("\n");

if (removeOldMods.Value)
    RemoveOldMods();
else
    PackageOldMods();

// Now, previous mods are handled, and now we can download the new ones
KeyValuePair<string, string> chosenModpackLiteral;
void ChoseModpack()
{
    print("What modpack would you like to download? (Press number)");
    int modpackIteration = 1;
    foreach (KeyValuePair<string, string> modpack in serverModpacks)
    {
        print(modpackIteration + ": " + modpack.Key);
        modpackIteration++;
    }

    int chosenModpack = -1;
    while (chosenModpack == -1)
    {
        ConsoleKeyInfo info = Console.ReadKey();
        if (Char.IsNumber(info.KeyChar))
        {
            int charInt = int.Parse(info.KeyChar.ToString());
            if (charInt >= 1 && charInt <= serverModpacks.Count)
                chosenModpack = charInt;
            else
                print("Number not in range");
        }
        else
        {
            print("Please chose a valid option.");
        }
    }
    print("\n");

    // chosenModpack - 1 because if the user enters 1, they really want the first value, 0
    chosenModpackLiteral = serverModpacks.ElementAt(chosenModpack - 1);
    print("Modpack: " + chosenModpackLiteral.Key + ", chosen!");
}

ChoseModpack();


// Verify that chosen modpack is the right one
bool correctOption = false;
while (!correctOption)
{
    print("Is this the correct option? (Y/N)");
    ConsoleKeyInfo correctModpack = Console.ReadKey();
    if (correctModpack.Key == ConsoleKey.Y)
        correctOption = true;
    else if (correctModpack.Key == ConsoleKey.N)
        ChoseModpack();
    else
        print("Please enter the keys Y or N");
}
print("\n");

print("Beginning modpack download.");

// Download the zipped mods file
bool doneDownloadingMods = false;
Task.Run(() => ConsoleLoadingAnimation());
using (var client = new HttpClient())
{
    using (var download = client.GetStreamAsync(chosenModpackLiteral.Value))
    {
        using (var fileStream = new FileStream(newZippedModsPath, FileMode.OpenOrCreate))
        {
            download.Result.CopyTo(fileStream);
            doneDownloadingMods = true;
        }
    }
}

void ConsoleLoadingAnimation()
{
    static char GetLoadingSymbol(int iteration)
    {
        char[] symbols = { '|', '/', '|', '\\' };
        return symbols[iteration % symbols.Length];
    }

    int iteration = 0;
    while (!doneDownloadingMods)
    {
        Console.Write("\rDownloading: " + GetLoadingSymbol(iteration));
        iteration++;
        Thread.Sleep(1000 / 5);
    }
}

// Unzip the mods into the mods folder
print("Extracting mods...");
ZipFile.ExtractToDirectory(newZippedModsPath, modsFolderPath);
File.Delete(newZippedModsPath);

// Be done
print("Finished!");

// Start the shaderpack downloading
print("Additionally, you can also download the Complementary shaderpack. (Y/N)");
bool? downloadShader = null;
while (!downloadShader.HasValue)
{
    ConsoleKeyInfo input = Console.ReadKey();
    if (input.Key == ConsoleKey.Y)
        downloadShader = true;
    else if (input.Key == ConsoleKey.N)
        downloadShader = false;
    else
        print("Please enter the keys Y or N");
}
print("\n");

if (downloadShader.Value)
{
    print("Downloading Complementary shaders...");
    doneDownloadingMods = false;
    Task.Run(() => ConsoleLoadingAnimation());
    using (var client = new HttpClient())
    {
        using (var download = client.GetStreamAsync(complementaryShadersDownload))
        {
            using (var fileStream = new FileStream(newZippedShaderPath, FileMode.OpenOrCreate))
            {
                download.Result.CopyTo(fileStream);
                doneDownloadingMods = true;
            }
        }
    }
}
else
{
    print("Processes completed.");
}