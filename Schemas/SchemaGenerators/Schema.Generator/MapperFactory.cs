using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Shantiw.Data.Schema
{
    internal static class MapperFactory
    {
        public static IMapper Create(XElement mapping)
        {
            XElement? xMapper = mapping.Element(MapperVocab.Mapper);
            if (xMapper == null) return new Mapper(mapping);

            string typeName = xMapper.GetAttributeValue(MapperVocab.Type);
            switch (typeName)
            {
                case "Mapper":
                case "Shantiw.Data.Schema.Mapper":
                    return new Mapper(mapping);
                case "ConfigMapper":
                case "Shantiw.Data.Schema.ConfigMapper":
                    return new ConfigMapper(mapping);
                default:
                    break;
            }

            //
            string fileName = xMapper.GetAttributeValue(MapperVocab.Assembly);
            Assembly assembly = Assembly.LoadFile(fileName);
            Type? type = assembly.GetType(typeName) ?? throw new TypeLoadException("Type is not found in the assembly. Type: " + typeName);
            ConstructorInfo? constructorInfo = type.GetConstructor([]);
            object? mapper = (constructorInfo == null) ? Activator.CreateInstance(type, mapping) : Activator.CreateInstance(type);
            return mapper == null ? throw new TypeLoadException("Type is not a valid type. Type: " + type.FullName) : (IMapper)mapper;
        }

    }
}
