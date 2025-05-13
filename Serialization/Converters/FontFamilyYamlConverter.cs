using System;
using Avalonia.Media;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace DravusSensorPanel.Serialization.Converters;

public sealed class FontFamilyYamlConverter : IYamlTypeConverter {
    public bool Accepts(Type type) {
        return typeof(FontFamily).IsAssignableFrom(type);
    }

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer) {
        var scalar = parser.Consume<Scalar>();
        return new FontFamily(scalar.Value);
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer) {
        var font = ( FontFamily ) value!;
        emitter.Emit(new Scalar(font.Name));
    }
}
