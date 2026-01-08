using System;
using System.Collections.ObjectModel;
using System.Reactive;
using ReactiveUI;
using UTerminal.Models.Parser.Interfaces;

namespace UTerminal.Models.Parser;

/// <summary>
/// 파싱 포맷 프리셋 (여러 ParseData의 묶음)
/// </summary>
public sealed class ParsePreset : ReactiveObject, IParsePreset
{
    private string _name = "새 포맷";
    private string _description = "";
    private bool _isValid;
    private int _cachedLength = -1;

    /// <summary>
    /// 프리셋 이름
    /// </summary>
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    /// <summary>
    /// 프리셋 설명
    /// </summary>
    public string Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    /// <summary>
    /// 현재 활성화된 프리셋인지 여부
    /// </summary>
    public bool IsValid
    {
        get => _isValid;
        set => this.RaiseAndSetIfChanged(ref _isValid, value);
    }

    /// <summary>
    /// 파싱 포맷 데이터 리스트
    /// </summary>
    public ObservableCollection<IParseData> ParseDataList { get; set; } = [];

    /// <summary>
    /// 생성 시간
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.Now;
    
    
    # region Commands
    
    public ReactiveCommand<ParseData, Unit> MoveUpCommand { get; }
    public ReactiveCommand<ParseData, Unit> MoveDownCommand { get; }
    public ReactiveCommand<ParseData, Unit> RemoveCommand { get; }
    
    
    # endregion

    #region Command Funcs

    /// <summary>
    /// 아이템을 위로 이동
    /// </summary>
    private void MoveItemUp(ParseData item)
    {
        int index = ParseDataList.IndexOf(item);
        if (index > 0)
        {
            ParseDataList.Move(index, index - 1);
        }
    }
    
    /// <summary>
    /// 아이템을 아래로 이동
    /// </summary>
    private void MoveItemDown(ParseData item)
    {
        int index = ParseDataList.IndexOf(item);
        if (index < ParseDataList.Count - 1)
        {
            ParseDataList.Move(index, index + 1);
        }
    }
    
    /// <summary>
    /// 아이템 제거
    /// </summary>
    private void RemoveItem(ParseData item)
    {
        ParseDataList.Remove(item);
        InvalidateCache();
    }

    #endregion
    
    public ParsePreset()
    {
        MoveUpCommand = ReactiveCommand.Create<ParseData>(MoveItemUp);
        MoveDownCommand = ReactiveCommand.Create<ParseData>(MoveItemDown);
        RemoveCommand = ReactiveCommand.Create<ParseData>(RemoveItem);
    }
    
    /// <summary>
    /// 캐시 무효화
    /// </summary>
    private void InvalidateCache()
    {
        _cachedLength = -1;
    }

    /// <summary>
    /// 전체 데이터 길이 계산
    /// </summary>
    public int GetTotalLength()
    {
        // 캐시가 유효하면 재사용
        if (_cachedLength >= 0)
            return _cachedLength;

        // LINQ 제거하고 foreach 사용
        int total = 0;
        foreach (var p in ParseDataList)
        {
            total += p.Size;  // ✅ Size 프로퍼티 사용
        }

        _cachedLength = total;
        return total;
    }

    /// <summary>
    /// 프리셋 복제
    /// </summary>
    public ParsePreset Clone()
    {
        var clone = new ParsePreset
        {
            Name = $"{Name} (복사본)",
            Description = Description,
        };

        foreach (var parseData in ParseDataList)
        {
            clone.ParseDataList.Add(new ParseData(clone, parseData.ParseDataType, parseData.Name, parseData.Length));
        }

        return clone;
    }
}