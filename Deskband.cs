using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NowPlayingDeskband
{
    [ComVisible(true)]
    [Guid("8a0ab0b3-4928-4192-a8ff-4ee33c950c9d")]
    [CSDeskBand.CSDeskBandRegistration(Name = "Now Playing Deskband")]
    public class Deskband : CSDeskBand.CSDeskBandWin
    {
        private static Control _control;

        public Deskband()
        {
            SimpleLogger.DefaultLog("Deskband::constructor called...");
            Options.MinVerticalSize = new Size(78, 78 + 12 + 12);
            _control = new NowPlayingDeskbandControl();
            SimpleLogger.DefaultLog("Deskband::constructor DONE");
        }

        protected override Control Control => _control;

        protected override void DeskbandOnClosed() {
            SimpleLogger.DefaultLog("Deskband::DeskbandOnClosed called...");
            _control = null;
            SimpleLogger.DefaultLog("Deskband::DeskbandOnClosed DONE");
        }
    }
}
