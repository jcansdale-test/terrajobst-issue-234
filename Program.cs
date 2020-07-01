using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;

class Program
{
    static async Task Main()
    {
        var apiKey = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        apiKey = apiKey ?? throw new ApplicationException("Please set the GITHUB_TOKEN environment variable");

        var start = DateTimeOffset.Parse("2020-06-26T09:30:00");
        var filter = new IssueFilters()
        {
            Assignee = "*",
            Milestone = "*",
            Since = start,
        };
        var query = new Query()
            .Repository("runtime", "dotnet")
            .Issues(filterBy: filter)
            .AllPages()
            .Select(i => new FeedbackIssue
            {
                Owner = i.Repository.Owner.Login,
                Repo = i.Repository.Name,
                Number = i.Number,
                Title = i.Title,
                CreateAt = i.CreatedAt,
                Author = i.Author.Login,
                State = i.State,
                Url = i.Url,
                TimelineItems = i
                    .TimelineItems(null, null, null, null, null, start, null)
                    .AllPages()
                    .Select(tl => tl.Switch<ApiTimelineItem>(when =>
                    when.IssueComment(ic => new ApiTimelineComment
                    {
                        CreatedAt = ic.CreatedAt
                    }).LabeledEvent(l => new ApiTimelineLabel
                    {
                        CreatedAt = l.CreatedAt
                    }).ClosedEvent(c => new ApiTimelineClosure
                    {
                        CreatedAt = c.CreatedAt
                    }))).ToList()
            }).Compile();

        var productInformation = new ProductHeaderValue("octokit.graphql.net");
        var connection = new Connection(productInformation, apiKey);

        try
        {
            Console.WriteLine(query);
            var current = await connection.Run(query);

            foreach(var foo in current)
            {
                Console.WriteLine(foo);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}

internal sealed class FeedbackIssue
{
    public string Owner { get; set; }
    public string Repo { get; set; }
    public int Number { get; set; }
    public DateTimeOffset CreateAt { get; set; }
    public string Author { get; set; }
    public string Title { get; set; }
    public IssueState State { get; set; }
    public string Url { get; set; }
    public List<ApiTimelineItem> TimelineItems { get; set; }

    public override string ToString()
    {
        return $"{Owner}/{Repo}#{Number}: {Title}";
    }
}

internal abstract class ApiTimelineItem
{
    public string Actor { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

internal sealed class ApiTimelineComment : ApiTimelineItem
{
    public string Id { get; set; }
    public string Body { get; set; }
    public string Url { get; set; }
}

internal sealed class ApiTimelineLabel : ApiTimelineItem
{
    public string Name { get; set; }
}

internal sealed class ApiTimelineClosure : ApiTimelineItem
{
}
