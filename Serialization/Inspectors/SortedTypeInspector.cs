using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace DravusSensorPanel.Serialization.Inspectors;

public class SortedTypeInspector : TypeInspectorSkeleton {
    private readonly ITypeInspector _innerTypeInspector;

    public SortedTypeInspector(ITypeInspector innerTypeInspector) {
        _innerTypeInspector = innerTypeInspector;
    }

    public override string GetEnumName(Type enumType, string name) {
        return name;
    }

    public override string GetEnumValue(object enumValue) {
        return enumValue.ToString();
    }

    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container) {
        return _innerTypeInspector.GetProperties(type, container).OrderBy(x => x.Name);
    }
}
