using MyToDo.Common.Models;
using MyToDo.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MyToDo.ViewModels
{
    public class IndexViewModel : BindableBase
    {
        private readonly IToDoService _toDoService;
        private readonly IMemoService _memoService;

        public IndexViewModel(IToDoService toDoService, IMemoService memoService)
        {
            _toDoService = toDoService;
            _memoService = memoService;

            CreateTaskBars();
            _ = LoadSummaryAsync();
        }
        private ObservableCollection<TaskBar> taskBars;

        public ObservableCollection<TaskBar> TaskBars
        {
            get { return taskBars; }
            set { taskBars = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<ToDoDto> toDoDtos;

        public ObservableCollection<ToDoDto> ToDoDtos
        {
            get { return toDoDtos; }
            set { toDoDtos = value; RaisePropertyChanged(); }
        }


        private ObservableCollection<MemoDto> memoDtos;

        public ObservableCollection<MemoDto> MemoDtos
        {
            get { return memoDtos; }
            set { memoDtos = value; RaisePropertyChanged(); }
        }
        void CreateTaskBars()
        {
            TaskBars = new ObservableCollection<TaskBar>
            {
                new TaskBar{Icon="ClockFast",Title="汇总",Content="0",Color="#FF0AC0FF",Target=0},
                new TaskBar{Icon="ClockCheckOuline",Title="已完成",Content="0",Color="#FF0078D7",Target=1},
                new TaskBar{Icon="ChartLineVariant",Title="完成率",Content="0%",Color="#FF02C6DC",Target=2},
                new TaskBar{Icon="PlaylistStar",Title="备忘录",Content="0",Color="#FFFFA000",Target=3},
            };
        }

        private async Task LoadSummaryAsync()
        {
            var result = await _toDoService.GetSummaryAsync();
            if (result.Status && result.Result != null)
            {
                var summary = result.Result;
                TaskBars[0].Content = summary.ToDoCount.ToString();
                TaskBars[1].Content = summary.CompletedCount.ToString();
                TaskBars[2].Content = $"{summary.CompletedRatio}%";
                TaskBars[3].Content = summary.MemoCount.ToString();
                ToDoDtos = new ObservableCollection<ToDoDto>(summary.ToDoList);
                MemoDtos = new ObservableCollection<MemoDto>(summary.MemoList);
            }
        }
    }
}
