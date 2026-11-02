# Security

## Scope of the current implementation

The current integration is intentionally read-only.

- Connection status invokes `op whoami --format json`, but discards the returned identity data.
- Item search invokes `op item list --format json` and parses only item title, category, and vault name.
- The plugin does not call `op item get`, `op read`, or any command that requests secret field values.
- CLI stdout/stderr is not written to plugin logs.
- CLI processes have a bounded execution time.

## Credential handling

The plugin must never request or store a 1Password account password, Secret Key, recovery code, or CLI session token. Authentication belongs to the official 1Password app and CLI.

Do not add a custom sign-in screen or ask users to paste credentials into plugin settings.

## Future secret-bearing actions

Any future clipboard or secret-retrieval feature must:

1. Require a direct, explicit user action.
2. Retrieve only the selected field from the selected item.
3. Never display secret values on device screens.
4. Avoid persistent caching and logging.
5. Explain clipboard exposure and provide a clear way to clear the clipboard where the operating system permits it.
6. Fail closed when authorization or item selection is ambiguous.

## Reporting

Please report suspected vulnerabilities privately to the repository owner rather than publishing exploit details in a public issue.
