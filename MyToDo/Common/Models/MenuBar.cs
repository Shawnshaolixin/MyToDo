using System;
using System.Collections.Generic;
using System.Text;

namespace MyToDo.Common.Models
{
    public class MenuBar:BindableBase 
    {
        private string icon;
        public string Icon
        {
            get { return icon; }
            set { icon = value; }
        }
        private string title;
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        private string nameSpace;
        public string NameSpace
        {
            get { return nameSpace; }
            set { nameSpace = value; }
        }
    }
}
