# 1Password for Logi

An independent third-party plugin bringing 1Password workflows to compatible Logitech devices through the Logi Actions SDK.

The plugin's marketplace display name is **1Password**. Its internal plugin identifier is `OnePasswordForLogi`.

> This project is not affiliated with, endorsed by, or an official product of 1Password or Logitech. Product names and trademarks belong to their respective owners.

## Current status

**Phase 1 — foundation and read-only integration**

- [x] C# Logi Actions SDK project scaffold
- [x] 1Password CLI process integration
- [x] Connection-status action using `op whoami --format json`
- [x] Read-only item-title search using `op item list --format json`
- [x] Process timeouts and safe handling of missing CLI/authentication
- [x] Search reads item metadata only; it does not request passwords, OTPs, or other secret fields
- [ ] Local build and hardware validation
- [ ] Interactive result selection and item-opening workflow
- [ ] Carefully gated clipboard actions
- [ ] Password generator
- [ ] Developer workflows
- [ ] Haptics and device-specific polish
- [ ] Marketplace packaging and review

The first milestone deliberately avoids exposing secret values. It establishes the plugin lifecycle and a small, auditable integration boundary before sensitive actions are added.

## Requirements

- Logitech Options+ or Loupedeck software with Logi Plugin Service
- .NET SDK compatible with the project's target framework
- 1Password desktop app and 1Password CLI
- A signed-in 1Password CLI session

The CLI executable must be available as `op` in the environment used by Logi Plugin Service. The plugin does not store a 1Password password, Secret Key, CLI token, or session token.

## Actions in this milestone

### 1Password → Connection Status

Runs `op whoami --format json` and reports a simple state on the action:

- Connected
- Sign in
- Install CLI
- Timed out
- Check failed

It does not display or log the account identity returned by the CLI.

### Vault → Search Items

A text profile action accepts a title or keyword, then searches item titles returned by `op item list --format json`.

Only title, category, and vault-name metadata are parsed. Secret fields are not requested or displayed. The action shows the first matching title and the number of additional matches.

## Development

Open `OnePasswordPlugin/OnePasswordPlugin.slnx` in Visual Studio or build it with the .NET CLI:

```powershell
dotnet build .\OnePasswordPlugin\OnePasswordPlugin.slnx --configuration Release
```

The project uses the official `LogiPluginApi` package and the SDK's development link-file workflow. The build must be validated on a machine with the Logi Plugin Service installed.

## Security principles

- Never ask for or store the 1Password account password or Secret Key.
- Never store CLI session tokens in plugin settings.
- Never log CLI stdout/stderr or secret values.
- Do not retrieve passwords, OTPs, or private keys for display on device screens.
- Keep secret retrieval behind explicit, narrowly scoped user actions.
- Use process argument lists rather than shell-concatenated command strings.
- Apply timeouts to external CLI processes.

See [SECURITY.md](SECURITY.md) for the security model and reporting guidance.

## Device support

The package metadata declares the SDK's `LoupedeckCtFamily` and `LoupedeckExtendedFamily` device families. Actual action availability depends on the installed Logitech software, device model, and SDK support. Each device must be tested before marketplace claims are made.

## License

The package metadata currently declares MIT. A repository-level license file and final licensing decision still need to be added before distribution.
