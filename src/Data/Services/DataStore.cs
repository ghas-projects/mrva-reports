using System.Collections.Immutable;
using MRVA.Reports.Data.Helpers;
using MRVA.Reports.Data.Models;

namespace MRVA.Reports.Data.Services;

public class DataStore
{
    
    public IReadOnlySet<Rule> RuleSet { get; }
    
    public IReadOnlySet<Repository> RepositorySet { get; }
    
    public IReadOnlySet<Run> RunSet { get; }
    
    public IReadOnlySet<Alert> AlertSet { get; }
    
    public DataStore()
    {
        RuleSet = LoadSet("rule.pb", RuleList.Parser, list => list.Rules);
        RepositorySet = LoadSet("repository.pb", RepositoryList.Parser, list => list.Repositories);
        RunSet = LoadSet("run.pb", RunList.Parser, list => list.Runs);
        AlertSet = LoadSet("alert.pb", AlertList.Parser, list => list.Alerts);
    }
    
    private static IReadOnlySet<T> LoadSet<TList, T>(
        string resourceName,
        Google.Protobuf.MessageParser<TList> parser,
        Func<TList, IEnumerable<T>> selector)
        where TList : Google.Protobuf.IMessage<TList>
    {
        var bytes = ResourceHelper.GetResource(resourceName);
        
        if (bytes.IsEmpty)
        {
            return new HashSet<T>();
        }
        
        return selector(parser.ParseFrom(bytes)).ToHashSet();
    }

}
