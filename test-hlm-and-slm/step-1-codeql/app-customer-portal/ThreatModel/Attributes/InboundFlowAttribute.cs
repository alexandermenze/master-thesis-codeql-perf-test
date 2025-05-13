namespace ThreatModel.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class InboundFlowAttribute(string Dataflow) : Attribute;
