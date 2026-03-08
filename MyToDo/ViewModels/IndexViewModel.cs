using MyToDo.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MyToDo.ViewModels
{
    public class IndexViewModel:BindableBase
    {

        public IndexViewModel()
        {
            CreateTaskBars();
        }
        private ObservableCollection<TaskBar> taskBars;

        public ObservableCollection<TaskBar> TaskBars
        {
            get { return taskBars; }
            set { taskBars = value; RaisePropertyChanged(); }
        }

        void CreateTaskBars()
        {
            TaskBars = new ObservableCollection<TaskBar>
            {
                new TaskBar{Icon="ClockFast",Title="汇总",Content="待办事项内容",Color="#FF0AC0FF",Target=0},
                new TaskBar{Icon="ClockCheckOuline",Title="已完成",Content="日程安排内容",Color="#FF0078D7",Target=1},
                new TaskBar{Icon="ChartLineVariant",Title="完成率",Content="100%",Color="#FF02C6DC",Target=2},
                new TaskBar{Icon="PlaylistStar",Title="备忘录",Content="联系人内容",Color="#FFFFA000",Target=3},
            };
        }   

    }
}
