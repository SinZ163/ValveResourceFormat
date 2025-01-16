using Microsoft.UI.Xaml.Controls;
using SteamDatabase.ValvePak;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace GUI2
{
    static class VrfGlobalSingleton
    {
        public static ObservableCollection<TabViewItem> Tabs { get; set; } = new ObservableCollection<TabViewItem>();

        public static async Task OpenFile(StorageFile file)
        {
            await OpenFileInternal(file.Name, () => new VrfGuiContext().ProcessStorageFile(file));
        }
        public static async Task OpenFile(string filename, byte[] data, SteamDatabase.ValvePak.Package package)
        {
            await OpenFileInternal(filename, () => new VrfGuiContext(filename, data, package).Process());
        }

        private static async Task OpenFileInternal(string filename, Func<Task<VrfGuiContext>> func)
        {
            var tab = new TabViewItem();
            tab.Header = filename;

            Frame frame = new();
            tab.Content = frame;
            frame.Navigate(typeof(LoadingPage));
            Tabs.Add(tab);

#if BROWSERWASM
            try {
                var result = await func();
                
                frame.Navigate(result.XamlPage, result);
            } catch(Exception ex) {
                frame.Navigate(typeof(ErrorPage), ex.ToString());
            }
#else
            var task = Task.Factory.StartNew(func);

            task.ContinueWith(
                t =>
                {
                    t.Exception?.Flatten().Handle(ex =>
                    {
                        frame.Navigate(typeof(ErrorPage), ex.ToString());

                        return false;
                    });
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);

            task.ContinueWith(
                t =>
                {
                    var result = t.Unwrap().Result;
                    frame.Navigate(result.XamlPage, result);
                },
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default);
#endif
        }
    }
}
