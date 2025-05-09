using System;
using System.Diagnostics;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PulseTune.Backend.Services
{
    public class ProfileManager
    {
        private Dictionary<string, Action> _profiles;
        
        public ProfileManager()
        {
            InitializeProfiles();
        }
        
        private void InitializeProfiles()
        {
            _profiles = new Dictionary<string, Action>
            {
                { "Normal", ApplyNormalProfile },
                { "Gaming", ApplyGamingProfile },
                { "PowerSaver", ApplyPowerSaverProfile },
                { "Office", ApplyOfficeProfile }
            };
        }
        
        public void ApplyProfile(string profileName)
        {
            try
            {
                if (_profiles.ContainsKey(profileName))
                {
                    _profiles[profileName].Invoke();
                    Debug.WriteLine($"{profileName} profili uygulandı.");
                }
                else
                {
                    Debug.WriteLine($"Bilinmeyen profil: {profileName}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Profil uygulanırken hata: {ex.Message}");
            }
        }
        
        private void ApplyNormalProfile()
        {
            try
            {
                // Varsayılan güç planını seç
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = "/S SCHEME_BALANCED",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                
                // Varsayılan optimize ayarları
                SetVisualEffects("Normal");
                
                // İşlem önceliği ayarları varsayılan hale getirilir
                // (Sistem tarafından otomatik yapılır)
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Normal profil uygulanırken hata: {ex.Message}");
            }
        }
        
        private void ApplyGamingProfile()
        {
            try
            {
                // Yüksek performans güç planı
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = "/S SCHEME_MIN",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                
                // Oyun optimizasyonu için görsel efektleri kapat
                SetVisualEffects("Performance");
                
                // Diğer oyun optimizasyonları burada yapılabilir
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Oyun profili uygulanırken hata: {ex.Message}");
            }
        }
        
        private void ApplyPowerSaverProfile()
        {
            try
            {
                // Güç tasarrufu planı
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = "/S SCHEME_MAX",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                
                // Güç tasarrufu için görsel efektleri kapat
                SetVisualEffects("Basic");
                
                // Diğer güç tasarrufu optimizasyonları
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Güç tasarrufu profili uygulanırken hata: {ex.Message}");
            }
        }
        
        private void ApplyOfficeProfile()
        {
            try
            {
                // Dengeli güç planı 
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = "/S SCHEME_BALANCED",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                
                // Ofis için görsel efektleri normal yap
                SetVisualEffects("Normal");
                
                // Ofis uygulamaları için sistem optimizasyonları
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ofis profili uygulanırken hata: {ex.Message}");
            }
        }
        
        private void SetVisualEffects(string preset)
        {
            try
            {
                switch (preset.ToLower())
                {
                    case "performance":
                        // Performans odaklı görsel ayarlar
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true))
                        {
                            if (key != null)
                                key.SetValue("VisualFXSetting", 2);
                        }
                        break;
                        
                    case "basic":
                        // Temel görsel ayarlar (minimum)
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true))
                        {
                            if (key != null)
                                key.SetValue("VisualFXSetting", 3);
                        }
                        break;
                        
                    case "normal":
                    default:
                        // Normal görsel ayarlar
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true))
                        {
                            if (key != null)
                                key.SetValue("VisualFXSetting", 0);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Görsel efektler ayarlanırken hata: {ex.Message}");
            }
        }
    }
}
