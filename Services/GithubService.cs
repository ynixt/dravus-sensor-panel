using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DravusSensorPanel.Models;
using Octokit;

namespace DravusSensorPanel.Services;

public class GithubService {
    private readonly GitHubClient _client = new(new ProductHeaderValue("dravus-sensor-panel"));

    public async Task<(bool, List<GithubSensorPanel>)> ListPanels(
        RepoSearchSort sort,
        SortDirection direction,
        int page = 0) {
        int perPage = 20;

        var search = new SearchRepositoriesRequest("topic:dravus-sensor-panel") {
            SortField = sort,
            Order = direction,
            PerPage = perPage,
            Page = page,
        };


        SearchRepositoryResult? result = await _client.Search.SearchRepo(search);

        int totalPages = ( int ) Math.Ceiling(result.TotalCount / ( double ) perPage);
        bool hasNextPage = page + 1 < totalPages;
        List<GithubSensorPanel>? items = result.Items.Select(item => new GithubSensorPanel {
                                                   Name = item.Name, Description = item.Description, Url = item.HtmlUrl,
                                                   Stars = item.StargazersCount,
                                                   UpdatedAt = item.UpdatedAt, Author = item.Owner.Login,
                                               })
                                               .ToList();

        return ( hasNextPage, items );
    }

    public async Task GetReadme(GithubSensorPanel item) {
        if ( item.Readme != null ) return;

        Readme? content = await _client.Repository.Content.GetReadme(item.Author, item.Name);

        if ( content != null ) {
            item.Readme = content.Content;
        }
    }

    public async Task<string?> DownloadSensorPanel(GithubSensorPanel item) {
        byte[]? contentBytes = await _client.Repository.Content.GetRawContent(item.Author, item.Name, "panel.dravus");

        if ( contentBytes == null ) {
            return null;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), $"dravus_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        string path = Path.Combine(tempDir, "panel.dravus");
        await File.WriteAllBytesAsync(path, contentBytes);

        return path;
    }
}
