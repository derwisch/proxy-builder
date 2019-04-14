using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ProxyBuilder
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        internal static ProxyPrinter Printer { get; private set; }
        internal static CardFetcher CardFetcher { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Printer = new ProxyPrinter();
            CardFetcher = new CardFetcher();
        }
    }
}
