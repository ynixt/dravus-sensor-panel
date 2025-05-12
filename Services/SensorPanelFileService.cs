using System;
using System.Collections.Generic;
using System.IO;
using DravusSensorPanel.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DravusSensorPanel.Services;

public class SensorPanelFileService {
    public const string FilePath = "sensorpanel.yaml";

    public void Save(SensorPanel sensorPanel) {
        if ( sensorPanel == null ) throw new ArgumentNullException(nameof(sensorPanel));

        ISerializer serializer = new SerializerBuilder()
                                 .WithNamingConvention(CamelCaseNamingConvention.Instance)
                                 .Build();

        string yaml = serializer.Serialize(sensorPanel);
        File.WriteAllText(FilePath, yaml);
    }

    public SensorPanel? Load() {
        if ( !File.Exists(FilePath) ) {
            return null;
        }

        string yaml = File.ReadAllText(FilePath);
        if ( string.IsNullOrWhiteSpace(yaml) ) {
            return null;
        }

        IDeserializer deserializer = new DeserializerBuilder()
                                     .WithNamingConvention(CamelCaseNamingConvention.Instance)
                                     .IgnoreUnmatchedProperties()
                                     .WithTypeDiscriminatingNodeDeserializer(o => {
                                         var keyMappings = new Dictionary<string, Type> {
                                             { SensorPanelItemType.Label.ToString(), typeof(PanelItemLabel) }, {
                                                 SensorPanelItemType.SensorValue.ToString(),
                                                 typeof(PanelItemValue)
                                             }, {
                                                 SensorPanelItemType.SensorChart.ToString(),
                                                 typeof(PanelItemChart)
                                             },
                                             { SensorPanelItemType.Image.ToString(), typeof(PanelItemImage) },
                                         };

                                         o.AddUniqueKeyTypeDiscriminator<PanelItem>(keyMappings);
                                     })
                                     .Build();

        return deserializer.Deserialize<SensorPanel>(yaml);
    }
}
