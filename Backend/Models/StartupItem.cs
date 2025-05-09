using System;

namespace PulseTune.Backend.Models
{
    public class StartupItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string Location { get; set; }  // Registry veya başlangıç klasörü
        public bool IsEnabled { get; set; }
        public string Impact { get; set; }  // Düşük, Orta, Yüksek
    }
}
