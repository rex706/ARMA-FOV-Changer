﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ARMA_FOV_Changer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private static Version latest;

        private static string fileParams;

        private static string profilePath;
        private static string[] arrLine;

        private static int fovTopLine;
        private static int fovLeftLine;
        private static int uiLine;

        private static int desiredFov;
        private static string fovstring;

        private static int button;
        private static int ColdWar = 0;
        private static int ArmA2 = 1;
        private static int ArmA3 = 2;
        private static int DayZ = 3;
        private static int ArmAAA = 4;

        private static bool getValueFromTextBox = false;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            #region Initial Update Check

            // Verion number from assembly.
            string AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            MenuItem ver = new MenuItem();
            MenuItem newExistMenuItem = (MenuItem)this.FileMenu.Items[1];
            ver.Header = "v" + AssemblyVersion;
            ver.IsEnabled = false;
            newExistMenuItem.Items.Add(ver);

            try
            {
                int idx = AssemblyVersion.LastIndexOf('0') - 1;
                AssemblyVersion = AssemblyVersion.Substring(0, idx);
            }
            catch { }

            // Check for a new version.
            int updateResult = await CheckForUpdate();
            if (updateResult == -1)
            {
                // Some Error occurred.
                // TODO: Handle this Error.
            }
            else if (updateResult == 1)
            {
                // An update is available, but user has chosen not to update.
                // TODO: Change version number label to show there is an update and re-prompt user when clicked.
                ver.Header = "Update available!";
            }
            else if (updateResult == 2)
            {
                // An update is available, and the user has chosen to update.
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = true;
                startInfo.FileName = "Updater.exe";
                startInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = fileParams;

                Process.Start(startInfo);
                Environment.Exit(0);
            }

            #endregion

            LoadProfile();
        }

        private void LoadProfile()
        {
            // Open splash window for game selection.
            Splash splashWindow = new Splash();
            splashWindow.ShowDialog();

            // Profile file path from splashWindow.
            profilePath = splashWindow.filePath;

            if (splashWindow.filePath == null)
                Environment.Exit(0);

            while (profilePath.Contains(".vars."))
            {
                MessageBox.Show("Incorrect file selected!\nBe sure you are selecting the profile without 'vars' in the file name.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                splashWindow = new Splash();
                splashWindow.ShowDialog();
                profilePath = splashWindow.filePath;
            }

            // Get profile name from file path string.
            int lastSlashIdx = profilePath.LastIndexOf("\\") + 1;
            int profNameLength = profilePath.LastIndexOf(".") - lastSlashIdx;

            int cwSlashIdx = lastSlashIdx > 0 ? profilePath.LastIndexOf("\\", lastSlashIdx - 2) : -1;
            int profNameLengthCW = profilePath.LastIndexOf("\\") - cwSlashIdx;

            string profileName;

            button = splashWindow.buttonPressed;

            // Get profile name from either parent directory or selected file.
            if (button == ColdWar)
            {
                profileName = profilePath.Substring(cwSlashIdx + 1, profNameLengthCW - 1);
                autoCheckBox.Visibility = Visibility.Visible;
            }
            else
            {
                profileName = profilePath.Substring(lastSlashIdx, profNameLength);
            }

            // DayZ Standalone only uses vertical fov.
            if (button == DayZ)
            {
                fovLeftLabel.IsEnabled = false;
                DesiredFovLeftTextBox.IsEnabled = false;
            }

            // Replace %20 (space char) with an actual space.
            if (profileName.Contains("%20"))
                profileName = profileName.Replace("%20", " ");

            profileLabel.Content = profileName;

            // Get and set default resolution info.
            widthTextBox.Text = SystemParameters.PrimaryScreenWidth.ToString();
            heightTextBox.Text = SystemParameters.PrimaryScreenHeight.ToString();

            // Read entire file into an array of strings.
            // Get info we want from desired lines.
            arrLine = File.ReadAllLines(profilePath);

            if (button == DayZ)
                fovstring = "fov";
            else
                fovstring = "fovTop";

            // Slightly reduce runtime. We don't need to look for ui line until after we find fov.
            // It's all about that efficiency, baby.
            bool foundFovLine = false;

            // Find fov control line.
            for (int i = 0; i < arrLine.Length; i++)
            {
                if (arrLine[i].Contains(fovstring))
                {
                    fovTopLine = i;
                    fovLeftLine = i + 1;

                    if (button != ColdWar)
                        break;
                    else
                        foundFovLine = true;
                }

                if (foundFovLine && arrLine[i].Contains("uiTopLeftX"))
                {
                    uiLine = i;
                    break;
                }
            }

            string fovTop = arrLine[fovTopLine];
            string fovLeft = arrLine[fovLeftLine];

            int eqIdx = fovTop.IndexOf("=") + 1;
            int fovLen = fovTop.IndexOf(";") - eqIdx;
            fovTop = fovTop.Substring(eqIdx, fovLen);

            eqIdx = fovLeft.IndexOf("=") + 1;
            fovLen = fovLeft.IndexOf(";") - eqIdx;
            fovLeft = fovLeft.Substring(eqIdx, fovLen);

            if (button == ColdWar)
            {
                string uiTLX = arrLine[uiLine];
                string uiTLY = arrLine[uiLine + 1];
                string uiBRX = arrLine[uiLine + 2];
                string uiBRY = arrLine[uiLine + 3];

                eqIdx = uiTLX.IndexOf("=") + 1;
                fovLen = uiTLX.IndexOf(";") - eqIdx;
                uiTLX = uiTLX.Substring(eqIdx, fovLen);

                eqIdx = uiTLY.IndexOf("=") + 1;
                fovLen = uiTLY.IndexOf(";") - eqIdx;
                uiTLY = uiTLY.Substring(eqIdx, fovLen);

                eqIdx = uiBRX.IndexOf("=") + 1;
                fovLen = uiBRX.IndexOf(";") - eqIdx;
                uiBRX = uiBRX.Substring(eqIdx, fovLen);

                eqIdx = uiBRY.IndexOf("=") + 1;
                fovLen = uiBRY.IndexOf(";") - eqIdx;
                uiBRY = uiBRY.Substring(eqIdx, fovLen);

                uiTopLeftXLabel_Copy2.Content = uiTLX;
                uiTopLeftYLabel_Copy2.Content = uiTLY;
                uiBottomRightXLabel_Copy2.Content = uiBRX;
                uiBottomRightYLabel_Copy2.Content = uiBRY;
            }

            // Update labels
            CurrentFovTopLabel.Content = fovTop;
            if (button != DayZ)
            {
                CurrentFovLeftLabel.Content = fovLeft;
            }

            refreshMath();
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            // Overwrite fov values.
            arrLine[fovTopLine] = fovstring + "=" + DesiredFovTopTextBox.Text + ";";
            if (button != DayZ)
            {
                arrLine[fovLeftLine] = "fovLeft=" + DesiredFovLeftTextBox.Text + ";";
            }

            // Overwrite ui values if game is Cold War Assault.
            if (button == ColdWar)
            {
                arrLine[uiLine] = "uiTopLeftX=" + uiTopLeftXTextBox.Text + ";";
                arrLine[uiLine + 1] = "uiTopLeftY=" + uiTopLeftYTextBox.Text + ";";
                arrLine[uiLine + 2] = "uiBottomRightX=" + uiBottomRightXTextBox.Text + ";";
                arrLine[uiLine + 3] = "uiBottomRightY=" + uiBottomRightYTextBox.Text + ";";
            }

            try
            {
                // Write back to file.
                File.WriteAllLines(profilePath, arrLine);

                // Update 'current' labels.
                CurrentFovTopLabel.Content = DesiredFovTopTextBox.Text;
                if (button != DayZ)
                {
                    CurrentFovLeftLabel.Content = DesiredFovLeftTextBox.Text;
                }

                if (button == ColdWar)
                {
                    uiTopLeftXLabel_Copy2.Content = uiTopLeftXTextBox.Text;
                    uiTopLeftYLabel_Copy2.Content = uiTopLeftYTextBox.Text;
                    uiBottomRightXLabel_Copy2.Content = uiBottomRightXTextBox.Text;
                    uiBottomRightYLabel_Copy2.Content = uiBottomRightYTextBox.Text;
                }
            }
            catch (Exception m)
            {
                MessageBox.Show(m.Message, "Exception!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }

            MessageBox.Show("Settings Updated!", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void refreshMath()
        {
            if (getValueFromTextBox)
            {
                try
                {
                    desiredFov = Int32.Parse(fovTextBox.Text);
                }
                catch
                {
                    fovTextBox.Text = "90";
                    desiredFov = 90;
                }

                getValueFromTextBox = false;
            }
            else
            {
                desiredFov = (int)fovSlider.Value;
            }
            
            // Calculate aspect ratio.
            string aspectRatio = "";
            int nGCD = GetGreatestCommonDivisor(Int32.Parse(heightTextBox.Text), Int32.Parse(widthTextBox.Text));
            int aspect1 = Int32.Parse(widthTextBox.Text) / nGCD;
            int aspect2 = Int32.Parse(heightTextBox.Text) / nGCD;

            double testRatio = Math.Round(Double.Parse(widthTextBox.Text) / Double.Parse(heightTextBox.Text), 2);

            // Initialize ui variables for Cold War Assault.
            double uiTopLeftX = 0.0;
            double uiTopLeftY = 0.0;
            double uiBottomRightX = 1.0;
            double uiBottomRightY = 1.0;
            double cwFovTop = 0.75;
            double cwFovLeft = 1.0;

            // Set up aspect ratio variables and ui scaling based on http://ofp-faguss.com/files/ofp_aspect_ratio.pdf
            if (testRatio == 1.25)
            {
                aspectRatio = "5:4";
                cwFovTop = 0.8;
                uiTopLeftY = 0.03125;
                uiBottomRightY = 0.96875;
            }
            else if (testRatio == 1.33 || testRatio == 1.37)
            {
                aspectRatio = "4:3";
            }
            else if (testRatio == 1.50)
            {
                aspectRatio = "3:2";
            }
            else if (testRatio == 1.60)
            {
                aspectRatio = "16:10";
                cwFovLeft = 1.2;
                uiTopLeftX = 0.083333;
                uiBottomRightX = 0.916667;
            }
            else if (testRatio == 1.67)
            {
                aspectRatio = "15:9";
                cwFovLeft = 1.25;
                uiTopLeftX = 0.1;
                uiBottomRightX = 0.9;
            }
            else if (testRatio == 1.71)
            {
                aspectRatio = "128:75";
            }
            else if (testRatio == 1.78)
            {
                aspectRatio = "16:9";
                cwFovLeft = 1.333333;
                uiTopLeftX = 0.125;
                uiBottomRightX = 0.875;
            }
            else if (/*testRatio == 1.85 ||*/ testRatio == 2.33 || testRatio == 2.37 || testRatio == 2.39)
            {
                aspectRatio = "21:9";
                cwFovLeft = 1.777778;
                uiTopLeftX = 0.21875;
                uiBottomRightX = 0.78125;
            }

            // Triplehead
            else if (testRatio == 3.75)
            {
                aspectRatio = "15:4";
                cwFovTop = 0.8;
                cwFovLeft = 3.0;
                uiTopLeftX = 0.333333;
                uiTopLeftY = 0.03125;
                uiBottomRightX = 0.666667;
                uiBottomRightY = 0.96875;
            }
            else if (testRatio == 4.00 || testRatio == 4.11)
            {
                aspectRatio = "12:3";
                cwFovLeft = 3.0;
                uiTopLeftX = 0.333333;
                uiBottomRightX = 0.666667;
            }
            else if (testRatio == 4.80)
            {
                aspectRatio = "48:10";
                cwFovLeft = 3.6;
                uiTopLeftX = 0.361111;
                uiBottomRightX = 0.638889;
            }
            else if (testRatio == 5.00)
            {
                aspectRatio = "45:9";
                cwFovLeft = 3.75;
                uiTopLeftX = 0.366667;
                uiBottomRightX = 0.633333;
            }
            else if (testRatio == 5.33)
            {
                aspectRatio = "48:9";
                cwFovLeft = 4.0;
                uiTopLeftX = 0.375;
                uiBottomRightX = 0.625;
            }
            else if (testRatio == 7.00 || testRatio == 7.11 || testRatio == 7.17)
            {
                aspectRatio = "63:9";
                cwFovLeft = 5.333333;
                uiTopLeftX = 0.40625;
                uiBottomRightX = 0.59375;
            }
            else
            {
                aspectRatio = aspect1 + ":" + aspect2;
            }

            aspectRatioLabel.Content = aspectRatio;

            // If user chose Cold War Assault, set up respective ui labels.
            if (button == ColdWar)
            {
                uiTopLeftXLabel.IsEnabled = true;
                uiTopLeftXTextBox.IsEnabled = true;
                uiTopLeftYLabel.IsEnabled = true;
                uiTopLeftYTextBox.IsEnabled = true;
                uiBottomRightXLabel.IsEnabled = true;
                uiBottomRightXTextBox.IsEnabled = true;
                uiBottomRightYLabel.IsEnabled = true;
                uiBottomRightYTextBox.IsEnabled = true;

                uiTopLeftXTextBox.Text = uiTopLeftX.ToString();
                uiTopLeftYTextBox.Text = uiTopLeftY.ToString();
                uiBottomRightXTextBox.Text = uiBottomRightX.ToString();
                uiBottomRightYTextBox.Text = uiBottomRightY.ToString();
            }

            if (autoCheckBox.IsChecked == true)
            {
                DesiredFovTopTextBox.Text = cwFovTop.ToString();
                DesiredFovLeftTextBox.Text = cwFovLeft.ToString();
            }
            else
            {
                // Desired fov to radians
                double hfovRad = desiredFov * (Math.PI / 180);
                double hFoV = 2 * Math.Atan(Math.Tan(hfovRad / 2) * (Double.Parse(heightTextBox.Text) / Double.Parse(widthTextBox.Text)));
                hFoV = Math.Ceiling(hFoV * 100) / 100;

                DesiredFovTopTextBox.Text = hFoV.ToString();

                double wFoV = hFoV / (Double.Parse(heightTextBox.Text) / nGCD);
                wFoV = wFoV * (Double.Parse(widthTextBox.Text) / nGCD);
                wFoV = Math.Floor(wFoV * 100) / 100;

                DesiredFovLeftTextBox.Text = wFoV.ToString();
            }
        }

        private static async Task<int> CheckForUpdate()
        {
            //Nkosi Note: Always use asynchronous versions of network and IO methods.

            //Check for version updates
            var client = new HttpClient();
            client.Timeout = new TimeSpan(0, 0, 0, 10);
            try
            {
                // Open the text file using a stream reader
                using (Stream stream = await client.GetStreamAsync("http://textuploader.com/d5ivh/raw"))
                {
                    System.Version current = Assembly.GetExecutingAssembly().GetName().Version;
                    StreamReader reader = new StreamReader(stream);
                    latest = System.Version.Parse(reader.ReadLine());

                    List<string> newFiles = new List<string>();

                    while (!reader.EndOfStream)
                    {
                        newFiles.Add(await reader.ReadLineAsync());
                    }

                    for (int i = 0; i < newFiles.Count; i++)
                    {
                        if (i == 0)
                            fileParams += newFiles[i];
                        else
                            fileParams += " " + newFiles[i];
                    }

                    if (latest > current)
                    {
                        MessageBoxResult answer = MessageBox.Show("A new version of Field of Views is available!\n\nCurrent Version     " + current + "\nLatest Version     " + latest + "\n\nUpdate now?", "Field of Views Update", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        if (answer == MessageBoxResult.Yes)
                        {
                            //Update is available, and user wants to update. Requires app to close.
                            return 2;
                        }
                        //Update is available, but user chose not to update just yet.
                        return 1;
                    }
                }
                //No update available.
                return 0;
            }
            catch (Exception m)
            {
                //MessageBox.Show("Failed to check for update.\n" + m.Message,"Error", MessageBoxButtons.OK, MessageBoxImage.Error);
                return 0;
            }
        }

        private void ProfileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            autoCheckBox.IsChecked = false;
            autoCheckBox.Visibility = Visibility.Hidden;

            scrollBar.IsEnabled = true;
            fovTextBox.IsEnabled = true;
            fovSlider.IsEnabled = true;
            fovSlider.ToolTip = null;

            CurrentFovLeftLabel.Content = "";
            fovLeftLabel.IsEnabled = true;
            DesiredFovLeftTextBox.IsEnabled = true;

            uiTopLeftXLabel.IsEnabled = false;
            uiTopLeftXTextBox.IsEnabled = false;
            uiTopLeftYLabel.IsEnabled = false;
            uiTopLeftYTextBox.IsEnabled = false;
            uiBottomRightXLabel.IsEnabled = false;
            uiBottomRightXTextBox.IsEnabled = false;
            uiBottomRightYLabel.IsEnabled = false;
            uiBottomRightYTextBox.IsEnabled = false;

            uiTopLeftXTextBox.Text = "";
            uiTopLeftXLabel_Copy2.Content = "";
            uiTopLeftYTextBox.Text = "";
            uiTopLeftYLabel_Copy2.Content = "";
            uiBottomRightXTextBox.Text = "";
            uiBottomRightXLabel_Copy2.Content = "";
            uiBottomRightYTextBox.Text = "";
            uiBottomRightYLabel_Copy2.Content = "";

            LoadProfile();
            refreshMath();
        }

        private void fovSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (heightTextBox != null && widthTextBox != null && heightTextBox.Text.Length > 2 && widthTextBox.Text.Length > 2)
                fovTextBox.Text = fovSlider.Value.ToString();
            else
                Console.WriteLine("Not enough input!");
        }

        private void widthTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (Regex.IsMatch(widthTextBox.Text, @"^\d+$") && widthTextBox.Text.Length > 2 && Regex.IsMatch(widthTextBox.Text, @"^\d+$") && heightTextBox.Text.Length > 2)
                refreshMath();
        }

        private void heightTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (Regex.IsMatch(widthTextBox.Text, @"^\d+$") && heightTextBox.Text.Length > 2 && Regex.IsMatch(widthTextBox.Text, @"^\d+$") && widthTextBox.Text.Length > 2)
                refreshMath();
        }

        private static int GetGreatestCommonDivisor(int a, int b)
        {
            return b == 0 ? a : GetGreatestCommonDivisor(b, a % b);
        }

        private void GitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/rex706/ARMA-FOV-Changer/");
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void autoCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            scrollBar.IsEnabled = false;
            fovSlider.IsEnabled = false;
            degreeLabel.Visibility = Visibility.Hidden;
            fovTextBox.IsEnabled = false;
            refreshMath();
        }

        private void autoCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            scrollBar.IsEnabled = true;
            fovSlider.IsEnabled = true;
            degreeLabel.Visibility = Visibility.Visible;
            fovTextBox.IsEnabled = true;
            refreshMath();
        }

        private void scrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int value = 0;

            try
            {
                value = Int32.Parse(fovTextBox.Text);
            }
            catch
            {
                return;
            }

            if (e.NewValue < e.OldValue)
                value += 1;
                
            else if (e.NewValue > e.OldValue)
                value -= 1;

            fovTextBox.Text = value.ToString();
        }

        private void fovTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int value = 0;

            if (heightTextBox.Text.Length > 2 && widthTextBox.Text.Length > 2 && fovTextBox.Text.Length >= 2)
            {
                try
                {
                    value = Int32.Parse(fovTextBox.Text);
                }
                catch
                {
                    value = 90;
                }

                if (value >= 65 && value <= 165 && value != fovSlider.Value)
                {
                    fovSlider.Value = value;
                }
                else
                {
                    getValueFromTextBox = true;
                }

                refreshMath();
            }
        }
    }
}