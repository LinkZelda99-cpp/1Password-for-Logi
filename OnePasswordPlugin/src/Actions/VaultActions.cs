namespace Loupedeck.OnePasswordPlugin
{
    using System;
    using System.Text.Json;

    public class ListVaultsCommand : PluginDynamicCommand
    {
        private String _status = "Press to refresh";

        public ListVaultsCommand()
            : base("List Vaults", "Shows how many vaults are available to your signed-in account.", "1Password###Vault")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            var result = OnePasswordActionService.ListVaults();
            this._status = result.Success ? $"{result.Count} vault(s) available" : result.Message;
            this.ActionImageChanged();
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            $"List Vaults{Environment.NewLine}{this._status}";
    }

    public class ListFavoritesCommand : PluginDynamicCommand
    {
        private String _status = "Press to refresh";

        public ListFavoritesCommand()
            : base("Favorites", "Shows the number of favorite items in your account.", "1Password###Vault")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            var result = OnePasswordActionService.ListFavorites();
            this._status = result.Success ? $"{result.Count} favorite(s)" : result.Message;
            this.ActionImageChanged();
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            $"Favorites{Environment.NewLine}{this._status}";
    }
}
