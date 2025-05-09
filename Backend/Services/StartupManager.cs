using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using PulseTune.Backend.Models;
using System.Linq;

namespace PulseTune.Backend.Services
{
    public class StartupManager
    {
        private const string RUN_LOCATION = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string RUN_LOCATION_32 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
        
        public List<StartupItem> GetStartupItems()
        {
            List<StartupItem> startupItems = new List<StartupItem>();
            
            // Windows Registry'den başlangıç programlarını al
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RUN_LOCATION))
                {
                    if (key != null)
                    {
                        foreach (string valueName in key.GetValueNames())
                        {
                            startupItems.Add(new StartupItem
                            {
                                Name = valueName,
                                Path = key.GetValue(valueName).ToString(),
                                Location = "HKLM\\Run",
                                IsEnabled = true,
                                Impact = EstimateImpact(key.GetValue(valueName).ToString())
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registry erişim hatası: {ex.Message}");
            }
            
            // 32-bit uygulamalar için
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RUN_LOCATION_32))
                {
                    if (key != null)
                    {
                        foreach (string valueName in key.GetValueNames())
                        {
                            startupItems.Add(new StartupItem
                            {
                                Name = valueName,
                                Path = key.GetValue(valueName).ToString(),
                                Location = "HKLM\\Run (32-bit)",
                                IsEnabled = true,
                                Impact = EstimateImpact(key.GetValue(valueName).ToString())
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registry erişim hatası: {ex.Message}");
            }
            
            // Kullanıcı başlangıç kayıtlarını al
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_LOCATION))
                {
                    if (key != null)
                    {
                        foreach (string valueName in key.GetValueNames())
                        {
                            startupItems.Add(new StartupItem
                            {
                                Name = valueName,
                                Path = key.GetValue(valueName).ToString(),
                                Location = "HKCU\\Run",
                                IsEnabled = true,
                                Impact = EstimateImpact(key.GetValue(valueName).ToString())
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registry erişim hatası: {ex.Message}");
            }
            
            // Startup klasörünü kontrol et
            try
            {
                string startupFolderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup));
                
                if (Directory.Exists(startupFolderPath))
                {
                    foreach (string file in Directory.GetFiles(startupFolderPath))
                    {
                        if (file.EndsWith(".lnk") || file.EndsWith(".url"))
                        {
                            string fileName = Path.GetFileNameWithoutExtension(file);
                            startupItems.Add(new StartupItem
                            {
                                Name = fileName,
                                Path = file,
                                Location = "Başlangıç Klasörü",
                                IsEnabled = true,
                                Impact = EstimateImpact(file)
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Başlangıç klasörü erişim hatası: {ex.Message}");
            }
            
            return startupItems;
        }
        
        public void EnableStartupItem(StartupItem item)
        {
            // Bu fonksiyon bir başlangıç öğesini etkinleştirebilir
            // Gerçek uygulamada registry kilit ve devre dışı bırakma değerlerini değiştirmek gerekir
            Console.WriteLine($"{item.Name} etkinleştirildi");
        }
        
        public void DisableStartupItem(StartupItem item)
        {
            // Bu fonksiyon bir başlangıç öğesini devre dışı bırakabilir
            // Gerçek uygulamada registry kilit ve devre dışı bırakma değerlerini değiştirmek gerekir
            Console.WriteLine($"{item.Name} devre dışı bırakıldı");
        }
        
        private string EstimateImpact(string path)
        {
            // Basit bir etki tahmini
            // Gerçek uygulamada, daha karmaşık analiz yapabilirsiniz
            
            // Bazı yüksek etki uygulamaları listesi
            string[] highImpactKeywords = { "update", "sync", "cloud", "adobe", "java" };
            
            path = path.ToLower();
            
            if (highImpactKeywords.Any(k => path.Contains(k)))
                return "Yüksek";
                
            // Bilinen bazı orta etki uygulamaları
            string[] mediumImpactKeywords = { "messenger", "skype", "teams", "discord" };
            
            if (mediumImpactKeywords.Any(k => path.Contains(k)))
                return "Orta";
                
            // Varsayılan olarak düşük etki
            return "Düşük";
        }
    }
}
