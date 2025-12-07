using System;
using System.Windows.Forms;
using WMPLib;

namespace ScreenshotTool
{
    /// <summary>
    /// Simple Windows Media Player ActiveX host for video preview.
    /// </summary>
    public class WmpHost : AxHost
    {
        // Windows Media Player CLSID: {6BF52A52-394A-11D3-B153-00C04F79FAA6}
        public WmpHost()
            : base("6bf52a52-394a-11d3-b153-00c04f79faa6")
        {
        }

        /// <summary>
        /// Strongly-typed player interface. May be null if not fully created.
        /// </summary>
        public IWMPPlayer4? Player
        {
            get
            {
                if (!Created)
                    return null;

                try
                {
                    return (IWMPPlayer4)GetOcx();
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
