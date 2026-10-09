namespace BarberMenagment.Configuration;

public static class DotEnvLoader
{
    public static void Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        foreach (var rawLine in File.ReadLines(filePath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line["export ".Length..].TrimStart();
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            var isDoubleQuoted = value.Length >= 2 &&
                                 value[0] == '"' &&
                                 value[^1] == '"';
            var isSingleQuoted = value.Length >= 2 &&
                                 value[0] == '\'' &&
                                 value[^1] == '\'';
            if (isDoubleQuoted || isSingleQuoted)
            {
                value = value[1..^1];
            }

            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(
                    key,
                    value,
                    EnvironmentVariableTarget.Process);
            }
        }
    }
}
