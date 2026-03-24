using MyToDo.Service;
using MyToDo.ViewModels;
using MyToDo.Views;
using System.Configuration;
using System.Data;
using System.Net.Http;
using System.Windows;

namespace MyToDo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainView>();
        }
        override protected void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register HttpClient with base address of the API
            var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5081/") };
            containerRegistry.RegisterInstance(httpClient);

            // Register services
            containerRegistry.Register<IToDoService, ToDoHttpService>();
            containerRegistry.Register<IMemoService, MemoHttpService>();

            // Register navigation views with ViewModels
            containerRegistry.RegisterForNavigation<IndexView, IndexViewModel>();
            containerRegistry.RegisterForNavigation<ToDoView, ToDoViewModel>();
            containerRegistry.RegisterForNavigation<MemoView, MemoViewModel>();
            containerRegistry.RegisterForNavigation<SettingsView, SettingsViewModel>();
        }
    }
}
