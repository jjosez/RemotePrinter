/*
 * This file is part of RemotePrinter.
 * Copyright (C) 2018-2019 Juan José Prieto Dzul <juanjoseprieto88@gmail.com>
 * 
 * RemotePrinter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * RemotePrinter is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with RemotePrinter.  If not, see<https://www.gnu.org/licenses/>.
 */

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemotePrinter
{
    public partial class RemotePrinterForm : Form
    {
        public RemotePrinterForm()
        {
            InitializeComponent();
            LoadSettings();

            RunServer();
        }

        private void LoadSettings()
        {
            /// Server Settings
            string apiUrl = Properties.Settings.Default.server;
            textApiServerUrl.Text = string.IsNullOrEmpty(apiUrl) ? "http://facturascrtipts" : apiUrl;

            string apiKey = Properties.Settings.Default.apikey;
            textApiKey.Text = string.IsNullOrEmpty(apiKey) ? "Establecer api de impresion" : apiKey;

            /// Printer Settings
            string defaultPrinter = Properties.Settings.Default.defaultprinter;
            textCutCommand.Text = Properties.Settings.Default.cutcommand;
            textDrawerCommand.Text = Properties.Settings.Default.drawercommand;

            for (int i = 0; i < PrinterSettings.InstalledPrinters.Count; i++)
            {
                comboPrinters.Items.Add(PrinterSettings.InstalledPrinters[i]);
            }

            if (defaultPrinter != "")
            {
                int index = comboPrinters.FindStringExact(defaultPrinter);
                comboPrinters.SelectedIndex = index;
            }

            /// Autorun Settings
            if (Properties.Settings.Default.autorun)
            {
                checkAutoStart.Checked = true;

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key.GetValue("RemotePrinterAutorun") == null)
                    {
                        key.SetValue("RemotePrinterAutorun", "\"" + Application.ExecutablePath + "\"");
                    }
                }
            }
            else
            {
                checkAutoStart.Checked = false;

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    key.DeleteValue("RemotePrinterAutorun", false);
                }
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.defaultprinter = comboPrinters.Text;
            Properties.Settings.Default.server = textApiServerUrl.Text;
            Properties.Settings.Default.apikey = textApiKey.Text;
            Properties.Settings.Default.cutcommand = textCutCommand.Text;
            Properties.Settings.Default.drawercommand = textDrawerCommand.Text;
            Properties.Settings.Default.autorun = checkAutoStart.Checked;
            Properties.Settings.Default.Save();

            statusLabel.Text = "Configuracion guardada";
        }

        private void RunServer()
        {
            PrintServer printserver = new PrintServer(UpdateStatus)
            {
                Prefix = "http://localhost:10080/"
            };

            printserver.Start();
        }

        private void UpdateStatus(string serverStatus)
        {
            string textStatus = String.Format("Version {0} - {1}", ProductVersion, serverStatus);
            BeginInvoke(new MethodInvoker(() => statusLabel.Text = textStatus));
        }

        private void RemotePrinterForm_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                IconTray.Visible = true;
                IconTray.ShowBalloonTip(250);
                Hide();
            }

            else if (FormWindowState.Normal == this.WindowState)
            {
                IconTray.Visible = false;
            }
        }

        private void IconTray_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            IconTray.Visible = false;
        }
    }
}
