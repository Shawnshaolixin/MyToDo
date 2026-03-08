using MyToDo.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace MyToDo.ViewModels
{
    public class IndexViewModel : BindableBase
    {

        public IndexViewModel()
        {
            CreateTaskBars();
            CreateToDoDtos();
            CreateMemoDtos();
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
                new TaskBar{Icon="ClockFast",Title="汇总",Content="待办事项内容",Color="#FF0AC0FF",Target=0},
                new TaskBar{Icon="ClockCheckOuline",Title="已完成",Content="日程安排内容",Color="#FF0078D7",Target=1},
                new TaskBar{Icon="ChartLineVariant",Title="完成率",Content="100%",Color="#FF02C6DC",Target=2},
                new TaskBar{Icon="PlaylistStar",Title="备忘录",Content="联系人内容",Color="#FFFFA000",Target=3},
            };
        }

        void CreateToDoDtos()
        {
            ToDoDtos = new ObservableCollection<ToDoDto>
            {
                new ToDoDto{Title="吃饭",Content="吃饭了",Status=0},
                new ToDoDto{Title="睡觉",Content="睡觉了",Status=0},
                new ToDoDto{Title="打豆豆",Content="打豆豆了",Status=0},
                new ToDoDto{Title="打豆豆",Content="打豆豆了",Status=0},
                new ToDoDto{Title="打豆豆",Content="打豆豆了",Status=0},
            };
        }
        void CreateMemoDtos()
        {
            MemoDtos = new ObservableCollection<MemoDto>
            {
                new MemoDto{Title="吃饭",Content="吃饭了",Status=0},
                new MemoDto{Title="睡觉",Content="睡觉了",Status=0},
                new MemoDto{Title="打豆豆",Content="打豆豆了",Status=0},
                new MemoDto {Title="打豆豆",Content="打豆豆了",Status=0}, 
            };
        }

    }
}
