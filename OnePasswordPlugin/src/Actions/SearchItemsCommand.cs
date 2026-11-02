namespace Loupedeck.OnePasswordPlugin
{
    using System;
    using System.Linq;

    public class SearchItemsCommand : PluginDynamicCommand
    {
        private String _status = "Set a search term";

        public SearchItemsCommand()
            : base(
                displayName: "Search Items",
                description: "Searches 1Password item titles without retrieving secret fields.",
                groupName: "1Password###Vault")
        {
            this.MakeProfileAction("text;Enter a title or keyword to search:");
        }

        protected override void RunCommand(String actionParameter)
        {
            if (String.IsNullOrWhiteSpace(actionParameter))
            {
                this._status = "Enter a search term";
                this.ActionImageChanged();
                return;
            }

            var result = ((OnePasswordPlugin)this.Plugin).CliClient.SearchItems(actionParameter);

            if (result.State != OnePasswordConnectionState.Connected)
            {
                this._status = result.State switch
                {
                    OnePasswordConnectionState.CliNotInstalled => "Install CLI",
                    OnePasswordConnectionState.NeedsAuthentication => "Sign in",
                    OnePasswordConnectionState.TimedOut => "Timed out",
                    _ => "Search failed"
                };
            }
            else if (result.TotalMatches == 0)
            {
                this._status = "No matches";
            }
            else
            {
                var firstMatch = result.Matches.FirstOrDefault();
                var firstTitle = firstMatch?.Title ?? "Match found";
                var remaining = result.TotalMatches - 1;
                this._status = remaining > 0
                    ? $"{firstTitle}{Environment.NewLine}+{remaining} more"
                    : firstTitle;
            }

            this.ActionImageChanged();
        }

        protected override String GetCommandDisplayName(
            String actionParameter,
            PluginImageSize imageSize) =>
            $"Search Items{Environment.NewLine}{this._status}";
    }
}
