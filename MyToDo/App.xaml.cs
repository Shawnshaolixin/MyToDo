using MyToDo.Views;
using System.Configuration;
using System.Data;
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
            // Register any types or services here
        }
    }
}
