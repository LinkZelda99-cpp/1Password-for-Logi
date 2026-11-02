namespace Loupedeck.OnePasswordPlugin
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;

    public enum CredentialField
    {
        Username,
        Password,
        OneTimePassword
    }

    internal sealed record ActionResult(Boolean Success, Int32 Count, String Message);

    internal static class OnePasswordActionService
    {
        private const Int32 TimeoutMilliseconds = 15000;

        public static ActionResult ListVaults()
        {
            var result = RunOp("vault", "list", "--format", "json");
            return CountResult(result, "vault");
        }

        public static ActionResult ListFavorites()
        {
            var result = RunOp("item", "list", "--favorite", "--format", "json");
            return CountResult(result, "favorite item");
        }

        public static String CopyField(String itemTitle, CredentialField field)
        {
            var listResult = RunOp("item", "list", "--format", "json");

            if (!listResult.Success)
            {
                return listResult.Message;
            }

            try
            {
                using var document = JsonDocument.Parse(listResult.Output);
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Array)
                {
                    return "Unexpected CLI response";
                }

                var matches = root.EnumerateArray()
                    .Where(item => GetString(item, "title").Equals(itemTitle, StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                if (matches.Length == 0)
                {
                    return "Item not found";
                }

                if (matches.Length > 1)
                {
                    return "Duplicate title; use a unique title";
                }

                var itemId = GetString(matches[0], "id");
                if (String.IsNullOrWhiteSpace(itemId))
                {
                    return "Item ID unavailable";
                }

                var arguments = field switch
                {
                    CredentialField.Username => new[] { "item", "get", itemId, "--fields", "label=username", "--format", "json" },
                    CredentialField.Password => new[] { "item", "get", itemId, "--fields", "label=password", "--reveal", "--format", "json" },
                    _ => new[] { "item", "get", itemId, "--otp" }
                };

                var valueResult = RunOp(arguments);
                if (!valueResult.Success)
                {
                    return valueResult.Message;
                }

                var value = field == CredentialField.OneTimePassword
                    ? valueResult.Output.Trim()
                    : ReadFieldValue(valueResult.Output, field == CredentialField.Username ? "username" : "password");

                if (String.IsNullOrWhiteSpace(value))
                {
                    return "Field unavailable";
                }

                return CopyToClipboard(value) ? "Copied to clipboard" : "Clipboard unavailable";
            }
            catch (JsonException)
            {
                return "Unexpected CLI response";
            }
        }

        public static Boolean CopyToClipboard(String value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return false;
            }

            var fileName = OperatingSystem.IsWindows() ? "clip.exe" :
                OperatingSystem.IsMacOS() ? "pbcopy" : null;

            if (fileName is null)
            {
                return false;
            }

            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                if (!process.Start())
                {
                    return false;
                }

                process.StandardInput.Write(value);
                process.StandardInput.Close();

                if (!process.WaitForExit(5000))
                {
                    process.Kill(entireProcessTree: true);
                    return false;
                }

                return process.ExitCode == 0;
            }
            catch (Exception exception) when (
                exception is System.ComponentModel.Win32Exception ||
                exception is InvalidOperationException ||
                exception is System.IO.IOException)
            {
                return false;
            }
        }

        private static ActionResult CountResult(CliResult result, String itemName)
        {
            if (!result.Success)
            {
                return new ActionResult(false, 0, result.Message);
            }

            try
            {
                using var document = JsonDocument.Parse(result.Output);
                if (document.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return new ActionResult(false, 0, "Unexpected CLI response");
                }

                return new ActionResult(true, document.RootElement.GetArrayLength(), String.Empty);
            }
            catch (JsonException)
            {
                return new ActionResult(false, 0, "Unexpected CLI response");
            }
        }

        private static String ReadFieldValue(String json, String label)
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("fields", out var fields) ||
                fields.ValueKind != JsonValueKind.Array)
            {
                return String.Empty;
            }

            foreach (var field in fields.EnumerateArray())
            {
                if (GetString(field, "label").Equals(label, StringComparison.OrdinalIgnoreCase))
                {
                    return GetString(field, "value");
                }
            }

            return String.Empty;
        }

        private static String GetString(JsonElement element, String propertyName)
        {
            return element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var value) &&
                value.ValueKind == JsonValueKind.String
                    ? value.GetString() ?? String.Empty
                    : String.Empty;
        }

        private static CliResult RunOp(params String[] arguments)
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
                    return CliResult.Failed("Could not start 1Password CLI");
                }

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                if (!process.WaitForExit(TimeoutMilliseconds))
                {
                    try
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit();
                        Task.WaitAll(outputTask, errorTask);
                    }
                    catch (InvalidOperationException)
                    {
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                    }

                    return CliResult.Failed("1Password CLI timed out");
                }

                Task.WaitAll(outputTask, errorTask);
                return process.ExitCode == 0
                    ? CliResult.Completed(outputTask.Result)
                    : CliResult.Failed("1Password CLI request failed; check sign-in");
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return CliResult.Failed("Install 1Password CLI");
            }
            catch (InvalidOperationException)
            {
                return CliResult.Failed("1Password CLI could not run");
            }
            catch (System.IO.IOException)
            {
                return CliResult.Failed("1Password CLI could not run");
            }
        }

        private sealed record CliResult(Boolean Success, String Output, String Message)
        {
            public static CliResult Completed(String output) => new(true, output, String.Empty);
            public static CliResult Failed(String message) => new(false, String.Empty, message);
        }
    }
}
