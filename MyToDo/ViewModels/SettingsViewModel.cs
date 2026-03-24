using MaterialDesignColors;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace MyToDo.ViewModels
{
    public class SettingsViewModel : BindableBase
    {
        private bool isDarkTheme = true;
        public bool IsDarkTheme
        {
            get { return isDarkTheme; }
            set
            {
                if (SetProperty(ref isDarkTheme, value))
                    ApplyTheme(value);
            }
        }

        public SettingsViewModel()
        {
        }

        private void ApplyTheme(bool dark)
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(dark ? BaseTheme.Dark : BaseTheme.Light);
            paletteHelper.SetTheme(theme);
        }
    }
}
