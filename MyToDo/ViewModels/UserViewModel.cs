using MyToDo.Common.Models;
using MyToDo.Service;
using System.Collections.ObjectModel;

namespace MyToDo.ViewModels
{
    public class UserViewModel : BindableBase
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;

        private bool isRightDrawerOpen;
        public bool IsRightDrawerOpen
        {
            get => isRightDrawerOpen;
            set { isRightDrawerOpen = value; RaisePropertyChanged(); }
        }

        private string search = string.Empty;
        public string Search
        {
            get => search;
            set { search = value; RaisePropertyChanged(); }
        }

        private UserDto? currentDto;
        public UserDto? CurrentDto
        {
            get => currentDto;
            set { currentDto = value; RaisePropertyChanged(); }
        }

        private string currentUserName = string.Empty;
        public string CurrentUserName
        {
            get => currentUserName;
            set { currentUserName = value; RaisePropertyChanged(); }
        }

        private string currentPassword = string.Empty;
        public string CurrentPassword
        {
            get => currentPassword;
            set { currentPassword = value; RaisePropertyChanged(); }
        }

        private string currentEmail = string.Empty;
        public string CurrentEmail
        {
            get => currentEmail;
            set { currentEmail = value; RaisePropertyChanged(); }
        }

        private int currentStatus = 1;
        public int CurrentStatus
        {
            get => currentStatus;
            set { currentStatus = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<UserDto> userDtos = new();
        public ObservableCollection<UserDto> UserDtos
        {
            get => userDtos;
            set { userDtos = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<RoleDto> allRoles = new();
        public ObservableCollection<RoleDto> AllRoles
        {
            get => allRoles;
            set { allRoles = value; RaisePropertyChanged(); }
        }

        private bool isAssignRolesOpen;
        public bool IsAssignRolesOpen
        {
            get => isAssignRolesOpen;
            set { isAssignRolesOpen = value; RaisePropertyChanged(); }
        }

        private UserDto? assignTargetUser;
        public UserDto? AssignTargetUser
        {
            get => assignTargetUser;
            set { assignTargetUser = value; RaisePropertyChanged(); }
        }

        public DelegateCommand AddCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand<UserDto> DeleteCommand { get; }
        public DelegateCommand<UserDto> EditCommand { get; }
        public DelegateCommand<UserDto> AssignRolesCommand { get; }
        public DelegateCommand SaveAssignRolesCommand { get; }
        public DelegateCommand SearchCommand { get; }

        public UserViewModel(IUserService userService, IRoleService roleService)
        {
            _userService = userService;
            _roleService = roleService;
            UserDtos = new ObservableCollection<UserDto>();
            AllRoles = new ObservableCollection<RoleDto>();

            AddCommand = new DelegateCommand(() =>
            {
                CurrentDto = null;
                CurrentUserName = string.Empty;
                CurrentPassword = string.Empty;
                CurrentEmail = string.Empty;
                CurrentStatus = 1;
                IsRightDrawerOpen = true;
            });

            EditCommand = new DelegateCommand<UserDto>(user =>
            {
                if (user == null) return;
                CurrentDto = user;
                CurrentUserName = user.UserName;
                CurrentPassword = string.Empty;
                CurrentEmail = user.Email;
                CurrentStatus = user.Status;
                IsRightDrawerOpen = true;
            });

            SaveCommand = new DelegateCommand(async () =>
            {
                if (string.IsNullOrWhiteSpace(CurrentUserName)) return;
                try
                {
                    ApiResponse<UserDto> result;
                    if (CurrentDto != null && CurrentDto.Id > 0)
                    {
                        CurrentDto.UserName = CurrentUserName;
                        CurrentDto.Password = CurrentPassword;
                        CurrentDto.Email = CurrentEmail;
                        CurrentDto.Status = CurrentStatus;
                        result = await _userService.UpdateAsync(CurrentDto);
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(CurrentPassword)) return;
                        var dto = new UserDto
                        {
                            UserName = CurrentUserName,
                            Password = CurrentPassword,
                            Email = CurrentEmail,
                            Status = CurrentStatus
                        };
                        result = await _userService.AddAsync(dto);
                    }
                    if (result.Status)
                    {
                        IsRightDrawerOpen = false;
                        await LoadUsersAsync();
                    }
                }
                catch { }
            });

            DeleteCommand = new DelegateCommand<UserDto>(async user =>
            {
                if (user == null) return;
                try
                {
                    var result = await _userService.DeleteAsync(user.Id);
                    if (result.Status)
                        UserDtos.Remove(user);
                }
                catch { }
            });

            AssignRolesCommand = new DelegateCommand<UserDto>(async user =>
            {
                if (user == null) return;
                AssignTargetUser = user;
                await LoadAllRolesAsync();
                foreach (var role in AllRoles)
                    role.IsSelected = user.RoleIds.Contains(role.Id);
                IsAssignRolesOpen = true;
            });

            SaveAssignRolesCommand = new DelegateCommand(async () =>
            {
                if (AssignTargetUser == null) return;
                try
                {
                    var selectedIds = AllRoles.Where(r => r.IsSelected).Select(r => r.Id).ToList();
                    var result = await _userService.AssignRolesAsync(AssignTargetUser.Id, selectedIds);
                    if (result.Status)
                    {
                        IsAssignRolesOpen = false;
                        await LoadUsersAsync();
                    }
                }
                catch { }
            });

            SearchCommand = new DelegateCommand(async () => await LoadUsersAsync());

            _ = LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            var result = await _userService.GetAllAsync(Search);
            if (result.Status && result.Result != null)
                UserDtos = new ObservableCollection<UserDto>(result.Result);
        }

        private async Task LoadAllRolesAsync()
        {
            var result = await _roleService.GetAllAsync();
            if (result.Status && result.Result != null)
                AllRoles = new ObservableCollection<RoleDto>(result.Result);
        }
    }
}
