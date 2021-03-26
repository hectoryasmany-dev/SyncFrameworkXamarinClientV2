using BIT.Xpo.Providers.OfflineDataSync;
using DevExpress.Persistent.Base;
using SyncFrameworkXamarinClientV2.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace SyncFrameworkXamarinClientV2
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            SyncDataStoreAsynchronous.Register();
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            Tracing.UseConfigurationManager = false;
            Tracing.Initialize(3);
            MainPage = new LoginPage(); 
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
