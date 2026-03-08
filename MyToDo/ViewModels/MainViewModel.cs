using MyToDo.Common.Models;
using MyToDo.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MyToDo.ViewModels
{
    public class MainViewModel : BindableBase
    {
        public MainViewModel(IRegionManager regionManager)
        {
            CreateMenuBar();
            NavigateCommand = new DelegateCommand<MenuBar>(Navigate);
            GoBackCommand= new DelegateCommand(() =>
            {
                if (journal != null && journal.CanGoBack)
                {
                    journal.GoBack();
                }
            });
            GoForwardCommand= new DelegateCommand(() =>
            {
                if (journal != null && journal.CanGoForward)
                {
                    journal.GoForward();
                }
            });
            this.regionManager = regionManager;
        }

        private void Navigate(MenuBar bar)
        { 
            regionManager.Regions[PrismManager.MainViewRegionName].RequestNavigate(bar.NameSpace, back =>
            {
                journal = back.Context.NavigationService.Journal;
            });
        }
        private readonly IRegionManager regionManager;
        public DelegateCommand<MenuBar> NavigateCommand { get; set; }


        public DelegateCommand GoBackCommand { get; set; }
        public DelegateCommand GoForwardCommand { get; set; }

        private IRegionNavigationJournal journal;
        private ObservableCollection<MenuBar> menuBars;
        public ObservableCollection<MenuBar> MenuBars
        {
            get { return menuBars; }
            set { menuBars = value; RaisePropertyChanged(); }
        }

        void CreateMenuBar()
        {
            MenuBars = new ObservableCollection<MenuBar>();
            MenuBars.Add(new MenuBar() { Icon = "Home", Title = "首页", NameSpace = "IndexView" });
            MenuBars.Add(new MenuBar() { Icon = "NotebookOutline", Title = "代办事项", NameSpace = "ToDoView" });
            MenuBars.Add(new MenuBar() { Icon = "NotebookPlus", Title = "备忘录", NameSpace = "MemoView" });
            MenuBars.Add(new MenuBar() { Icon = "Cog", Title = "设置", NameSpace = "SettingsView" });
        }
    }
}
