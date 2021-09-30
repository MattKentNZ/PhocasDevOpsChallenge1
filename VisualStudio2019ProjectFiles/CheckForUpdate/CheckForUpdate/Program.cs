using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Net;
using System.IO.Compression;
using System.Windows.Forms;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace CheckForUpdate
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create the required directories to work in if they don't exist already
            Directory.CreateDirectory(@"C:\Windows\Temp\terraform");
            Directory.CreateDirectory(@"C:\Windows\Temp\terraform\VersionControl");
            Directory.CreateDirectory(@"C:\Windows\Temp\terraform\BAT");

            // Incase TEMP copy of VersionInfo does not exist, pulls from Program Files. Just helps to avoid running a .bat file if it's not needed due to having to accept for permissions.
            if (!File.Exists(@"C:\Windows\Temp\terraform\VersionControl\VersionInfo.txt"))
            {
                string message = "No version history exists, Update?";
                string title = "Terraform Updater";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result = MessageBox.Show(message, title, buttons);
                if (result == DialogResult.Yes)
                {
                    // Grab the current stored version type from Program Files for temporary use in Windows\Temp\ to ensure the program can access it.
                    string copyVersion = ("\"" + @"C:\Program Files\terraform\VersionControl\VersionInfo.txt" + "\"" + " " + "\"" + @"C:\Windows\Temp\terraform\VersionControl" + "\"");

                    string[] copyVersionLines = { "NET SESSION", "IF %ERRORLEVEL% NEQ 0 GOTO ELEVATE", "GOTO ADMINTASKS", "", ":ELEVATE", "CD /d %~dp0", "MSHTA " + "\"javascript: var shell = new ActiveXObject('shell.application'); shell.ShellExecute('%~nx0', " +
                     "'', '', 'runas', 1); close();\"", "EXIT", "", ":ADMINTASKS", "copy " + copyVersion, "END"};

                    string docPathVersion =
                        (@"C:\Windows\Temp\\terraform\BAT");

                    // Create the BAT file from the lines string
                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPathVersion, "Copy.bat")))
                    {
                        foreach (string line in copyVersionLines)
                            outputFile.WriteLine(line);
                    }

                    // Start the BAT file
                    Process a = new Process();
                    a.StartInfo.CreateNoWindow = true;
                    a.StartInfo.UseShellExecute = false;
                    a.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    a.StartInfo.FileName = (@"C:\Windows\Temp\terraform\BAT\Copy.bat");
                    a.Start();
                    a.WaitForExit();
                    // 3 second pause to ensure the script has enough time to finish
                    Thread.Sleep(3000);
                }
                else
                {
                    // No Version History = No Update = Unable to check for update.
                    return;
                }
                    
            }
            // Get current version of terraform
            string currentVersion = System.IO.File.ReadAllText(@"C:\Windows\Temp\terraform\VersionControl\VersionInfo.txt");

            // Using HTMLAgilityPack to scrape the 'new' link from the terraform page.
            // The results are compared to see if they contain "windows_amd64.zip" to ensure the 64bit Windows URL is found.
            // Easiest way was to collect all the <li> and check against those for the required link.
            string urlToSplit = "";
            HtmlWeb hw = new HtmlWeb();

            // Loading from the inital terraform downloads page instead of the https://releases.hashicorp.com/terraform/ page, as from what I can see they post their newest STABLE release to the downloads page.
            HtmlDocument doc = hw.Load("https://www.terraform.io/downloads.html");
            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//li"))
            {
                // Find associated li that contains the link required for download and version type.
                if (link.InnerHtml.Contains("windows_amd64.zip"))
                {
                    urlToSplit = link.InnerHtml;
                }
            }


            // Split the string, as the version number is surrounded by two _ which makes an easy slice to extract the version number by itself.
            string[] URL = urlToSplit.Split('_');
            string newVersion = URL[1];

            // Split the string, to get the download link required which is between the "".
            string[] downloadURL = urlToSplit.Split('"');
            string finalDownloadURL = downloadURL[1];


            // Check if the new version matches the current version
            if (currentVersion.Trim() != newVersion.Trim())
            {
                string message = "Update Available, Update to Verion " + newVersion;
                string title = "Terraform Updater";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result = MessageBox.Show(message, title, buttons);
                if (result == DialogResult.Yes)
                {
                    // Create new directory to move and download files into
                    Directory.CreateDirectory(@"C:\Windows\Temp\terraform\VersionControl\" + newVersion);
                    //Download new ZIP file once as it has been confirmed that it is a newer version
                    //Keeping the terraform.exe in their respective .zip files to reduce storage space used.
                    WebClient webClient = new WebClient();
                    webClient.DownloadFile(finalDownloadURL, @"C:\Windows\Temp\terraform\VersionControl\" + newVersion + "\\" + newVersion + ".zip");

                    //Write new Version number in the VersionInfo.txt file
                    using (StreamWriter sr = new StreamWriter(@"C:\Windows\Temp\terraform\VersionControl\VersionInfo.txt", false))
                    {
                        sr.WriteLine(newVersion);
                    }

                    //Create new directory to place ZIP file in
                    string movePath = ("\"" + @"C:\Windows\Temp\terraform\VersionControl\" + newVersion + "\"" + " " + "\"" + @"C:\Program Files\terraform\VersionControl\" + newVersion + "\"");

                    string moveNewTerra = ("\"" + @"C:\Program Files\terraform\VersionControl\" + newVersion + "\\" + "terraform.exe" + "\"" + " " + "\"" + @"C:\Program Files\terraform" + "\"");

                    string returnVersion = ("\"" + @"C:\Windows\Temp\terraform\VersionControl\VersionInfo.txt" + "\"" + " " + "\"" + @"C:\Program Files\terraform\VersionControl" + "\"");

                    // Create the unZIP path as this needs powershell privilage to unZIP in program files, so it's easier to unZIP before moving the folder.
                    string newUnzipPath = ("\"" + @"C:\Windows\Temp\terraform\VersionControl\" + newVersion + "\\" + newVersion + ".zip" + "\"" + " " + "\"" + @"C:\Windows\Temp\terraform\VersionControl\" + newVersion + "\"");

                    // Create Temp BAT file to run creation CMD commands to make a new directory file for new version. Unzip file to folder, move the new folder to the directory, delete current terraform.exe, move new terraform.exe
                    string[] unzipPathLines = { "NET SESSION", "IF %ERRORLEVEL% NEQ 0 GOTO ELEVATE", "GOTO ADMINTASKS", "", ":ELEVATE", "CD /d %~dp0", "MSHTA " + "\"javascript: var shell = new ActiveXObject('shell.application'); shell.ShellExecute('%~nx0', '', '', 'runas', 1); close();\"",
                    "EXIT", "", ":ADMINTASKS", "PowerShell Expand-Archive -Path " + newUnzipPath, "move " + movePath, "del " +  "\"" + @"C:\Program Files\terraform\terraform.exe" + "\"",  "move " + moveNewTerra, "copy " + returnVersion, "END"};

                    string unzipDocPath =
                        (@"C:\Windows\Temp\terraform\BAT");

                    // Create the BAT file from the lines string
                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(unzipDocPath, "Update.bat")))
                    {
                        foreach (string line in unzipPathLines)
                            outputFile.WriteLine(line);
                    }
                    // Run the BAT file
                    Process b = new Process();
                    b.StartInfo.CreateNoWindow = true;
                    b.StartInfo.UseShellExecute = false;
                    b.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    b.StartInfo.FileName = (@"C:\Windows\Temp\terraform\BAT\Update.bat");
                    b.Start();
                    b.WaitForExit();
                    Thread.Sleep(3000);


                    string Cmessage = "Terraform has been updated to version " + newVersion;
                    string Ctitle = "Terraform Updater";
                    MessageBoxButtons Cbuttons = MessageBoxButtons.OK;
                    MessageBox.Show(Cmessage, Ctitle, Cbuttons);

                    
                }          
            }
            else
            {
                string NUmessage = "Terraform version " + newVersion + " is up to date";
                string NUtitle = "Terraform Updater";
                MessageBoxButtons NUbuttons = MessageBoxButtons.OK;
                MessageBox.Show(NUmessage, NUtitle, NUbuttons);
            }
        }
    }
}
