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
        public ToDoViewModel()
        {
            CreateToDoDtos();
            AddCommand = new DelegateCommand((() => { 
                IsRightDrawerOpen=!IsRightDrawerOpen;
            }));
        }

        public DelegateCommand AddCommand { get; set; }
        private ObservableCollection<ToDoDto> toDoDtos;

        public ObservableCollection<ToDoDto> ToDoDtos
        {
            get { return toDoDtos; }
            set { toDoDtos = value; RaisePropertyChanged(); }
        }
        void CreateToDoDtos()
        {
            ToDoDtos = new ObservableCollection<ToDoDto>();

            for (int i = 0; i < 20; i++)
            {
                ToDoDtos.Add(new ToDoDto { Title = $"吃饭{i}", Content = $"吃饭了{i}", Status = 0 });
            }
        }
    }
}
