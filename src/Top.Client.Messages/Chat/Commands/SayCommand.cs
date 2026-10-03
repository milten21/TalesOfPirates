using System;

namespace Top.Client.Messages.Chat.Commands
{
    public class SayCommand : IServerCommand
    {
        public SayCommand(string text)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
        }

        public string Text { get; }
    }
}
