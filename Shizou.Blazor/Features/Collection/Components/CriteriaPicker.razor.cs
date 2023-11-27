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

    protected override void OnInitialized()
    {
        _isSumOfProducts = Filter.Criteria is OrAnyCriterion or &&
                           or.Criteria.All(c => c is AndAllCriterion and &&
                                                and.Criteria.All(t => _validTermTypes.ContainsValue(t.GetType())));
    }

    private void AddOrUpdate(OrAnyCriterion or, AndAllCriterion? and, int index, string newTermType)
    {
        var term = and?.Criteria[index];
        if (term is null)
        {
        }
    }
}
