namespace ThreatModel.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class ThreatModelProcessAttribute(string ProcessName) : Attribute;
