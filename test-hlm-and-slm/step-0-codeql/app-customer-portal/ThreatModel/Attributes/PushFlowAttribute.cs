namespace ThreatModel.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class PushFlowAttribute(string Dataflow, Type Type, string? MethodNameOrPattern = null)
    : Attribute;
