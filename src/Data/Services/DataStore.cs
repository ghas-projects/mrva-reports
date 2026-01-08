using System.Collections.Immutable;
using MRVA.Reports.Data.Helpers;
using MRVA.Reports.Data.Models;
using SolTechnology.Avro;

namespace MRVA.Reports.Data.Services;

public class DataStore
{
    
    private readonly List<Rule> _ruleList;
    
    public DataStore()
    {
        var bytes = ResourceHelper.GetResource("rule.avro");
        _ruleList = AvroConvert.Deserialize<List<Rule>>(bytes) ?? [];
    }
    
    public IList<Rule> ListRule()
    {
        return _ruleList
            .OrderBy(rule => rule.RuleId)
            .ToImmutableList();
    }

    public Rule? SingleRule(int rowId)
    {
        return _ruleList
            .FirstOrDefault(rule => rule.RowId == rowId);
    }
    
}