﻿using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Natsurainko.FluentLauncher.ViewModels.Common;
using Natsurainko.FluentLauncher.Views.AuthenticationWizard;
using Nrk.FluentCore.Classes.Datas.Authenticate;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace Natsurainko.FluentLauncher.ViewModels.AuthenticationWizard;

internal partial class ConfirmProfileViewModel : WizardViewModelBase
{
    public override bool CanCancel => Loading == Visibility.Collapsed;

    public override bool CanPrevious => Loading == Visibility.Collapsed;

    public override bool CanNext => Loading == Visibility.Collapsed && SelectedAccount != null;

    private readonly Func<IEnumerable<Account>> _authenticateAction;

    public ObservableCollection<Account> Accounts { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanNext))]
    private Visibility loading = Visibility.Collapsed;

    [ObservableProperty]
    private bool faulted;

    [ObservableProperty]
    private string faultedMessage;

    [ObservableProperty]
    private string loadingProgressText;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanNext))]
    private Account selectedAccount;

    public ConfirmProfileViewModel(Func<IEnumerable<Account>> authenticateAction)
    {
        XamlPageType = typeof(ConfirmProfilePage);
        _authenticateAction = authenticateAction;

        Accounts = new();

        Task.Run(() =>
        {
            App.MainWindow.DispatcherQueue.TryEnqueue(() => Loading = Visibility.Visible);
            var accountsList = new List<Account>(_authenticateAction());
            App.MainWindow.DispatcherQueue.TryEnqueue(() => 
            {
                Loading = Visibility.Collapsed;
                accountsList.ForEach(account => Accounts.Add(account));
            });
        }).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                App.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    Loading = Visibility.Collapsed;
                    Faulted = true;

                    var builder = new StringBuilder();
                    builder.AppendLine("Failed to fetch account list");
                    builder.AppendLine(task.Exception.InnerException.Message);
                    builder.AppendLine(task.Exception.InnerException.HelpLink);
                    builder.AppendLine(task.Exception.InnerException.StackTrace);

                    FaultedMessage = builder.ToString();
                });
            }
        });
    }
}