using System.Collections.Generic;

namespace MyToDo.Common.Models
{
    public class RoleDto : BaseDto
    {
        private string name = string.Empty;
        public string Name
        {
            get => name;
            set { name = value; RaisePropertyChanged(); }
        }

        private string description = string.Empty;
        public string Description
        {
            get => description;
            set { description = value; RaisePropertyChanged(); }
        }

        private List<int> permissionIds = new();
        public List<int> PermissionIds
        {
            get => permissionIds;
            set { permissionIds = value; RaisePropertyChanged(); }
        }

        private List<string> permissionNames = new();
        public List<string> PermissionNames
        {
            get => permissionNames;
            set { permissionNames = value; RaisePropertyChanged(); }
        }

        public string PermissionNamesText => string.Join(", ", PermissionNames);

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set { isSelected = value; RaisePropertyChanged(); }
        }
    }
}
