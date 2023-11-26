using Microsoft.AspNetCore.Components;
using Shizou.Data.FilterCriteria;
using Shizou.Data.Models;

namespace Shizou.Blazor.Features.Collection.Components;

public partial class CriteriaPicker
{
    private bool _isSumOfProducts;

    private List<Type> _validCriterions = new() { typeof(AirDateCriterion) };

    [Parameter]
    [EditorRequired]
    public AnimeFilter Filter { get; set; } = default!;

    protected override void OnInitialized()
    {
        Filter.Criteria = new OrAnyCriterion(new AndAllCriterion());
        _isSumOfProducts = Filter.Criteria is OrAnyCriterion or &&
                           or.Criteria.All(c => c is AndAllCriterion and &&
                                                and.Criteria.All(t =>
                                                    t is not OrAnyCriterion && t is not AndAllCriterion));
    }
}
