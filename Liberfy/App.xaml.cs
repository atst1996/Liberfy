﻿using Hardcodet.Wpf.TaskbarNotification;
using Liberfy.Components;
using Liberfy.Managers;
using Liberfy.Settings;
using Liberfy.Utils;
using Liberfy.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace Liberfy
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    internal partial class App : Application
    {
        /// <summary>
        /// Appのインスタンス
        /// </summary>
        internal static App Instance { get; private set; }

        /// <summary>
        /// 設定データ
        /// </summary>
        internal static Setting Setting { get; private set; }

        /// <summary>
        /// アプリケーションの実行状態
        /// </summary>
        public static ApplicationStatus Status { get; } = new ApplicationStatus();

        /// <summary>
        /// アカウント情報管理
        /// </summary>
        internal static AccountManager Accounts { get; }

        /// <summary>
        /// カラム管理
        /// </summary>
        internal static ColumnManageer Columns { get; }

        static App()
        {
            Accounts = new AccountManager();
            Columns = new ColumnManageer(Accounts);
        }

        /// <summary>
        /// アセンブリ情報
        /// </summary>
        internal static readonly Assembly AssemblyInfo = Assembly.GetExecutingAssembly();

        public static ProfileImageCache ProfileImageCache { get; private set; }

        public const string Name = "Liberfy";
        public const string Version = "0.2.3.1";

        public bool IsRequireSaveSetting { get; private set; } = true;

        internal static DictionaryEx<NotifyCode, bool> NotificationEvents { get; } = new DictionaryEx<NotifyCode, bool>();

        internal TaskbarIcon TaskbarIcon { get; private set; }

        public UISettingManager UIManager { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public App() : base()
        {
            App.Instance = this;
        }

        /// <summary>
        /// アセンブリが配置されているディレクトリを取得する。
        /// </summary>
        /// <returns></returns>
        public static string GetLocalDirectory()
        {
            return Path.GetDirectoryName(AssemblyInfo.Location);
        }

        /// <summary>
        /// 指定の相対パスからアセンブリのディレクトリ配下にあるファイルの絶対パスを取得する。
        /// </summary>
        /// <param name="filename">相対パス</param>
        /// <returns>ファイルの絶対パス</returns>
        public static string GetLocalFilePath(string filename)
        {
            return Path.Combine(GetLocalDirectory(), filename);
        }

        /// <summary>
        /// アプリケーションの起動
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 作業ディレクトリの再指定（自動起動時に作業ディレクトリが変わってしまう対策）
            Directory.SetCurrentDirectory(GetLocalDirectory());

            // 設定ファイルを読み込む
            try
            {
                var task = this.LoadSettings();
                task.Wait();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.GetMessage(), App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                this.ForceShutdown();
                return;
            }

            this.UIManager = new UISettingManager(this, App.Setting);
            this.UIManager.Apply();

            this.TaskbarIcon = this.TryFindResource("taskbarIcon") as TaskbarIcon;

            ProfileImageCache = new ProfileImageCache();

            if (!App.Accounts.Any() && !this.IsNeedInitialUserSettings())
            {
                this.ForceShutdown();
                return;
            }

            this.SetNetworkConnectConfiguration();

            this.StartClient();
        }

        /// <summary>
        /// 設定を読み込む。
        /// </summary>
        /// <returns></returns>
        private async Task LoadSettings()
        {
            var settings = await SettingsManager.Load()
                .ConfigureAwait(false);

            // 一般設定
            Setting = settings.Application;

            // アカウント設定
            this.LoadAccountSettings(settings.Accounts);
        }

        /// <summary>
        /// 設定データからアカウント情報を読み込む。
        /// </summary>
        /// <param name="accounts"></param>
        private void LoadAccountSettings(AccountSettings settings)
        {
            Accounts.Restore(settings.Accounts);
            Columns.Restore(settings.Columns);
        }

        /// <summary>
        /// ネットワークの接続設定を行う。
        /// </summary>
        private void SetNetworkConnectConfiguration()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol =
                 SecurityProtocolType.Tls
                 | SecurityProtocolType.Tls11
                 | SecurityProtocolType.Tls12
                 | SecurityProtocolType.Tls13;
        }

        /// <summary>
        /// クライアントの処理を開始する。
        /// </summary>
        private void StartClient()
        {
            var tasks = App.Accounts.Select(a => a.StartActivity());

            Task.WhenAll(tasks).ContinueWith(_ =>
            {
                Status.IsAccountLoaded = true;
            },

            TaskScheduler.FromCurrentSynchronizationContext());
        }

        /// <summary>
        /// ユーザ登録が必要化どうかを返す。
        /// </summary>
        /// <returns>ユーザ登録処理が必要かどうか</returns>
        private bool IsNeedInitialUserSettings()
        {
            var tempShutdownMode = this.ShutdownMode;
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            DialogUtils.ShowSettingWindow(1, null, true);

            this.ShutdownMode = tempShutdownMode;

            return App.Accounts.Count > 0;
        }

        /// <summary>
        /// システムのセッション切断時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSystemSessionEnding(object sender, SessionEndingEventArgs e)
        {
            if (e.Reason == SessionEndReasons.Logoff)
            {
                e.Cancel = Setting.SystemCancelSignout;
            }
            else if (e.Reason == SessionEndReasons.SystemShutdown)
            {
                e.Cancel = Setting.SystemCancelShutdown;
            }
        }

        /// <summary>
        /// すべての設定を保存する。
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                var setting = App.Setting;
                var accountsSetting = new AccountSettings
                {
                    Accounts = Accounts.Select(a => a.GetSetting()).ToArray(),
                    Columns = Columns.Select(c => c.GetSetting()).ToArray(),
                };

                var task = SettingsManager.Save(new()
                {
                    Application = Setting,
                    Accounts = accountsSetting,
                });

                task.Wait();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                MessageBox.Show(ex.Message, App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 強制終了する。
        /// </summary>
        public void ForceShutdown()
        {
            this.IsRequireSaveSetting = true;

            this.Shutdown();
        }

        /// <summary>
        /// アプリケーション終了時
        /// </summary>
        /// <param name="e"></param>
        protected override void OnExit(ExitEventArgs e)
        {
            SystemEvents.SessionEnding -= this.OnSystemSessionEnding;

            if (this.IsRequireSaveSetting)
            {
                this.SaveSettings();
            }

            base.OnExit(e);
        }

        /// <summary>
        /// 指定のパスを開く
        /// </summary>
        /// <param name="path"></param>
        internal static void Open(string path)
        {
            // https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/

            try
            {
                Process.Start(path);
            }
            catch (Exception ex)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var escapedPath = path.Replace("&", "^&");
                    var processStartInfo = new ProcessStartInfo("cmd", $"/c start {escapedPath}")
                    {
                        CreateNoWindow = true,
                    };

                    Process.Start(processStartInfo);
                    return;
                }

                throw;
            }
        }

        /// <summary>
        /// 指定URIを開く
        /// </summary>
        /// <param name="uri">URI</param>
        internal static void Open(Uri uri)
        {
            App.Open(uri.AbsoluteUri);
        }

        /// <summary>
        /// リソースの格納値を取得する。
        /// </summary>
        /// <typeparam name="T">型</typeparam>
        /// <param name="resourceKey">リソースキー</param>
        /// <returns>格納値</returns>
        public T FindResource<T>(object resourceKey)
        {
            return this.FindResource(resourceKey) is T tValue ? tValue : default;
        }

        /// <summary>
        /// リソースの格納値を取得を試行する。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resourceKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public T TryFindResource<T>(object resourceKey)
        {
            return this.TryFindResource(resourceKey) is T tValue ? tValue : default;
        }

        /// <summary>
        /// ウィンドウを列挙する。
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Window> EnumerateWindows()
        {
            return this.Windows.Cast<Window>();
        }

        /// <summary>
        /// ウィンドウ一覧からViewModelを列挙する。
        /// </summary>
        /// <typeparam name="T">ViewModelの型</typeparam>
        /// <returns>ViewModel一覧</returns>
        public IEnumerable<T> FindViewModel<T>() where T : ViewModelBase
        {
            return this.EnumerateWindows()
                .Select(w => w.DataContext is T viewModel ? viewModel : default)
                .Where(vm => vm != null);
        }

        /// <summary>
        /// 特定の型のViewModelが指定されているウィンドウを列挙する。
        /// </summary>
        /// <typeparam name="T">ViewModelの型</typeparam>
        /// <returns></returns>
        public IEnumerable<Window> FindViewModelWindow<T>() where T : ViewModelBase
        {
            return this.EnumerateWindows()
                .Where(w => w.DataContext is T);
        }

        /// <summary>
        /// 特定の型のViewModelが指定されているウィンドウとViewModelを列挙する。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<(Window view, T viewModel)> FindViewModelWithWindow<T>() where T : ViewModelBase
        {
            foreach (var window in this.EnumerateWindows())
            {
                if (window.DataContext is T viewModel)
                {
                    yield return (window, viewModel);
                }
            }
        }
    }
}
