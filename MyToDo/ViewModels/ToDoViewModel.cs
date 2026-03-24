using MyToDo.Common.Models;
using MyToDo.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MyToDo.ViewModels
{
    public class ToDoViewModel : BindableBase
    {
        private readonly IToDoService _toDoService;

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
            set { filterIndex = value; RaisePropertyChanged(); _ = LoadToDosAsync(); }
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

        private ToDoDto currentDto;
        public ToDoDto CurrentDto
        {
            get { return currentDto; }
            set { currentDto = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<ToDoDto> toDoDtos;
        public ObservableCollection<ToDoDto> ToDoDtos
        {
            get { return toDoDtos; }
            set { toDoDtos = value; RaisePropertyChanged(); }
        }

        public DelegateCommand AddCommand { get; set; }
        public DelegateCommand SaveCommand { get; set; }
        public DelegateCommand<ToDoDto> DeleteCommand { get; set; }
        public DelegateCommand SearchCommand { get; set; }

        public ToDoViewModel(IToDoService toDoService)
        {
            _toDoService = toDoService;
            ToDoDtos = new ObservableCollection<ToDoDto>();

            AddCommand = new DelegateCommand(() =>
            {
                CurrentTitle = string.Empty;
                CurrentContent = string.Empty;
                CurrentStatus = 0;
                CurrentDto = null;
                IsRightDrawerOpen = true;
            });

            SaveCommand = new DelegateCommand(async () =>
            {
                if (string.IsNullOrWhiteSpace(CurrentTitle))
                    return;
                try
                {
                    ApiResponse<ToDoDto> result;
                    if (CurrentDto != null && CurrentDto.Id > 0)
                    {
                        CurrentDto.Title = CurrentTitle;
                        CurrentDto.Content = CurrentContent;
                        CurrentDto.Status = CurrentStatus;
                        result = await _toDoService.UpdateAsync(CurrentDto);
                    }
                    else
                    {
                        var dto = new ToDoDto
                        {
                            Title = CurrentTitle,
                            Content = CurrentContent,
                            Status = CurrentStatus
                        };
                        result = await _toDoService.AddAsync(dto);
                    }

                    if (result.Status)
                    {
                        IsRightDrawerOpen = false;
                        await LoadToDosAsync();
                    }
                }
                catch { /* network errors are handled in service layer */ }
            });

            DeleteCommand = new DelegateCommand<ToDoDto>(async todo =>
            {
                if (todo != null)
                {
                    try
                    {
                        var result = await _toDoService.DeleteAsync(todo.Id);
                        if (result.Status)
                            ToDoDtos.Remove(todo);
                    }
                    catch { /* network errors are handled in service layer */ }
                }
            });

            SearchCommand = new DelegateCommand(async () => await LoadToDosAsync());

            _ = LoadToDosAsync();
        }

        private async Task LoadToDosAsync()
        {
            int? statusFilter = FilterIndex switch
            {
                1 => 0,
                2 => 1,
                _ => null
            };
            var result = await _toDoService.GetAllAsync(Search, statusFilter);
            if (result.Status && result.Result != null)
                ToDoDtos = new ObservableCollection<ToDoDto>(result.Result);
        }
    }
}

