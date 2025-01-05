using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Automatic9045.BveEx.CityOneman.Data
{
    public class Vehicle
    {
        public bool IsEnabledByDefault = true;
        public KeySet Keys = new KeySet();
        public AtsPanelValueSet AtsPanelValues = new AtsPanelValueSet();
        public SoundSet Sounds = new SoundSet();
    }
}
