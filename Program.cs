using System;

namespace PulseTune
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            var app = new PulseTune.App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
