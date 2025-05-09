using System;
using System.Windows;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PulseTune
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Hata yakalama mekanizmasını ekle
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }
        
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException(e.ExceptionObject as Exception);
        }
        
        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException(e.Exception);
            e.SetObserved(); // Uygulama çökmesini önle
        }
        
        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception);
            e.Handled = true; // Uygulama çökmesini önle
        }
        
        private void LogException(Exception ex)
        {
            if (ex == null) return;
            
            try
            {
                Debug.WriteLine($"Hata: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                
                // Gerçek bir uygulamada bu bilgileri dosyaya da kaydedebilirsiniz
            }
            catch
            {
                // Log alma esnasında bir hata oluştuğu için sessizce devam et
            }
        }
    }
}
