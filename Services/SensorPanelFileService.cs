using System;
using System.Collections.Generic;
using System.IO;
using DravusSensorPanel.Enums;
using DravusSensorPanel.Models;
using DravusSensorPanel.Models.Dtos;
using DravusSensorPanel.Serialization.Converters;
using DravusSensorPanel.Serialization.Inspectors;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DravusSensorPanel.Services;

public class SensorPanelFileService {
    public const string DefaultSensorPanelPath = "sensorpanel.yaml";

    private readonly List<IYamlTypeConverter> _converters = [new FontFamilyYamlConverter(), new ColorYamlConverter()];

    public void Save(SensorPanel sensorPanel, string filePath) {
        if ( sensorPanel == null ) throw new ArgumentNullException(nameof(sensorPanel));

        SerializerBuilder sb = new SerializerBuilder()
                               .WithTypeInspector(x => new SortedTypeInspector(x))
                               .WithNamingConvention(UnderscoredNamingConvention.Instance);

        foreach ( IYamlTypeConverter converter in _converters ) {
            sb.WithTypeConverter(converter);
        }

        ISerializer serializer = sb.Build();

        string yaml = serializer.Serialize(sensorPanel.ToDto());
        File.WriteAllText(filePath, yaml);
    }

    public SensorPanel? Load(string path) {
        if ( !File.Exists(path) ) {
            return null;
        }

        string yaml = File.ReadAllText(path);
        if ( string.IsNullOrWhiteSpace(yaml) ) {
            return null;
        }

        DeserializerBuilder db = new DeserializerBuilder()
                                 .WithNamingConvention(UnderscoredNamingConvention.Instance)
                                 .IgnoreUnmatchedProperties()
                                 .WithTypeDiscriminatingNodeDeserializer(o => {
                                     var keyMappings = new Dictionary<string, Type>(StringComparer.Ordinal) {
                                         [SensorPanelItemType.Label.ToString()] = typeof(PanelItemLabelDto),
                                         [SensorPanelItemType.SensorValue.ToString()] = typeof(PanelItemValueDto),
                                         [SensorPanelItemType.SensorChart.ToString()] = typeof(PanelItemChartDto),
                                         [SensorPanelItemType.Image.ToString()] = typeof(PanelItemImageDto),
                                     };

                                     o.AddKeyValueTypeDiscriminator<PanelItemDto>("type", keyMappings);
                                 });


        IDeserializer deserializer = db.Build();

        foreach ( IYamlTypeConverter converter in _converters ) {
            db.WithTypeConverter(converter);
        }

        return deserializer.Deserialize<SensorPanelDto>(yaml).ToModel();
    }
}
