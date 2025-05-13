namespace ThreatModel.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class PullFlowAttribute(string Dataflow, Type Type, string? MethodNameOrPattern = null)
    : Attribute;
