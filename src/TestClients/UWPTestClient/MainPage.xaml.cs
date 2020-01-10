using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UWPTestClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static string clientId = "Android-" + Guid.NewGuid().ToString().Split("-")[0];

        private readonly MainViewModel viewModel;
        private readonly AblyService ably;

        public MainPage()
        {
            ably = new AblyService("lNj80Q.iGyVcQ:2QKX7FFASfX-7H9H");
            ably.Init(clientId);

            this.InitializeComponent();
            var connectionStateObserver = new ConnectionStatusObserver(x => viewModel.ConnectionStatus = x);
            ably.Subscribe(connectionStateObserver);

            DataContext = viewModel = new MainViewModel(ably);
        }
    }

    public class ConnectionStatusObserver : IObserver<string>
    {
        private readonly Action<string> _onChange;

        public ConnectionStatusObserver(Action<string> onChange)
        {
            _onChange = onChange;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(string value)
        {
            _onChange.Invoke(value);
        }
    }
}