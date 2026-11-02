namespace Loupedeck.OnePasswordPlugin
{
    using System;

    public class OnePasswordPlugin : Plugin
    {
        public override Boolean UsesApplicationApiOnly => true;

        public override Boolean HasNoApplication => true;

        internal OnePasswordCliClient CliClient { get; }

        public OnePasswordPlugin()
        {
            PluginLog.Init(this.Log);
            PluginResources.Init(this.Assembly);
            this.CliClient = new OnePasswordCliClient();
        }

        public override void Load()
        {
        }

        public override void Unload()
        {
        }
    }
}
