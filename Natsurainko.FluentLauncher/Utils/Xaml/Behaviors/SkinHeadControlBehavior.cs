﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;
using Natsurainko.FluentLauncher.Services.Storage;
using Nrk.FluentCore.Classes.Datas.Authenticate;

namespace Natsurainko.FluentLauncher.Utils.Xaml.Behaviors;

internal class SkinHeadControlBehavior : DependencyObject, IBehavior
{
    public Account Account
    {
        get { return (Account)GetValue(AccountProperty); }
        set { SetValue(AccountProperty, value); }
    }

    // Using a DependencyProperty as the backing store for Account.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty AccountProperty =
        DependencyProperty.Register("Account", typeof(Account), typeof(SkinHeadControlBehavior), new PropertyMetadata(null));

    private readonly SkinCacheService skinCacheService = App.GetService<SkinCacheService>();

    public DependencyObject AssociatedObject { get; set; }

    public void Attach(DependencyObject associatedObject)
    {
        if (associatedObject is not Border) return;

        AssociatedObject = associatedObject;
        Border border = (Border)associatedObject;
        border.Loaded += Border_Loaded;
        border.Unloaded += Border_Unloaded;

    }

    private void Border_Unloaded(object sender, RoutedEventArgs e)
    {
        ((Border)AssociatedObject).Loaded -= Border_Loaded;
    }

    private void Border_Loaded(object sender, RoutedEventArgs e)
    {
        if (Account != null)
            skinCacheService.SetSkinHeadControlContent((Border)AssociatedObject, Account);
    }

    public void Detach()
    {

    }
}
