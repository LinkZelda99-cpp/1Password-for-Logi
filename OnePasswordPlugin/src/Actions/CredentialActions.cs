namespace Loupedeck.OnePasswordPlugin
{
    using System;
    using System.Security.Cryptography;

    public abstract class CopyCredentialCommand : PluginDynamicCommand
    {
        private readonly CredentialField _field;
        private readonly String _actionName;
        private String _status = "Enter item title";

        protected CopyCredentialCommand(String actionName, String description, CredentialField field)
            : base(actionName, description, "1Password###Credentials")
        {
            this._actionName = actionName;
            this._field = field;
            this.MakeProfileAction("text;Enter the exact 1Password item title:");
        }

        protected override void RunCommand(String actionParameter)
        {
            if (String.IsNullOrWhiteSpace(actionParameter))
            {
                this._status = "Enter item title";
            }
            else
            {
                this._status = OnePasswordActionService.CopyField(actionParameter.Trim(), this._field);
            }

            this.ActionImageChanged();
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            $"{this._actionName}{Environment.NewLine}{this._status}";
    }

    public class CopyUsernameCommand : CopyCredentialCommand
    {
        public CopyUsernameCommand()
            : base("Copy Username", "Copies the username from an exact item match to the system clipboard.", CredentialField.Username)
        {
        }
    }

    public class CopyPasswordCommand : CopyCredentialCommand
    {
        public CopyPasswordCommand()
            : base("Copy Password", "Copies the password from an exact item match to the system clipboard.", CredentialField.Password)
        {
        }
    }

    public class CopyOneTimePasswordCommand : CopyCredentialCommand
    {
        public CopyOneTimePasswordCommand()
            : base("Copy One-Time Password", "Copies the current one-time password from an exact item match.", CredentialField.OneTimePassword)
        {
        }
    }

    public class GeneratePasswordCommand : PluginDynamicCommand
    {
        private const String Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*_-+=";
        private String _status = "Press to generate";

        public GeneratePasswordCommand()
            : base("Generate Password", "Generates a random 24-character password and copies it to the system clipboard.", "1Password###Tools")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            var password = new Char[24];

            for (var i = 0; i < password.Length; i++)
            {
                password[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
            }

            this._status = OnePasswordActionService.CopyToClipboard(new String(password))
                ? "Generated and copied"
                : "Clipboard unavailable";

            Array.Clear(password);
            this.ActionImageChanged();
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            $"Generate Password{Environment.NewLine}{this._status}";
    }
}
