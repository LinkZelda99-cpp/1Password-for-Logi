# Security

## Scope of the current implementation

The plugin uses the official 1Password CLI and deliberately keeps secret retrieval behind explicit actions.

- Connection status invokes `op whoami --format json`, but discards the returned identity data.
- Item search, vault count, and favorites count use metadata-only list commands.
- Credential-copy actions resolve an exact item title, reject duplicate-title ambiguity, and request only the selected username, password, or one-time password.
- Credential values are sent directly to the operating-system clipboard. They are not displayed on device screens, written to plugin logs, or stored in plugin settings.
- The password generator uses .NET's cryptographic random-number generator and copies the result to the clipboard.
- CLI stdout/stderr is not written to plugin logs.
- CLI processes have a bounded execution time.

## Credential handling

The plugin must never request or store a 1Password account password, Secret Key, recovery code, or CLI session token. Authentication belongs to the official 1Password app and CLI.

Do not add a custom sign-in screen or ask users to paste credentials into plugin settings.

## Clipboard behavior

Clipboard actions are deliberate user-triggered operations. A copied value may remain in clipboard history, be synchronized by the operating system, or be readable by other applications depending on system settings. The plugin does not currently implement automatic clipboard clearing or expiry. Users should avoid copying secrets on shared or untrusted devices.

## Future secret-bearing actions

Any future secret-retrieval feature must:

1. Require a direct, explicit user action.
2. Retrieve only the selected field from the selected item.
3. Never display secret values on device screens.
4. Avoid persistent caching and logging.
5. Fail closed when authorization or item selection is ambiguous.
6. Explain clipboard exposure before adding new clipboard workflows.

## Reporting

Please report suspected vulnerabilities privately to the repository owner rather than publishing exploit details in a public issue.
