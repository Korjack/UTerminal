using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using ReactiveUI;
using UTerminal.ViewModels;

namespace UTerminal.Views;

public partial class PresetModeView : ReactiveWindow<PresetModeViewModel>
{
    public PresetModeView()
    {
        InitializeComponent();
        
        // Folder Interaction Handler
        this.WhenActivated(d =>
        {
            ViewModel?.ShowFilePickerInteraction
                .RegisterHandler(ShowFilePickerAsync)
                .DisposeWith(d);
        });
    }
    
    /// <summary>
    /// Folder sweep as instructed in the Avalonia UI sample
    /// </summary>
    private async Task ShowFilePickerAsync(IInteractionContext<Unit, IReadOnlyList<IStorageFile>> context)
    {
        // Get our parent top level control in order to get the needed service (in our sample the storage provider. Can also be the clipboard etc.)
        var topLevel = TopLevel.GetTopLevel(this);

        var files = await topLevel!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "파일 선택",
            AllowMultiple = true,
            FileTypeFilter =
            [
                new FilePickerFileType("텍스트 파일")
                {
                    Patterns = ["*.json"]
                }
            ]
        });

        // 결과 반환
        context.SetOutput(files);
    }
}