using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Automatic9045.AtsEx.CityOneman.Data
{
    [XmlRoot]
    public class AtsPanelValue
    {
        [XmlAttribute]
        public int Index = -1;
    }
}
