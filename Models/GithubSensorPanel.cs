using System;

namespace DravusSensorPanel.Models;

public class GithubSensorPanel : SuperReactiveObject {
    private string _url;
    private string _name;
    private string _author;
    private string _description;
    private string? _readme;
    private int _stars;
    private DateTimeOffset _updatedAt;

    public string Url {
        get => _url;
        set => SetField(ref _url, value);
    }

    public string Name {
        get => _name;
        set => SetField(ref _name, value);
    }

    public string Author {
        get => _author;
        set => SetField(ref _author, value);
    }

    public string Description {
        get => _description;
        set => SetField(ref _description, value);
    }

    public string? Readme {
        get => _readme;
        set => SetField(ref _readme, value);
    }

    public int Stars {
        get => _stars;
        set => SetField(ref _stars, value);
    }

    public DateTimeOffset UpdatedAt {
        get => _updatedAt;
        set => SetField(ref _updatedAt, value);
    }
}
