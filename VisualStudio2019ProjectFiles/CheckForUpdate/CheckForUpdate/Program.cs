using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            // Grab the current version type that's stored in a .txt file for the time being.
            string currentVersion = System.IO.File.ReadAllText(@"C:\Program Files\terraform\VersionControl\VersionInfo.txt");

            // Using HTMLAgilityPack to scrape the 'new' link from the terraform page.
            // The results are compared to see if they contain "windows_amd64.zip" to ensure the 64bit Windows URL is found.
            // Easiest way was to collect all the <li> and check against those for the required link.
            string urlToSplit = "";
            HtmlWeb hw = new HtmlWeb();

            // Loading from the inital terraform downloads page instead of the https://releases.hashicorp.com/terraform/ page, as from what I can see they post their newest STABLE release to the downloads page.
            HtmlDocument doc = hw.Load("https://www.terraform.io/downloads.html");
            foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//li"))
            {
                // Get Version Type
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
                    Directory.CreateDirectory(@"C:\ProgramData\terraform");
                    Directory.CreateDirectory(@"C:\ProgramData\terraform\VersionControl");
                    Directory.CreateDirectory(@"C:\ProgramData\terraform\VersionControl\" + newVersion);
                    //Download new ZIP file once as it has been confirmed that it is a newer version
                    //Keeping the terraform.exe in their respective .zip files to reduce storage space used.
                    WebClient webClient = new WebClient();
                    webClient.DownloadFile(finalDownloadURL, @"C:\ProgramData\terraform\VersionControl\" + newVersion + "\\" + newVersion + ".zip");

                    //Write new Version number in the VersionInfo.txt file
                    using (StreamWriter sr = new StreamWriter(@"C:\Program Files\terraform\VersionControl\VersionInfo.txt", false))
                    {
                        sr.WriteLine(newVersion);
                    }

                    //Create new directory to place ZIP file in
                    string movePath = ("\"" + @"C:\ProgramData\terraform\VersionControl\" + newVersion + "\"" + " " + "\"" + @"C:\Program Files\terraform\VersionControl\" + newVersion + "\"");

                    string moveNewTerra = ("\"" + @"C:\Program Files\terraform\VersionControl\" + newVersion + "\\" + "terraform.exe" + "\"" + " " + "\"" + @"C:\Program Files\terraform" + "\"");

                    // Create the unZIP path as this needs powershell privilage to unZIP in program files, so it's easier to unZIP before moving the folder.
                    string newUnzipPath = ("\"" + @"C:\ProgramData\terraform\VersionControl\" + newVersion + "\\" + newVersion + ".zip" + "\"" + " " + "\"" + @"C:\ProgramData\terraform\VersionControl\" + newVersion + "\"");

                    // Create Temp BAT file to run creation CMD commands to make a new directory file for new version. Unzip file to folder, move the new folder to the directory, delete current terraform.exe, move new terraform.exe
                    string[] lines = { "NET SESSION", "IF %ERRORLEVEL% NEQ 0 GOTO ELEVATE", "GOTO ADMINTASKS", "", ":ELEVATE", "CD /d %~dp0", "MSHTA " + "\"javascript: var shell = new ActiveXObject('shell.application'); shell.ShellExecute('%~nx0', '', '', 'runas', 1); close();\"",
                    "EXIT", "", ":ADMINTASKS", "PowerShell Expand-Archive -Path " + newUnzipPath, "move " + movePath, "del " +  "\"" + @"C:\Program Files\terraform\terraform.exe" + "\"",  "move " + moveNewTerra, "END"};

                    string docPath =
                        (@"C:\ProgramData\terraform");

                    // Create the BAT file from the lines string
                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, "Update.bat")))
                    {
                        foreach (string line in lines)
                            outputFile.WriteLine(line);
                    }
                    // Run the BAT file
                    Process b = new Process();
                    b.StartInfo.CreateNoWindow = true;
                    b.StartInfo.UseShellExecute = false;
                    b.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    b.StartInfo.FileName = (@"C:\ProgramData\terraform\Update.bat");
                    b.Start();
                    b.WaitForExit();

                    
                    string Cmessage = "Terraform has been update to version " + newVersion;
                    string Ctitle = "Terraform Updater";
                    MessageBoxButtons Cbuttons = MessageBoxButtons.OK;
                    DialogResult Cresult = MessageBox.Show(Cmessage, Ctitle, Cbuttons);
                    if (Cresult == DialogResult.OK);
                }          
            }
            else
            {
                string NUmessage = "Terraform " + newVersion + " is up to date";
                string NUtitle = "Terraform Updater";
                MessageBoxButtons NUbuttons = MessageBoxButtons.OK;
                DialogResult NUresult = MessageBox.Show(NUmessage, NUtitle, NUbuttons);
                if (NUresult == DialogResult.OK);
            }
        }
    }
}
