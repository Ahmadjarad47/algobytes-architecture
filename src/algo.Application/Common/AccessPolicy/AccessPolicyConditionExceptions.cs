namespace algo.Application.Common.AccessPolicy;

public sealed class AccessPolicyConditionParseException(string message) : Exception(message);

public sealed class AccessPolicyConditionValidationException(string message) : Exception(message);
