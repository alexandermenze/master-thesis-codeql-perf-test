module CallGraph {
  import csharp
  import mappings

  query predicate edges(Callable source, Callable sink) {
    source.calls(sink)
    or
    exists(ConstructedGeneric cg | cg.getUnboundGeneric() = sink and edgesWithoutTag(source, cg))
    or
    sink.getEnclosingCallable() = source
    or
    sink.getACall().getEnclosingCallable() = source
  }

  query predicate edgesWithoutTag(Callable source, Callable sink) {
    not Mappings::isDataflowTagMethod(sink) and
    (
      source.calls(sink)
      or
      exists(ConstructedGeneric cg | cg.getUnboundGeneric() = sink and edgesWithoutTag(source, cg))
      or
      sink.getEnclosingCallable() = source
      or
      sink.getACall().getEnclosingCallable() = source
    )
  }

  predicate reachableIn(int d, Callable caller, Callable callee) {
    d = 0 and caller = callee
    or
    exists(int prevDist, Callable between |
      reachableIn(prevDist, caller, between) and
      edges(between, callee) and
      prevDist = d - 1
    )
  }

  int distanceTo(Method caller, Callable callee) {
    result = min(int d | reachableIn(d, caller, callee) | d)
  }

  predicate isClosestMethod(Method caller, Callable callee) {
    edgesWithoutTag+(caller, callee) and
    not exists(Method between |
      edgesWithoutTag+(caller, between) and
      edgesWithoutTag+(between, callee)
    )
  }
}
