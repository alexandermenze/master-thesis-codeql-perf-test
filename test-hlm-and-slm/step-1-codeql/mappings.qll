module Mappings {
  import csharp

  /**
   * Gibt den ProcessName aus dem ThreatModelProcessAttribute zurück,
   * oder "" wenn kein solches Attribut auf `source` existiert.
   */
  string getProcessNameOrEmpty(Callable source) {
    result = max(string s | s = "" or s = getProcessName(source) | s order by s.length())
  }

  private string getProcessName(Callable source) {
    exists(Attribute a |
      a.getTarget() = source and
      a.getType().getName() = "ThreatModelProcessAttribute"
    |
      result = a.getConstructorArgument(0).getValue()
    )
  }

  predicate isDataflowTagMethod(Callable c) {
    c.getDeclaringType().hasFullyQualifiedName("ThreatModel.Tags", "Dataflow")
  }
}
