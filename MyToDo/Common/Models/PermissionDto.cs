namespace MyToDo.Common.Models
{
    public class PermissionDto : BaseDto
    {
        private string code = string.Empty;
        public string Code
        {
            get => code;
            set { code = value; RaisePropertyChanged(); }
        }

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

        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set { isSelected = value; RaisePropertyChanged(); }
        }
    }
}
