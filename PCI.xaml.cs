using MakuTweakerNew.Properties;
using MicaWPF.Core.Enums;
using MicaWPF.Core.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Vortice.DXGI;
using System.Security.Cryptography;

namespace MakuTweakerNew
{
    public partial class PCI : Page
    {
        private dynamic _pci;
        MainWindow mw = (MainWindow)Application.Current.MainWindow;
        private List<GpuInfo> _gpus = new List<GpuInfo>();
        private List<StorageInfo> _storageDevices = new List<StorageInfo>();
        private List<RamStickInfo> _ramSticks = new();

        private double _lastSingleScore = 0;
        private double _lastMultiScore = 0;

        public PCI()
        {
            Environment.SetEnvironmentVariable("LHM_NO_RING0", "1");
            InitializeComponent();
            this.PreviewKeyDown += PCI_PreviewKeyDown;
            LoadLang();
            ShowRamInfo();
            ShowCpuInfo();
            ShowCpuExtraInfo();
            ShowMotherboardInfo();
            LoadGpuList();
            LoadStorageList();
            ShowComputerInfo();
            ShowSecurityInfo();
            LoadRamSticks();
        }

        void FadeOut(UIElement element)
        {
            if (element.Visibility != Visibility.Visible)
                return;
            if (element.ReadLocalValue(UIElement.OpacityProperty) != DependencyProperty.UnsetValue)
                return;

            DoubleAnimation fade = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250),
                FillBehavior = FillBehavior.Stop
            };

            fade.Completed += (s, e) =>
            {
                element.BeginAnimation(UIElement.OpacityProperty, null);
                element.Opacity = 1;
                element.Visibility = Visibility.Hidden;
            };

            element.BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private void PCI_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                SaveDataToTxt();
                FadeOut(buttontooltip);
            }
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && e.Key == Key.S)
            {
                SaveDataToTxt();
                FadeOut(buttontooltip);
                e.Handled = true;
            }
        }

        private async Task RunBenchmarkAsync(bool runMultithreadedByDefault)
        {
            singleBench.IsEnabled = false;
            multiBench.IsEnabled = false;
            lookresults.IsEnabled = false;
            mw.NavigationView_Root.IsEnabled = false;
            ssdComboBox.IsEnabled = false;
            videoComboBox.IsEnabled = false;
            ramStickComboBox.IsEnabled = false;

            const int benchmarkDurationMilliseconds = 10_000;
            var pci = MainWindow.Localization.LoadLocalization(Properties.Settings.Default.lang ?? "en", "pci");
            bool isMultithreaded = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) || runMultithreadedByDefault;

            benchmarkResultText.Text = isMultithreaded
                ? $"{pci["main"]["running_multicore"]}"
                : $"{pci["main"]["running"]}";

            var result = await Task.Run(() =>
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                long totalOps = 0;

                if (isMultithreaded)
                {
                    int threads = Environment.ProcessorCount;
                    long[] threadOps = new long[threads];

                    Parallel.For(0, threads, new ParallelOptions { MaxDegreeOfParallelism = threads }, i =>
                    {
                        double a = 1.000001 + i * 0.00001;
                        double b = 1.000002 + i * 0.00002;
                        long x = 1234567 + i;
                        long localOps = 0;
                        var rnd = new Random(i * 37 + Environment.TickCount);

                        while (stopwatch.ElapsedMilliseconds < benchmarkDurationMilliseconds)
                        {
                            for (int k = 0; k < 200_000; k++)
                            {
                                a = Math.Sin(a) * Math.Cos(b) + Math.Sqrt(Math.Abs(a + b));
                                b = a * 0.999999 + b * 0.000001 + rnd.NextDouble();
                                x = (x * 1664525 + 1013904223) & 0xFFFFFFFF;
                                localOps += 3;
                            }
                        }

                        threadOps[i] = localOps;
                    });

                    totalOps = threadOps.Sum();
                }
                else
                {
                    double a = 1.000001;
                    double b = 1.000002;
                    long x = 1234567;
                    long ops = 0;
                    var rnd = new Random(Environment.TickCount);

                    while (stopwatch.ElapsedMilliseconds < benchmarkDurationMilliseconds)
                    {
                        for (int k = 0; k < 200_000; k++)
                        {
                            a = Math.Sin(a) * Math.Cos(b) + Math.Sqrt(Math.Abs(a + b));
                            b = a * 0.999999 + b * 0.000001 + rnd.NextDouble();
                            x = (x * 1664525 + 1013904223) & 0xFFFFFFFF;
                            ops += 3;
                        }
                    }

                    totalOps = ops;
                }

                stopwatch.Stop();

                double seconds = stopwatch.Elapsed.TotalSeconds;
                double score = (totalOps / seconds) / 100000.0;

                return (score, stopwatch.ElapsedMilliseconds);
            });

            string scoreText = $"{result.score:N0}";

            benchmarkResultText.Text = isMultithreaded
                ? $"{pci["main"]["test1multi"]} {pci["main"]["test2"]} {scoreText} {pci["main"]["test3"]}"
                : $"{pci["main"]["test1"]} {pci["main"]["test2"]} {scoreText} {pci["main"]["test3"]}";

            string benchType = isMultithreaded ? "multi" : "single";

            if (isMultithreaded)
                _lastMultiScore = result.score;
            else
                _lastSingleScore = result.score;

            singleBench.IsEnabled = true;
            multiBench.IsEnabled = true;
            lookresults.IsEnabled = true;
            mw.NavigationView_Root.IsEnabled = true;
            ssdComboBox.IsEnabled = true;
            videoComboBox.IsEnabled = true;
            ramStickComboBox.IsEnabled = true;
        }

        private async void singleBench_Click(object sender, RoutedEventArgs e)
        {
            await RunBenchmarkAsync(false);
        }

        private async void multiBench_Click(object sender, RoutedEventArgs e)
        {
            await RunBenchmarkAsync(true);
        }

        private void LoadLang()
        {
            var languageCode = Properties.Settings.Default.lang ?? "en";
            _pci = MainWindow.Localization.LoadLocalization(languageCode, "pci");

            label.Text = _pci["main"]["label"];

            summaryCpuCard.Header = _pci["main"]["processorlabel"];
            summaryRamCard.Header = _pci["main"]["ramlabel"];
            summaryGpuCard.Header = _pci["main"]["vlabel"];
            summaryVramCard.Header = _pci["main"]["allvram"];
            bmanu.Header = _pci["main"]["devicemanu"];
            bmodel.Header = _pci["main"]["modeln"];

            cpuSection.Header = _pci["main"]["processorlabel"];
            cpul.Header = _pci["main"]["processorname"];
            cpucorel.Header = _pci["main"]["processorcores"];
            corespeedl.Header = _pci["main"]["processorfreq"];
            l3cashl.Header = _pci["main"]["processorcache"];

            motherboardSectionEx.Header = _pci["main"]["mblabel"];
            mbnamel.Header = _pci["main"]["mbname"];
            biosverl.Header = _pci["main"]["mbver"];
            biosdatel.Header = _pci["main"]["mbdate"];

            gpuSectionEx.Header = _pci["main"]["vlabel"];
            videol.Header = _pci["main"]["vname"];
            vraml.Header = _pci["main"]["vmem"];

            storageSection.Header = _pci["main"]["ssdl"];

            benchmarkLabel.Text = _pci["main"]["benchtitle"];
            singleBench.Content = _pci["main"]["benchbutton"];
            multiBench.Content = _pci["main"]["benchbutton2"];
            benchmarkResultText.Text = _pci["main"]["benchtip"];
            lookresults.Content = _pci["main"]["lookresulbutton"];
            tpml.Header = _pci["main"]["tpmtitle"];

            ramStickSection.Header = _pci["main"]["ramsticktitle"];
            ramsmanu.Header = _pci["main"]["manu"];
            capacram.Header = _pci["main"]["capac"];
            partnuml.Header = _pci["main"]["partnum"];

            ssdnl.Header = _pci["main"]["sname"];
            ssdcl.Header = _pci["main"]["capac"];

            pci_tooltip.Content = _pci["main"]["tooltip"];
        }

        private void ShowTpmStatus(bool enabled)
        {
            if (_pci == null)
            {
                tpmStatus.Text = enabled ? "Enabled" : "Disabled";
                return;
            }

            tpmStatus.Text = enabled
                ? _pci["main"]["tpmy"]
                : _pci["main"]["tpmn"];
        }

        private void ShowCpuInfo()
        {
            try
            {
                string cpuName = "Unknown";
                int coreCount = 0;
                int threadCount = 0;
                using (var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, ThreadCount FROM Win32_Processor"))
                using (var results = searcher.Get())
                {
                    foreach (var item in results)
                    {
                        cpuName = item["Name"]?.ToString()?.Trim() ?? cpuName;
                        coreCount += Convert.ToInt32(item["NumberOfCores"] ?? 0);

                        if (item["ThreadCount"] != null)
                        {
                            threadCount += Convert.ToInt32(item["ThreadCount"]);
                        }
                        item.Dispose();
                    }
                }

                if (threadCount == 0 || threadCount == coreCount)
                {
                    using (var searcherCS = new ManagementObjectSearcher("SELECT NumberOfLogicalProcessors FROM Win32_ComputerSystem"))
                    using (var resultsCS = searcherCS.Get())
                    {
                        foreach (var item in resultsCS)
                        {
                            if (item["NumberOfLogicalProcessors"] != null)
                            {
                                int csThreads = Convert.ToInt32(item["NumberOfLogicalProcessors"]);
                                if (csThreads > threadCount)
                                {
                                    threadCount = csThreads;
                                }
                            }
                            item.Dispose();
                        }
                    }
                }

                if (threadCount == 0)
                {
                    threadCount = Environment.ProcessorCount;
                }

                Dispatcher.Invoke(() => {
                    cpue.Text = cpuName;
                    summaryCpuText.Text = cpuName;
                    cpucore.Text = coreCount.ToString();
                    threads.Text = threadCount.ToString();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => {
                    cpue.Text = "Error reading CPU";
                    cpucore.Text = "N/A";
                    threads.Text = "N/A";
                });
            }
        }

        private void ShowCpuExtraInfo()
        {
            try
            {
                int maxClockSpeed = 0;
                int l3Cache = 0;

                using (var searcher = new ManagementObjectSearcher("select MaxClockSpeed, L3CacheSize from Win32_Processor"))
                {
                    foreach (var item in searcher.Get())
                    {
                        maxClockSpeed = Convert.ToInt32(item["MaxClockSpeed"]);
                        l3Cache += Convert.ToInt32(item["L3CacheSize"]);
                    }
                }

                double l3MB = Math.Round(l3Cache / 1024.0, 1);
                double maxGHz = Math.Round(maxClockSpeed / 1000.0, 2);

                corespeed.Text = $"{maxGHz} GHz";
                l3cash.Text = $"{l3MB} MB";
            }
            catch (Exception ex)
            {
                corespeed.Text = $"{ex.Message}";
                l3cash.Text = "N/A";
            }
        }
        private void ShowRamInfo()
        {
            try
            {
                ulong totalBytes = 0;
                int memoryTypeCode = 0;
                int averageSpeed = 0;
                int stickCount = 0;

                using (var searcher = new ManagementObjectSearcher("SELECT Capacity, MemoryType, SMBIOSMemoryType, Speed FROM Win32_PhysicalMemory"))
                {
                    using (var results = searcher.Get())
                    {
                        foreach (ManagementObject item in results)
                        {
                            try
                            {
                                if (item["Capacity"] != null)
                                    totalBytes += (ulong)item["Capacity"];

                                int smbios = item["SMBIOSMemoryType"] != null ? Convert.ToInt32(item["SMBIOSMemoryType"]) : 0;
                                int legacy = item["MemoryType"] != null ? Convert.ToInt32(item["MemoryType"]) : 0;

                                if (item["Speed"] != null)
                                {
                                    averageSpeed += Convert.ToInt32(item["Speed"]);
                                    stickCount++;
                                }

                                int detectedType = (smbios > 2) ? smbios : (legacy > 2 ? legacy : 0);
                                if (memoryTypeCode == 0 && detectedType != 0)
                                    memoryTypeCode = detectedType;
                            }
                            finally
                            {
                                item.Dispose();
                            }
                        }
                    }
                }

                if (totalBytes == 0)
                {
                    return;
                }

                double totalGB = totalBytes / (1024.0 * 1024 * 1024);
                string memoryType = "N/A";

                if (memoryTypeCode > 0)
                {
                    memoryType = memoryTypeCode switch
                    {
                        20 => "DDR",
                        21 => "DDR2",
                        22 => "DDR2 FB-DIMM",
                        24 => "DDR3",
                        26 => "DDR4",
                        27 => "LPDDR",
                        28 => "LPDDR2",
                        29 => "LPDDR3",
                        30 => "LPDDR4",
                        32 => "HBM",
                        33 => "HBM2",
                        34 => "DDR5",
                        35 => "LPDDR5",
                        36 => "HBM3",
                        _ => "N/A"
                    };
                }

                if (memoryType == "N/A" && stickCount > 0)
                {
                    int speed = averageSpeed / stickCount;

                    if (speed >= 4800) memoryType = "DDR5";
                    else if (speed >= 2133) memoryType = "DDR4";
                    else if (speed >= 800) memoryType = "DDR3";
                    else if (speed >= 400) memoryType = "DDR2";
                    else if (speed > 0) memoryType = "DDR";
                }

                summaryRamText.Text = $"{Math.Round(totalGB)} GB / {memoryType}";
            }
            catch
            {
            }
        }

        private void ShowMotherboardInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Product, Manufacturer FROM Win32_BaseBoard"))
                {
                    foreach (var item in searcher.Get())
                    {
                        string product = item["Product"]?.ToString() ?? "Unknown";
                        string manufacturer = item["Manufacturer"]?.ToString() ?? "Unknown";

                        string fullName = $"{manufacturer} {product}";
                        mbname.Text = WrapAfterWords(fullName, 5);
                    }
                }

                using (var searcher = new ManagementObjectSearcher("SELECT SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS"))
                {
                    foreach (var item in searcher.Get())
                    {
                        string biosVersion = item["SMBIOSBIOSVersion"]?.ToString() ?? "Unknown";

                        string biosDateRaw = item["ReleaseDate"]?.ToString() ?? "";
                        string biosDateFormatted = "Unknown";
                        if (!string.IsNullOrEmpty(biosDateRaw) && biosDateRaw.Length >= 8)
                        {
                            string year = biosDateRaw.Substring(0, 4);
                            string month = biosDateRaw.Substring(4, 2);
                            string day = biosDateRaw.Substring(6, 2);
                            biosDateFormatted = $"{day}.{month}.{year}";
                        }

                        biosver.Text = biosVersion;
                        biosdate.Text = biosDateFormatted;
                    }
                }
            }
            catch (Exception ex)
            {
                mbname.Text = $"{ex.Message}";
                biosver.Text = "N/A";
                biosdate.Text = "N/A";
            }
        }

        private void LoadStorageList()
        {
            try
            {
                _storageDevices = StorageHelper.GetAllStorageDevices()
                    .OrderByDescending(d => d.CapacityBytes)
                    .ToList();

                ssdComboBox.Items.Clear();
                if (_storageDevices.Count == 0)
                {
                    ssdnValue.Text = "N/A";
                    ssdcValue.Text = "N/A";
                    ssdComboBoxCard.Visibility = Visibility.Collapsed;
                    return;
                }

                if (_storageDevices.Count <= 1)
                    ssdComboBoxCard.Visibility = Visibility.Collapsed;
                else
                    ssdComboBoxCard.Visibility = Visibility.Visible;

                for (int i = 0; i < _storageDevices.Count; i++)
                {
                    string displayName = !string.IsNullOrWhiteSpace(_storageDevices[i].Name)
                        ? _storageDevices[i].Name
                        : $"Drive #{i + 1}";
                    ssdComboBox.Items.Add($"{i + 1}. {displayName}");
                }

                ssdComboBox.SelectedIndex = 0;
                UpdateStorageInfo(0);
            }
            catch (Exception ex)
            {
                ssdnValue.Text = "N/A";
                ssdcValue.Text = "N/A";
                ssdComboBoxCard.Visibility = Visibility.Collapsed;
            }
        }

        private void SSDComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ssdComboBox.SelectedIndex >= 0)
            {
                UpdateStorageInfo(ssdComboBox.SelectedIndex);
            }
        }

        private void UpdateStorageInfo(int index)
        {
            if (index < 0 || index >= _storageDevices.Count) return;

            var storage = _storageDevices[index];
            ssdnValue.Text = storage.Name;
            ssdcValue.Text = storage.CapacityFormatted;
        }

        private void LoadGpuList()
        {
            try
            {
                _gpus = GpuHelper.GetAllGpus()
                    .OrderByDescending(g => g.VRamBytes)
                    .ToList();

                videoComboBox.Items.Clear();
                if (_gpus.Count == 0)
                {
                    videon.Text = "N/A";
                    vram.Text = "N/A";
                    videoComboBoxCard.Visibility = Visibility.Collapsed;
                    return;
                }

                if (_gpus.Count <= 1)
                    videoComboBoxCard.Visibility = Visibility.Collapsed;
                else
                    videoComboBoxCard.Visibility = Visibility.Visible;

                for (int i = 0; i < _gpus.Count; i++)
                {
                    string displayName = !string.IsNullOrWhiteSpace(_gpus[i].Name)
                        ? _gpus[i].Name
                        : $"GPU #{i + 1}";
                    videoComboBox.Items.Add($"{i + 1}. {displayName}");
                }

                int maxIndex = _gpus
                    .Select((gpu, index) => new { gpu, index })
                    .OrderByDescending(x => x.gpu.VRamBytes)
                    .First().index;

                videoComboBox.SelectedIndex = maxIndex;
                summaryGpuText.Text = _gpus[maxIndex].Name;

                ulong totalVram = (ulong)_gpus.Sum(g => (long)g.VRamBytes);
                summaryVramText.Text = GpuInfo.FormatBytes(totalVram);
                UpdateGpuInfo(maxIndex);
            }
            catch (Exception ex)
            {
                videon.Text = "N/A";
                vram.Text = "N/A";
                videoComboBoxCard.Visibility = Visibility.Collapsed;
            }
        }

        private void VideoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (videoComboBox.SelectedIndex >= 0)
            {
                UpdateGpuInfo(videoComboBox.SelectedIndex);
            }
        }

        private void UpdateGpuInfo(int index)
        {
            if (index < 0 || index >= _gpus.Count) return;

            var gpu = _gpus[index];
            videon.Text = gpu.Name;
            vram.Text = gpu.VRamFormatted;
        }

        private void LookResults_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://adderly.top/makubench",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show($"Default Browser Error.", "MakuTweaker", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowComputerInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Manufacturer, Model FROM Win32_ComputerSystem");

                var item = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

                string manufacturer = item?["Manufacturer"]?.ToString();
                string model = item?["Model"]?.ToString();

                string[] invalidModels = { "System Product Name", "To Be Filled By O.E.M." };

                bool invalid = string.IsNullOrWhiteSpace(manufacturer) ||
                               string.IsNullOrWhiteSpace(model) ||
                               manufacturer.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ||
                               model.Equals("Unknown", StringComparison.OrdinalIgnoreCase) ||
                               invalidModels.Any(x => model.Equals(x, StringComparison.OrdinalIgnoreCase));

                if (!invalid)
                {
                    pcManufacturer.Text = manufacturer;
                    pcModel.Text = model;
                    bmanu.Visibility = Visibility.Visible;
                    bmodel.Visibility = Visibility.Visible;
                }
                else
                {
                    bmanu.Visibility = Visibility.Collapsed;
                    bmodel.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                bmanu.Visibility = Visibility.Collapsed;
                bmodel.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowSecurityInfo()
        {
            ShowTpm();
        }

        private void ShowTpm()
        {
            bool tpmFoundAndEnabled = false;

            try
            {
                var scope = new ManagementScope(
                    @"\\.\root\cimv2\security\microsofttpm");
                scope.Connect();

                using var searcher = new ManagementObjectSearcher(
                    scope,
                    new ObjectQuery("SELECT IsEnabled_InitialValue FROM Win32_Tpm"));

                foreach (var item in searcher.Get())
                {
                    tpmFoundAndEnabled = Convert.ToBoolean(item["IsEnabled_InitialValue"] ?? false);
                    break;
                }
            }
            catch
            {
            }
            ShowTpmStatus(tpmFoundAndEnabled);
        }

        private void LoadRamSticks()
        {
            try
            {
                var tempSticks = new List<RamStickInfo>();

                using (var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Capacity, Speed, PartNumber FROM Win32_PhysicalMemory"))
                using (var results = searcher.Get())
                {
                    foreach (var item in results)
                    {
                        tempSticks.Add(new RamStickInfo
                        {
                            Manufacturer = item["Manufacturer"]?.ToString()?.Trim() ?? "N/A",
                            CapacityBytes = Convert.ToUInt64(item["Capacity"] ?? 0),
                            Speed = Convert.ToInt32(item["Speed"] ?? 0),
                            PartNumber = item["PartNumber"]?.ToString()?.Trim() ?? "N/A"
                        });
                        item.Dispose();
                    }
                }

                Dispatcher.Invoke(() => {
                    _ramSticks = tempSticks;
                    ramStickComboBox.Items.Clear();

                    if (_ramSticks.Count <= 1)
                        ramStickComboBoxCard.Visibility = Visibility.Collapsed;
                    else
                        ramStickComboBoxCard.Visibility = Visibility.Visible;

                    int i = 1;
                    foreach (var stick in _ramSticks)
                    {
                        ramStickComboBox.Items.Add($"{i}. {stick.CapacityFormatted} — {stick.Manufacturer}");
                        i++;
                    }

                    if (_ramSticks.Count > 0)
                    {
                        ramStickComboBox.SelectedIndex = 0;
                        UpdateRamStickInfo(0);
                    }
                    else
                    {
                        ramStickComboBoxCard.Visibility = Visibility.Collapsed;
                    }
                });
            }
            catch
            {
                Dispatcher.Invoke(() => ramStickManufacturer.Text = "Error");
                ramStickComboBoxCard.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateRamStickInfo(int index)
        {
            if (index < 0 || index >= _ramSticks.Count) return;

            var stick = _ramSticks[index];

            ramStickManufacturer.Text = stick.Manufacturer;
            ramStickCapacity.Text = stick.CapacityFormatted;
            ramStickPart.Text = stick.PartNumber;
        }

        private void SaveDataToTxt()
        {
            try
            {
                var pci = MainWindow.Localization.LoadLocalization(
                    Properties.Settings.Default.lang ?? "en", "pci");

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "TXT File| *.txt",
                    Title = "MakuTweaker",
                    FileName = "MakuTweaker System Info.txt"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine("MakuTweaker // MarkAdderly");
                    sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    sb.AppendLine();

                    sb.AppendLine($"{pci["main"]["manu"]} {pcManufacturer.Text}");
                    sb.AppendLine($"{pci["main"]["modeln"]} {pcModel.Text}");
                    sb.AppendLine();

                    sb.AppendLine($"=== {pci["main"]["processorlabel"]} ===");
                    sb.AppendLine($"{pci["main"]["processorname"]} {cpue.Text}");
                    sb.AppendLine($"{pci["main"]["processorcores"]} {cpucore.Text}");
                    sb.AppendLine($"{pci["main"]["processorfreq"]} {corespeed.Text}");
                    sb.AppendLine($"{pci["main"]["processorcache"]} {l3cash.Text}");
                    sb.AppendLine();

                    sb.AppendLine($"=== {pci["main"]["ramlabel"]} ===");
                    sb.AppendLine($"{pci["main"]["ramtotal"]} {summaryRamText.Text}");
                    sb.AppendLine();

                    sb.AppendLine($"=== {pci["main"]["mblabel"]} ===");
                    sb.AppendLine($"{pci["main"]["mbname"]} {mbname.Text}");
                    sb.AppendLine($"{pci["main"]["mbver"]} {biosver.Text}");
                    sb.AppendLine($"{pci["main"]["mbdate"]} {biosdate.Text}");
                    sb.AppendLine($"{pci["main"]["tpmtitle"]} {tpmStatus.Text}");
                    sb.AppendLine();
                    sb.AppendLine();

                    sb.AppendLine($"=== {pci["main"]["ramsticktitle"]} ===");

                    if (_ramSticks.Count == 0)
                    {
                        sb.AppendLine("N/A");
                    }
                    else
                    {
                        for (int i = 0; i < _ramSticks.Count; i++)
                        {
                            var stick = _ramSticks[i];

                            sb.AppendLine($"[{i + 1}]");
                            sb.AppendLine($"{pci["main"]["manu"]} {stick.Manufacturer}");
                            sb.AppendLine($"{pci["main"]["capac"]} {stick.CapacityFormatted}");
                            sb.AppendLine($"{pci["main"]["partnum"]} {stick.PartNumber}");
                            sb.AppendLine();
                        }
                    }

                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine();

                    sb.AppendLine($"=== {pci["main"]["vlabel"]} ===");

                    if (_gpus.Count == 0)
                    {
                        sb.AppendLine("No GPU found");
                    }
                    else
                    {
                        for (int i = 0; i < _gpus.Count; i++)
                        {
                            var gpu = _gpus[i];

                            sb.AppendLine($"[{i + 1}] {gpu.Name}");
                            sb.AppendLine($"{pci["main"]["vmem"]} {gpu.VRamFormatted}");
                            sb.AppendLine();
                        }
                    }

                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine();

                    sb.AppendLine($"=== {pci["main"]["ssdl"]} ===");

                    if (_storageDevices.Count == 0)
                    {
                        sb.AppendLine("N/A");
                    }
                    else
                    {
                        for (int i = 0; i < _storageDevices.Count; i++)
                        {
                            var storage = _storageDevices[i];

                            sb.AppendLine($"[{i + 1}] {storage.Name}");
                            sb.AppendLine($"{pci["main"]["smem"]} {storage.CapacityFormatted}");
                            sb.AppendLine();
                        }
                    }

                    sb.AppendLine();

                    File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);

                    iNKORE.UI.WPF.Modern.Controls.MessageBox.Show("System information saved successfully!\nСистемная информация была успешно сохранена!", "MakuTweaker", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                iNKORE.UI.WPF.Modern.Controls.MessageBox.Show($"{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ramStickComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ramStickComboBox.SelectedIndex >= 0)
                UpdateRamStickInfo(ramStickComboBox.SelectedIndex);
        }

        string WrapAfterWords(string text, int wordsPerLine = 5)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var words = text.Split(' ');
            var lines = new List<string>();

            for (int i = 0; i < words.Length; i += wordsPerLine)
            {
                lines.Add(string.Join(" ", words.Skip(i).Take(wordsPerLine)));
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    public class GpuInfo
    {
        public string Name { get; set; } = string.Empty;
        public ulong VRamBytes { get; set; }
        public string VRamFormatted => FormatBytes(VRamBytes);
        public static string FormatBytes(ulong bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
    public static class GpuHelper
    {
        public static List<GpuInfo> GetAllGpus()
        {
            var gpus = new List<GpuInfo>();

            try
            {
                using var factory = Vortice.DXGI.DXGI.CreateDXGIFactory1<Vortice.DXGI.IDXGIFactory1>();
                int i = 0;
                while (true)
                {
                    try
                    {
                        factory.EnumAdapters1((uint)i, out Vortice.DXGI.IDXGIAdapter1? adapter);
                        if (adapter == null)
                            break;

                        using (adapter)
                        {
                            var desc = adapter.Description1;
                            string name = desc.Description?.Trim() ?? "";

                            if (name.Contains("Microsoft Basic Render Driver", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("spacedesk", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Parsec", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Virtual Desktop", StringComparison.OrdinalIgnoreCase) ||
                                name.Contains("Apollo", StringComparison.OrdinalIgnoreCase))
                            {
                                i++;
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(name) || name.Equals("Null", StringComparison.OrdinalIgnoreCase))
                            {
                                i++;
                                continue;
                            }

                            if ((desc.Flags & Vortice.DXGI.AdapterFlags.Software) != 0)
                            {
                                i++;
                                continue;
                            }

                            if (gpus.Any(g => g.Name == name && g.VRamBytes == desc.DedicatedVideoMemory))
                            {
                                i++;
                                continue;
                            }

                            gpus.Add(new GpuInfo
                            {
                                Name = name,
                                VRamBytes = desc.DedicatedVideoMemory
                            });
                        }

                        i++;
                    }
                    catch
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                return FallbackToWmi();
            }

            return gpus.Count > 0 ? gpus : FallbackToWmi();
        }

        private static List<GpuInfo> FallbackToWmi()
        {
            var gpus = new List<GpuInfo>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
                foreach (var obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "Unknown GPU";
                    ulong vram = obj["AdapterRAM"] != null ? Convert.ToUInt64(obj["AdapterRAM"]) : 0;

                    if (name.Contains("spacedesk", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Parsec", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Virtual Desktop", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!gpus.Any(g => g.Name == name && g.VRamBytes == vram))
                    {
                        gpus.Add(new GpuInfo { Name = name, VRamBytes = vram });
                    }
                }
            }
            catch { }
            return gpus;
        }
    }

    public class StorageInfo
    {
        public string Name { get; set; } = string.Empty;
        public ulong CapacityBytes { get; set; }
        public string CapacityFormatted => FormatBytes(CapacityBytes);

        private static string FormatBytes(ulong bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            if (bytes == 0)
            {
                return ("N/A");
            }
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public class RamStickInfo
    {
        public string Manufacturer { get; set; } = "";
        public ulong CapacityBytes { get; set; }
        public int Speed { get; set; }
        public string PartNumber { get; set; } = "";

        public string CapacityFormatted =>
            $"{Math.Round(CapacityBytes / (1024.0 * 1024 * 1024), 1)} GB";
    }

    public static class StorageHelper
    {
        public static List<StorageInfo> GetAllStorageDevices()
        {
            var devices = new List<StorageInfo>();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Caption, Size FROM Win32_DiskDrive");

                foreach (var obj in searcher.Get())
                {
                    string name = obj["Caption"]?.ToString() ?? "Unknown Device";
                    ulong size = obj["Size"] != null ? Convert.ToUInt64(obj["Size"]) : 0;

                    if (size == 0 ||
                        name.Contains("Virtual", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("iSCSI", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    devices.Add(new StorageInfo
                    {
                        Name = name,
                        CapacityBytes = size
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            return devices;
        }
    }
}