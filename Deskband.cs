using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NowPlayingDeskband
{
    [ComVisible(true)]
    [Guid("8a0ab0b3-4928-4192-a8ff-4ee33c950c9d")]
    [CSDeskBand.CSDeskBandRegistration(Name = "Now Playing Deskband", ShowDeskBand = true)]
    public class Deskband : CSDeskBand.CSDeskBandWin
    {
        private static Control _control;

        public Deskband()
        {
            Options.MinVerticalSize = new Size(78, 78 + 12 + 12);
            _control = new NowPlayingDeskbandControl();
        }

        protected override Control Control => _control;
    }
}
