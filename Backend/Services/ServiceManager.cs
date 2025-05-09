using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ServiceProcess;
using PulseTune.Backend.Models;
using System.Linq;
using System.Management;

namespace PulseTune.Backend.Services
{
    public class ServiceManager
    {
        public List<ServiceInfo> GetServices()
        {
            List<ServiceInfo> services = new List<ServiceInfo>();
            
            try
            {
                ServiceController[] serviceControllers = ServiceController.GetServices();
                
                foreach (ServiceController service in serviceControllers)
                {
                    services.Add(new ServiceInfo
                    {
                        Name = service.ServiceName,
                        DisplayName = service.DisplayName,
                        Status = service.Status.ToString(),
                        StartupType = GetStartupType(service.ServiceName),
                        Description = GetServiceDescription(service.ServiceName)
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Servis bilgileri alınamadı: {ex.Message}");
            }
            
            return services.OrderBy(s => s.Name).ToList();
        }
        
        private string GetStartupType(string serviceName)
        {
            try
            {
                using (ManagementObject wmiService = new ManagementObject($"Win32_Service.Name='{serviceName}'"))
                {
                    wmiService.Get();
                    return wmiService["StartMode"].ToString();
                }
            }
            catch
            {
                return "Bilinmiyor";
            }
        }
        
        private string GetServiceDescription(string serviceName)
        {
            try
            {
                using (ManagementObject wmiService = new ManagementObject($"Win32_Service.Name='{serviceName}'"))
                {
                    wmiService.Get();
                    return wmiService["Description"]?.ToString() ?? "Açıklama yok";
                }
            }
            catch
            {
                return "Açıklama alınamadı";
            }
        }
        
        public void StartService(string serviceName)
        {
            try
            {
                using (ServiceController service = new ServiceController(serviceName))
                {
                    if (service.Status == ServiceControllerStatus.Stopped)
                    {
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Servis başlatılamadı: {ex.Message}");
            }
        }
        
        public void StopService(string serviceName)
        {
            try
            {
                using (ServiceController service = new ServiceController(serviceName))
                {
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Servis durdurulamadı: {ex.Message}");
            }
        }
        
        public List<string> GetOptimizableServices()
        {
            // Bu metod, güvenli bir şekilde optimize edilebilecek servisleri önerir
            List<string> optimizableServices = new List<string>();
            
            // Bilinen güvenli optimize edilebilecek servisler listesi (örnek)
            string[] knownSafeToOptimize = {
                "DiagTrack", // Connected User Experiences and Telemetry
                "dmwappushservice", // WAP Push Message Routing Service
                "FontCache", // Windows Font Cache Service (düşük öncelikli)
                "lfsvc", // Geolocation Service
                "MapsBroker", // Downloaded Maps Manager
                "PcaSvc", // Program Compatibility Assistant Service
                "RemoteRegistry", // Remote Registry (genellikle kapalı olmalı)
                "WSearch" // Windows Search (çok kaynak tüketiyorsa)
            };
            
            var services = GetServices();
            foreach (var service in services)
            {
                if (knownSafeToOptimize.Contains(service.Name) && service.Status == "Running")
                {
                    optimizableServices.Add($"{service.DisplayName} - Bu servis güvenli bir şekilde optimize edilebilir");
                }
            }
            
            return optimizableServices;
        }
    }
}
