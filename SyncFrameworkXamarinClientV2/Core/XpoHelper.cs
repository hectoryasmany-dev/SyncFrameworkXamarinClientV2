using BIT.Data.Services;
using BIT.Xpo.Providers.OfflineDataSync;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using DevExpress.Xpo;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.DB.Helpers;
using System;
using System.IO;
using System.Net.Http;
using Orm.Model;
using BIT.Xpo.Providers.OfflineDataSync.UtilitiesExtensions;
using BIT.Xpo.Providers.OfflineDataSync.NetworkExtensions;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Security.ClientServer;
using BIT.Data.Sync;
using System.Collections.Generic;
using RestClient.Net;
using BIT.Data.Functions.RestClientNet;
using BIT.Data.Functions;

namespace SyncFrameworkXamarinClientV2.Core
{
    public static class XpoHelper
    {
        static SecuredObjectSpaceProvider ObjectSpaceProvider;
        static AuthenticationStandard Authentication;
        public static SecurityStrategyComplex Security;

        #region Sync
        private static SyncDataStoreServerConfiguration syncDataStoreServerConfiguration;
        private static string syncUrl;
        
        public static  Client client = new Client(new NewtonsoftSerializationAdapter(), createHttpClient: (name) => new HttpClient());
        public static ApiFunction Function = new ApiFunction(client, "https://b5789a9e2a2c.ngrok.io" + "/Sync", null);

        private const string ConnectionTemplate = @"XpoProvider=SQLite;Data Source=$DatabaseName.db";
        static CompressXmlObjectSerializationService serializationService;
        static StringSerializationHelper stringSerializactionService;
        static SyncDataStoreAsynchronous syncDataStore;

        public static SyncDataStoreAsynchronous SyncDataStore { get => syncDataStore; private set => syncDataStore = value; }
        public static string SyncUrl { get => syncUrl; set => syncUrl = value; }
        public static SyncDataStoreServerConfiguration SyncDataStoreServerConfiguration { get => syncDataStoreServerConfiguration; set => syncDataStoreServerConfiguration = value; }

        public static string GetConnectionString(string DatabaseName)
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            return ConnectionTemplate.Replace("$DatabaseName", Path.Combine(basePath, DatabaseName));
        }
        private static List<Delta> GetDeltasFromServer(IObjectSerializationService SerializationService, int LastDelta, string Identity)
        {
            
            DataParameters parameters = new DataParameters() { MemberName = "GetDeltas" };
            parameters.ParametersValue = SerializationService.ToByteArray(new GetDeltaParams(LastDelta, Identity));
            var Result = Function.ExecuteFunction(parameters);
            var Deltas = SerializationService.GetObjectsFromByteArray<List<Delta>>(Result.ResultValue);
            return Deltas;
        }
        //TODO remove hard code identity
        public static void Sync()
        {

            serializationService = new BIT.Data.Services.CompressXmlObjectSerializationService();

            var deltas=SyncDataStore.PullDeltas(XpoHelper.SyncDataStoreServerConfiguration);
            
            List<Delta> DeltasA = SyncDataStore.GetSerializableDeltas();
            var LastProccesedDeltaKeyB = SyncDataStore.GetLastProcessedDeltaKey();

            var Client_A_DataStoreMissingDeltas = GetDeltasFromServer(serializationService, LastProccesedDeltaKeyB, "Mobile");

            SyncDataStore.PushDeltas(XpoHelper.SyncDataStoreServerConfiguration);


        }


        #endregion

        public static void InitXpoSync(string connectionString, string SyncUrl)
        {


            serializationService = new BIT.Data.Services.CompressXmlObjectSerializationService();
            stringSerializactionService = new BIT.Data.Services.StringSerializationHelper();
            SyncDataStoreServerConfiguration = new SyncDataStoreServerConfiguration(serializationService, stringSerializactionService, SyncUrl, createHttpClient: null);
            SyncDataStore = SyncDataStoreAsynchronous.CreateProviderFromString(connectionString, AutoCreateOption.DatabaseAndSchema, out _) as SyncDataStoreAsynchronous;


            Type[] Types = new Type[] {
                typeof(Employee),
                typeof(PermissionPolicyUser),
                typeof(PermissionPolicyRole),
                typeof(PermissionPolicyTypePermissionObject),
                typeof(PermissionPolicyObjectPermissionsObject),
                typeof(PermissionPolicyNavigationPermissionObject),
                typeof(Department)
            };

            SyncDataStore.UpdateTargetSchema(Types, false, false);



        }

        public static void InitXpo(string connectionString, string login, string password)
        {



            RegisterEntities();
            InitSecurity();
            XpoDefault.RegisterBonusProviders();
            //DataStoreBase.RegisterDataStoreProvider(WebApiDataStoreClient.XpoProviderTypeString, CreateWebApiDataStoreFromString);
            ObjectSpaceProvider = new SecuredObjectSpaceProvider(Security, connectionString, null);
            UpdateDataBase();
            LogIn(login, password);
            XpoDefault.Session = null;
        }

        static void UpdateDataBase()
        {
            var space = ObjectSpaceProvider.CreateUpdatingObjectSpace(true);
            Updater updater = new Updater(space);
            updater.UpdateDatabase();
        }

        public static UnitOfWork CreateUnitOfWork()
        {
            var space = (XPObjectSpace)ObjectSpaceProvider.CreateObjectSpace();
            return (UnitOfWork)space.Session;
        }
        static void LogIn(string login, string password)
        {
            Authentication.SetLogonParameters(new AuthenticationStandardLogonParameters(login, password));
            IObjectSpace loginObjectSpace = ObjectSpaceProvider.CreateObjectSpace();
            Security.Logon(loginObjectSpace);
        }

        static void InitSecurity()
        {
            Authentication = new AuthenticationStandard();
            Security = new SecurityStrategyComplex(typeof(PermissionPolicyUser), typeof(PermissionPolicyRole), Authentication);
            Security.RegisterXPOAdapterProviders();
        }
        private static void RegisterEntities()
        {
            XpoTypesInfoHelper.GetXpoTypeInfoSource();
            XafTypesInfo.Instance.RegisterEntity(typeof(Employee));
            XafTypesInfo.Instance.RegisterEntity(typeof(PermissionPolicyUser));
            XafTypesInfo.Instance.RegisterEntity(typeof(PermissionPolicyRole));
        }

        static IDataStore CreateWebApiDataStoreFromString(string connectionString, AutoCreateOption autoCreateOption, out IDisposable[] objectsToDisposeOnDisconnect)
        {
            ConnectionStringParser parser = new ConnectionStringParser(connectionString);
            if (!parser.PartExists("uri"))
                throw new ArgumentException("Connection string does not contain the 'uri' part.");
            string uri = parser.GetPartByName("uri");
#if DEBUG
            HttpClient client = new HttpClient(GetInsecureHandler());
#else
            HttpClient client = new HttpClient();
#endif
            client.BaseAddress = new Uri(uri);
            objectsToDisposeOnDisconnect = new IDisposable[] { client };
            return new WebApiDataStoreClient(client, autoCreateOption);
        }
        static HttpClientHandler GetInsecureHandler()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            return handler;
        }
    }
}
