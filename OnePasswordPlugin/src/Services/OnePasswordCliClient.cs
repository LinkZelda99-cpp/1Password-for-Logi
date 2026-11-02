namespace Loupedeck.OnePasswordPlugin
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json;

    internal sealed class OnePasswordCliClient
    {
        private const Int32 CommandTimeoutMilliseconds = 15000;

        public OnePasswordConnectionState CheckConnection()
        {
            var result = Execute("whoami", "--format", "json");

            if (result.Status == CliExecutionStatus.NotInstalled)
            {
                return OnePasswordConnectionState.CliNotInstalled;
            }

            if (result.Status == CliExecutionStatus.TimedOut)
            {
                return OnePasswordConnectionState.TimedOut;
            }

            if (result.Status != CliExecutionStatus.Completed)
            {
                return OnePasswordConnectionState.Error;
            }

            return result.ExitCode == 0
                ? OnePasswordConnectionState.Connected
                : OnePasswordConnectionState.NeedsAuthentication;
        }

        public OnePasswordItemSearchResult SearchItems(String query)
        {
            if (String.IsNullOrWhiteSpace(query))
            {
                return OnePasswordItemSearchResult.EmptyQuery();
            }

            var result = Execute("item", "list", "--format", "json");

            if (result.Status == CliExecutionStatus.NotInstalled)
            {
                return OnePasswordItemSearchResult.Failed(OnePasswordConnectionState.CliNotInstalled);
            }

            if (result.Status == CliExecutionStatus.TimedOut)
            {
                return OnePasswordItemSearchResult.Failed(OnePasswordConnectionState.TimedOut);
            }

            if (result.Status != CliExecutionStatus.Completed || result.ExitCode != 0)
            {
                return OnePasswordItemSearchResult.Failed(OnePasswordConnectionState.NeedsAuthentication);
            }

            try
            {
                using var document = JsonDocument.Parse(result.StandardOutput);
                var items = GetItemsArray(document.RootElement);

                if (items is null)
                {
                    return OnePasswordItemSearchResult.Failed(OnePasswordConnectionState.Error);
                }

                var matches = new List<OnePasswordItemSummary>();

                foreach (var item in items.Value.EnumerateArray())
                {
                    var title = GetString(item, "title");

                    if (String.IsNullOrWhiteSpace(title) ||
                        !title.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    matches.Add(new OnePasswordItemSummary(
                        title,
                        GetString(item, "category"),
                        GetVaultName(item)));
                }

                var orderedMatches = matches
                    .OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToArray();

                return OnePasswordItemSearchResult.Success(matches.Count, orderedMatches);
            }
            catch (JsonException)
            {
                return OnePasswordItemSearchResult.Failed(OnePasswordConnectionState.Error);
            }
        }

        private static JsonElement? GetItemsArray(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Array)
            {
                return root;
            }

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("items", out var items) &&
                items.ValueKind == JsonValueKind.Array)
            {
                return items;
            }

            return null;
        }

        private static String GetString(JsonElement element, String propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var value) &&
                value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? String.Empty;
            }

            return String.Empty;
        }

        private static String GetVaultName(JsonElement item)
        {
            if (item.ValueKind == JsonValueKind.Object &&
                item.TryGetProperty("vault", out var vault) &&
                vault.ValueKind == JsonValueKind.Object)
            {
                return GetString(vault, "name");
            }

            return String.Empty;
        }

        private static CliExecutionResult Execute(params String[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "op",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            try
            {
                using var process = new Process { StartInfo = startInfo };

                if (!process.Start())
                {
                    return CliExecutionResult.Failed();
                }

                var standardOutputTask = process.StandardOutput.ReadToEndAsync();
                var standardErrorTask = process.StandardError.ReadToEndAsync();

                if (!process.WaitForExit(CommandTimeoutMilliseconds))
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit();
                        Task.WaitAll(standardOutputTask, standardErrorTask);
                    }
                    catch (InvalidOperationException)
                    {
                        // The process may have exited between the timeout and the kill attempt.
                    }
                    catch (Win32Exception)
                    {
                        // The operating system may reject a late kill request.
                    }

                    return CliExecutionResult.TimedOut();
                }

                Task.WaitAll(standardOutputTask, standardErrorTask);

                return CliExecutionResult.Completed(
                    process.ExitCode,
                    standardOutputTask.Result);
            }
            catch (Win32Exception)
            {
                return CliExecutionResult.NotInstalled();
            }
            catch (InvalidOperationException)
            {
                return CliExecutionResult.Failed();
            }
            catch (System.IO.IOException)
            {
                return CliExecutionResult.Failed();
            }
        }
    }

    internal enum OnePasswordConnectionState
    {
        Connected,
        CliNotInstalled,
        NeedsAuthentication,
        TimedOut,
        Error
    }

    internal enum CliExecutionStatus
    {
        Completed,
        NotInstalled,
        TimedOut,
        Failed
    }

    internal sealed record OnePasswordItemSummary(String Title, String Category, String VaultName);

    internal sealed record OnePasswordItemSearchResult(
        OnePasswordConnectionState State,
        Int32 TotalMatches,
        IReadOnlyList<OnePasswordItemSummary> Matches)
    {
        public static OnePasswordItemSearchResult Success(
            Int32 totalMatches,
            IReadOnlyList<OnePasswordItemSummary> matches) =>
            new(OnePasswordConnectionState.Connected, totalMatches, matches);

        public static OnePasswordItemSearchResult Failed(OnePasswordConnectionState state) =>
            new(state, 0, Array.Empty<OnePasswordItemSummary>());

        public static OnePasswordItemSearchResult EmptyQuery() =>
            new(OnePasswordConnectionState.Error, 0, Array.Empty<OnePasswordItemSummary>());
    }

    internal sealed record CliExecutionResult(
        CliExecutionStatus Status,
        Int32 ExitCode,
        String StandardOutput)
    {
        public static CliExecutionResult Completed(Int32 exitCode, String standardOutput) =>
            new(CliExecutionStatus.Completed, exitCode, standardOutput);

        public static CliExecutionResult NotInstalled() =>
            new(CliExecutionStatus.NotInstalled, -1, String.Empty);

        public static CliExecutionResult TimedOut() =>
            new(CliExecutionStatus.TimedOut, -1, String.Empty);

        public static CliExecutionResult Failed() =>
            new(CliExecutionStatus.Failed, -1, String.Empty);
    }
}
