namespace algo.Application.Common.AccessPolicy;

public abstract record AccessPolicyConditionAst;

public sealed record AccessPolicyAllAst(IReadOnlyList<AccessPolicyConditionAst> All) : AccessPolicyConditionAst;

public sealed record AccessPolicyAnyAst(IReadOnlyList<AccessPolicyConditionAst> Any) : AccessPolicyConditionAst;

public sealed record AccessPolicyFieldAst(string Field, string Operator, object? Value) : AccessPolicyConditionAst;
