using System;
using System.Collections.ObjectModel;
using ReactiveUI;

namespace UTerminal.Models.Parser;

/// <summary>
/// 파싱 포맷 프리셋 (여러 ParseData의 묶음)
/// </summary>
public sealed class ParseFormatPreset : ReactiveObject
{
    private string _name = "새 포맷";
    private string _description = "";
    private bool _isValid = false;
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
    public ObservableCollection<ParseData> ParseFormat { get; set; } = new();

    /// <summary>
    /// 생성 시간
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

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
        foreach (var p in ParseFormat)
        {
            total += p.Size;  // ✅ Size 프로퍼티 사용
        }

        _cachedLength = total;
        return total;
    }

    /// <summary>
    /// 프리셋 복제
    /// </summary>
    public ParseFormatPreset Clone()
    {
        var clone = new ParseFormatPreset
        {
            Name = $"{Name} (복사본)",
            Description = Description,
        };

        foreach (var parseData in ParseFormat)
        {
            clone.ParseFormat.Add(new ParseData(parseData.ParseDataType, parseData.Name, parseData.Length));
        }

        return clone;
    }
}