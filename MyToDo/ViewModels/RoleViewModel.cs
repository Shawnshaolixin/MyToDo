using MyToDo.Common.Models;
using MyToDo.Service;
using System.Collections.ObjectModel;

namespace MyToDo.ViewModels
{
    public class RoleViewModel : BindableBase
    {
        private readonly IRoleService _roleService;

        private bool isRoleDrawerOpen;
        public bool IsRoleDrawerOpen
        {
            get => isRoleDrawerOpen;
            set { isRoleDrawerOpen = value; RaisePropertyChanged(); }
        }

        private bool isPermissionDrawerOpen;
        public bool IsPermissionDrawerOpen
        {
            get => isPermissionDrawerOpen;
            set { isPermissionDrawerOpen = value; RaisePropertyChanged(); }
        }

        private bool isAssignPermOpen;
        public bool IsAssignPermOpen
        {
            get => isAssignPermOpen;
            set { isAssignPermOpen = value; RaisePropertyChanged(); }
        }

        private string roleSearch = string.Empty;
        public string RoleSearch
        {
            get => roleSearch;
            set { roleSearch = value; RaisePropertyChanged(); }
        }

        private RoleDto? currentRole;
        public RoleDto? CurrentRole
        {
            get => currentRole;
            set { currentRole = value; RaisePropertyChanged(); }
        }

        private string currentRoleName = string.Empty;
        public string CurrentRoleName
        {
            get => currentRoleName;
            set { currentRoleName = value; RaisePropertyChanged(); }
        }

        private string currentRoleDescription = string.Empty;
        public string CurrentRoleDescription
        {
            get => currentRoleDescription;
            set { currentRoleDescription = value; RaisePropertyChanged(); }
        }

        private PermissionDto? currentPermission;
        public PermissionDto? CurrentPermission
        {
            get => currentPermission;
            set { currentPermission = value; RaisePropertyChanged(); }
        }

        private string currentPermCode = string.Empty;
        public string CurrentPermCode
        {
            get => currentPermCode;
            set { currentPermCode = value; RaisePropertyChanged(); }
        }

        private string currentPermName = string.Empty;
        public string CurrentPermName
        {
            get => currentPermName;
            set { currentPermName = value; RaisePropertyChanged(); }
        }

        private string currentPermDescription = string.Empty;
        public string CurrentPermDescription
        {
            get => currentPermDescription;
            set { currentPermDescription = value; RaisePropertyChanged(); }
        }

        private RoleDto? assignTargetRole;
        public RoleDto? AssignTargetRole
        {
            get => assignTargetRole;
            set { assignTargetRole = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<RoleDto> roles = new();
        public ObservableCollection<RoleDto> Roles
        {
            get => roles;
            set { roles = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<PermissionDto> permissions = new();
        public ObservableCollection<PermissionDto> Permissions
        {
            get => permissions;
            set { permissions = value; RaisePropertyChanged(); }
        }

        public DelegateCommand AddRoleCommand { get; }
        public DelegateCommand SaveRoleCommand { get; }
        public DelegateCommand<RoleDto> DeleteRoleCommand { get; }
        public DelegateCommand<RoleDto> EditRoleCommand { get; }
        public DelegateCommand<RoleDto> AssignPermissionsCommand { get; }
        public DelegateCommand SaveAssignPermissionsCommand { get; }
        public DelegateCommand SearchRoleCommand { get; }

        public DelegateCommand AddPermissionCommand { get; }
        public DelegateCommand SavePermissionCommand { get; }
        public DelegateCommand<PermissionDto> DeletePermissionCommand { get; }
        public DelegateCommand<PermissionDto> EditPermissionCommand { get; }

        public RoleViewModel(IRoleService roleService)
        {
            _roleService = roleService;

            AddRoleCommand = new DelegateCommand(() =>
            {
                CurrentRole = null;
                CurrentRoleName = string.Empty;
                CurrentRoleDescription = string.Empty;
                IsRoleDrawerOpen = true;
            });

            EditRoleCommand = new DelegateCommand<RoleDto>(role =>
            {
                if (role == null) return;
                CurrentRole = role;
                CurrentRoleName = role.Name;
                CurrentRoleDescription = role.Description;
                IsRoleDrawerOpen = true;
            });

            SaveRoleCommand = new DelegateCommand(async () =>
            {
                if (string.IsNullOrWhiteSpace(CurrentRoleName)) return;
                try
                {
                    ApiResponse<RoleDto> result;
                    if (CurrentRole != null && CurrentRole.Id > 0)
                    {
                        CurrentRole.Name = CurrentRoleName;
                        CurrentRole.Description = CurrentRoleDescription;
                        result = await _roleService.UpdateAsync(CurrentRole);
                    }
                    else
                    {
                        var dto = new RoleDto { Name = CurrentRoleName, Description = CurrentRoleDescription };
                        result = await _roleService.AddAsync(dto);
                    }
                    if (result.Status)
                    {
                        IsRoleDrawerOpen = false;
                        await LoadRolesAsync();
                    }
                }
                catch { }
            });

            DeleteRoleCommand = new DelegateCommand<RoleDto>(async role =>
            {
                if (role == null) return;
                try
                {
                    var result = await _roleService.DeleteAsync(role.Id);
                    if (result.Status)
                        Roles.Remove(role);
                }
                catch { }
            });

            AssignPermissionsCommand = new DelegateCommand<RoleDto>(async role =>
            {
                if (role == null) return;
                AssignTargetRole = role;
                await LoadPermissionsAsync();
                foreach (var perm in Permissions)
                    perm.IsSelected = role.PermissionIds.Contains(perm.Id);
                IsAssignPermOpen = true;
            });

            SaveAssignPermissionsCommand = new DelegateCommand(async () =>
            {
                if (AssignTargetRole == null) return;
                try
                {
                    var selectedIds = Permissions.Where(p => p.IsSelected).Select(p => p.Id).ToList();
                    var result = await _roleService.AssignPermissionsAsync(AssignTargetRole.Id, selectedIds);
                    if (result.Status)
                    {
                        IsAssignPermOpen = false;
                        await LoadRolesAsync();
                    }
                }
                catch { }
            });

            SearchRoleCommand = new DelegateCommand(async () => await LoadRolesAsync());

            AddPermissionCommand = new DelegateCommand(() =>
            {
                CurrentPermission = null;
                CurrentPermCode = string.Empty;
                CurrentPermName = string.Empty;
                CurrentPermDescription = string.Empty;
                IsPermissionDrawerOpen = true;
            });

            EditPermissionCommand = new DelegateCommand<PermissionDto>(perm =>
            {
                if (perm == null) return;
                CurrentPermission = perm;
                CurrentPermCode = perm.Code;
                CurrentPermName = perm.Name;
                CurrentPermDescription = perm.Description;
                IsPermissionDrawerOpen = true;
            });

            SavePermissionCommand = new DelegateCommand(async () =>
            {
                if (string.IsNullOrWhiteSpace(CurrentPermCode) || string.IsNullOrWhiteSpace(CurrentPermName)) return;
                try
                {
                    ApiResponse<PermissionDto> result;
                    if (CurrentPermission != null && CurrentPermission.Id > 0)
                    {
                        CurrentPermission.Code = CurrentPermCode;
                        CurrentPermission.Name = CurrentPermName;
                        CurrentPermission.Description = CurrentPermDescription;
                        result = await _roleService.UpdatePermissionAsync(CurrentPermission);
                    }
                    else
                    {
                        var dto = new PermissionDto { Code = CurrentPermCode, Name = CurrentPermName, Description = CurrentPermDescription };
                        result = await _roleService.AddPermissionAsync(dto);
                    }
                    if (result.Status)
                    {
                        IsPermissionDrawerOpen = false;
                        await LoadPermissionsAsync();
                    }
                }
                catch { }
            });

            DeletePermissionCommand = new DelegateCommand<PermissionDto>(async perm =>
            {
                if (perm == null) return;
                try
                {
                    var result = await _roleService.DeletePermissionAsync(perm.Id);
                    if (result.Status)
                        Permissions.Remove(perm);
                }
                catch { }
            });

            _ = LoadRolesAsync();
            _ = LoadPermissionsAsync();
        }

        private async Task LoadRolesAsync()
        {
            var result = await _roleService.GetAllAsync(RoleSearch);
            if (result.Status && result.Result != null)
                Roles = new ObservableCollection<RoleDto>(result.Result);
        }

        private async Task LoadPermissionsAsync()
        {
            var result = await _roleService.GetAllPermissionsAsync();
            if (result.Status && result.Result != null)
                Permissions = new ObservableCollection<PermissionDto>(result.Result);
        }
    }
}
