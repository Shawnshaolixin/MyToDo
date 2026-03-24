using MyToDo.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MyToDo.ViewModels
{
    public class MemoViewModel : BindableBase
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

        private ObservableCollection<MemoDto> memoDtos;
        public ObservableCollection<MemoDto> MemoDtos
        {
            get { return memoDtos; }
            set { memoDtos = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<MemoDto> allMemoDtos;

        public DelegateCommand AddCommand { get; set; }
        public DelegateCommand SaveCommand { get; set; }
        public DelegateCommand<MemoDto> DeleteCommand { get; set; }
        public DelegateCommand SearchCommand { get; set; }

        public MemoViewModel()
        {
            CreateMemoDtos();

            AddCommand = new DelegateCommand(() =>
            {
                CurrentTitle = string.Empty;
                CurrentContent = string.Empty;
                IsRightDrawerOpen = true;
            });

            SaveCommand = new DelegateCommand(() =>
            {
                if (string.IsNullOrWhiteSpace(CurrentTitle))
                    return;
                var memo = new MemoDto
                {
                    Title = CurrentTitle,
                    Content = CurrentContent,
                    Status = 0  // 0 = active/normal memo (MemoDto.Status is inherited from base model)
                };
                allMemoDtos.Add(memo);
                MemoDtos.Add(memo);
                IsRightDrawerOpen = false;
            });

            DeleteCommand = new DelegateCommand<MemoDto>(memo =>
            {
                if (memo != null)
                {
                    allMemoDtos.Remove(memo);
                    MemoDtos.Remove(memo);
                }
            });

            SearchCommand = new DelegateCommand(() =>
            {
                if (string.IsNullOrWhiteSpace(Search))
                {
                    MemoDtos = new ObservableCollection<MemoDto>(allMemoDtos);
                }
                else
                {
                    var keyword = Search.Trim().ToLower();
                    var filtered = new ObservableCollection<MemoDto>();
                    foreach (var item in allMemoDtos)
                    {
                        if ((item.Title != null && item.Title.ToLower().Contains(keyword)) ||
                            (item.Content != null && item.Content.ToLower().Contains(keyword)))
                        {
                            filtered.Add(item);
                        }
                    }
                    MemoDtos = filtered;
                }
            });
        }

        void CreateMemoDtos()
        {
            allMemoDtos = new ObservableCollection<MemoDto>
            {
                new MemoDto { Title = "购物清单", Content = "牛奶、面包、鸡蛋", Status = 0 },
                new MemoDto { Title = "会议记录", Content = "讨论项目进度和下阶段目标", Status = 0 },
                new MemoDto { Title = "读书笔记", Content = "《深入理解计算机系统》第三章要点", Status = 0 },
                new MemoDto { Title = "健身计划", Content = "每周三次有氧，两次力量训练", Status = 0 },
            };
            MemoDtos = new ObservableCollection<MemoDto>(allMemoDtos);
        }
    }
}
