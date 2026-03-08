using System;
using System.Collections.Generic;
using System.Text;

namespace MyToDo.Common.Models
{
    public class TaskBar:BindableBase
    {
        private string icon;
        public string Icon
        {
            get => icon;
            set
            {
                icon = value;
            }
        }
        private string title;
        public string Title
        {
            get => title;
            set
            {
                title = value;
            }
        }

        private string content;
        public string Content
        {
            get => content;
            set
            {
                content = value;
            }
        }

        private string color;

        public string Color
        {
            get { return color; }
            set { color = value; }
        }

        private int target;

        public int Target
        {
            get { return target; }
            set { target = value; }
        }

    }
}
