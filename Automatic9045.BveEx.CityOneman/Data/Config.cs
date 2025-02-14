﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Automatic9045.BveEx.CityOneman.Data
{
    [XmlRoot]
    public class Config
    {
        private static readonly string AssemblyLocation = Assembly.GetExecutingAssembly().Location;
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(Config));

        internal static string BaseDirectory { get; } = Path.GetDirectoryName(AssemblyLocation);


        [XmlAttribute]
        public bool ShowDebugLabel = false;

        public Map Map = new Map();
        public Vehicle Vehicle = new Vehicle();

        public void Serialize(string fileName)
        {
            string path = Path.Combine(BaseDirectory, fileName);
            using (StreamWriter sw = new StreamWriter(path, false, Encoding.UTF8))
            {
                Serializer.Serialize(sw, this);
            }
        }

        public static Config Deserialize(string fileName, bool throwExceptionIfNotExists)
        {
            string path = Path.Combine(BaseDirectory, fileName);
            if (!File.Exists(path) && !throwExceptionIfNotExists) return new Config();

            using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
            {
                return (Config)Serializer.Deserialize(sr);
            }
        }

        public static Config Deserialize(bool throwExceptionIfNotExists)
            => Deserialize(Path.GetFileNameWithoutExtension(AssemblyLocation), throwExceptionIfNotExists);
    }
}
