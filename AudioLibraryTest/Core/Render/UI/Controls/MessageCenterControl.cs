using CircularBuffer;
using player.Core.Service;
using player.Core.Settings;
using player.Utility;
using player.Utility.UI;

namespace player.Core.Render.UI.Controls
{
    class MessageCenterControl : ControlBase
    {
        public override bool CanFocus { get { return false; } }
        public override bool CanSelect { get { return false; } }
        public override string Name { get { return "Message Center"; } }


        CircularBuffer<MessageCenterMessage> messageQueue = new CircularBuffer<MessageCenterMessage>(10); //10 messages
        SettingsAccessor<double> messageLife;
        SettingsAccessor<double> fadeTime;

        public MessageCenterControl() : base()
        {
            var settings = ServiceManager.GetService<SettingsService>();
            messageLife = settings.GetAccessor<double>(SettingsKeys.MessageCenter_MessageLife, 5);
            fadeTime = settings.GetAccessor<double>(SettingsKeys.MessageCenter_MessageFade, 1);
        }

        public void ShowMessage(string message)
        {
            messageQueue.PushFront(new MessageCenterMessage(message, fadeTime.Get(), messageLife.Get()));
        }

        public override void Render(double time)
        {
            //oooooo bby
            for (int i = messageQueue.Size - 1; i >= 0; i--) //Start from back. Update and then draw
            {
                var msg = messageQueue[i];
                msg.Update(time);
                if (!msg.Alive) messageQueue.PopBack();
            }
            float textOffset = 0f;

            for (int i = 0; i < messageQueue.Size; i++)
            {
                var msg = messageQueue[i];
                float yPos = TransformedBounds.Y - uiManagerInst.TextRenderer.FontHeight - textOffset;
                uiManagerInst.TextRenderer.RenderingColor = new OpenTK.Graphics.Color4(1f, 1f, 1f, msg.Alpha);
                uiManagerInst.TextRenderer.RenderText(msg.Message, new OpenTK.Vector2(TransformedBounds.X, yPos), Anchor.HasFlag(System.Windows.Forms.AnchorStyles.Left) ? QuickFont.QFontAlignment.Left : QuickFont.QFontAlignment.Right);

                textOffset += uiManagerInst.TextRenderer.FontHeight;
            }

            uiManagerInst.TextRenderer.RenderingColor = new OpenTK.Graphics.Color4(1f, 1f, 1f, 1f); //reset color
        }
    }
}
