using System;
using System.Collections.Generic;

namespace MyToDo.Common.Models
{
    public class UserDto : BaseDto
    {
        private string userName = string.Empty;
        public string UserName
        {
            get => userName;
            set { userName = value; RaisePropertyChanged(); }
        }

        private string password = string.Empty;
        public string Password
        {
            get => password;
            set { password = value; RaisePropertyChanged(); }
        }

        private string email = string.Empty;
        public string Email
        {
            get => email;
            set { email = value; RaisePropertyChanged(); }
        }

        private int status = 1;
        public int Status
        {
            get => status;
            set { status = value; RaisePropertyChanged(); }
        }

        private List<int> roleIds = new();
        public List<int> RoleIds
        {
            get => roleIds;
            set { roleIds = value; RaisePropertyChanged(); }
        }

        private List<string> roleNames = new();
        public List<string> RoleNames
        {
            get => roleNames;
            set { roleNames = value; RaisePropertyChanged(); }
        }

        public string RoleNamesText => string.Join(", ", RoleNames);
        public string StatusText => Status == 1 ? "启用" : "禁用";
    }
}
