using player.Core.Render.UI.Controls;
using player.Core.Service;

namespace player.Core.Render
{
    //thin wrapper.
    class MessageCenterService : IService
    {
        public string ServiceName { get { return "Message Center Service"; } }

        private MessageCenterControl messageCenter;

        internal MessageCenterService()
        {

        }

        public void Initialize()
        {
            messageCenter = new MessageCenterControl();
            messageCenter.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Top;
            messageCenter.Location = new System.Drawing.PointF(0, VisGameWindow.RenderResolution.Y * 0.5f);
        }

        public void Cleanup()
        {

        }

        public void ShowMessage(string message)
        {
            messageCenter.ShowMessage(message);
        }
    }
}
