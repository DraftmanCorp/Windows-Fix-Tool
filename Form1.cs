using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.ServiceProcess;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Reflection;
using System.Drawing;

namespace FixTool
{
    public partial class Form1 : Form
    {
        private Button btnToggleSuperfetch;  // <-- dichiarazione campo Button
        private bool inizializzazioneInCorso = true;
        private bool trimAttivo = false;
        private bool superfetchAttivo = false; // stato Superfetch
        ComboBox comboBoxDrives;


        public Form1()
        {
            InitializeComponent();

            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;

            // Carica la GIF da risorsa incorporata
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream("FixTool.gif.gif"); // verifica nome e percorso

            if (stream != null)
            {
                var img = Image.FromStream(stream);
                pictureBox.Image = img;
                ImageAnimator.Animate(img, (s, e) => pictureBox.Invalidate());
            }
            else
            {
                MessageBox.Show("Errore: risorsa GIF non trovata.");
            }

            //const int buttonWidth = 320;

            int leftColX = 240;
            int rightColX = 580;
            int topLeft = 20;
            int topRight = 20;

            lblManutenzione.Left = leftColX; lblManutenzione.Top = topLeft; topLeft += 40;
            lblFix.Left = rightColX; lblFix.Top = topRight; topRight += 40;
            lblManutenzione.ForeColor = Color.FromArgb(0, 153, 255);
            lblFix.ForeColor = Color.FromArgb(0, 153, 255);

            // Colonna sinistra - pulsanti 320x40 + pulsante info accanto (40x40)
            this.Controls.AddRange(CreateButtonWithHelp(
                "DISM 1 - Analisi Stato Sistema",
                topLeft,
                async (s, e) => await RunAsAdminAsync("cmd.exe", "/k DISM /Online /Cleanup-Image /CheckHealth"),
                leftColX,
                "Specificamente, /CheckHealth esegue un controllo rapido dell'immagine di Windows per rilevare se ci sono segni di corruzione. Non tenta di risolvere i problemi, ma ti dice semplicemente se l'immagine di Windows è integra, ripristinabile o non ripristinabile, fornendo una diagnosi preliminare sullo stato dei componenti di sistema."
            ));
            topLeft += 45;

            this.Controls.AddRange(CreateButtonWithHelp(
                "DISM 2 - Analisi Dettagliata Stato Sistema",
                topLeft,
                async (s, e) => await RunAsAdminAsync("cmd.exe", "/c DISM /Online /Cleanup-Image /ScanHealth"),
                leftColX,
                "Simile a CheckHealth, questo comando esegue una scansione più approfondita e completa per verificare l'integrità dell'immagine di Windows. Ci mette più tempo rispetto a CheckHealth perché analizza in modo più dettagliato i componenti del sistema operativo per rilevare eventuali corruzioni o problemi latenti. Alla fine, ti fornisce un rapporto sullo stato di salute complessivo dell'immagine di sistema, indicando se sono necessari interventi di riparazione."
            ));
            topLeft += 45;

            this.Controls.AddRange(CreateButtonWithHelp(
                "DISM 3 - Ripara Immagine di Sistema",
                topLeft,
                async (s, e) => await RunAsAdminAsync("cmd.exe", "/c DISM /Online /Cleanup-Image /RestoreHealth"),
                leftColX,
                "Questo comando è il più potente del gruppo DISM per la riparazione. Quando lo esegui, tenta di riparare automaticamente qualsiasi corruzione rilevata nell'immagine di Windows. Scarica e sostituisce i file danneggiati o mancanti utilizzando componenti validi da fonti online (come Windows Update) o da una sorgente locale, ripristinando così l'integrità e la funzionalità del sistema operativo."
            ));
            topLeft += 45;

            this.Controls.AddRange(CreateButtonWithHelp(
                "Controllo/Ripara File di Sistema",
                topLeft,
                async (s, e) => await RunAsAdminAsync("cmd.exe", "/c sfc /scannow"),
                leftColX,
                "Questo comando è un utility di Windows che controlla e ripara i file di sistema danneggiati o corrotti. Quando lo esegui, scansiona tutti i file importanti del sistema operativo e, se trova delle anomalie, tenta di ripristinarli alle loro versioni originali e corrette. È utile per risolvere problemi di stabilità o malfunzionamenti di Windows causati da file di sistema compromessi. Comando = sfc /scannow."
            ));
            topLeft += 45;

            this.Controls.AddRange(CreateButtonWithHelp(
                "Pulizia Componenti Obsoleti",
                topLeft,
                async (s, e) => await RunAsAdminAsync("cmd.exe", "/c DISM /Online /Cleanup-Image /StartComponentCleanup"),
                leftColX,
                "Questo comando serve a liberare spazio su disco eliminando versioni obsolete di componenti di sistema e file di aggiornamento di Windows. Durante gli aggiornamenti, Windows conserva vecchie versioni dei file per consentire il rollback. StartComponentCleanup rimuove questi file non più necessari, contribuendo a ottimizzare lo spazio occupato dal sistema operativo sul tuo disco rigido."
            ));
            topLeft += 65;

            //GESTIONE DISCHI
            int colWidth = 320;
            lblGestioneDischi.Left = leftColX + (colWidth - lblGestioneDischi.Width) / 2;
            lblGestioneDischi.Top = topLeft;
            this.Controls.Add(lblGestioneDischi);
            topLeft += lblGestioneDischi.Height + 10;

            // COMBO BOX
            comboBoxDrives = new ComboBox()
            {
                Left = leftColX,
                Top = topLeft,
                Width = 320,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };

            comboBoxDrives.FlatStyle = FlatStyle.Flat;
            comboBoxDrives.BackColor = Color.FromArgb(30, 30, 30);
            comboBoxDrives.ForeColor = Color.White;
            this.Controls.Add(comboBoxDrives);
            topLeft += comboBoxDrives.Height + 10;

            // Pulsante CHKDSK solo scan
            var btnChkdskScan = CreateButtonWithHelp(
                "Check Disk - Solo scan", topLeft, OnScanOnlyClick, leftColX,
                "Esegue un controllo del disco col comando CHKDSK senza modifiche o riparazioni."
            );
            this.Controls.AddRange(btnChkdskScan);
            topLeft += btnChkdskScan[0].Height + 5;

            // Pulsante CHKDSK scan + repair
            var btnChkdskRepair = CreateButtonWithHelp(
                "Check Disk + Riparazione", topLeft, OnScanRepairClick, leftColX,
                "Esegue un controllo del disco con CHKDSK e i parametri /f /r /x per tentare di riparare settori danneggiati"
            );
            this.Controls.AddRange(btnChkdskRepair);
            topLeft += btnChkdskRepair[0].Height + 5;
            PopulateDrivesComboBox();

            //PULIZIA DISCO CLEANMGR
            this.Controls.AddRange(CreateButtonWithHelp(
                "Pulizia Spazio su Disco",
                topLeft,
                async (s, e) => await RunAsAdminAsync("cleanmgr.exe", ""),
                leftColX,
                "Questo è il comando che avvia l'Utilità Pulizia Disco di Windows, un programma grafico facile da usare. Ti permette di eliminare file temporanei, file di log, elementi nel Cestino e altri dati non necessari che si accumulano sul tuo disco rigido. È un modo semplice e sicuro per liberare spazio di archiviazione e mantenere il sistema più ordinato."
            ));
            topLeft += 45;

            // FIX E CORREZIONI VARIE
            this.Controls.AddRange(CreateButtonWithHelp(
                "Fix Miniature/Anteprima icone",
                topRight,
                async (s, e) =>
                {
                    string command = "taskkill /f /im explorer.exe && del /f /s /q %localappdata%\\Microsoft\\Windows\\Explorer\\thumbcache_*.db && start explorer.exe";
                    await RunAsAdminAsync("cmd.exe", "/c " + command);
                },
                rightColX,
                "Questo comando serve a eliminare i file della cache delle miniature (thumbnail cache) dal tuo computer. Questi file (thumbcache_*.db) vengono creati da Windows per visualizzare rapidamente le anteprime delle immagini e dei video. Eliminandoli, costringi il sistema a ricrearle, il che può essere utile per risolvere problemi di visualizzazione delle miniature corrotte o per liberare una piccola quantità di spazio su disco."
            )); topRight += 45;

            //FIX MENU START
            this.Controls.AddRange(CreateButtonWithHelp(
                "Fix Menu Start",
                topRight,
                async (s, e) =>
                {
                    string command = "Get-AppxPackage | Foreach {Add-AppxPackage -DisableDevelopmentMode -Register ($_.InstallLocation + '\\AppXManifest.xml')}";
                    await RunAsAdminAsync("powershell.exe", $"-Command \"{command}\"");
                },
                rightColX,
                "Reinstalla i pacchetti dell'interfaccia per correggere eventuali problemi con il Menu Start."
            )); topRight += 45;

            //FIX WINDOWS UPDARE
            this.Controls.AddRange(CreateButtonWithHelp(
            "Fix Windows Update",
            topRight,
            async (s, e) =>
            {
                try
                {
                    MessageBox.Show("Avvio del ripristino di Windows Update...", "Fix Windows Update");

                    // Disattivazione servizi
                    await RunAsAdminAsync("cmd.exe", "/c net stop wuauserv");
                    await RunAsAdminAsync("cmd.exe", "/c net stop cryptSvc");
                    await RunAsAdminAsync("cmd.exe", "/c net stop bits");
                    await RunAsAdminAsync("cmd.exe", "/c net stop msiserver");

                    // Rinomina cartelle
                    await RunAsAdminAsync("cmd.exe", "/c ren C:\\Windows\\SoftwareDistribution SoftwareDistribution.old");
                    await RunAsAdminAsync("cmd.exe", "/c ren C:\\Windows\\System32\\catroot2 catroot2.old");

                    // Riavvio servizi
                    await RunAsAdminAsync("cmd.exe", "/c net start wuauserv");
                    await RunAsAdminAsync("cmd.exe", "/c net start cryptSvc");
                    await RunAsAdminAsync("cmd.exe", "/c net start bits");
                    await RunAsAdminAsync("cmd.exe", "/c net start msiserver");

                    MessageBox.Show("Windows Update è stato ripristinato correttamente.", "Fix completato");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Errore durante il fix di Windows Update: " + ex.Message, "Errore");
                }
            },
            rightColX,
            "Ripristina i componenti di Windows Update in caso di errori o blocchi nel download degli aggiornamenti."
            )); topRight += 45;

            //FIX MICROSOFT STORE
            this.Controls.AddRange(CreateButtonWithHelp(
                "Fix Microsoft Store",
                topRight,
                async (s, e) =>
                {
                    string script = @"
            Write-Output 'Ripristino di Microsoft Store in corso...'
            Get-AppxPackage *WindowsStore* | Foreach {
                try {
                    Add-AppxPackage -DisableDevelopmentMode -Register ($_.InstallLocation + '\AppXManifest.xml')
                    Write-Output 'Microsoft Store reinstallato correttamente.'
                } catch {
                    Write-Error ""Errore durante la reinstallazione: $_""
                }
            }
            pause";

                    string tempPath = Path.Combine(Path.GetTempPath(), "FixStore.ps1");
                    File.WriteAllText(tempPath, script);

                    await RunAsAdminAsync("powershell.exe", $"-NoExit -ExecutionPolicy Bypass -File \"{tempPath}\"");
                },
                rightColX,
                "Reinstalla Microsoft Store per risolvere problemi di apertura, aggiornamento o funzionamento."
            )); topRight += 45;

            //FIX RICERCA
            this.Controls.AddRange(CreateButtonWithHelp(
                "Fix Ricerca di Windows",
                topRight,
                async (s, e) =>
                {
                    await RunAsAdminAsync("powershell.exe", "-Command \"Get-AppxPackage Microsoft.Windows.Search | Reset-AppxPackage\"");
                },
                rightColX,
                "Ripristina il componente di ricerca per risolvere problemi con la barra di ricerca di Windows."
            )); topRight += 45;

            //FIX BARRA DEGLI STRUMENTI
            this.Controls.AddRange(CreateButtonWithHelp(
                "Fix Barra Strumenti",
                topRight,
                async (s, e) =>
                {
                    string command = "taskkill /f /im explorer.exe && timeout /t 1 && start explorer.exe";
                    await RunAsAdminAsync("cmd.exe", "/c " + command);
                },
                rightColX,
                "Riavvia Esplora risorse per risolvere problemi di visualizzazione o blocchi della barra delle applicazioni."
            )); topRight += 65;

            //UTILITA
            Button btnIbernazioneOn = new Button();
            lblUtilita.Left = rightColX + (colWidth - lblUtilita.Width) / 2;
            lblUtilita.Top = topRight;
            this.Controls.Add(lblUtilita);
            topRight += lblUtilita.Height + 10;

            //IBERNAZIONE
            this.Controls.AddRange(CreateToggleButtonWithHelp(
                "Ibernazione",
                topRight,
                IsHibernationEnabled,
                async (enable) =>
                {
                    string arg = enable ? "/c powercfg -h on" : "/c powercfg -h off";
                    await RunAsAdminAsync("cmd.exe", arg);
                },
                rightColX,
                "Disattiva o attiva la modalità ibernazione, utile per liberare spazio su disco ed evitare problemi con il resume."
            ));
            topRight += 45; // 40 per il bottone + un po' di margine

            //MENU CLASSICO
            InitializeMenuClassicoButton(rightColX, ref topRight);
            VerificaStatoMenuClassico(rightColX, topRight);

            //TOGGLE TRIM SSD
            var btnToggleTrim = new Button()
            {
                Text = "TRIM (SSD): OFF",
                Width = 280,
                Height = 40,
                Left = leftColX,
                Top = topLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
            };
            btnToggleTrim.FlatAppearance.BorderColor = Color.FromArgb(30, 30, 30);
            btnToggleTrim.FlatAppearance.BorderSize = 1;
            btnToggleTrim.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 25, 25);
            btnToggleTrim.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 15, 15);
            btnToggleTrim.Region = new Region(CreateRoundedRectanglePath(new Rectangle(0, 0, btnToggleTrim.Width, btnToggleTrim.Height), 10));
            btnToggleTrim.MouseEnter += (s, e) => { btnToggleTrim.ForeColor = Color.FromArgb(0, 153, 255); };
            btnToggleTrim.MouseLeave += (s, e) => { btnToggleTrim.ForeColor = Color.White; };
            btnToggleTrim.Click += async (s, e) =>
            {
                trimAttivo = !trimAttivo;
                if (trimAttivo)
                {
                    btnToggleTrim.BackColor = Color.FromArgb(0, 153, 255);
                    btnToggleTrim.Text = "TRIM (SSD): ON";
                    btnToggleTrim.ForeColor = Color.White;
                    await EnableTrimAsync();
                }
                else
                {
                    btnToggleTrim.BackColor = Color.FromArgb(30, 30, 30);
                    btnToggleTrim.Text = "TRIM (SSD): OFF";
                    btnToggleTrim.ForeColor = Color.White;
                    await DisableTrimAsync();
                }
            };
            this.Controls.Add(btnToggleTrim);
            var helpButtonTrim = new Button()
            {
                Text = "?",
                Width = 40,
                Height = 40,
                Left = btnToggleTrim.Left + btnToggleTrim.Width + 5,  // 5 px di spazio tra i bottoni
                Top = btnToggleTrim.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            helpButtonTrim.FlatAppearance.BorderSize = 0;
            helpButtonTrim.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 80);
            helpButtonTrim.Region = new Region(CreateRoundedRectanglePath(new Rectangle(0, 0, helpButtonTrim.Width, helpButtonTrim.Height), 10));
            helpButtonTrim.Click += (s, e) => ShowHelpPopup("Abilita o disabilita la funzione TRIM per SSD.");
            this.Controls.Add(helpButtonTrim);
            topLeft += 45;

            //TOGGLE SUPERFETCH 
            btnToggleSuperfetch = new Button()
            {
                Text = "Superfetch e Prefetch (HDD): OFF",
                Width = 280,
                Height = 40,
                Left = leftColX,
                Top = topLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
            };
            btnToggleSuperfetch.FlatAppearance.BorderColor = Color.FromArgb(30, 30, 30);
            btnToggleSuperfetch.FlatAppearance.BorderSize = 1;
            btnToggleSuperfetch.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 25, 25);
            btnToggleSuperfetch.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 15, 15);
            btnToggleSuperfetch.Region = new Region(CreateRoundedRectanglePath(new Rectangle(0, 0, btnToggleSuperfetch.Width, btnToggleSuperfetch.Height), 10));
            btnToggleSuperfetch.MouseEnter += (s, e) => { btnToggleSuperfetch.ForeColor = Color.FromArgb(0, 153, 255); };
            btnToggleSuperfetch.MouseLeave += (s, e) => { btnToggleSuperfetch.ForeColor = Color.White; };

            btnToggleSuperfetch.Click += async (s, e) =>
            {
                superfetchAttivo = !superfetchAttivo;

                if (superfetchAttivo)
                {
                    btnToggleSuperfetch.BackColor = Color.FromArgb(0, 153, 255);
                    btnToggleSuperfetch.Text = "Superfetch e Prefetch (HDD): ON";
                    btnToggleSuperfetch.ForeColor = Color.White;
                }
                else
                {
                    btnToggleSuperfetch.BackColor = Color.FromArgb(30, 30, 30);
                    btnToggleSuperfetch.Text = "Superfetch e Prefetch (HDD): OFF";
                    btnToggleSuperfetch.ForeColor = Color.White;
                }

                if (!inizializzazioneInCorso)
                {
                    await CambiaStatoSuperfetchAsync(superfetchAttivo);
                }
            };

            this.Controls.Add(btnToggleSuperfetch);
            var helpButtonSuperfetch = new Button()
            {
                Text = "?",
                Width = 40,
                Height = 40,
                Left = btnToggleSuperfetch.Left + btnToggleSuperfetch.Width + 5,
                Top = btnToggleSuperfetch.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            helpButtonSuperfetch.FlatAppearance.BorderSize = 0;
            helpButtonSuperfetch.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 80);
            helpButtonSuperfetch.Region = new Region(CreateRoundedRectanglePath(new Rectangle(0, 0, helpButtonSuperfetch.Width, helpButtonSuperfetch.Height), 10));
            helpButtonSuperfetch.Click += (s, e) => ShowHelpPopup("Attiva o disattiva il servizio Superfetch (SysMain).");

            this.Controls.Add(helpButtonSuperfetch);

            topLeft += 45;

            // Aggiorna stato TRIM e Superfetch all'avvio
            UpdateTrimStatus();

            try
            {
                using (ServiceController sc = new ServiceController("SysMain"))
                {
                    superfetchAttivo = (sc.Status == ServiceControllerStatus.Running);
                    // Aggiorna bottone con stato reale
                    btnToggleSuperfetch.Text = $"Superfetch e Prefetch (HDD): {(superfetchAttivo ? "ON" : "OFF")}";
                    btnToggleSuperfetch.BackColor = superfetchAttivo ? Color.FromArgb(0, 153, 255) : Color.FromArgb(30, 30, 30);
                    btnToggleSuperfetch.ForeColor = Color.White;
                }
            }
            catch
            {
                superfetchAttivo = false;
                btnToggleSuperfetch.Text = "Superfetch e Prefetch (HDD): Stato non rilevato";
            }

            inizializzazioneInCorso = false;

            // Imposta stato iniziale toggle trim da cmd
            RunWithoutAdmin("cmd.exe", "/c fsutil behavior query DisableDeleteNotify", output =>
            {
                var match = Regex.Match(output, @"DisableDeleteNotify\s*=\s*(\d)");
                if (match.Success)
                {
                    trimAttivo = (match.Groups[1].Value == "0");
                    if (trimAttivo)
                    {
                        btnToggleTrim.BackColor = Color.FromArgb(0, 153, 255);
                        btnToggleTrim.Text = "TRIM (SSD): ON";
                        btnToggleTrim.ForeColor = Color.White;
                    }
                    else
                    {
                        btnToggleTrim.BackColor = Color.FromArgb(30, 30, 30);
                        btnToggleTrim.Text = "TRIM (SSD): OFF";
                        btnToggleTrim.ForeColor = Color.White;
                    }
                }
            });
            topLeft += 5;

        }
        //MENU CLASSIC IN WIN 11
        private bool menuClassicoAttivo = false;
        private Button? btnToggleMenuClassico;

        private void InitializeMenuClassicoButton(int left, ref int top)
        {
            btnToggleMenuClassico = new Button()
            {
                Text = "Menu destro classico: OFF",
                Width = 280,
                Height = 40,
                Left = left,
                Top = top,
                TextAlign = ContentAlignment.MiddleCenter,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
            };
            btnToggleMenuClassico.FlatAppearance.BorderColor = Color.FromArgb(30, 30, 30);
            btnToggleMenuClassico.FlatAppearance.BorderSize = 1;
            btnToggleMenuClassico.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 25, 25);
            btnToggleMenuClassico.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 15, 15);
            btnToggleMenuClassico.Region = new Region(CreateRoundedRectanglePath(new Rectangle(0, 0, btnToggleMenuClassico.Width, btnToggleMenuClassico.Height), 10));
            btnToggleMenuClassico.MouseEnter += (s, e) => { btnToggleMenuClassico.ForeColor = Color.FromArgb(0, 153, 255); };
            btnToggleMenuClassico.MouseLeave += (s, e) => { btnToggleMenuClassico.ForeColor = Color.White; };

            btnToggleMenuClassico.Click += async (s, e) =>
            {
                menuClassicoAttivo = !menuClassicoAttivo;

                if (menuClassicoAttivo)
                {
                    await AbilitaMenuClassicoAsync();
                    btnToggleMenuClassico.BackColor = Color.FromArgb(0, 153, 255);
                    btnToggleMenuClassico.Text = "Menu destro classico: ON";
                }
                else
                {
                    await DisabilitaMenuClassicoAsync();
                    btnToggleMenuClassico.BackColor = Color.FromArgb(30, 30, 30);
                    btnToggleMenuClassico.Text = "Menu destro classico: OFF";
                }
                btnToggleMenuClassico.ForeColor = Color.White;
                MessageBox.Show("Riavvia il PC per applicare le modifiche.");
            };

            this.Controls.Add(btnToggleMenuClassico);

            var helpButtonMenuClassico = new Button()
            {
                Text = "?",
                Width = 40,
                Height = 40,
                Left = btnToggleMenuClassico.Left + btnToggleMenuClassico.Width + 5,
                Top = btnToggleMenuClassico.Top,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            helpButtonMenuClassico.FlatAppearance.BorderSize = 0;
            helpButtonMenuClassico.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 80);
            helpButtonMenuClassico.Region = new Region(CreateRoundedRectanglePath(new Rectangle(0, 0, helpButtonMenuClassico.Width, helpButtonMenuClassico.Height), 10));
            helpButtonMenuClassico.Click += (s, e) => ShowHelpPopup("Abilita o disabilita il menu contestuale classico di Windows 11.\nRichiede il riavvio del sistema per applicare le modifiche.");

            this.Controls.Add(helpButtonMenuClassico);

            top += 45;
        }

        private async Task AbilitaMenuClassicoAsync()
        {
            string cmd1 = @"reg add ""HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}"" /f";
            string cmd2 = @"reg add ""HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32"" /f /ve";

            await RunAsAdminAsync("cmd.exe", "/c " + cmd1);
            await RunAsAdminAsync("cmd.exe", "/c " + cmd2);
        }

        private async Task DisabilitaMenuClassicoAsync()
        {
            string cmd = @"reg delete ""HKCU\Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}"" /f";
            await RunAsAdminAsync("cmd.exe", "/c " + cmd);
        }

        private void VerificaStatoMenuClassico(int left, int top)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}"))
                {
                    menuClassicoAttivo = key != null;

                    if (btnToggleMenuClassico != null)
                    {
                        btnToggleMenuClassico.Text = $"Menu destro classico: {(menuClassicoAttivo ? "ON" : "OFF")}";
                        btnToggleMenuClassico.BackColor = menuClassicoAttivo ? Color.FromArgb(0, 153, 255) : Color.FromArgb(30, 30, 30);
                        btnToggleMenuClassico.ForeColor = Color.White;
                    }
                }
            }
            catch
            {
                menuClassicoAttivo = false;
            }
        }

        private Control[] CreateButtonWithHelp(string text, int top, EventHandler onClick, int left, string helpText)
        {
            var mainButton = new Button()
            {
                Text = text,
                Width = 280,
                Height = 40,
                Top = top,
                Left = left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
            };
            mainButton.FlatAppearance.BorderColor = Color.FromArgb(30, 30, 30);
            mainButton.FlatAppearance.BorderSize = 1;
            mainButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 25, 25);
            mainButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 15, 15);
            mainButton.Region = new Region(CreateRoundedRectanglePath(new Rectangle(0, 0, mainButton.Width, mainButton.Height), 10));
            mainButton.MouseEnter += (s, e) => { mainButton.ForeColor = Color.FromArgb(0, 153, 255); };
            mainButton.MouseLeave += (s, e) => { mainButton.ForeColor = Color.White; };
            mainButton.Click += onClick;

            var helpButton = new Button()
            {
                Text = "?",
                Width = 40,
                Height = 40,
                Top = top,
                Left = left + 285,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            helpButton.FlatAppearance.BorderSize = 0;
            helpButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 80);
            helpButton.Click += (s, e) => ShowHelpPopup(helpText);
            helpButton.Region = new Region(CreateRoundedRectanglePath(new Rectangle(0, 0, helpButton.Width, helpButton.Height), 8));

            return new Control[] { mainButton, helpButton };
        }


        private System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private async Task RunAsAdminAsync(string fileName, string arguments)
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
                var process = Process.Start(psi);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Errore: " + ex.Message);
            }
        }

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

        private void UpdateTrimStatus()
        {
            // Puoi implementare qui la lettura dello stato TRIM se vuoi aggiornarlo dinamicamente
        }

        private async Task EnableTrimAsync()
        {
            await RunAsAdminAsync("cmd.exe", "/c fsutil behavior set DisableDeleteNotify 0");
            MessageBox.Show("TRIM abilitato.");
            await Task.Delay(2000);
            UpdateTrimStatus();
        }

        private async Task DisableTrimAsync()
        {
            await RunAsAdminAsync("cmd.exe", "/c fsutil behavior set DisableDeleteNotify 1");
            MessageBox.Show("TRIM disabilitato.");
            await Task.Delay(2000);
            UpdateTrimStatus();
        }

        private async Task CambiaStatoSuperfetchAsync(bool attiva)
        {
            string servizio = "SysMain";
            try
            {
                // Modifica tipo avvio con sc config
                string startType = attiva ? "auto" : "disabled";
                await RunAsAdminAsync("sc.exe", $"config {servizio} start= {startType}");

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
                        MessageBox.Show("Superfetch (SysMain) attivato.");
                    }
                    else
                    {
                        if (sc.Status != ServiceControllerStatus.Stopped)
                        {
                            sc.Stop();
                            await Task.Delay(1000);
                            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(5));
                        }
                        MessageBox.Show("Superfetch (SysMain) disattivato.");
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
                    bool running = sc.Status == ServiceControllerStatus.Running;
                    superfetchAttivo = running;
                    btnToggleSuperfetch.Text = $"Superfetch e Prefetch (HDD): {(running ? "ON" : "OFF")}";
                    btnToggleSuperfetch.BackColor = running ? Color.FromArgb(0, 153, 255) : Color.FromArgb(30, 30, 30);
                    btnToggleSuperfetch.ForeColor = Color.White;
                }
            }
            catch
            {
                btnToggleSuperfetch.Text = "Superfetch e Prefetch: Stato non rilevato";
                superfetchAttivo = false;
            }
        }
        private void ShowHelpPopup(string message)
        {
            MessageBox.Show(message, "Informazioni", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        bool IsHibernationEnabled()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Power"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("HibernateEnabled");
                        if (val is int intVal && intVal == 1)
                        {
                            // Controlla anche che il file esista
                            return System.IO.File.Exists(@"C:\hiberfil.sys");
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        //TOGGLEBUTTON WITH HELP
        private Control[] CreateToggleButtonWithHelp(
        string label, int top,
        Func<bool> getState,
        Func<bool, Task> setState,
        int left,
        string helpText)
        {
            // Bottone toggle principale (stile uguale a CreateButtonWithHelp)
            var toggleButton = new Button()
            {
                Text = label + (getState() ? ": ON" : ": OFF"),
                Width = 280,
                Height = 40,
                Top = top,
                Left = left,
                FlatStyle = FlatStyle.Flat,
                BackColor = getState() ? Color.FromArgb(0, 153, 255) : Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            toggleButton.FlatAppearance.BorderColor = Color.FromArgb(30, 30, 30);
            toggleButton.FlatAppearance.BorderSize = 1;
            toggleButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 25, 25);
            toggleButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(15, 15, 15);
            toggleButton.Region = new Region(CreateRoundedRectanglePath(new Rectangle(0, 0, toggleButton.Width, toggleButton.Height), 10));
            toggleButton.MouseEnter += (s, e) => { toggleButton.ForeColor = Color.FromArgb(0, 153, 255); };
            toggleButton.MouseLeave += (s, e) => { toggleButton.ForeColor = Color.White; };

            // Help button stile identico agli altri help button
            var helpButton = new Button()
            {
                Text = "?",
                Width = 40,
                Height = 40,
                Top = top,
                Left = left + 285,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
            };
            helpButton.FlatAppearance.BorderSize = 0;
            helpButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 80);
            helpButton.Region = new Region(CreateRoundedRectanglePath(new Rectangle(0, 0, helpButton.Width, helpButton.Height), 10));

            helpButton.Click += (s, e) => ShowHelpPopup(helpText);

            // Evento click toggle
            toggleButton.Click += async (s, e) =>
            {
                bool newState = !getState();
                toggleButton.BackColor = newState ? Color.FromArgb(0, 153, 255) : Color.FromArgb(30, 30, 30);
                toggleButton.Text = label + (newState ? ": ON" : ": OFF");
                toggleButton.ForeColor = Color.White;
                await setState(newState);
            };

            return new Control[] { toggleButton, helpButton };
        }

        private async void RunChkdsk(string drive, bool simpleScan)
        {
            if (string.IsNullOrWhiteSpace(drive))
            {
                MessageBox.Show("Nessun disco selezionato.");
                return;
            }

            string args = simpleScan ? $"{drive}:" : $"{drive}: /f /r /x";

            try
            {
                // Esegui chkdsk tramite processo esterno
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "chkdsk.exe";
                process.StartInfo.Arguments = args;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;

                process.Start();

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                process.WaitForExit();

                MessageBox.Show($"CHKDSK terminato.\nOutput:\n{output}\nErrori:\n{error}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante l'esecuzione di CHKDSK:\n{ex.Message}");
            }
        }

        private void RunChkdskWithConsole(string driveLetter, bool simpleScan)
        {
            try
            {
                string arguments = simpleScan ? $"/K chkdsk {driveLetter}" : $"/K chkdsk {driveLetter} /f /r";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = arguments,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal,
                    Verb = "runas" // Esegue come amministratore
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Errore durante l'avvio di CHKDSK:\n{ex.Message}");
            }
        }

        //CHECKFISCK SOLO SCAN
        private void OnScanOnlyClick(object? sender, EventArgs e)
        {
            string? selectedDrive = comboBoxDrives.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedDrive))
            {
                MessageBox.Show("Seleziona un disco prima di eseguire CHKDSK.");
                return;
            }
            if (comboBoxDrives.SelectedIndex <= 0)
            {
                MessageBox.Show("Seleziona prima un'unità disco.");
                return;
            }
            RunChkdskWithConsole(selectedDrive, simpleScan: true);
        }

        //CHECKFISCK CON REPAIR
        private void OnScanRepairClick(object? sender, EventArgs e)
        {
            string? selectedDrive = comboBoxDrives.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedDrive))
            {
                MessageBox.Show("Seleziona un disco prima di eseguire CHKDSK.");
                return;
            }
            if (comboBoxDrives.SelectedIndex <= 0)
            {
                MessageBox.Show("Seleziona prima un'unità disco.");
                return;
            }
            RunChkdskWithConsole(selectedDrive, simpleScan: false);
        }

        //COMBO BOX SELETTORE DISCHI            
            private void PopulateDrivesComboBox()
            {
                comboBoxDrives.Items.Clear();

                // Aggiunge la voce fittizia come "etichetta"
                comboBoxDrives.Items.Add("-> Seleziona un disco...");

                var drives = System.IO.DriveInfo.GetDrives()
                    .Where(d => d.DriveType == DriveType.Fixed && d.IsReady);

                foreach (var drive in drives)
                {
                    comboBoxDrives.Items.Add(drive.Name.TrimEnd('\\').Replace(":", ""));
                }

                // Imposta la voce iniziale come selezionata
                comboBoxDrives.SelectedIndex = 0;
            }


        
    }
}
