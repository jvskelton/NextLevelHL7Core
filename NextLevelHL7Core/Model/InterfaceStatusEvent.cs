using System;

namespace NextLevelHL7
{
    public class InterfaceStatusEvent : EventArgs
    {
        public string Text { get; private set; }
        public InterfaceStatusEvent(string text)
        {
            Text = text;
        }
    }
}
