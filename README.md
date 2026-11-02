# 1Password for Logi

An independent third-party plugin bringing 1Password workflows to compatible Logitech devices through the Logi Actions SDK.

The plugin's marketplace display name is **1Password**. Its internal plugin identifier is `OnePasswordForLogi`.

> This project is not affiliated with, endorsed by, or an official product of 1Password or Logitech. Product names and trademarks belong to their respective owners.

## Current status

**Phase 2 — expanded actions, pending local validation**

- [x] C# Logi Actions SDK project scaffold
- [x] 1Password CLI process integration with bounded timeouts
- [x] Connection-status action using `op whoami --format json`
- [x] Read-only item-title search using `op item list --format json`
- [x] Vault count and favorites count actions
- [x] Explicit profile actions to copy username, password, or current one-time password
- [x] Local cryptographically-random password generator with clipboard output
- [ ] Build validation after the expanded action set
- [ ] Interactive result selection and item-opening workflow
- [ ] Vault/category browsing with selectable results
- [ ] Clipboard-clearing control and configurable clipboard expiry where supported
- [ ] Developer credential workflows
- [ ] Haptics and device-specific polish
- [ ] Marketplace packaging and review

This is not yet a complete 1Password client. The action set is being expanded incrementally and must be built and tested in Logi Options+ before being treated as stable.

## Requirements

- Logitech Options+ or Loupedeck software with Logi Plugin Service
- .NET SDK compatible with the project's target framework
- 1Password CLI 2
- A signed-in 1Password CLI session

The CLI executable must be available as `op` in the environment used by Logi Plugin Service. The plugin does not store a 1Password password, Secret Key, CLI token, or session token.

## Actions

All actions are implemented as Logi Actions SDK `PluginDynamicCommand` actions. Parameterized item actions use the SDK's text profile-action format. Group names use the SDK's `###` hierarchy separator.

### 1Password → Account

**Connection Status**

Runs `op whoami --format json` and reports a simple state:

- Connected
- Sign in
- Install CLI
- Timed out
- Check failed

It does not display or log the account identity returned by the CLI.

### 1Password → Vault

**Search Items**

A text profile action accepts a title or keyword, then searches item titles returned by `op item list --format json`. Only title, category, and vault-name metadata are parsed.

**List Vaults**

Runs `op vault list --format json` and displays the number of accessible vaults, not their contents.

**Favorites**

Runs `op item list --favorite --format json` and displays the number of favorite items.

### 1Password → Credentials

These profile actions require the exact item title:

- **Copy Username**
- **Copy Password**
- **Copy One-Time Password**

The action resolves an exact title match, rejects ambiguous duplicate titles, requests only the selected field, and sends the value directly to the operating-system clipboard. Secret values are never displayed on the device or written to plugin logs.

Clipboard contents may remain available to other apps or clipboard-sync services according to operating-system settings. Use these actions only when you intend to place the selected value on the clipboard.

### 1Password → Tools

**Generate Password**

Generates a 24-character password using .NET's cryptographic random-number generator and copies it to the clipboard. The generated value is not displayed on the device or logged.

## Development

Open `OnePasswordPlugin/OnePasswordPlugin.slnx` in Visual Studio or build it with the .NET CLI:

```powershell
dotnet build .\OnePasswordPlugin\OnePasswordPlugin.slnx --configuration Release
```

The project uses the official `LogiPluginApi` package and the SDK's development link-file workflow. The expanded action set still needs a fresh local build and hardware validation.

## Security principles

- Never ask for or store the 1Password account password, Secret Key, recovery code, or CLI session token.
- Never log CLI stdout/stderr or secret values.
- Never display secret values on device screens.
- Retrieve only the field explicitly requested by the user.
- Keep secret retrieval behind a direct, explicit user action.
- Reject ambiguous item-title matches.
- Do not persistently cache secret values.
- Use process argument lists rather than shell-concatenated command strings.
- Apply timeouts to external CLI processes.
- Explain clipboard exposure: copied values are subject to operating-system clipboard behavior.

See [SECURITY.md](SECURITY.md) for the security model and reporting guidance.

## Device support

The package metadata declares the SDK's `LoupedeckCtFamily` and `LoupedeckExtendedFamily` device families. Actual action availability depends on the installed Logitech software, device model, and SDK support. Each device must be tested before marketplace claims are made.

## License

The package metadata currently declares MIT. A repository-level license file and final licensing decision still need to be added before distribution.
