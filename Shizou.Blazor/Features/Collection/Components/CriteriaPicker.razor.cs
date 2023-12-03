using Microsoft.AspNetCore.Components;
using Shizou.Data.Enums;
using Shizou.Data.FilterCriteria;
using Shizou.Data.Models;

namespace Shizou.Blazor.Features.Collection.Components;

public partial class CriteriaPicker
{
    private Dictionary<string, Type> _validTermTypes = new()
    {
        { nameof(AirDateCriterion), typeof(AirDateCriterion) },
        { nameof(UnwatchedFilesCriterion), typeof(UnwatchedFilesCriterion) },
        { nameof(EpisodeWithoutFilesCriterion), typeof(EpisodeWithoutFilesCriterion) },
        { nameof(AnimeTypeCriterion), typeof(AnimeTypeCriterion) }
    };

    [Parameter]
    [EditorRequired]
    public AnimeFilter Filter { get; set; } = default!;

    private void Update(OrAnyCriterion or, AndAllCriterion? and, int index, string? newTermType)
    {
        if (string.IsNullOrWhiteSpace(newTermType))
        {
            if (and?.Criteria.Count > index)
                and.Criteria.RemoveAt(index);
            or.Criteria.RemoveAll(a => a.Criteria.Count == 0);
            return;
        }

        TermCriterion term = _validTermTypes[newTermType] switch
        {
            { } t when t == typeof(AirDateCriterion) => new AirDateCriterion(false, AirDateTermType.AirDate, AirDateTermRange.Before),
            { } t when t == typeof(UnwatchedFilesCriterion) => new UnwatchedFilesCriterion(false),
            { } t when t == typeof(EpisodeWithoutFilesCriterion) => new EpisodeWithoutFilesCriterion(false),
            { } t when t == typeof(AnimeTypeCriterion) => new AnimeTypeCriterion(false, AnimeType.TvSeries),
            _ => throw new ArgumentOutOfRangeException()
        };
        if (and?.Criteria.Count > index)
        {
            and.Criteria[index] = term;
        }
        else
        {
            if (and is null)
                or.Criteria.Add(and = new AndAllCriterion(new List<TermCriterion>()));
            and.Criteria.Add(term);
        }
    }

    private void Replace(AndAllCriterion and, int index, TermCriterion newCriterion)
    {
        and.Criteria[index] = newCriterion;
    }
}
