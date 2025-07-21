using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.ServiceProcess;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.ServiceProcess;


namespace FixTool
{
    public class Form1 : Form
    {
        private Label lblTrimStatus;
        private Label superfetchStatusLabel;


        public Form1()
        {
            this.Text = "Windows Fix Tool";
            this.Width = 500;
            this.Height = 900;
            this.StartPosition = FormStartPosition.CenterScreen;

            int top = 20;

            // Pulsanti esistenti
            this.Controls.Add(CreateButton("SFC /scannow", top, RunSFC)); top += 45;
            this.Controls.Add(CreateButton("DISM - CheckHealth", top, RunDISM_CheckHealth)); top += 45;
            this.Controls.Add(CreateButton("DISM - ScanHealth", top, RunDISM_ScanHealth)); top += 45;
            this.Controls.Add(CreateButton("DISM - RestoreHealth", top, RunDISM_RestoreHealth)); top += 45;
            this.Controls.Add(CreateButton("DISM - Cleanup componenti", top, RunDISM_StartCleanup)); top += 45;
            this.Controls.Add(CreateButton("DISM - Analizza component store", top, RunDISM_AnalyzeStore)); top += 45;
            this.Controls.Add(CreateButton("Pulizia disco", top, RunCleanmgr)); top += 45;
            this.Controls.Add(CreateButton("Fix cache miniature", top, FixThumbnails)); top += 45;
            this.Controls.Add(CreateButton("Fix Start Menu", top, FixStartMenu)); top += 45;
            this.Controls.Add(CreateButton("Fix Windows Update", top, FixWindowsUpdate)); top += 45;
            this.Controls.Add(CreateButton("Fix Microsoft Store", top, FixMicrosoftStore)); top += 45;
            this.Controls.Add(CreateButton("Fix Ricerca di Windows", top, FixWindowsSearch)); top += 45;
            this.Controls.Add(CreateButton("Fix Barra strumenti (taskbar)", top, FixTaskbar)); top += 45;
            this.Controls.Add(CreateButton("Aggiorna Stato Superfetch", top, (s,e) => AggiornaStatoSuperfetch()));
top += 45;


            // Pulsanti TRIM con label stato
            var btnEnableTrim = CreateButton("Attiva TRIM (SSD)", top, EnableTrim);
            btnEnableTrim.Left = 20;
            btnEnableTrim.Width = 160;
            this.Controls.Add(btnEnableTrim);

            var btnDisableTrim = CreateButton("Disattiva TRIM (SSD)", top + 45, DisableTrim);
            btnDisableTrim.Left = 20;
            btnDisableTrim.Width = 160;
            this.Controls.Add(btnDisableTrim);

            lblTrimStatus = new Label();
            lblTrimStatus.Top = top + 15;
            lblTrimStatus.Left = 190;
            lblTrimStatus.Width = 220;
            lblTrimStatus.Height = 30;
            lblTrimStatus.Text = "Stato TRIM: ...";
            lblTrimStatus.Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold);
            this.Controls.Add(lblTrimStatus);

            top += 100;

// Pulsanti Superfetch/Prefetch
var btnDisableSuperfetch = CreateButton("Disattiva Superfetch e Prefetch (SSD)", top, async (s, e) => await CambiaStatoSuperfetchAsync(false));
btnDisableSuperfetch.Left = 20;
btnDisableSuperfetch.Width = 280;
this.Controls.Add(btnDisableSuperfetch);

var btnEnableSuperfetch = CreateButton("Attiva Superfetch e Prefetch (HDD)", top + 45, async (s, e) => await CambiaStatoSuperfetchAsync(true));
btnEnableSuperfetch.Left = 20;
btnEnableSuperfetch.Width = 280;
this.Controls.Add(btnEnableSuperfetch);

// Posiziona la label **a destra** del primo pulsante (disattiva)
superfetchStatusLabel = new Label();
superfetchStatusLabel.Top = top + 10;  // leggermente centrata rispetto all'altezza del pulsante
superfetchStatusLabel.Left = btnDisableSuperfetch.Right + 10; // 10 pixel di margine a destra del pulsante
superfetchStatusLabel.Width = 200;  // dimensione sufficiente
superfetchStatusLabel.Height = 30;
superfetchStatusLabel.Text = "Stato Superfetch: ...";
superfetchStatusLabel.Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold);
this.Controls.Add(superfetchStatusLabel);
superfetchStatusLabel.BringToFront();


            top += 100;

            // Altri 3 pulsanti nuovi sotto Superfetch/TRIM
            this.Controls.Add(CreateButton("Disattiva Ibernazione", top, DisableHibernation)); top += 45;
            this.Controls.Add(CreateButton("Abilita menu destro classico (Win11)", top, EnableClassicContextMenu)); top += 45;

            // Aggiorna stati all'avvio
            UpdateTrimStatus();
            AggiornaStatoSuperfetch();
        }

        private Button CreateButton(string text, int top, EventHandler onClick)
        {
            var button = new Button();
            button.Text = text;
            button.Width = 420;
            button.Height = 40;
            button.Top = top;
            button.Left = 20;
            button.Click += onClick;
            return button;
        }

        // Metodo per eseguire comandi con elevazione (senza output leggibile)
        private void RunAsAdmin(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore: " + ex.Message);
            }
        }

        // Metodo per eseguire comandi senza elevazione ma con output leggibile
        private void RunWithoutAdmin(string fileName, string arguments, Action<string> onOutput)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                var process = Process.Start(psi);
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    onOutput(output);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore: " + ex.Message);
            }
        }

        // Stato TRIM
        private void UpdateTrimStatus()
        {
            RunWithoutAdmin("cmd.exe", "/c fsutil behavior query DisableDeleteNotify", output =>
            {
                var match = Regex.Match(output, @"DisableDeleteNotify\s*=\s*(\d)");
                if (match.Success)
                {
                    var value = match.Groups[1].Value;
                    if (value == "0")
                        lblTrimStatus.Text = "Stato TRIM: Attivo";
                    else
                        lblTrimStatus.Text = "Stato TRIM: Disattivo";
                }
                else
                {
                    lblTrimStatus.Text = "Stato TRIM: Impossibile leggere";
                }
            });
        }
        // TRIM
        private async void EnableTrim(object sender, EventArgs e)
        {
            RunAsAdmin("cmd.exe", "/c fsutil behavior set DisableDeleteNotify 0");
            MessageBox.Show("TRIM abilitato.");
            await Task.Delay(2000);
            UpdateTrimStatus();
        }

        private async void DisableTrim(object sender, EventArgs e)
        {
            RunAsAdmin("cmd.exe", "/c fsutil behavior set DisableDeleteNotify 1");
            MessageBox.Show("TRIM disabilitato.");
            await Task.Delay(2000);
            UpdateTrimStatus();
        }

        // Superfetch e Prefetch
        private async void DisableSuperfetchPrefetch(object sender, EventArgs e)
        {
            string psCommand = @"
                sc stop ""SysMain"" -ErrorAction SilentlyContinue; 
                sc config ""SysMain"" start=disabled; 
                reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters"" /v EnablePrefetcher /t REG_DWORD /d 0 /f";
            RunAsAdmin("powershell.exe", $"-Command \"{psCommand}\"");
            MessageBox.Show("Superfetch e Prefetch disattivati.");
            await Task.Delay(2000);
            AggiornaStatoSuperfetch();
        }

        private async void EnableSuperfetchPrefetch(object sender, EventArgs e)
        {
            string psCommand = @"
                sc config ""SysMain"" start=auto; 
                sc start ""SysMain""; 
                reg add ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters"" /v EnablePrefetcher /t REG_DWORD /d 3 /f";
            RunAsAdmin("powershell.exe", $"-Command \"{psCommand}\"");
            MessageBox.Show("Superfetch e Prefetch attivati.");
            await Task.Delay(2000);
            AggiornaStatoSuperfetch();
        }

        private string GetServiceStatus(string serviceName)
        {
            try
            {
                using (var sc = new System.ServiceProcess.ServiceController(serviceName))
                {
                    return sc.Status.ToString();
                }
            }
            catch
            {
                return "Non trovato";
            }
        }

        private int ReadRegistryPrefetcher()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("EnablePrefetcher");
                        if (val != null)
                            return Convert.ToInt32(val);
                    }
                }
            }
            catch
            {
                // Ignora errori
            }
            return -1;
        }

        // Altri comandi già esistenti

        private void RunSFC(object sender, EventArgs e) =>
            RunAsAdmin("cmd.exe", "/c sfc /scannow");

        private void RunDISM_CheckHealth(object sender, EventArgs e) =>
            RunAsAdmin("cmd.exe", "/c DISM /Online /Cleanup-Image /CheckHealth");

        private void RunDISM_ScanHealth(object sender, EventArgs e) =>
            RunAsAdmin("cmd.exe", "/c DISM /Online /Cleanup-Image /ScanHealth");

        private void RunDISM_RestoreHealth(object sender, EventArgs e) =>
            RunAsAdmin("cmd.exe", "/c DISM /Online /Cleanup-Image /RestoreHealth");

        private void RunDISM_StartCleanup(object sender, EventArgs e) =>
            RunAsAdmin("cmd.exe", "/c DISM /Online /Cleanup-Image /StartComponentCleanup");

        private void RunDISM_AnalyzeStore(object sender, EventArgs e) =>
            RunAsAdmin("cmd.exe", "/c DISM /Online /Cleanup-Image /AnalyzeComponentStore");

        private void RunCleanmgr(object sender, EventArgs e) =>
            RunAsAdmin("cleanmgr.exe", "");

        private void FixThumbnails(object sender, EventArgs e)
        {
            string command = "taskkill /f /im explorer.exe && " +
                             "del /f /s /q %localappdata%\\Microsoft\\Windows\\Explorer\\thumbcache_*.db && " +
                             "start explorer.exe";

            RunAsAdmin("cmd.exe", "/c " + command);
        }

        private void FixStartMenu(object sender, EventArgs e)
        {
            string command = "Get-AppxPackage | Foreach {Add-AppxPackage -DisableDevelopmentMode -Register ($_.InstallLocation + '\\AppXManifest.xml')}";
            RunAsAdmin("powershell.exe", $"-Command \"{command}\"");
        }

        private void FixWindowsUpdate(object sender, EventArgs e)
        {
            string cmd = "net stop wuauserv && net stop cryptSvc && net stop bits && net stop msiserver && " +
                         "ren C:\\Windows\\SoftwareDistribution SoftwareDistribution.old && " +
                         "ren C:\\Windows\\System32\\catroot2 catroot2.old && " +
                         "net start wuauserv && net start cryptSvc && net start bits && net start msiserver";
            RunAsAdmin("cmd.exe", "/c " + cmd);
        }

        private void FixMicrosoftStore(object sender, EventArgs e)
        {
            string cmd = "Get-AppxPackage *WindowsStore* | Foreach {Add-AppxPackage -DisableDevelopmentMode -Register ($_.InstallLocation + '\\AppXManifest.xml')}";
            RunAsAdmin("powershell.exe", $"-Command \"{cmd}\"");
        }

        private void FixWindowsSearch(object sender, EventArgs e)
        {
            RunAsAdmin("powershell.exe", "-Command \"Get-AppxPackage Microsoft.Windows.Search | Reset-AppxPackage\"");
        }

        private void FixTaskbar(object sender, EventArgs e)
        {
            string command = "taskkill /f /im explorer.exe && timeout /t 1 && start explorer.exe";
            RunAsAdmin("cmd.exe", "/c " + command);
        }

        private void DisableHibernation(object sender, EventArgs e)
        {
            RunAsAdmin("cmd.exe", "/c powercfg -h off");
        }

        private void EnableClassicContextMenu(object sender, EventArgs e)
        {
            string regCommands = @"
                reg add ""HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}"" /f;
                reg add ""HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"" /f /ve";
            RunAsAdmin("cmd.exe", "/c " + regCommands);
            MessageBox.Show("Modifica applicata. Riavvia il PC per vedere il menu classico.");
        }
        private async Task CambiaStatoSuperfetchAsync(bool attiva)
{
    string servizio = "SysMain";
    try
    {
        using (ServiceController sc = new ServiceController(servizio))
        {
            if (attiva)
            {
                if (sc.Status != ServiceControllerStatus.Running)
                {
                    sc.Start();
                    await Task.Delay(1000);
                    sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(5));
                }

                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 3, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnableSuperfetch", 3, RegistryValueKind.DWord);
                MessageBox.Show("Superfetch e Prefetch attivati.");
            }
            else
            {
                if (sc.Status != ServiceControllerStatus.Stopped)
                {
                    sc.Stop();
                    await Task.Delay(1000);
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
                }

                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnablePrefetcher", 0, RegistryValueKind.DWord);
                Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters", "EnableSuperfetch", 0, RegistryValueKind.DWord);
                MessageBox.Show("Superfetch e Prefetch disattivati.");
            }
        }
    }
    catch (Exception ex)
    {
        MessageBox.Show("Errore durante la modifica di Superfetch: " + ex.Message);
    }

    AggiornaStatoSuperfetch();
}

private void AggiornaStatoSuperfetch()
{
    try
    {
        using (ServiceController sc = new ServiceController("SysMain"))
        {
            string stato = sc.Status == ServiceControllerStatus.Running ? "Attivo" : "Disattivato";
            superfetchStatusLabel.Text = $"Stato Superfetch: {stato}";
        }
    }
    catch (Exception ex)
    {
        superfetchStatusLabel.Text = "Stato Superfetch: Non rilevato";
    }
}


    }
}


