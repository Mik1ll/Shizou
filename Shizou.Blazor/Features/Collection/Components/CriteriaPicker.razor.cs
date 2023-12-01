using Microsoft.AspNetCore.Components;
using Shizou.Data.FilterCriteria;
using Shizou.Data.Models;

namespace Shizou.Blazor.Features.Collection.Components;

public partial class CriteriaPicker
{
    private bool _isSumOfProducts;

    private Dictionary<string, Type> _validTermTypes = new() { { nameof(AirDateCriterion), typeof(AirDateCriterion) } };

    [Parameter]
    [EditorRequired]
    public AnimeFilter Filter { get; set; } = default!;

    private void AddOrUpdate(OrAnyCriterion or, AndAllCriterion? and, int index, string? newTermType)
    {
        if (string.IsNullOrWhiteSpace(newTermType))
        {
            if (and?.Criteria.Count > index)
                and.Criteria.RemoveAt(index);
            or.Criteria.RemoveAll(a => a.Criteria.Count == 0);
            return;
        }

        var term = _validTermTypes[newTermType] switch
        {
            { } t when t == typeof(AirDateCriterion) => new AirDateCriterion(false, AirDateCriterionType.Before),
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
