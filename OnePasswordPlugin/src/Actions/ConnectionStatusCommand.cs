namespace Loupedeck.OnePasswordPlugin
{
    using System;

    public class ConnectionStatusCommand : PluginDynamicCommand
    {
        private String _status = "Check connection";

        public ConnectionStatusCommand()
            : base(
                displayName: "Connection Status",
                description: "Checks whether 1Password CLI is installed and authenticated.",
                groupName: "1Password###Account")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            var result = ((OnePasswordPlugin)this.Plugin).CliClient.CheckConnection();

            this._status = result switch
            {
                OnePasswordConnectionState.Connected => "Connected",
                OnePasswordConnectionState.CliNotInstalled => "Install CLI",
                OnePasswordConnectionState.NeedsAuthentication => "Sign in",
                OnePasswordConnectionState.TimedOut => "Timed out",
                _ => "Check failed"
            };

            this.ActionImageChanged();
        }

        protected override String GetCommandDisplayName(
            String actionParameter,
            PluginImageSize imageSize) =>
            $"1Password{Environment.NewLine}{this._status}";
    }
}
