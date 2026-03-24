using MyToDo.Common.Models;
using MyToDo.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MyToDo.ViewModels
{
    public class MemoViewModel : BindableBase
    {
        private readonly IMemoService _memoService;

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

        private MemoDto currentDto;
        public MemoDto CurrentDto
        {
            get { return currentDto; }
            set { currentDto = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<MemoDto> memoDtos;
        public ObservableCollection<MemoDto> MemoDtos
        {
            get { return memoDtos; }
            set { memoDtos = value; RaisePropertyChanged(); }
        }

        public DelegateCommand AddCommand { get; set; }
        public DelegateCommand SaveCommand { get; set; }
        public DelegateCommand<MemoDto> DeleteCommand { get; set; }
        public DelegateCommand SearchCommand { get; set; }

        public MemoViewModel(IMemoService memoService)
        {
            _memoService = memoService;
            MemoDtos = new ObservableCollection<MemoDto>();

            AddCommand = new DelegateCommand(() =>
            {
                CurrentTitle = string.Empty;
                CurrentContent = string.Empty;
                CurrentDto = null;
                IsRightDrawerOpen = true;
            });

            SaveCommand = new DelegateCommand(async () =>
            {
                if (string.IsNullOrWhiteSpace(CurrentTitle))
                    return;
                try
                {
                    ApiResponse<MemoDto> result;
                    if (CurrentDto != null && CurrentDto.Id > 0)
                    {
                        CurrentDto.Title = CurrentTitle;
                        CurrentDto.Content = CurrentContent;
                        result = await _memoService.UpdateAsync(CurrentDto);
                    }
                    else
                    {
                        var dto = new MemoDto
                        {
                            Title = CurrentTitle,
                            Content = CurrentContent,
                            Status = 0
                        };
                        result = await _memoService.AddAsync(dto);
                    }

                    if (result.Status)
                    {
                        IsRightDrawerOpen = false;
                        await LoadMemosAsync();
                    }
                }
                catch { /* network errors are handled in service layer */ }
            });

            DeleteCommand = new DelegateCommand<MemoDto>(async memo =>
            {
                if (memo != null)
                {
                    try
                    {
                        var result = await _memoService.DeleteAsync(memo.Id);
                        if (result.Status)
                            MemoDtos.Remove(memo);
                    }
                    catch { /* network errors are handled in service layer */ }
                }
            });

            SearchCommand = new DelegateCommand(async () => await LoadMemosAsync());

            _ = LoadMemosAsync();
        }

        private async Task LoadMemosAsync()
        {
            var result = await _memoService.GetAllAsync(Search);
            if (result.Status && result.Result != null)
                MemoDtos = new ObservableCollection<MemoDto>(result.Result);
        }
    }
}
