using System;
using System.Diagnostics;
using System.Management;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using PulseTune.Backend.Models;
using PulseTune.Backend.Services;

namespace PulseTune.Backend
{
    public class SystemMonitor
    {
        // Performans sayaçlarını sınıf düzeyinde önbelleğe alma
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramCounter;
        private float _totalRamMB = 0;
        private Dictionary<string, PerformanceCounter> _processCounters = new Dictionary<string, PerformanceCounter>();
        
        // Ağ istatistikleri için
        private long _lastBytesReceived = 0;
        private long _lastBytesSent = 0;
        private DateTime _lastNetworkCheck = DateTime.Now;
        private NetworkInterface _activeNetworkInterface = null;
        
        // Servis yöneticisi
        private ServiceManager _serviceManager;
        
        public SystemMonitor()
        {
            InitializeCounters();
            // Toplam RAM'i yalnızca bir kez hesapla
            _totalRamMB = GetTotalPhysicalMemory();
            
            // En aktif ağ arayüzünü bul
            FindMostActiveNetworkInterface();
            
            // Servis yöneticisini başlat
            _serviceManager = new ServiceManager();
        }
        
        private void InitializeCounters()
        {
            try 
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                _cpuCounter.NextValue(); // İlk değer sıfır olacak
                
                _ramCounter = new PerformanceCounter("Memory", "Available MBytes", true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Performans sayaçları başlatılamadı: {ex.Message}");
            }
        }
        
        private void FindMostActiveNetworkInterface()
        {
            try
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                
                _activeNetworkInterface = interfaces
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                           (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                            ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                    .OrderByDescending(ni => ni.GetIPv4Statistics().BytesReceived + ni.GetIPv4Statistics().BytesSent)
                    .FirstOrDefault();
                    
                if (_activeNetworkInterface != null)
                {
                    var stats = _activeNetworkInterface.GetIPv4Statistics();
                    _lastBytesReceived = stats.BytesReceived;
                    _lastBytesSent = stats.BytesSent;
                    _lastNetworkCheck = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ağ arayüzleri alınamadı: {ex.Message}");
            }
        }
        
        public async Task<float> GetCpuUsageAsync()
        {
            return await Task.Run(() => {
                try 
                {
                    if (_cpuCounter == null)
                        return 0;
                    
                    return _cpuCounter.NextValue();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CPU kullanımı alınamadı: {ex.Message}");
                    return 0;
                }
            });
        }
        
        public async Task<float> GetRamUsageAsync()
        {
            return await Task.Run(() => {
                try
                {
                    if (_ramCounter == null || _totalRamMB <= 0)
                        return 0;
                    
                    float availableRam = _ramCounter.NextValue();
                    return ((_totalRamMB - availableRam) / _totalRamMB) * 100;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"RAM kullanımı alınamadı: {ex.Message}");
                    return 0;
                }
            });
        }
        
        // Geriye dönük uyumluluk için senkron metotları da tutalım
        public float GetCpuUsage()
        {
            try
            {
                if (_cpuCounter == null)
                    return 0;
                
                return _cpuCounter.NextValue();
            }
            catch 
            {
                return 0; 
            }
        }
        
        public float GetRamUsage()
        {
            try
            {
                if (_ramCounter == null || _totalRamMB <= 0)
                    return 0;
                
                float availableRam = _ramCounter.NextValue();
                return ((_totalRamMB - availableRam) / _totalRamMB) * 100;
            }
            catch
            {
                return 0;
            }
        }

        private float GetTotalPhysicalMemory()
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject obj in results)
                    {
                        return Convert.ToSingle(obj["TotalPhysicalMemory"]) / (1024 * 1024); // MB cinsinden
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Toplam bellek bilgisi alınamadı: {ex.Message}");
            }
            return 0;
        }

        public async Task<float> GetDiskUsageAsync(string drive = "C")
        {
            return await Task.Run(() => {
                return GetDiskUsage(drive);
            });
        }
        
        public float GetDiskUsage(string drive = "C")
        {
            try
            {
                DriveInfo di = new DriveInfo(drive);
                if (!di.IsReady) return 0;
                
                float totalSize = di.TotalSize;
                float freeSpace = di.AvailableFreeSpace;
                return ((totalSize - freeSpace) / totalSize) * 100;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Disk kullanımı alınamadı: {ex.Message}");
                return 0;
            }
        }
        
        public async Task<List<ProcessInfo>> GetTopProcessesByCpuAsync(int count = 5)
        {
            return await Task.Run(() => GetTopProcessesByCpu(count));
        }
        
        public List<ProcessInfo> GetTopProcessesByCpu(int count = 5)
        {
            var result = new List<ProcessInfo>();
            
            try
            {
                var processes = Process.GetProcesses();
                
                // İşlem sayısını sınırlayarak performansı artıralım
                var topMemoryProcesses = processes
                    .OrderByDescending(p => {
                        try { return p.WorkingSet64; } 
                        catch { return 0; }
                    })
                    .Take(count * 2)
                    .ToList();
                
                foreach (var process in topMemoryProcesses)
                {
                    try
                    {
                        var info = new ProcessInfo
                        {
                            Name = process.ProcessName,
                            Id = process.Id,
                            MemoryMB = process.WorkingSet64 / (1024 * 1024)
                        };
                        
                        string processName = process.ProcessName;
                        if (!_processCounters.ContainsKey(processName))
                        {
                            try
                            {
                                _processCounters[processName] = new PerformanceCounter("Process", "% Processor Time", processName, true);
                                _processCounters[processName].NextValue();
                            }
                            catch
                            {
                                // Bazı süreçler için performans sayacı oluşturulamayabilir
                                continue;
                            }
                        }
                        
                        info.CpuUsage = _processCounters[processName].NextValue() / Environment.ProcessorCount;
                        result.Add(info);
                    }
                    catch 
                    {
                        // Hataları atla
                    }
                }
                
                return result.OrderByDescending(p => p.CpuUsage).Take(count).ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"İşlem listesi alınamadı: {ex.Message}");
                return result;
            }
        }

        public List<string> GetSystemOptimizationTips()
        {
            List<string> tips = new List<string>();
            
            float cpuUsage = GetCpuUsage();
            float ramUsage = GetRamUsage();
            float diskUsage = GetDiskUsage();
            
            if (cpuUsage > 80)
            {
                tips.Add("CPU kullanımı yüksek. Yüksek kullanıma neden olan uygulamaları kontrol edin.");
            }
            
            if (ramUsage > 85)
            {
                tips.Add("RAM kullanımı yüksek. Gereksiz uygulamaları kapatmayı deneyin.");
            }
            
            if (diskUsage > 90)
            {
                tips.Add("Disk doluluk oranı kritik seviyede. Yer açmak için disk temizliği yapmanız önerilir.");
            }
            
            return tips;
        }

        public async Task<NetworkUsage> GetNetworkUsageAsync()
        {
            return await Task.Run(() => {
                try
                {
                    if (_activeNetworkInterface == null || _activeNetworkInterface.OperationalStatus != OperationalStatus.Up)
                    {
                        FindMostActiveNetworkInterface();
                        return new NetworkUsage { BytesReceived = 0, BytesSent = 0, BytesPerSecond = 0 };
                    }
                    
                    var stats = _activeNetworkInterface.GetIPv4Statistics();
                    long bytesReceived = stats.BytesReceived;
                    long bytesSent = stats.BytesSent;
                    
                    DateTime now = DateTime.Now;
                    double seconds = (now - _lastNetworkCheck).TotalSeconds;
                    
                    double bytesPerSecond = 0;
                    if (seconds > 0)
                    {
                        bytesPerSecond = (bytesReceived - _lastBytesReceived + bytesSent - _lastBytesSent) / seconds;
                    }
                    
                    _lastBytesReceived = bytesReceived;
                    _lastBytesSent = bytesSent;
                    _lastNetworkCheck = now;
                    
                    return new NetworkUsage
                    {
                        BytesReceived = bytesReceived,
                        BytesSent = bytesSent,
                        BytesPerSecond = bytesPerSecond
                    };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ağ kullanımı alınamadı: {ex.Message}");
                    return new NetworkUsage { BytesReceived = 0, BytesSent = 0, BytesPerSecond = 0 };
                }
            });
        }
        
        public void KillProcess(int processId)
        {
            try
            {
                Process process = Process.GetProcessById(processId);
                if (process != null)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"İşlem sonlandırılamadı: {ex.Message}");
            }
        }
        
        public List<string> PerformQuickScan()
        {
            List<string> results = new List<string>();
            
            try
            {
                // CPU kullanımı kontrolü
                float cpuUsage = GetCpuUsage();
                if (cpuUsage > 80)
                {
                    results.Add($"Yüksek CPU kullanımı: {cpuUsage:F1}%");
                    
                    var highCpuProcesses = GetTopProcessesByCpu(3);
                    if (highCpuProcesses.Any())
                    {
                        results.Add("En çok CPU kullanan işlemler:");
                        foreach (var process in highCpuProcesses)
                        {
                            results.Add($"  - {process.Name}: {process.CpuUsage:F1}%");
                        }
                    }
                }
                
                // RAM kullanımı kontrolü
                float ramUsage = GetRamUsage();
                if (ramUsage > 85)
                {
                    results.Add($"Yüksek RAM kullanımı: {ramUsage:F1}%");
                    
                    var processes = Process.GetProcesses()
                        .OrderByDescending(p => {
                            try { return p.WorkingSet64; } 
                            catch { return 0; }
                        })
                        .Take(3);
                        
                    results.Add("En çok bellek kullanan işlemler:");
                    foreach (var process in processes)
                    {
                        try
                        {
                            float memMB = process.WorkingSet64 / (1024 * 1024);
                            results.Add($"  - {process.ProcessName}: {memMB:F1} MB");
                        }
                        catch { }
                    }
                }
                
                // Disk doluluk kontrolü
                float diskUsage = GetDiskUsage();
                if (diskUsage > 90)
                {
                    results.Add($"Disk doluluk oranı yüksek: {diskUsage:F1}%");
                    results.Add("Disk temizliği yapmanız önerilir.");
                }
                
                // Başlangıç programları kontrolü
                var startupManager = new StartupManager();
                var startupItems = startupManager.GetStartupItems();
                
                var highImpactItems = startupItems.Where(s => s.Impact == "Yüksek").ToList();
                if (highImpactItems.Any())
                {
                    results.Add("Yüksek etki düzeyine sahip başlangıç öğeleri:");
                    foreach (var item in highImpactItems.Take(3))
                    {
                        results.Add($"  - {item.Name}");
                    }
                }
                
                // Optimize edilebilir servisler
                var optimizableServices = _serviceManager.GetOptimizableServices();
                if (optimizableServices.Any())
                {
                    results.Add("Optimize edilebilir servisler:");
                    foreach (var service in optimizableServices.Take(3))
                    {
                        results.Add($"  - {service}");
                    }
                }
                
                // Genel sistem önerileri
                if (!results.Any())
                {
                    results.Add("Sistemde herhangi bir performans sorunu tespit edilmedi.");
                }
                
                results.Add("\nİyileştirme önerisi: " + GetRandomOptimizationTip());
            }
            catch (Exception ex)
            {
                results.Add($"Tarama sırasında hata oluştu: {ex.Message}");
            }
            
            return results;
        }
        
        private string GetRandomOptimizationTip()
        {
            string[] tips = new[] {
                "Kullanmadığınız programları kaldırın.",
                "Düzenli olarak disk temizliği yapın.",
                "Tarayıcınızdaki önbelleği ve çerezleri temizleyin.",
                "Windows güncellemelerini yükleyin.",
                "Virüs taraması yapın.",
                "SSD kullanıyorsanız, TRIM fonksiyonunun etkin olup olmadığını kontrol edin.",
                "Güç planınızı ihtiyacınıza göre ayarlayın.",
                "Gereksiz başlangıç programlarını devre dışı bırakın.",
                "RAM kullanımını azaltmak için gereksiz hizmetleri kapatın."
            };
            
            Random rnd = new Random();
            return tips[rnd.Next(tips.Length)];
        }

        // Eksik olan CPU sıcaklık bilgisi metodunu ekleyelim
        public async Task<float> GetCpuTemperatureAsync()
        {
            return await Task.Run(() => {
                try
                {
                    using (var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature"))
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject obj in collection)
                        {
                            // Kelvin'den Celsius'a çevir (0.1K hassasiyetle)
                            double tempKelvin = Convert.ToDouble(obj["CurrentTemperature"]);
                            double tempCelsius = tempKelvin / 10.0 - 273.15;
                            return (float)tempCelsius;
                        }
                    }
                    
                    // WMI ile okunamazsa alternatif yöntem
                    using (var searcher = new ManagementObjectSearcher(@"root\CIMV2", "SELECT * FROM Win32_PerfFormattedData_Counters_ThermalZoneInformation"))
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject obj in collection)
                        {
                            double temp = Convert.ToDouble(obj["Temperature"]);
                            return (float)temp;
                        }
                    }

                    return 0; // Sıcaklık okunamazsa varsayılan değer
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"CPU sıcaklığı alınamadı: {ex.Message}");
                    return 0;
                }
            });
        }

        // GPU bilgisi metodu ekleyelim (NVIDIA için)
        public async Task<Dictionary<string, float>> GetGpuInfoAsync()
        {
            return await Task.Run(() => {
                Dictionary<string, float> gpuInfo = new Dictionary<string, float>();
                
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
                    using (var collection = searcher.Get())
                    {
                        foreach (ManagementObject obj in collection)
                        {
                            string name = obj["Name"]?.ToString() ?? "Unknown GPU";
                            gpuInfo.Add("Name", name);
                            
                            if (obj["AdapterRAM"] != null)
                            {
                                ulong ram = Convert.ToUInt64(obj["AdapterRAM"]);
                                gpuInfo.Add("MemoryMB", ram / (1024 * 1024));
                            }
                            
                            break; // Sadece ilk GPU'yu al
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"GPU bilgisi alınamadı: {ex.Message}");
                }
                
                return gpuInfo;
            });
        }
    }
    
    public class ProcessInfo
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public float CpuUsage { get; set; }
        public float MemoryMB { get; set; }
    }
}
