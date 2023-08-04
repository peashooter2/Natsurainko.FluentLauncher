﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Natsurainko.FluentLauncher.Utils;
using Nrk.FluentCore.Classes.Datas;
using Nrk.FluentCore.Classes.Datas.Launch;
using Nrk.FluentCore.DefaultComponents.Manage;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;

namespace Natsurainko.FluentLauncher.ViewModels.Cores.Manage;

internal partial class CoreModsViewModel: ObservableObject
{
    private readonly DefaultModsManager modsManager;

    public CoreModsViewModel(GameInfo gameInfo)
    {
        SupportMod = gameInfo.IsSupportMod();

        if (SupportMod)
        {
            ModsFolder = Path.Combine(gameInfo.GetGameDirectory(), "mods");
            if (!Directory.Exists(ModsFolder)) Directory.CreateDirectory(ModsFolder);
            modsManager = new DefaultModsManager(ModsFolder);

            Task.Run(() =>
            {
                Mods = modsManager.EnumerateMods().ToList();
                UpdateDisplayMods();
            });
        }
    }

    public bool SupportMod { get; init; }

    public string ModsFolder { get; init; }

    private IReadOnlyList<ModInfo> Mods { get; set; }

    [ObservableProperty]
    private IEnumerable<ModInfo> displayMods;

    [ObservableProperty]
    private string searchBoxInput;

    [RelayCommand]
    public void ToggleSwitchLoaded(object args)
    {
        void ToggleSwitch_Toggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var toggleSwitch = sender as ToggleSwitch;
            var modInfo = toggleSwitch.DataContext as ModInfo;

            if (modInfo != null)
                modsManager.Switch(modInfo, toggleSwitch.IsOn);
        }

        var toggleSwitch = args.As<ToggleSwitch, object>().sender;

        toggleSwitch.Toggled += ToggleSwitch_Toggled;
        toggleSwitch.Unloaded += (_, _) => toggleSwitch.Toggled -= ToggleSwitch_Toggled;
    }

    [RelayCommand]
    public void OpenFolder() => _ = Launcher.LaunchFolderPathAsync(ModsFolder);

    private void UpdateDisplayMods()
    {
        IEnumerable<ModInfo> mods = Mods;

        if (!string.IsNullOrEmpty(SearchBoxInput))
            mods = mods.Where(x => x.DisplayName.ToLower().Contains(SearchBoxInput.ToLower()));

        mods = mods.ToList();

        App.MainWindow.DispatcherQueue.TryEnqueue(() => DisplayMods = mods);
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SearchBoxInput))
            Task.Run(UpdateDisplayMods);
    }
}