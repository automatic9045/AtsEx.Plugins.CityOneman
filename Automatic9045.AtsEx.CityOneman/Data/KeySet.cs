using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Automatic9045.AtsEx.CityOneman.Data
{
    public class KeySet
    {
        public Key LeftOpen = new Key(Keys.E);
        public Key LeftClose = new Key(Keys.R);
        public Key LeftReopen = new Key(Keys.T);

        public Key RightOpen = new Key(Keys.O);
        public Key RightClose = new Key(Keys.I);
        public Key RightReopen = new Key(Keys.U);

        public Key RequestFixStopPosition = new Key();
        public Key ConductorValve = new Key();
    }
}
