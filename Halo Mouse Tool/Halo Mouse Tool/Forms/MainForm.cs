﻿using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Principal;
using System.Reflection;
using Microsoft.Win32;
using System.Drawing;
using System.IO;
/*
* I will refactor all this BS out of mainform when it all works lol
*/
namespace Halo_Mouse_Tool
{
    public partial class MainForm : Form
    {
        static Settings settings = new Settings();
        static SettingsForm settingsForm = new SettingsForm(settings);
        static DonateForm donateForm = new DonateForm();
        static AboutForm aboutForm = new AboutForm();
        KeysConverter kc = new KeysConverter();

        public static bool IsAdministrator()
        {
            return (new WindowsPrincipal(WindowsIdentity.GetCurrent()))
                      .IsInRole(WindowsBuiltInRole.Administrator);
        }

        public MainForm()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            saveSettings();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                LoadSettings();
            }
            catch (Exception ex)
            {
                if (ex is NullReferenceException || ex is ArgumentException || ex is ArgumentOutOfRangeException)
                {
                    MessageBoxSnd("Error loading registry", "Failed to load settings from the registry. Settings reset to defaults. Error Message: " + ex.Message, SoundHandlingUtils.SoundType.Error);
                    saveSettings();
                }
            }

            if (settings.CheckForUpdates)
            {
                CheckForUpdates();
            }

            string title = "Halo Mouse Tool v" + Assembly.GetExecutingAssembly().GetName().Version.ToString()[0];
            if (!IsAdministrator())
            {
                title += " -NOT ADMIN-";
            }
            Text = title;

            setControls();
        }

        private void ExitBtn_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void DonateBtn_Click(object sender, EventArgs e)
        {
            if (!FormHandlingUtils.Formopen(donateForm))
            {
                donateForm = new DonateForm();
            }
            donateForm.Show();
        }

        private void OptionsBtn_Click(object sender, EventArgs e)
        {
            if (!FormHandlingUtils.Formopen(settingsForm))
            {
                settingsForm = new SettingsForm(settings);
            }
            settingsForm.Show();
        }

        private void AboutBtn_Click(object sender, EventArgs e)
        {
            if (!FormHandlingUtils.Formopen(donateForm))
            {
                aboutForm = new AboutForm();
            }
            aboutForm.Show();
        }

        private void HelpBtn_Click(object sender, EventArgs e)
        {
            string chmPath = Path.Combine(Path.GetTempPath(), "Halo Mouse Tool.chm");
            if (!File.Exists(chmPath))
            {
                byte[] chmBytes;
                chmBytes = Properties.Resources.Halo_Mouse_Tool;
                using (FileStream chmFile = new FileStream(chmPath, FileMode.Create))
                {
                    chmFile.Write(chmBytes, 0, chmBytes.Length);
                }
            }
            Help.ShowHelp(this, chmPath);

        }

        private void SaveSettingsBtn_Click(object sender, EventArgs e)
        {
            saveSettings();
        }

        private void DeployDllBtn_Click(object sender, EventArgs e)
        {
            //Deploy the DLL
        }

        private void CheckForUpdateBtn_Click(object sender, EventArgs e)
        {
            CheckForUpdates();
        }

        private void HaloCustomEditionBtn_Click(object sender, EventArgs e)
        {
            settings.Current_Game = Settings.Game.CustomEdition;
            HaloCustomEditionBtn.Checked = true;
            HaloCombatEvolvedBtn.Checked = false;
        }

        private void HaloCombatEvolvedBtn_Click(object sender, EventArgs e)
        {
            settings.Current_Game = Settings.Game.CombatEvolved;
            HaloCustomEditionBtn.Checked = false;
            HaloCombatEvolvedBtn.Checked = true;
        }

        private void UpdateStatusLabel_Click(object sender, EventArgs e)
        {
            Process.Start(@"https://github.com/AWilliams17/Halo-CE-Mouse-Tool/releases");
        }

        private void WriteBtn_Click(object sender, EventArgs e) //Note: SuccessMessages needs to be added
        {
            try
            {
                WriteHaloMemory();
                MessageBoxSnd("Success", "Successfully wrote to memory.", SoundHandlingUtils.SoundType.Success);
            }
            catch (WriteProcessException ex)
            {
                MessageBoxSnd("Failed to write to memory", ex.Message, SoundHandlingUtils.SoundType.Error);
            }
        }

        private void CheckForUpdates()
        {
            try
            {
                if (UpdateHandlingUtils.UpdateAvailable(settings.UpdateTimeout))
                {
                    UpdateStatusLabel.IsLink = true;
                    UpdateStatusLabel.Text = "Yes!";
                }
                else
                {
                    UpdateStatusLabel.Text = "None.";
                }
            }
            catch (System.Net.WebException ex)
            {
                MessageBoxSnd("Update Error", "An error occurred while checking for updates: " + ex.Message, SoundHandlingUtils.SoundType.Error);
                UpdateStatusLabel.Text = "Error.";
            }
        }

        private void LoadSettings() //ToDo: Refactor this garbage
        {
            RegistryKey HaloMouseToolRegistry = Registry.CurrentUser.OpenSubKey("Software\\HaloMouseTool", false);
            if (HaloMouseToolRegistry == null)
            {
                saveSettings();
            }
            else
            {
                object sensX = HaloMouseToolRegistry.GetValue("SensX");
                object sensY = HaloMouseToolRegistry.GetValue("SensY");
                object mouseAcceleration = HaloMouseToolRegistry.GetValue("PatchMouseAcceleration");
                object hotKeyApplication = HaloMouseToolRegistry.GetValue("HotkeyApplication");
                object hotKeyDll = HaloMouseToolRegistry.GetValue("HotkeyDll");

                object incrementAmount = HaloMouseToolRegistry.GetValue("IncrementAmount");
                object incrementEnabled = HaloMouseToolRegistry.GetValue("IncrementKeysEnabled");
                object currentGame = HaloMouseToolRegistry.GetValue("CurrentGame");

                object checkForUpdates = HaloMouseToolRegistry.GetValue("CheckForUpdates");
                object updateTimeout = HaloMouseToolRegistry.GetValue("UpdateTimeout");
                object soundsEnabled = HaloMouseToolRegistry.GetValue("SoundsEnabled");
                object soundsEnabledDll = HaloMouseToolRegistry.GetValue("SoundsEnabledDll");
                object successMessages = HaloMouseToolRegistry.GetValue("SuccessMessages");

                settings.SensX = float.Parse(sensX.ToString());
                settings.SensY = float.Parse(sensY.ToString());
                if (mouseAcceleration.ToString() == "0")
                {
                    settings.PatchAcceleration = false;
                }
                settings.HotKeyApplication = int.Parse(hotKeyApplication.ToString());
                settings.HotKeyDll = int.Parse(hotKeyDll.ToString());
                if (checkForUpdates.ToString() == "0")
                {
                    settings.CheckForUpdates = false;
                }
                settings.UpdateTimeout = int.Parse(updateTimeout.ToString());
                if (soundsEnabled.ToString() == "0")
                {
                    settings.SoundsEnabled = false;
                }
                if (soundsEnabledDll.ToString() == "0")
                {
                    settings.SoundsEnabledDll = false;
                }
                if (successMessages.ToString() == "0")
                {
                    settings.SuccessMessages = false;
                }
                if (incrementEnabled.ToString() == "0")
                {
                    settings.IncrementKeysEnabled = false;
                }
                if ((int)currentGame == 1)
                {
                    settings.Current_Game = Settings.Game.CombatEvolved;
                }
                settings.IncrementAmount = float.Parse(incrementAmount.ToString());
            }
        }

        //Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "CurrentGame", settings.Current_Game == Settings.Game.CombatEvolved ), RegistryValueKind.DWord);
        private void saveSettings() //ToDo: Refactor this garbage.
        {
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "SensX", settings.SensX, RegistryValueKind.String);
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "SensY", settings.SensY, RegistryValueKind.String);
            if (settings.PatchAcceleration)
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "PatchMouseAcceleration", 1, RegistryValueKind.DWord);
            }
            else
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "PatchMouseAcceleration", 0, RegistryValueKind.DWord);
            }
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "HotkeyApplication", settings.HotKeyApplication, RegistryValueKind.DWord);
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "HotkeyDll", settings.HotKeyDll, RegistryValueKind.DWord);
            if (settings.CheckForUpdates)
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "CheckForUpdates", 1, RegistryValueKind.DWord);
            }
            else
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "CheckForUpdates", 0, RegistryValueKind.DWord);
            }

            Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "UpdateTimeout", settings.UpdateTimeout, RegistryValueKind.DWord);

            if (settings.HotKeyEnabled)
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "HotKeyEnabled", 1, RegistryValueKind.DWord);
            }
            else
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "HotKeyEnabled", 0, RegistryValueKind.DWord);
            }

            if (settings.SoundsEnabled)
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "SoundsEnabled", 1, RegistryValueKind.DWord);
            }
            else
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "SoundsEnabled", 0, RegistryValueKind.DWord);
            }

            if (settings.SoundsEnabledDll)
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "SoundsEnabledDll", 1, RegistryValueKind.DWord);
            }
            else
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "SoundsEnabledDll", 0, RegistryValueKind.DWord);
            }

            if (settings.SuccessMessages)
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "SuccessMessages", 1, RegistryValueKind.DWord);
            }
            else
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "SuccessMessages", 0, RegistryValueKind.DWord);
            }

            if (settings.Current_Game == Settings.Game.CombatEvolved)
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "CurrentGame", 1, RegistryValueKind.DWord);
            }
            else
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "CurrentGame", 0, RegistryValueKind.DWord);
            }

            if (settings.IncrementKeysEnabled)
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "IncrementKeysEnabled", 1, RegistryValueKind.DWord);
            }
            else
            {
                Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "IncrementKeysEnabled", 0, RegistryValueKind.DWord);
            }

            Registry.SetValue("HKEY_CURRENT_USER\\Software\\HaloMouseTool", "IncrementAmount", settings.IncrementAmount, RegistryValueKind.String);
        }

        private void setControls()
        {
            SensXTextBox.Text = settings.SensX.ToString();
            SensYTextBox.Text = settings.SensY.ToString();
            if (settings.Current_Game == Settings.Game.CustomEdition)
            {
                HaloCustomEditionBtn.Checked = true;
                HaloCombatEvolvedBtn.Checked = false;
            }
            else
            {
                HaloCustomEditionBtn.Checked = false;
                HaloCombatEvolvedBtn.Checked = true;
            }
        }

        private void WriteHaloMemory() //ToDo: Refactor this garbage lol
        {
            byte[] mouseaccelnop = { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 }; //For noping the acceleration
            int currAddr = 0;
            byte[] currVal = { };
            string game;
            if (settings.Current_Game == Settings.Game.CombatEvolved)
            {
                game = "halo";
                for (int i = 0; i != 3; i++)
                {
                    if (i == 0)
                    {
                        currVal = BitConverter.GetBytes((settings.SensX * 0.25F));
                        currAddr = 0x310B50;
                    }
                    if (i == 1)
                    {
                        currVal = BitConverter.GetBytes((settings.SensY * 0.25F));
                        currAddr = 0x310B54;
                    }
                    if (i == 2 && settings.PatchAcceleration)
                    {
                        currVal = mouseaccelnop;
                        currAddr = 0x963C0;
                    }
                    MemoryHandlingUtils.WriteToProcessMemory(game, currVal, currAddr);
                }
            }
            else
            {
                game = "haloce";
                for (int i = 0; i != 3; i++)
                {
                    if (i == 0)
                    {
                        currVal = BitConverter.GetBytes((settings.SensX * 0.25F));
                        currAddr = 0x2ABB50;
                    }
                    if (i == 1)
                    {
                        currVal = BitConverter.GetBytes((settings.SensY * 0.25F));
                        currAddr = 0x2ABB54;
                    }
                    if (i == 2 && settings.PatchAcceleration)
                    {
                        currVal = mouseaccelnop;
                        currAddr = 0x8F836;
                    }
                    if (i == 3 && settings.PatchAcceleration)
                    {
                        currVal = mouseaccelnop;
                        currAddr = 0x8F830;
                    }
                    MemoryHandlingUtils.WriteToProcessMemory(game, currVal, currAddr);
                }
            }
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            string status;
            string game;
            string proc;
            if (settings.Current_Game == Settings.Game.CombatEvolved)
            {
                game = "Halo PC";
                proc = "halo";
            }
            else
            {
                game = "Halo CE";
                proc = "haloce";
            }
            Color labelColor;
            if (ProcessHandlingUtils.ProcessIsRunning(proc))
            {
                labelColor = Color.Green;
                status = game + " found.";
                WriteBtn.Enabled = true;
            }
            else
            {
                labelColor = Color.Red;
                status = game + " not found.";
                WriteBtn.Enabled = false;
            }
            HaloStatusLabel.Text = status;
            HaloStatusLabel.ForeColor = labelColor;
        }

        private void HotkeyLabelTimer_Tick(object sender, EventArgs e)
        {
            if (settings.HotKeyEnabled)
            {
                HotkeyLabel.Text = kc.ConvertToString(settings.HotKeyApplication);
            }
            else
            {
                HotkeyLabel.Text = "Disabled.";
            }
        }

        private void HotkeyTimer_Tick(object sender, EventArgs e)
        {
            if (WriteBtn.Enabled && settings.HotKeyEnabled)
            {
                if (KeybindHandlingUtils.IsKeyPushedDown((Keys)Enum.Parse(typeof(Keys), ((Keys)settings.HotKeyApplication).ToString())))
                {
                    WriteHaloMemory();
                }
            }

            if (WriteBtn.Enabled && settings.IncrementKeysEnabled) //ToDo: these error messages are horrible. Fix them.
            {
                if (KeybindHandlingUtils.IsKeyPushedDown(Keys.Oemplus))
                {
                    try
                    {
                        settings.SensX = settings.SensX += settings.IncrementAmount;
                        settings.SensY = settings.SensY += settings.IncrementAmount;
                        WriteHaloMemory();
                        SensXTextBox.Text = settings.SensX.ToString();
                        SensYTextBox.Text = settings.SensY.ToString();
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        MessageBoxSnd("Invalid setting", ex.Message, SoundHandlingUtils.SoundType.Error);
                    }
                    catch (WriteProcessException ex)
                    {
                        MessageBoxSnd("Failed to write memory.", ex.Message, SoundHandlingUtils.SoundType.Error);
                    }
                }

                else if (KeybindHandlingUtils.IsKeyPushedDown(Keys.OemMinus))
                {
                    try
                    {
                        settings.SensX = settings.SensX -= settings.IncrementAmount;
                        settings.SensY = settings.SensY -= settings.IncrementAmount;
                        WriteHaloMemory();
                        SensXTextBox.Text = settings.SensX.ToString();
                        SensYTextBox.Text = settings.SensY.ToString();
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        MessageBoxSnd("Invalid setting", ex.Message, SoundHandlingUtils.SoundType.Error);
                    }
                    catch (WriteProcessException ex)
                    {
                        MessageBoxSnd("Failed to write memory.", ex.Message, SoundHandlingUtils.SoundType.Error);
                    }
                }
            }
        }

        private void SensXTextBox_TextChanged(object sender, EventArgs e)
        {
            float res;
            if (!float.TryParse(SensXTextBox.Text, out res))
            {
                MessageBoxSnd("Invalid Input", "Please make sure this is a valid value.", SoundHandlingUtils.SoundType.Error);
                SensXTextBox.Text = "0";
                SensXTextBox.Focus();
            }
        }

        private void SensYTextBox_TextChanged(object sender, EventArgs e)
        {
            float res;
            if (!float.TryParse(SensYTextBox.Text, out res))
            {
                MessageBoxSnd("Invalid Input", "Please make sure this is a valid value.", SoundHandlingUtils.SoundType.Error);
                SensYTextBox.Text = "0";
                SensYTextBox.Focus();
            }
        }

        private void SensYTextBox_Leave(object sender, EventArgs e)
        {
            if (SensYTextBox.Text == "")
            {
                MessageBoxSnd("Can not be blank", "Error: this field cannot be left blank!", SoundHandlingUtils.SoundType.Error);
                SensYTextBox.Focus();
            }
        }

        private void SensXTextBox_Leave(object sender, EventArgs e)
        {
            if (SensXTextBox.Text == "")
            {
                MessageBoxSnd("Can not be blank", "Error: this field cannot be left blank!", SoundHandlingUtils.SoundType.Error);
                SensXTextBox.Focus();
            }
        }

        private void SensYTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.OemMinus || e.KeyData == Keys.Space)
            {
                MessageBoxSnd("Invalid Input", "This cannot be a negative value or contain a space.", SoundHandlingUtils.SoundType.Error);
                SensYTextBox.Text = "0";
            }
        }

        private void SensXTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.OemMinus || e.KeyData == Keys.Space)
            {
                MessageBoxSnd("Invalid Input", "This cannot be a negative value or contain a space.", SoundHandlingUtils.SoundType.Error);
                SensXTextBox.Text = "0";
            }
        }

        private void MessageBoxSnd(string title, string text, SoundHandlingUtils.SoundType soundtype)
        {
            MessageBox.Show(text, title);
            if (settings.SoundsEnabled)
            {
                if (soundtype == SoundHandlingUtils.SoundType.Success)
                {
                    SoundHandlingUtils.sound_success();
                }

                if (soundtype == SoundHandlingUtils.SoundType.Error)
                {
                    SoundHandlingUtils.sound_error();
                }

                if (soundtype == SoundHandlingUtils.SoundType.IncrementError)
                {
                    SoundHandlingUtils.sound_incrementerror();
                }

                if (soundtype == SoundHandlingUtils.SoundType.Alert)
                {
                    SoundHandlingUtils.sound_alert();
                }
            }
        }

        private void DeployDLL()
        {
            if (settings.Current_Game == Settings.Game.CombatEvolved) //Just for testers. this if branch will be removed in release.
            {
                MessageBoxSnd("Combat Evolved DLL isn't done yet", "I haven't made Combat Evolved's DLL yet :p select Custom Edition", SoundHandlingUtils.SoundType.Error);
            }
            else
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    string description =
                        "Select the Halo CE/SPV3 Controls directory." + Environment.NewLine +
                        "If you wish to only use this for SPV3, you just have to select SPV3's control directory. " +
                        "If you want to use it for Halo CE, select Halo CE's control directory." +
                        "For reference, this folder will have a dll in it called 'controls.dll'.";
                    fbd.ShowNewFolderButton = false;
                    fbd.Description = description;
                    DialogResult result = fbd.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        bool validselection = false;
                        try
                        {
                            string[] files = Directory.GetFiles(fbd.SelectedPath);
                            for (int i = 0; i < files.Length; i++)
                            {
                                if (files[i].ToLower().Contains("controls.dll"))
                                {
                                    validselection = true;
                                    break;
                                }
                            }
                            if (validselection)
                            {
                                if (settings.Current_Game == Settings.Game.CombatEvolved)
                                {
                                    string dll = Path.Combine(fbd.SelectedPath, "HaloCombatEvolvedMouseFix.dll");
                                    //File.WriteAllBytes(dll, Properties.Resources.HaloCombatEvolvedMouseFix);
                                }
                                else
                                {
                                    string dll = Path.Combine(fbd.SelectedPath, "HaloCustomEditionMouseFix.dll");
                                    File.WriteAllBytes(dll, Properties.Resources.HaloCustomEditionMouseFix);
                                }
                                string successMsg =
                                    "Successfully deployed DLL to controls folder." +
                                    " All you must do from here is just set your settings in this application, and then make sure you have saved them " +
                                    "(file -> save settings just to be sure), run the game, and press your hotkey!";
                                MessageBoxSnd("Successfully deployed DLL", successMsg, SoundHandlingUtils.SoundType.Success);
                            }
                            else
                            {
                                MessageBoxSnd("Wrong folder!", "Error - the selected folder did not have a controls.dll file.", SoundHandlingUtils.SoundType.Error);
                            }
                        }
                        catch (UnauthorizedAccessException)
                        {
                            MessageBoxSnd("Access Denied!", "Error - you do not have access to this location. Are you running as admin?", SoundHandlingUtils.SoundType.Error);
                        }
                    }
                }
            }
        }
    }
}
