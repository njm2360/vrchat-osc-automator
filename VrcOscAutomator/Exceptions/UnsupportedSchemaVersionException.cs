namespace VrcOscAutomator.Exceptions;

public sealed class UnsupportedSchemaVersionException : Exception
{
    public int SchemaVersion { get; }

    public UnsupportedSchemaVersionException(int schemaVersion)
        : base($"Schema version {schemaVersion} is not supported by this version of the application.")
    {
        SchemaVersion = schemaVersion;
    }
}
