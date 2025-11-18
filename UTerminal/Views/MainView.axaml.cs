using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using ReactiveUI;
using UTerminal.ViewModels;

namespace UTerminal.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();

        // Folder Interaction Handler
        this.WhenActivated(d => { d(ViewModel!.SelectFolderInteraction.RegisterHandler(InteractionHandler)); });
    }

    /// <summary>
    /// Folder sweep as instructed in the Avalonia UI sample
    /// </summary>
    private async Task InteractionHandler(IInteractionContext<string?, string?> context)
    {
        // Get our parent top level control in order to get the needed service (in our sample the storage provider. Can also be the clipboard etc.)
        var topLevel = TopLevel.GetTopLevel(this);

        var storageFolders = await topLevel!.StorageProvider
            .OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title = "Chose Folder Path",
                    AllowMultiple = false
                });

        context.SetOutput(storageFolders.Count > 0 ? storageFolders[0].Path.LocalPath : string.Empty);
    }
}