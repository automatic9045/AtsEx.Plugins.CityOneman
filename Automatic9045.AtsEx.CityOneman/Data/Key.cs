using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace Automatic9045.AtsEx.CityOneman.Data
{
    [XmlRoot]
    public class Key
    {
        [XmlAttribute]
        public Keys KeyCode = Keys.None;

        public Key()
        {
        }

        public Key(Keys code)
        {
            KeyCode = code;
        }
    }
}
