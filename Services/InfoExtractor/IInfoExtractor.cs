using System;
using System.Collections.Generic;
using DravusSensorPanel.Models.Sensors;

namespace DravusSensorPanel.Services.InfoExtractor;

public interface IInfoExtractor : IDisposable {
    public string SourceName { get; }
    public List<Sensor> Start();
    public void Update();
}
