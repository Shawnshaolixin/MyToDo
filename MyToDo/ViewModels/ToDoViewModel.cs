using MyToDo.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MyToDo.ViewModels
{
    public class ToDoViewModel : BindableBase
    {
        private bool isRightDrawerOpen;
        public bool IsRightDrawerOpen
        {
            get { return isRightDrawerOpen; }
            set { isRightDrawerOpen = value; RaisePropertyChanged(); }
        }

        private string search;
        public string Search
        {
            get { return search; }
            set { search = value; RaisePropertyChanged(); }
        }

        private int filterIndex;
        public int FilterIndex
        {
            get { return filterIndex; }
            set { filterIndex = value; RaisePropertyChanged(); ApplyFilter(); }
        }

        private int currentStatus;
        public int CurrentStatus
        {
            get { return currentStatus; }
            set { currentStatus = value; RaisePropertyChanged(); }
        }

        private string currentTitle;
        public string CurrentTitle
        {
            get { return currentTitle; }
            set { currentTitle = value; RaisePropertyChanged(); }
        }

        private string currentContent;
        public string CurrentContent
        {
            get { return currentContent; }
            set { currentContent = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<ToDoDto> toDoDtos;
        public ObservableCollection<ToDoDto> ToDoDtos
        {
            get { return toDoDtos; }
            set { toDoDtos = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<ToDoDto> allToDoDtos;

        public DelegateCommand AddCommand { get; set; }
        public DelegateCommand SaveCommand { get; set; }
        public DelegateCommand<ToDoDto> DeleteCommand { get; set; }
        public DelegateCommand SearchCommand { get; set; }

        public ToDoViewModel()
        {
            CreateToDoDtos();

            AddCommand = new DelegateCommand(() =>
            {
                CurrentTitle = string.Empty;
                CurrentContent = string.Empty;
                CurrentStatus = 0;
                IsRightDrawerOpen = true;
            });

            SaveCommand = new DelegateCommand(() =>
            {
                if (string.IsNullOrWhiteSpace(CurrentTitle))
                    return;
                var todo = new ToDoDto
                {
                    Title = CurrentTitle,
                    Content = CurrentContent,
                    Status = CurrentStatus
                };
                allToDoDtos.Add(todo);
                ApplyFilter();
                IsRightDrawerOpen = false;
            });

            DeleteCommand = new DelegateCommand<ToDoDto>(todo =>
            {
                if (todo != null)
                {
                    allToDoDtos.Remove(todo);
                    ToDoDtos.Remove(todo);
                }
            });

            SearchCommand = new DelegateCommand(ApplyFilter);
        }

        void ApplyFilter()
        {
            // FilterIndex: 0=全部(All), 1=待办(Pending, Status=0), 2=已完成(Completed, Status=1)
            const int StatusPending = 0;
            const int StatusCompleted = 1;

            var keyword = (Search ?? string.Empty).Trim().ToLower();
            var filtered = new ObservableCollection<ToDoDto>();
            foreach (var item in allToDoDtos)
            {
                // Status filter
                if (FilterIndex == 1 && item.Status != StatusPending) continue;
                if (FilterIndex == 2 && item.Status != StatusCompleted) continue;

                // Keyword filter
                if (!string.IsNullOrEmpty(keyword))
                {
                    if ((item.Title == null || !item.Title.ToLower().Contains(keyword)) &&
                        (item.Content == null || !item.Content.ToLower().Contains(keyword)))
                        continue;
                }
                filtered.Add(item);
            }
            ToDoDtos = filtered;
        }

        void CreateToDoDtos()
        {
            allToDoDtos = new ObservableCollection<ToDoDto>();
            for (int i = 0; i < 20; i++)
            {
                allToDoDtos.Add(new ToDoDto { Title = $"吃饭{i}", Content = $"吃饭了{i}", Status = 0 });
            }
            ApplyFilter();
        }
    }
}

