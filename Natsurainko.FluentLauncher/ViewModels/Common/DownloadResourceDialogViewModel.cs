﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentLauncher.Infra.UI.Navigation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using Natsurainko.FluentLauncher.Services.Launch;
using Natsurainko.FluentLauncher.Utils;
using Nrk.FluentCore.Management;
using Nrk.FluentCore.Resources;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

#nullable disable
namespace Natsurainko.FluentLauncher.ViewModels.Common;

internal partial class DownloadResourceDialogViewModel : ObservableObject
{
    private ContentDialog _dialog;
    private ResourceFileItem[] ResourceFileItems;

    private readonly object _resource;
    private readonly INavigationService _navigationService;

    private readonly CurseForgeClient _curseForgeClient = App.GetService<CurseForgeClient>();
    private readonly ModrinthClient _modrinthClient = App.GetService<ModrinthClient>();
    private readonly GameService _gameService = App.GetService<GameService>();

    public GameInfo GameInfo { get; private set; }

    public DownloadResourceDialogViewModel(object resource, INavigationService navigationService)
    {
        _resource = resource;
        _navigationService = navigationService;

        GameInfo = _gameService.ActiveGame;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SelectedLoader)
            || e.PropertyName == nameof(SelectedVersion))
            FilterFiles();

        if (e.PropertyName == nameof(DownloadToCurrentGame)
            || e.PropertyName == nameof(DownloadToDesignated)
            || e.PropertyName == nameof(IsModpack))
        {
            if (DownloadToCurrentGame) DownloadToDesignated = IsModpack = false;
            if (DownloadToDesignated) DownloadToCurrentGame = IsModpack = false;
            if (IsModpack) DownloadToDesignated = DownloadToCurrentGame = false;

            if (DownloadToCurrentGame || DownloadToDesignated || IsModpack)
            {
                CheckBox1 = DownloadToCurrentGame ? Visibility.Visible : Visibility.Collapsed;
                CheckBox2 = DownloadToDesignated ? Visibility.Visible : Visibility.Collapsed;
                CheckBox3 = IsModpack ? Visibility.Visible : Visibility.Collapsed;
            }
            else CheckBox1 = CheckBox2 = CheckBox3 = Visibility.Visible;
        }
    }

    [ObservableProperty]
    private string[] versions;

    [ObservableProperty]
    private string selectedVersion;

    [ObservableProperty]
    private string[] loaders;

    [ObservableProperty]
    private string selectedLoader;

    [ObservableProperty]
    private string resourceName;

    [ObservableProperty]
    private ResourceFileItem[] displayItems;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private ResourceFileItem selectedItem;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private bool downloadToDesignated;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private bool downloadToCurrentGame;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private bool isModpack;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    private string designatedFilePath;

    [ObservableProperty]
    private Visibility checkBox1;

    [ObservableProperty]
    private Visibility checkBox2;

    [ObservableProperty]
    private Visibility checkBox3;

    public bool CanConfirm 
    {
        get
        {
            if (SelectedItem == null
                || !(DownloadToCurrentGame || DownloadToDesignated || IsModpack)
                || (DownloadToDesignated && string.IsNullOrEmpty(DesignatedFilePath)))
                return false;

            return true;
        }
    }

    [RelayCommand]
    public void LoadEvent(object args)
    {
        var grid = args.As<Grid, object>().sender;
        _dialog = grid.FindName("Dialog") as ContentDialog;

        Task.Run(ParseResource);
    }

    [RelayCommand]
    public void Cancel() => _dialog.Hide();

    [RelayCommand]
    public void Confirm()
    {

    }

    [RelayCommand]
    public void SaveFile()
    {
        var saveFileDialog = new SaveFileDialog
        {
            FileName = SelectedItem == null ? string.Empty : SelectedItem.FileName,
            InitialDirectory = _gameService.ActiveMinecraftFolder
        };

        if (saveFileDialog.ShowDialog().GetValueOrDefault())
            DesignatedFilePath = saveFileDialog.FileName;
    }

    async void ParseResource()
    {
        string resourceName = string.Empty;

        var fileItems = new List<ResourceFileItem>();
        var loaders = new List<string>();
        var versions = new List<string>();

        if (_resource is CurseForgeResource curseForgeResource)
        {
            resourceName = curseForgeResource.Name;

            foreach (var x in curseForgeResource.Files)
            {
                var loader = x.ModLoaderType.ToString();

                if (!loaders.Contains(loader))
                    loaders.Add(loader);

                if (!versions.Contains(x.McVersion))
                    versions.Add(x.McVersion);

                fileItems.Add(new ResourceFileItem(_curseForgeClient.GetFileUrlAsync(x))
                {
                    FileName = x.FileName,
                    Version = x.McVersion,
                    Loaders = [loader]
                });
            }
        }
        else if (_resource is ModrinthResource modrinthResource)
        {
            resourceName = modrinthResource.Name;

            foreach (var x in await _modrinthClient.GetProjectVersionsAsync(modrinthResource.Id))
            {
                foreach (var loader in x.Loaders)
                    if (!loaders.Contains(loader))
                        loaders.Add(loader);

                if (!versions.Contains(x.McVersion))
                    versions.Add(x.McVersion);

                fileItems.Add(new ResourceFileItem(Task.FromResult(x.Url))
                {
                    FileName = x.FileName,
                    Version = x.McVersion,
                    Loaders = x.Loaders
                });
            }
        }

        ResourceFileItems = [.. fileItems];

        App.DispatcherQueue.TryEnqueue(() =>
        {
            ResourceName = resourceName;
            Versions = [.. versions];
            Loaders = [.. loaders];

            SelectedVersion = Versions.First();
            SelectedLoader = Loaders.First();

            FilterFiles();
        });
    }

    void FilterFiles() => DisplayItems = [.. ResourceFileItems.Where(x => x.Version == SelectedVersion && ( x.Loaders.Contains("Any") || x.Loaders.Contains(SelectedLoader)))];

    internal class ResourceFileItem
    {
        private readonly Task<string> getUrl;

        public ResourceFileItem(Task<string> func) => getUrl = func;

        public string[] Loaders { get; set; }

        public string FileName { get; set; }

        public string Version { get; set; }

        public Task<string> GetUrl() => getUrl;
    }
}
