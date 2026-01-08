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
    
    public List<Rule> GetRuleList()
    {
        return _ruleList;
    }

    public Rule? FindRuleByRowId(int rowId)
    {
        return _ruleList.FirstOrDefault(r => r.RowId == rowId);
    }
    
}