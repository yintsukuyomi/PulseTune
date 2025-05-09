using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Shapes;
using PulseTune.Backend;
using PulseTune.Backend.Models;
using PulseTune.Backend.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;

namespace PulseTune.UI
{
    public partial class MainWindow : Window
    {
        private SystemMonitor monitor;
        private StartupManager startupManager;
        private ServiceManager serviceManager;
        private ProfileManager profileManager;
        private DispatcherTimer updateTimer;
        private bool autoUpdateEnabled = false;
        private bool isUpdating = false;
        
        // Grafik çizimi için listeleri
        private List<double> cpuHistory = new List<double>();
        private List<double> ramHistory = new List<double>();
        private List<double> diskHistory = new List<double>();
        private List<double> networkHistory = new List<double>();
        private const int MAX_HISTORY_POINTS = 60; // 60 veri noktası saklayacağız
        
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }
        
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Uygulama başlatılıyor...";
                
                // Gerekli servisleri başlat
                monitor = new SystemMonitor();
                startupManager = new StartupManager();
                serviceManager = new ServiceManager();
                profileManager = new ProfileManager();
                
                // Zamanlayıcı oluştur
                updateTimer = new DispatcherTimer();
                updateTimer.Interval = TimeSpan.FromSeconds(2);
                updateTimer.Tick += Timer_Tick;
                
                // İlk verileri yükle
                StatusText.Text = "Sistem bilgileri yükleniyor...";
                await UpdateStatsAsync();
                
                // Başlangıç programlarını ve servisleri yükle
                await LoadStartupItemsAsync();
                await LoadServicesAsync();
                
                // Otomatik güncellemeyi başlat
                autoUpdateEnabled = true;
                updateTimer.Start();
                AutoUpdateButton.Content = "Oto Güncellemeyi Durdur";
                
                StatusText.Text = "Uygulama hazır";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Uygulama başlatılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            await UpdateStatsAsync();
        }

        private async void UpdateStats(object sender, RoutedEventArgs e)
        {
            await UpdateStatsAsync();
        }
        
        private async Task UpdateStatsAsync()
        {
            if (isUpdating) return; // Önceki güncelleme henüz tamamlanmadıysa çık
            
            isUpdating = true;
            
            try
            {
                // Tüm verileri paralel olarak topla
                var cpuTask = monitor.GetCpuUsageAsync();
                var ramTask = monitor.GetRamUsageAsync();
                var diskTask = monitor.GetDiskUsageAsync();
                var networkTask = monitor.GetNetworkUsageAsync();
                var processesTask = monitor.GetTopProcessesByCpuAsync();
                
                // Tüm görevler tamamlanana kadar bekle
                await Task.WhenAll(cpuTask, ramTask, diskTask, networkTask, processesTask);
                
                // UI'yi güncelle
                float cpuUsage = await cpuTask;
                float ramUsage = await ramTask;
                float diskUsage = await diskTask;
                NetworkUsage networkUsage = await networkTask;
                
                CpuUsageText.Text = $"Kullanım: {cpuUsage:F1}%";
                RamUsageText.Text = $"Kullanım: {ramUsage:F1}%";
                DiskUsageText.Text = $"Kullanım: {diskUsage:F1}%";
                NetworkUsageText.Text = $"Kullanım: {networkUsage.BytesPerSecond / 1024:F1} KB/s";
                
                // Grafik verilerini güncelle
                UpdateGraphData(cpuUsage, ramUsage, diskUsage, networkUsage.BytesPerSecond / 1024);
                DrawGraphs();
                
                // İşlem listesini güncelle
                var processes = await processesTask;
                if (processes != null)
                {
                    ProcessListView.ItemsSource = null;
                    ProcessListView.ItemsSource = processes;
                }
                
                // Optimizasyon önerilerini güncelle
                var tips = monitor.GetSystemOptimizationTips();
                TipsListBox.ItemsSource = tips ?? new List<string>();
                
                StatusText.Text = "Son güncelleme: " + DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Güncelleme hatası: {ex.Message}");
                StatusText.Text = "Hata: Sistem bilgileri alınamadı";
            }
            finally
            {
                isUpdating = false;
            }
        }
        
        private void UpdateGraphData(double cpu, double ram, double disk, double network)
        {
            // CPU verisi ekle
            cpuHistory.Add(cpu);
            if (cpuHistory.Count > MAX_HISTORY_POINTS)
                cpuHistory.RemoveAt(0);
                
            // RAM verisi ekle
            ramHistory.Add(ram);
            if (ramHistory.Count > MAX_HISTORY_POINTS)
                ramHistory.RemoveAt(0);
                
            // Disk verisi ekle
            diskHistory.Add(disk);
            if (diskHistory.Count > MAX_HISTORY_POINTS)
                diskHistory.RemoveAt(0);
                
            // Ağ verisi ekle (basit bir normalizasyon uygulayarak)
            double normalizedNetwork = Math.Min(network, 1000) / 10; // 0-100 arası bir değere normalize et
            networkHistory.Add(normalizedNetwork);
            if (networkHistory.Count > MAX_HISTORY_POINTS)
                networkHistory.RemoveAt(0);
        }
        
        private void DrawGraphs()
        {
            // Canvas'ları temizle
            CpuCanvas.Children.Clear();
            RamCanvas.Children.Clear();
            DiskCanvas.Children.Clear();
            NetworkCanvas.Children.Clear();
            
            // Grafikleri çiz
            DrawLineGraph(CpuCanvas, cpuHistory, Colors.Red);
            DrawLineGraph(RamCanvas, ramHistory, Colors.Green);
            DrawLineGraph(DiskCanvas, diskHistory, Colors.Blue);
            DrawLineGraph(NetworkCanvas, networkHistory, Colors.Yellow);
        }
        
        private void DrawLineGraph(Canvas canvas, List<double> data, Color color)
        {
            if (data == null || data.Count < 2 || canvas.ActualWidth < 10 || canvas.ActualHeight < 10)
                return;
            
            double canvasHeight = canvas.ActualHeight;
            double canvasWidth = canvas.ActualWidth;
            
            Polyline polyline = new Polyline();
            polyline.Stroke = new SolidColorBrush(color);
            polyline.StrokeThickness = 1;
            
            double xStep = canvasWidth / (MAX_HISTORY_POINTS - 1);
            
            for (int i = 0; i < data.Count; i++)
            {
                double x = i * xStep;
                double normalizedValue = Math.Min(data[i], 100) / 100; // 0-1 arası değere dönüştür
                double y = canvasHeight - (canvasHeight * normalizedValue);
                
                polyline.Points.Add(new Point(x, y));
            }
            
            canvas.Children.Add(polyline);
            
            // Yatay referans çizgileri ekle (10%, 50%, %90)
            foreach (double percent in new[] { 0.1, 0.5, 0.9 })
            {
                Line line = new Line();
                line.X1 = 0;
                line.X2 = canvasWidth;
                line.Y1 = line.Y2 = canvasHeight - (canvasHeight * percent);
                line.Stroke = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
                line.StrokeThickness = 0.5;
                
                canvas.Children.Add(line);
            }
        }
        
        private async Task LoadStartupItemsAsync()
        {
            try
            {
                var startupItems = await Task.Run(() => startupManager.GetStartupItems());
                StartupItemsListView.ItemsSource = startupItems;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Başlangıç öğeleri yüklenirken hata: {ex.Message}");
            }
        }
        
        private async Task LoadServicesAsync()
        {
            try
            {
                var services = await Task.Run(() => serviceManager.GetServices());
                ServicesListView.ItemsSource = services;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Servisler yüklenirken hata: {ex.Message}");
            }
        }
        
        private void ToggleAutoUpdate(object sender, RoutedEventArgs e)
        {
            autoUpdateEnabled = !autoUpdateEnabled;
            
            if (autoUpdateEnabled)
            {
                updateTimer.Start();
                AutoUpdateButton.Content = "Oto Güncellemeyi Durdur";
            }
            else
            {
                updateTimer.Stop();
                AutoUpdateButton.Content = "Oto Güncelleme";
            }
        }
        
        private void EndProcess_Click(object sender, RoutedEventArgs e)
        {
            var selectedProcess = ProcessListView.SelectedItem as ProcessInfo;
            if (selectedProcess != null)
            {
                try
                {
                    monitor.KillProcess(selectedProcess.Id);
                    MessageBox.Show($"{selectedProcess.Name} işlemi sonlandırıldı.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"İşlem sonlandırılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void ProcessDetails_Click(object sender, RoutedEventArgs e)
        {
            var selectedProcess = ProcessListView.SelectedItem as ProcessInfo;
            if (selectedProcess != null)
            {
                MessageBox.Show($"İşlem Detayları:\nİsim: {selectedProcess.Name}\nID: {selectedProcess.Id}\nCPU Kullanımı: {selectedProcess.CpuUsage:F1}%\nBellek Kullanımı: {selectedProcess.MemoryMB:F1} MB", 
                    "İşlem Detayları", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private async void EnableStartup_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = StartupItemsListView.SelectedItem as StartupItem;
            if (selectedItem != null)
            {
                await Task.Run(() => startupManager.EnableStartupItem(selectedItem));
                await LoadStartupItemsAsync();
            }
        }
        
        private async void DisableStartup_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = StartupItemsListView.SelectedItem as StartupItem;
            if (selectedItem != null)
            {
                await Task.Run(() => startupManager.DisableStartupItem(selectedItem));
                await LoadStartupItemsAsync();
            }
        }
        
        private async void RefreshStartup_Click(object sender, RoutedEventArgs e)
        {
            await LoadStartupItemsAsync();
        }
        
        private async void StartService_Click(object sender, RoutedEventArgs e)
        {
            var selectedService = ServicesListView.SelectedItem as ServiceInfo;
            if (selectedService != null)
            {
                try
                {
                    await Task.Run(() => serviceManager.StartService(selectedService.Name));
                    await LoadServicesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Servis başlatılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private async void StopService_Click(object sender, RoutedEventArgs e)
        {
            var selectedService = ServicesListView.SelectedItem as ServiceInfo;
            if (selectedService != null)
            {
                try
                {
                    await Task.Run(() => serviceManager.StopService(selectedService.Name));
                    await LoadServicesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Servis durdurulamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private async void RefreshServices_Click(object sender, RoutedEventArgs e)
        {
            await LoadServicesAsync();
        }
        
        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (profileManager == null) return;
            
            int selectedIndex = ProfileComboBox.SelectedIndex;
            string profileName = "Normal";
            
            switch (selectedIndex)
            {
                case 0: profileName = "Normal"; break;
                case 1: profileName = "Gaming"; break;
                case 2: profileName = "PowerSaver"; break;
                case 3: profileName = "Office"; break;
            }
            
            profileManager.ApplyProfile(profileName);
            StatusText.Text = $"{profileName} profili uygulandı";
        }
        
        private async void QuickScan_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Hızlı tarama yapılıyor...";
            
            try
            {
                // Tarama yap
                var scanResults = await Task.Run(() => monitor.PerformQuickScan());
                
                // Sonuçları göster
                if (scanResults.Count > 0)
                {
                    string resultMessage = "Tarama sonuçları:\n\n";
                    foreach (var result in scanResults)
                    {
                        resultMessage += $"• {result}\n";
                    }
                    
                    MessageBox.Show(resultMessage, "Tarama Sonuçları", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Sistemde herhangi bir optimizasyon sorunu bulunamadı.", "Tarama Sonuçları", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                
                StatusText.Text = "Tarama tamamlandı";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Tarama başarısız oldu";
                MessageBox.Show($"Tarama sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
