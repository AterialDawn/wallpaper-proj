namespace player.Core.Logging
{
    public class MessageLoggedEventArgs
    {
        public string Message { get; private set; }
        public bool WasLoggedToConsole { get; private set; }
        public bool WasLoggedToFile { get; private set; }

        public MessageLoggedEventArgs(string message, bool consoleLogged, bool fileLogged)
        {
            Message = message;
            WasLoggedToConsole = consoleLogged;
            WasLoggedToFile = fileLogged;
        }
    }
}
